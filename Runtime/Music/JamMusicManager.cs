using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JamAudioToolkit
{
    /// <summary>
    /// Persistent runtime service for music playback, scene persistence, and crossfades.
    /// </summary>
    [AddComponentMenu("Jam Audio Toolkit/Jam Music Manager")]
    [DisallowMultipleComponent]
    public class JamMusicManager : MonoBehaviour
    {
        private AudioSource activeSource;
        private AudioSource inactiveSource;
        private JamMusicEvent currentMusicEvent;
        private Coroutine transitionCoroutine;
        private bool musicIsPaused;

        /// <summary>
        /// Gets the active music manager, creating one if needed during Play Mode.
        /// </summary>
        internal static JamMusicManager GetOrCreate()
        {
            if (!Application.isPlaying)
            {
                return null;
            }

            JamMusicManager manager = FindExistingManager();
            if (manager != null)
            {
                return manager;
            }

            GameObject managerObject = new GameObject("Jam Music Manager");
            return managerObject.AddComponent<JamMusicManager>();
        }

        /// <summary>
        /// The music event currently requested by the manager.
        /// </summary>
        public JamMusicEvent CurrentMusicEvent => currentMusicEvent;

        /// <summary>
        /// True when the current music is paused.
        /// </summary>
        public bool IsPaused => musicIsPaused;

        private static JamMusicManager FindExistingManager()
        {
#if UNITY_2023_1_OR_NEWER
            return Object.FindAnyObjectByType<JamMusicManager>();
#else
            return Object.FindObjectOfType<JamMusicManager>();
#endif
        }

        private void Awake()
        {
            if (HasOtherManagerInScene())
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);
            EnsureSources();
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        /// <summary>
        /// Plays a music event, crossfading away from the current music if needed.
        /// </summary>
        public void PlayMusic(JamMusicEvent musicEvent)
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Jam Music Manager playback runs in Play Mode.");
                return;
            }

            if (musicEvent == null)
            {
                Debug.LogWarning("Cannot play a missing Jam Music Event.", this);
                return;
            }

            if (musicEvent.musicClip == null)
            {
                Debug.LogWarning($"{musicEvent.name} has no music clip assigned.", musicEvent);
                return;
            }

            EnsureSources();

            if (currentMusicEvent == musicEvent && (transitionCoroutine != null || activeSource.isPlaying))
            {
                return;
            }

            StopTransitionCoroutine();
            currentMusicEvent = musicEvent;
            musicIsPaused = false;
            transitionCoroutine = StartCoroutine(TransitionToMusic(musicEvent));
        }

        /// <summary>
        /// Stops the current music, fading out unless the duration is zero.
        /// </summary>
        public void StopMusic(float fadeOutOverride = -1f)
        {
            if (!Application.isPlaying)
            {
                return;
            }

            EnsureSources();
            StopTransitionCoroutine();

            float fadeOutDuration = fadeOutOverride >= 0f
                ? Mathf.Max(0f, fadeOutOverride)
                : (currentMusicEvent != null ? currentMusicEvent.GetFadeOutDuration() : 0f);

            musicIsPaused = false;
            currentMusicEvent = null;
            transitionCoroutine = StartCoroutine(StopAllMusic(fadeOutDuration));
        }

        /// <summary>
        /// Pauses the current music, fading out unless the duration is zero.
        /// </summary>
        public void PauseMusic(float fadeOutOverride = -1f)
        {
            if (!Application.isPlaying || currentMusicEvent == null || musicIsPaused)
            {
                return;
            }

            EnsureSources();
            StopTransitionCoroutine();

            AudioSource sourceToPause = GetCurrentMusicSource();
            if (sourceToPause == null || sourceToPause.clip == null)
            {
                return;
            }

            MakeActiveSource(sourceToPause);
            StopAndClear(inactiveSource);

            float fadeOutDuration = fadeOutOverride >= 0f
                ? Mathf.Max(0f, fadeOutOverride)
                : currentMusicEvent.GetFadeOutDuration();

            transitionCoroutine = StartCoroutine(PauseCurrentMusic(activeSource, fadeOutDuration));
        }

        /// <summary>
        /// Resumes paused music, fading in unless the duration is zero.
        /// </summary>
        public void ResumeMusic(float fadeInOverride = -1f)
        {
            if (!Application.isPlaying || currentMusicEvent == null || !musicIsPaused)
            {
                return;
            }

            EnsureSources();
            StopTransitionCoroutine();

            AudioSource sourceToResume = GetCurrentMusicSource();
            if (sourceToResume == null || sourceToResume.clip == null)
            {
                musicIsPaused = false;
                PlayMusic(currentMusicEvent);
                return;
            }

            MakeActiveSource(sourceToResume);
            StopAndClear(inactiveSource);

            float fadeInDuration = fadeInOverride >= 0f
                ? Mathf.Max(0f, fadeInOverride)
                : currentMusicEvent.GetFadeInDuration();

            transitionCoroutine = StartCoroutine(ResumeCurrentMusic(activeSource, fadeInDuration));
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (currentMusicEvent != null && !currentMusicEvent.persistAcrossScenes)
            {
                StopMusic(0f);
            }
        }

        private IEnumerator TransitionToMusic(JamMusicEvent musicEvent)
        {
            AudioSource outgoingSource = activeSource;
            AudioSource incomingSource = inactiveSource;

            ConfigureSource(incomingSource, musicEvent);
            incomingSource.volume = 0f;
            incomingSource.Play();

            float targetVolume = musicEvent.GetVolume();
            float fadeInDuration = musicEvent.GetFadeInDuration();
            float fadeOutDuration = musicEvent.GetFadeOutDuration();
            float outgoingStartVolume = outgoingSource.isPlaying ? outgoingSource.volume : 0f;
            float transitionDuration = Mathf.Max(fadeInDuration, fadeOutDuration);

            if (transitionDuration <= 0f)
            {
                incomingSource.volume = targetVolume;
                StopAndClear(outgoingSource);
                SwapSources();
                transitionCoroutine = null;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;

                incomingSource.volume = fadeInDuration <= 0f
                    ? targetVolume
                    : Mathf.Lerp(0f, targetVolume, Mathf.Clamp01(elapsed / fadeInDuration));

                if (outgoingSource.isPlaying)
                {
                    outgoingSource.volume = fadeOutDuration <= 0f
                        ? 0f
                        : Mathf.Lerp(outgoingStartVolume, 0f, Mathf.Clamp01(elapsed / fadeOutDuration));
                }

                yield return null;
            }

            incomingSource.volume = targetVolume;
            StopAndClear(outgoingSource);
            SwapSources();
            transitionCoroutine = null;
        }

        private IEnumerator StopAllMusic(float fadeOutDuration)
        {
            AudioSource firstSource = activeSource;
            AudioSource secondSource = inactiveSource;
            float firstStartVolume = firstSource.volume;
            float secondStartVolume = secondSource.volume;

            if (fadeOutDuration <= 0f)
            {
                StopAndClear(firstSource);
                StopAndClear(secondSource);
                musicIsPaused = false;
                currentMusicEvent = null;
                transitionCoroutine = null;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float fadeProgress = Mathf.Clamp01(elapsed / fadeOutDuration);

                if (firstSource.isPlaying)
                {
                    firstSource.volume = Mathf.Lerp(firstStartVolume, 0f, fadeProgress);
                }

                if (secondSource.isPlaying)
                {
                    secondSource.volume = Mathf.Lerp(secondStartVolume, 0f, fadeProgress);
                }

                yield return null;
            }

            StopAndClear(firstSource);
            StopAndClear(secondSource);
            musicIsPaused = false;
            currentMusicEvent = null;
            transitionCoroutine = null;
        }

        private IEnumerator PauseCurrentMusic(AudioSource source, float fadeOutDuration)
        {
            musicIsPaused = true;

            if (fadeOutDuration <= 0f)
            {
                source.volume = 0f;
                source.Pause();
                transitionCoroutine = null;
                yield break;
            }

            float startVolume = source.volume;
            float elapsed = 0f;

            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                source.volume = Mathf.Lerp(startVolume, 0f, Mathf.Clamp01(elapsed / fadeOutDuration));
                yield return null;
            }

            source.volume = 0f;
            source.Pause();
            transitionCoroutine = null;
        }

        private IEnumerator ResumeCurrentMusic(AudioSource source, float fadeInDuration)
        {
            musicIsPaused = false;
            source.UnPause();

            float targetVolume = currentMusicEvent.GetVolume();

            if (fadeInDuration <= 0f)
            {
                source.volume = targetVolume;
                transitionCoroutine = null;
                yield break;
            }

            float startVolume = source.volume;
            float elapsed = 0f;

            while (elapsed < fadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                source.volume = Mathf.Lerp(startVolume, targetVolume, Mathf.Clamp01(elapsed / fadeInDuration));
                yield return null;
            }

            source.volume = targetVolume;
            transitionCoroutine = null;
        }

        private void EnsureSources()
        {
            if (activeSource == null)
            {
                activeSource = CreateSource("Jam Music Source A");
            }

            if (inactiveSource == null)
            {
                inactiveSource = CreateSource("Jam Music Source B");
            }
        }

        private AudioSource CreateSource(string sourceName)
        {
            GameObject sourceObject = new GameObject(sourceName);
            sourceObject.transform.SetParent(transform);

            AudioSource source = sourceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.loop = true;

            return source;
        }

        private static void ConfigureSource(AudioSource source, JamMusicEvent musicEvent)
        {
            source.clip = musicEvent.musicClip;
            source.loop = musicEvent.loop;
            source.volume = 0f;
            source.pitch = 1f;
            source.spatialBlend = 0f;
            source.outputAudioMixerGroup = musicEvent.outputMixerGroup;
            JamAudioFilterUtility.Apply(
                source,
                musicEvent.GetLowPassFilterAmount(),
                musicEvent.GetHighPassFilterAmount());
        }

        private void SwapSources()
        {
            AudioSource previousActiveSource = activeSource;
            activeSource = inactiveSource;
            inactiveSource = previousActiveSource;
        }

        private AudioSource GetCurrentMusicSource()
        {
            AudioClip currentClip = currentMusicEvent != null ? currentMusicEvent.musicClip : null;

            if (currentClip != null)
            {
                if (activeSource.clip == currentClip)
                {
                    return activeSource;
                }

                if (inactiveSource.clip == currentClip)
                {
                    return inactiveSource;
                }
            }

            if (activeSource.clip != null)
            {
                return activeSource;
            }

            return inactiveSource.clip != null ? inactiveSource : null;
        }

        private void MakeActiveSource(AudioSource source)
        {
            if (source == activeSource)
            {
                return;
            }

            SwapSources();
        }

        private void StopAndClear(AudioSource source)
        {
            source.Stop();
            source.clip = null;
            source.volume = 0f;
            source.outputAudioMixerGroup = null;
            JamAudioFilterUtility.Clear(source);
        }

        private void StopTransitionCoroutine()
        {
            if (transitionCoroutine == null)
            {
                return;
            }

            StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;
        }

        private bool HasOtherManagerInScene()
        {
#if UNITY_6000_0_OR_NEWER
            JamMusicManager[] managers = Object.FindObjectsByType<JamMusicManager>(FindObjectsInactive.Include);
#else
            JamMusicManager[] managers = Object.FindObjectsByType<JamMusicManager>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
#endif

            foreach (JamMusicManager manager in managers)
            {
                if (manager != null && manager != this)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

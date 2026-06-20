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
        private static JamMusicManager instance;
        private static bool applicationIsQuitting;

        private AudioSource activeSource;
        private AudioSource inactiveSource;
        private JamMusicEvent currentMusicEvent;
        private Coroutine transitionCoroutine;

        /// <summary>
        /// Gets the active music manager, creating one if needed during Play Mode.
        /// </summary>
        public static JamMusicManager Instance
        {
            get
            {
                if (instance != null)
                {
                    return instance;
                }

                if (applicationIsQuitting)
                {
                    return null;
                }

                instance = FindExistingManager();
                if (instance != null)
                {
                    return instance;
                }

                GameObject managerObject = new GameObject("Jam Music Manager");
                instance = managerObject.AddComponent<JamMusicManager>();
                return instance;
            }
        }

        /// <summary>
        /// The music event currently requested by the manager.
        /// </summary>
        public JamMusicEvent CurrentMusicEvent => currentMusicEvent;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            instance = null;
            applicationIsQuitting = false;
        }

        private static JamMusicManager FindExistingManager()
        {
#if UNITY_2023_1_OR_NEWER
            return Object.FindFirstObjectByType<JamMusicManager>();
#else
            return Object.FindObjectOfType<JamMusicManager>();
#endif
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureSources();
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDestroy()
        {
            if (instance != this)
            {
                return;
            }

            SceneManager.sceneLoaded -= HandleSceneLoaded;
            instance = null;
        }

        private void OnApplicationQuit()
        {
            applicationIsQuitting = true;
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

            currentMusicEvent = null;
            transitionCoroutine = StartCoroutine(StopAllMusic(fadeOutDuration));
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
            currentMusicEvent = null;
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
        }

        private void SwapSources()
        {
            AudioSource previousActiveSource = activeSource;
            activeSource = inactiveSource;
            inactiveSource = previousActiveSource;
        }

        private void StopAndClear(AudioSource source)
        {
            source.Stop();
            source.clip = null;
            source.volume = 0f;
            source.outputAudioMixerGroup = null;
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
    }
}

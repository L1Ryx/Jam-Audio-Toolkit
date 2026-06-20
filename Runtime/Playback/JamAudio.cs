using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JamAudioToolkit
{
    /// <summary>
    /// Static convenience API for playing Jam audio from code.
    /// </summary>
    public static class JamAudio
    {
        /// <summary>
        /// Plays a sound event using its default positioning.
        /// </summary>
        /// <returns>The AudioSource used for playback, or null if playback could not start.</returns>
        public static AudioSource Play(JamSoundEvent soundEvent)
        {
            bool use3DPosition = soundEvent != null && soundEvent.positionMode == JamAudioPositionMode.Position3D;
            return PlayInternal(soundEvent, Vector3.zero, use3DPosition);
        }

        /// <summary>
        /// Plays a sound event at a GameObject's current world position.
        /// </summary>
        /// <returns>The AudioSource used for playback, or null if playback could not start.</returns>
        public static AudioSource Play(JamSoundEvent soundEvent, GameObject target)
        {
            return Play(soundEvent, target != null ? target.transform : null);
        }

        /// <summary>
        /// Plays a sound event at a component's current world position.
        /// </summary>
        /// <returns>The AudioSource used for playback, or null if playback could not start.</returns>
        public static AudioSource Play(JamSoundEvent soundEvent, Component target)
        {
            return Play(soundEvent, target != null ? target.transform : null);
        }

        /// <summary>
        /// Plays a sound event at a Transform's current world position.
        /// </summary>
        /// <returns>The AudioSource used for playback, or null if playback could not start.</returns>
        public static AudioSource Play(JamSoundEvent soundEvent, Transform target)
        {
            if (target == null)
            {
                Debug.LogWarning("Cannot play a Jam Sound Event on a missing target.");
                return null;
            }

            return PlayInternal(soundEvent, target.position, true);
        }

        /// <summary>
        /// Plays a sound event at a world position.
        /// </summary>
        /// <returns>The AudioSource used for playback, or null if playback could not start.</returns>
        public static AudioSource PlayAtPosition(JamSoundEvent soundEvent, Vector3 position)
        {
            return PlayInternal(soundEvent, position, true);
        }

        /// <summary>
        /// Plays a music event, crossfading away from the current music if needed.
        /// </summary>
        public static void Play(JamMusicEvent musicEvent)
        {
            PlayMusic(musicEvent);
        }

        /// <summary>
        /// Plays a music event, crossfading away from the current music if needed.
        /// </summary>
        public static void PlayMusic(JamMusicEvent musicEvent)
        {
            JamMusicManager manager = JamMusicManager.GetOrCreate();
            if (manager == null)
            {
                return;
            }

            manager.PlayMusic(musicEvent);
        }

        /// <summary>
        /// Stops the current music, using the music event's fade-out duration when available.
        /// </summary>
        public static void StopMusic()
        {
            StopMusic(-1f);
        }

        /// <summary>
        /// Stops the current music, optionally overriding the fade-out duration in seconds.
        /// </summary>
        public static void StopMusic(float fadeOutSeconds)
        {
            JamMusicManager manager = JamMusicManager.GetOrCreate();
            if (manager == null)
            {
                return;
            }

            manager.StopMusic(fadeOutSeconds);
        }

        /// <summary>
        /// Pauses the current music, using the music event's fade-out duration when available.
        /// </summary>
        public static void PauseMusic()
        {
            PauseMusic(-1f);
        }

        /// <summary>
        /// Pauses the current music, optionally overriding the fade-out duration in seconds.
        /// </summary>
        public static void PauseMusic(float fadeOutSeconds)
        {
            JamMusicManager manager = JamMusicManager.GetOrCreate();
            if (manager == null)
            {
                return;
            }

            manager.PauseMusic(fadeOutSeconds);
        }

        /// <summary>
        /// Resumes paused music, using the music event's fade-in duration when available.
        /// </summary>
        public static void ResumeMusic()
        {
            ResumeMusic(-1f);
        }

        /// <summary>
        /// Resumes paused music, optionally overriding the fade-in duration in seconds.
        /// </summary>
        public static void ResumeMusic(float fadeInSeconds)
        {
            JamMusicManager manager = JamMusicManager.GetOrCreate();
            if (manager == null)
            {
                return;
            }

            manager.ResumeMusic(fadeInSeconds);
        }

        private static AudioSource PlayInternal(JamSoundEvent soundEvent, Vector3 position, bool use3DPosition)
        {
            if (soundEvent == null)
            {
                Debug.LogWarning("Cannot play a missing Jam Sound Event.");
                return null;
            }

            AudioClip clip = soundEvent.GetClip();
            if (clip == null)
            {
                return null;
            }

            return GetPool().Play(soundEvent, clip, position, use3DPosition);
        }

        private static JamAudioSourcePool GetPool()
        {
            JamAudioSourcePool existingPool = FindExistingPool();
            if (existingPool != null)
            {
                return existingPool;
            }

            GameObject poolObject = new GameObject("Jam Audio Runtime");
            Object.DontDestroyOnLoad(poolObject);

            return poolObject.AddComponent<JamAudioSourcePool>();
        }

        private static JamAudioSourcePool FindExistingPool()
        {
#if UNITY_2023_1_OR_NEWER
            return Object.FindAnyObjectByType<JamAudioSourcePool>();
#else
            return Object.FindObjectOfType<JamAudioSourcePool>();
#endif
        }
    }

    internal sealed class JamAudioSourcePool : MonoBehaviour
    {
        private readonly Stack<AudioSource> availableSources = new Stack<AudioSource>();
        private readonly HashSet<AudioSource> activeSources = new HashSet<AudioSource>();

        public AudioSource Play(JamSoundEvent soundEvent, AudioClip clip, Vector3 position, bool use3DPosition)
        {
            AudioSource source = GetSource();
            ConfigureSource(source, soundEvent, clip, position, use3DPosition);

            activeSources.Add(source);
            source.Play();

            if (soundEvent.loop)
            {
                StartCoroutine(ReleaseWhenStopped(source));
            }
            else
            {
                StartCoroutine(ReleaseAfterClip(source, clip.length, source.pitch));
            }

            return source;
        }

        private AudioSource GetSource()
        {
            while (availableSources.Count > 0)
            {
                AudioSource source = availableSources.Pop();
                if (source != null)
                {
                    return source;
                }
            }

            GameObject sourceObject = new GameObject("Jam Audio Source");
            sourceObject.transform.SetParent(transform);

            AudioSource newSource = sourceObject.AddComponent<AudioSource>();
            newSource.playOnAwake = false;

            return newSource;
        }

        private static void ConfigureSource(
            AudioSource source,
            JamSoundEvent soundEvent,
            AudioClip clip,
            Vector3 position,
            bool use3DPosition)
        {
            source.gameObject.SetActive(true);
            source.transform.position = position;
            source.clip = clip;
            source.loop = soundEvent.loop;
            source.volume = soundEvent.GetVolume();
            source.pitch = soundEvent.GetPitch();
            source.spatialBlend = use3DPosition ? 1f : 0f;
            source.outputAudioMixerGroup = soundEvent.outputMixerGroup;
            JamAudioFilterUtility.Apply(
                source,
                soundEvent.GetLowPassFilterAmount(),
                soundEvent.GetHighPassFilterAmount());
        }

        private IEnumerator ReleaseAfterClip(AudioSource source, float clipLength, float pitch)
        {
            float playbackSpeed = Mathf.Max(0.01f, Mathf.Abs(pitch));
            float endTime = Time.time + (clipLength / playbackSpeed);

            while (source != null && source.isPlaying && Time.time < endTime)
            {
                yield return null;
            }

            Release(source);
        }

        private IEnumerator ReleaseWhenStopped(AudioSource source)
        {
            yield return null;

            while (source != null && source.isPlaying)
            {
                yield return null;
            }

            Release(source);
        }

        private void Release(AudioSource source)
        {
            if (source == null || !activeSources.Remove(source))
            {
                return;
            }

            source.Stop();
            source.clip = null;
            source.loop = false;
            source.outputAudioMixerGroup = null;
            JamAudioFilterUtility.Clear(source);
            source.transform.SetParent(transform);
            source.gameObject.SetActive(false);

            availableSources.Push(source);
        }
    }
}

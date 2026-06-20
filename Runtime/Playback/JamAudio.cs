using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JamAudioToolkit
{
    /// <summary>
    /// Static convenience API for playing JamSoundEvents from code.
    /// </summary>
    public static class JamAudio
    {
        private static JamAudioSourcePool pool;

        /// <summary>
        /// Plays a sound event as a non-positional sound.
        /// </summary>
        /// <returns>The AudioSource used for playback, or null if playback could not start.</returns>
        public static AudioSource Play(JamSoundEvent soundEvent)
        {
            return PlayInternal(soundEvent, Vector3.zero, false);
        }

        /// <summary>
        /// Plays a sound event at a world position.
        /// </summary>
        /// <returns>The AudioSource used for playback, or null if playback could not start.</returns>
        public static AudioSource PlayAtPosition(JamSoundEvent soundEvent, Vector3 position)
        {
            return PlayInternal(soundEvent, position, true);
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
            if (pool != null)
            {
                return pool;
            }

            GameObject poolObject = new GameObject("Jam Audio Runtime");
            Object.DontDestroyOnLoad(poolObject);

            pool = poolObject.AddComponent<JamAudioSourcePool>();
            return pool;
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
            source.transform.SetParent(transform);
            source.gameObject.SetActive(false);

            availableSources.Push(source);
        }
    }
}

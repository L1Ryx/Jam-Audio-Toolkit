using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JamAudioToolkit
{
    /// <summary>
    /// Static convenience API for playing JamAudioEvents from code.
    /// </summary>
    public static class JamAudio
    {
        private static JamAudioSourcePool pool;

        /// <summary>
        /// Plays an audio event as a non-positional sound.
        /// </summary>
        /// <returns>The AudioSource used for playback, or null if playback could not start.</returns>
        public static AudioSource Play(JamAudioEvent audioEvent)
        {
            return PlayInternal(audioEvent, Vector3.zero, false);
        }

        /// <summary>
        /// Plays an audio event at a world position.
        /// </summary>
        /// <returns>The AudioSource used for playback, or null if playback could not start.</returns>
        public static AudioSource PlayAtPosition(JamAudioEvent audioEvent, Vector3 position)
        {
            return PlayInternal(audioEvent, position, true);
        }

        private static AudioSource PlayInternal(JamAudioEvent audioEvent, Vector3 position, bool use3DPosition)
        {
            if (audioEvent == null)
            {
                Debug.LogWarning("Cannot play a missing Jam Audio Event.");
                return null;
            }

            AudioClip clip = audioEvent.GetClip();
            if (clip == null)
            {
                return null;
            }

            return GetPool().Play(audioEvent, clip, position, use3DPosition);
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

        public AudioSource Play(JamAudioEvent audioEvent, AudioClip clip, Vector3 position, bool use3DPosition)
        {
            AudioSource source = GetSource();
            ConfigureSource(source, audioEvent, clip, position, use3DPosition);

            activeSources.Add(source);
            source.Play();

            if (audioEvent.loop)
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
            JamAudioEvent audioEvent,
            AudioClip clip,
            Vector3 position,
            bool use3DPosition)
        {
            source.gameObject.SetActive(true);
            source.transform.position = position;
            source.clip = clip;
            source.loop = audioEvent.loop;
            source.volume = audioEvent.GetVolume();
            source.pitch = audioEvent.GetPitch();
            source.spatialBlend = use3DPosition ? 1f : 0f;
            source.outputAudioMixerGroup = audioEvent.outputMixerGroup;
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

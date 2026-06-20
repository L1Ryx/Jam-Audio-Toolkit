using UnityEngine;
using UnityEngine.Audio;

namespace JamAudioToolkit
{
    /// <summary>
    /// Defines a reusable music track and its transition behavior.
    /// </summary>
    [CreateAssetMenu(menuName = "Jam Audio Toolkit/Music Event", fileName = "New Jam Music Event")]
    public class JamMusicEvent : ScriptableObject
    {
        [Tooltip("The music clip this event plays.")]
        public AudioClip musicClip;

        [Header("Playback")]
        [Tooltip("Playback volume using Unity's linear 0-1 AudioSource volume scale. This is not dB.")]
        [InspectorName("Volume (0-1)")]
        [Range(0f, 1f)] public float volume = 1f;

        [Tooltip("Loop the music clip while it is active.")]
        public bool loop = true;

        [Tooltip("Keep this music playing across scene loads.")]
        public bool persistAcrossScenes = true;

        [Header("Transition")]
        [Tooltip("Seconds to fade this music in.")]
        [InspectorName("Fade In (s)")]
        [Min(0f)] public float fadeInDuration = 1f;

        [Tooltip("Seconds to fade the previous music out.")]
        [InspectorName("Fade Out (s)")]
        [Min(0f)] public float fadeOutDuration = 1f;

        [Header("Routing")]
        [Tooltip("Optional mixer group. Leave empty to use the default audio output.")]
        public AudioMixerGroup outputMixerGroup;

        /// <summary>
        /// Returns this music event's volume clamped to Unity's normal volume range.
        /// </summary>
        public float GetVolume()
        {
            return Mathf.Clamp01(volume);
        }

        /// <summary>
        /// Returns a non-negative fade-in duration.
        /// </summary>
        public float GetFadeInDuration()
        {
            return Mathf.Max(0f, fadeInDuration);
        }

        /// <summary>
        /// Returns a non-negative fade-out duration.
        /// </summary>
        public float GetFadeOutDuration()
        {
            return Mathf.Max(0f, fadeOutDuration);
        }
    }
}

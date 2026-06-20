using UnityEngine;
using UnityEngine.Audio;

namespace JamAudioToolkit
{
    /// <summary>
    /// Defines a reusable music track and its transition behavior.
    /// </summary>
    [CreateAssetMenu(menuName = "Jam Audio/Empty Music Event", fileName = "Empty Music Event", order = 202)]
    public class JamMusicEvent : ScriptableObject
    {
        [Tooltip("The music clip this event plays.")]
        public AudioClip musicClip;

        [Tooltip("Playback volume shown as 0-100%. This is converted to Unity's linear 0-1 AudioSource volume scale, not dB.")]
        [InspectorName("Volume (%)")]
        [JamPercent(0f, 100f)] public float volume = 1f;

        [Tooltip("Loop the music clip while it is active.")]
        public bool loop = true;

        [Tooltip("Keep this music playing across scene loads.")]
        public bool persistAcrossScenes = true;

        [Tooltip("0% is clear and unfiltered. Higher values remove more high frequencies, making the music darker or more muffled.")]
        [InspectorName("Low-Pass Filter (%)")]
        [JamPercent(0f, 100f)] public float lowPassFilterAmount;

        [Tooltip("0% is full and unfiltered. Higher values remove more low frequencies, making the music thinner or more radio-like.")]
        [InspectorName("High-Pass Filter (%)")]
        [JamPercent(0f, 100f)] public float highPassFilterAmount;

        [Tooltip("Seconds to fade this music in.")]
        [InspectorName("Fade In (Seconds)")]
        [Min(0f)] public float fadeInDuration = 1f;

        [Tooltip("Seconds to fade the previous music out.")]
        [InspectorName("Fade Out (Seconds)")]
        [Min(0f)] public float fadeOutDuration = 1f;

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
        /// Returns the low-pass filter amount, where 0 is off and 1 is strongest.
        /// </summary>
        public float GetLowPassFilterAmount()
        {
            return Mathf.Clamp01(lowPassFilterAmount);
        }

        /// <summary>
        /// Returns the high-pass filter amount, where 0 is off and 1 is strongest.
        /// </summary>
        public float GetHighPassFilterAmount()
        {
            return Mathf.Clamp01(highPassFilterAmount);
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

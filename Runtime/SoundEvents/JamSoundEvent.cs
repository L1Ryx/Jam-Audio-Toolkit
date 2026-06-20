using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Serialization;

namespace JamAudioToolkit
{
    /// <summary>
    /// Defines reusable playback settings for a sound effect or ambience clip.
    /// </summary>
    [CreateAssetMenu(menuName = "Jam Audio/Empty Sound Event", fileName = "Empty Sound Event", order = 200)]
    public class JamSoundEvent : ScriptableObject
    {
        [Tooltip("One or more clips this event can choose from when played. One clip is perfectly fine.")]
        [InspectorName("Clip(s)")]
        public AudioClip[] clips;

        [Tooltip("Base playback volume shown as 0-100%. This is converted to Unity's linear 0-1 AudioSource volume scale, not dB.")]
        [InspectorName("Volume (%)")]
        [JamPercent(0f, 100f)] public float volume = 1f;

        [Tooltip("Base playback pitch shown as a percentage. 100% is normal pitch/speed.")]
        [InspectorName("Pitch (%)")]
        [JamPercent(0f, 300f)] public float pitch = 1f;

        [Tooltip("When enabled, this event loops until its AudioSource is stopped.")]
        public bool loop;

        [Tooltip("0% is clear and unfiltered. Higher values remove more high frequencies, making the sound darker or more muffled.")]
        [InspectorName("Low-Pass Filter (%)")]
        [JamPercent(0f, 100f)] public float lowPassFilterAmount;

        [Tooltip("0% is full and unfiltered. Higher values remove more low frequencies, making the sound thinner or more radio-like.")]
        [InspectorName("High-Pass Filter (%)")]
        [JamPercent(0f, 100f)] public float highPassFilterAmount;

        [Tooltip("Default positioning for this sound when it is played.")]
        [InspectorName("Positioning")]
        public JamAudioPositionMode positionMode = JamAudioPositionMode.None;

        [Tooltip("Random percentage variation around Volume. Min lowers volume, Max raises volume.")]
        [InspectorName("Volume Variation (%)")]
        [JamMinMax(true)]
        public Vector2 volumeRandomRange = Vector2.zero;

        [Tooltip("Random percentage variation around Pitch. Min lowers pitch, Max raises pitch.")]
        [InspectorName("Pitch Variation (%)")]
        [JamMinMax(true)]
        public Vector2 pitchRandomRange = Vector2.zero;

        [Tooltip("Choose a random clip each time this event plays.")]
        public bool randomizeClip = true;

        [Tooltip("How many recently played clips should be skipped when possible. Set to 0 to allow immediate repeats.")]
        [InspectorName("Recent Clips To Avoid")]
        [FormerlySerializedAs("preventImmediateRepeat")]
        [Min(0)] public int avoidRepeatingLastClips = 1;

        [Tooltip("Optional mixer group. Leave empty to use the default audio output.")]
        public AudioMixerGroup outputMixerGroup;

        private readonly List<int> recentClipIndexes = new List<int>();
        private readonly List<int> candidateClipIndexes = new List<int>();

        /// <summary>
        /// Returns a clip using this event's selection rules, or null if none are assigned.
        /// </summary>
        public AudioClip GetClip()
        {
            if (clips == null || clips.Length == 0)
            {
                Debug.LogWarning($"{name} has no audio clips assigned.", this);
                return null;
            }

            int selectedIndex = GetClipIndex();
            if (selectedIndex < 0)
            {
                Debug.LogWarning($"{name} has no playable audio clips assigned.", this);
                return null;
            }

            AudioClip selectedClip = clips[selectedIndex];

            if (selectedClip == null)
            {
                Debug.LogWarning($"{name} selected an empty audio clip slot.", this);
                return null;
            }

            RememberClipIndex(selectedIndex);
            return selectedClip;
        }

        /// <summary>
        /// Returns this event's final volume after randomization and clamping.
        /// </summary>
        public float GetVolume()
        {
            return Mathf.Clamp01(volume + GetRandomOffset(volumeRandomRange));
        }

        /// <summary>
        /// Returns this event's final pitch after randomization and clamping.
        /// </summary>
        public float GetPitch()
        {
            return Mathf.Clamp(pitch + GetRandomOffset(pitchRandomRange), 0f, 3f);
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

        private int GetClipIndex()
        {
            if (!randomizeClip || clips.Length == 1)
            {
                return 0;
            }

            int avoidCount = Mathf.Clamp(avoidRepeatingLastClips, 0, clips.Length - 1);
            candidateClipIndexes.Clear();

            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] != null && !IsRecentlyPlayed(i, avoidCount))
                {
                    candidateClipIndexes.Add(i);
                }
            }

            if (candidateClipIndexes.Count == 0)
            {
                for (int i = 0; i < clips.Length; i++)
                {
                    if (clips[i] != null)
                    {
                        candidateClipIndexes.Add(i);
                    }
                }
            }

            if (candidateClipIndexes.Count == 0)
            {
                return -1;
            }

            return candidateClipIndexes[Random.Range(0, candidateClipIndexes.Count)];
        }

        private bool IsRecentlyPlayed(int clipIndex, int avoidCount)
        {
            if (avoidCount <= 0)
            {
                return false;
            }

            int firstIndexToCheck = Mathf.Max(0, recentClipIndexes.Count - avoidCount);
            for (int i = recentClipIndexes.Count - 1; i >= firstIndexToCheck; i--)
            {
                if (recentClipIndexes[i] == clipIndex)
                {
                    return true;
                }
            }

            return false;
        }

        private void RememberClipIndex(int clipIndex)
        {
            recentClipIndexes.Add(clipIndex);

            int maxHistory = Mathf.Max(0, avoidRepeatingLastClips);
            while (recentClipIndexes.Count > maxHistory)
            {
                recentClipIndexes.RemoveAt(0);
            }
        }

        private static float GetRandomOffset(Vector2 range)
        {
            float min = Mathf.Min(range.x, range.y);
            float max = Mathf.Max(range.x, range.y);
            return Mathf.Approximately(min, max) ? min : Random.Range(min, max);
        }
    }
}

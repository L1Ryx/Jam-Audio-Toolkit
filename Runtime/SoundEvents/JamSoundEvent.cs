using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Serialization;

namespace JamAudioToolkit
{
    /// <summary>
    /// Defines reusable playback settings for a sound effect or ambience clip.
    /// </summary>
    [CreateAssetMenu(menuName = "Jam Audio Toolkit/Sound Event", fileName = "New Jam Sound Event")]
    public class JamSoundEvent : ScriptableObject
    {
        [Header("Clips")]
        [Tooltip("One or more clips this event can choose from when played.")]
        public AudioClip[] clips;

        [Header("Playback")]
        [Tooltip("Base playback volume using Unity's linear 0-1 AudioSource volume scale. This is not dB.")]
        [InspectorName("Volume (0-1)")]
        [Range(0f, 1f)] public float volume = 1f;

        [Tooltip("Base playback pitch using Unity's AudioSource pitch multiplier. 1 is normal pitch/speed.")]
        [InspectorName("Pitch (x)")]
        [Range(-3f, 3f)] public float pitch = 1f;

        [Tooltip("When enabled, this event loops until its AudioSource is stopped.")]
        public bool loop;

        [Header("Randomization")]
        [Tooltip("Random linear volume offset added to Volume. Example: -0.1 to 0.1 varies Unity volume by +/- 0.1.")]
        [InspectorName("Volume Variation (+/- 0-1)")]
        [JamMinMax]
        public Vector2 volumeRandomRange = Vector2.zero;

        [Tooltip("Random Unity pitch multiplier offset added to Pitch. Example: -0.05 to 0.05 varies pitch around 1 by +/- 0.05.")]
        [InspectorName("Pitch Variation (+/- x)")]
        [JamMinMax]
        public Vector2 pitchRandomRange = Vector2.zero;

        [Tooltip("Choose a random clip each time this event plays.")]
        public bool randomizeClip = true;

        [Tooltip("Avoid choosing any of the last N played clips when possible. Set to 0 to allow immediate repeats.")]
        [InspectorName("Avoid Last N Clips")]
        [FormerlySerializedAs("preventImmediateRepeat")]
        [Min(0)] public int avoidRepeatingLastClips = 1;

        [Header("Routing")]
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
            return Mathf.Clamp(pitch + GetRandomOffset(pitchRandomRange), -3f, 3f);
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

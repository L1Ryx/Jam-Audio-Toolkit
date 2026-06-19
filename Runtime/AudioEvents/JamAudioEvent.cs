using UnityEngine;
using UnityEngine.Audio;

namespace JamAudioToolkit
{
    /// <summary>
    /// Defines reusable playback settings for a sound effect or ambience clip.
    /// </summary>
    [CreateAssetMenu(menuName = "Jam Audio Toolkit/Audio Event", fileName = "New Jam Audio Event")]
    public class JamAudioEvent : ScriptableObject
    {
        [Header("Clips")]
        public AudioClip[] clips;

        [Header("Playback")]
        [Range(0f, 1f)] public float volume = 1f;
        public Vector2 volumeRandomRange = Vector2.zero;

        [Range(-3f, 3f)] public float pitch = 1f;
        public Vector2 pitchRandomRange = Vector2.zero;

        public bool loop;

        [Header("Selection")]
        public bool randomizeClip = true;
        public bool preventImmediateRepeat = true;

        [Header("Routing")]
        public AudioMixerGroup outputMixerGroup;

        private int lastPlayedIndex = -1;

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
            AudioClip selectedClip = clips[selectedIndex];

            if (selectedClip == null)
            {
                Debug.LogWarning($"{name} selected an empty audio clip slot.", this);
                return null;
            }

            lastPlayedIndex = selectedIndex;
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

            int selectedIndex = Random.Range(0, clips.Length);

            if (!preventImmediateRepeat || clips.Length <= 1)
            {
                return selectedIndex;
            }

            while (selectedIndex == lastPlayedIndex)
            {
                selectedIndex = Random.Range(0, clips.Length);
            }

            return selectedIndex;
        }

        private static float GetRandomOffset(Vector2 range)
        {
            float min = Mathf.Min(range.x, range.y);
            float max = Mathf.Max(range.x, range.y);
            return Mathf.Approximately(min, max) ? min : Random.Range(min, max);
        }
    }
}

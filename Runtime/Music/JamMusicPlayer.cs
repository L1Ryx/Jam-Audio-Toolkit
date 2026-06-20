using UnityEngine;

namespace JamAudioToolkit
{
    /// <summary>
    /// Plays a JamMusicEvent from a scene object without requiring custom code.
    /// </summary>
    [AddComponentMenu("Jam Audio Toolkit/Jam Music Player")]
    [DisallowMultipleComponent]
    public class JamMusicPlayer : MonoBehaviour
    {
        [Tooltip("The music event this component will request from the Jam Music Manager.")]
        public JamMusicEvent musicEvent;

        [Tooltip("Play this music automatically when the scene starts.")]
        public bool playOnStart = true;

        private void Start()
        {
            if (playOnStart)
            {
                Play();
            }
        }

        /// <summary>
        /// Requests playback of the assigned music event.
        /// </summary>
        [ContextMenu("Play")]
        public void Play()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Jam Music Player playback runs in Play Mode.", this);
                return;
            }

            if (musicEvent == null)
            {
                Debug.LogWarning($"{name} has no Jam Music Event assigned.", this);
                return;
            }

            JamMusicManager manager = JamMusicManager.Instance;
            if (manager == null)
            {
                return;
            }

            manager.PlayMusic(musicEvent);
        }

        /// <summary>
        /// Requests that the current music fade out and stop.
        /// </summary>
        [ContextMenu("Stop")]
        public void Stop()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            JamMusicManager manager = JamMusicManager.Instance;
            if (manager == null)
            {
                return;
            }

            manager.StopMusic();
        }
    }
}

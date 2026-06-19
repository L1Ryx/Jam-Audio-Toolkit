using UnityEngine;

namespace JamAudioToolkit
{
    /// <summary>
    /// Plays a JamAudioEvent from a GameObject using Unity lifecycle and physics callbacks.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public class JamAudioPlayer : MonoBehaviour
    {
        public JamAudioEvent audioEvent;

        [Header("Lifecycle")]
        public bool playOnAwake;
        public bool playOnStart;
        public bool playOnEnable;

        [Header("Physics")]
        public bool playOnTriggerEnter;
        public bool playOnTriggerExit;
        public bool playOnCollisionEnter;

        [Header("Playback")]
        public bool use3DPosition = true;

        private AudioSource audioSource;

        private void Awake()
        {
            EnsureAudioSource();

            if (playOnAwake)
            {
                Play();
            }
        }

        private void OnEnable()
        {
            if (playOnEnable)
            {
                Play();
            }
        }

        private void Start()
        {
            if (playOnStart)
            {
                Play();
            }
        }

        private void Reset()
        {
            EnsureAudioSource();
        }

        /// <summary>
        /// Plays the assigned audio event using this GameObject's AudioSource.
        /// </summary>
        [ContextMenu("Play")]
        public void Play()
        {
            if (audioEvent == null)
            {
                Debug.LogWarning($"{name} has no Jam Audio Event assigned.", this);
                return;
            }

            AudioClip clip = audioEvent.GetClip();
            if (clip == null)
            {
                return;
            }

            EnsureAudioSource();
            ConfigureAudioSource(clip);

            if (audioEvent.loop)
            {
                audioSource.clip = clip;
                audioSource.Play();
                return;
            }

            audioSource.PlayOneShot(clip, audioSource.volume);
        }

        /// <summary>
        /// Stops playback on this GameObject's AudioSource.
        /// </summary>
        [ContextMenu("Stop")]
        public void Stop()
        {
            EnsureAudioSource();
            audioSource.Stop();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (playOnTriggerEnter)
            {
                Play();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (playOnTriggerExit)
            {
                Play();
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (playOnCollisionEnter)
            {
                Play();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (playOnTriggerEnter)
            {
                Play();
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (playOnTriggerExit)
            {
                Play();
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (playOnCollisionEnter)
            {
                Play();
            }
        }

        private void EnsureAudioSource()
        {
            if (audioSource != null)
            {
                return;
            }

            if (!TryGetComponent(out audioSource))
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            audioSource.playOnAwake = false;
        }

        private void ConfigureAudioSource(AudioClip clip)
        {
            audioSource.clip = audioEvent.loop ? clip : null;
            audioSource.loop = audioEvent.loop;
            audioSource.volume = audioEvent.GetVolume();
            audioSource.pitch = audioEvent.GetPitch();
            audioSource.spatialBlend = use3DPosition ? 1f : 0f;
            audioSource.outputAudioMixerGroup = audioEvent.outputMixerGroup;
        }
    }
}

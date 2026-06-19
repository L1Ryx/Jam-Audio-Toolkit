using UnityEngine;
using UnityEngine.Serialization;

namespace JamAudioToolkit
{
    /// <summary>
    /// Plays a JamAudioEvent from a GameObject using Unity lifecycle and physics callbacks.
    /// </summary>
    [AddComponentMenu("Jam Audio Toolkit/Jam Audio Player")]
    [DisallowMultipleComponent]
    public class JamAudioPlayer : MonoBehaviour
    {
        [Tooltip("The audio event this component will play.")]
        public JamAudioEvent audioEvent;

        [Header("Lifecycle")]
        [Tooltip("Play during Awake, before most other scene objects have started.")]
        public bool playOnAwake;
        [Tooltip("Play during Start, after scene objects have awakened.")]
        public bool playOnStart;
        [Tooltip("Play each time this GameObject or component is enabled.")]
        public bool playOnEnable;

        [Header("Physics")]
        [Tooltip("Play when a 2D or 3D trigger enters this object.")]
        public bool playOnTriggerEnter;
        [Tooltip("Play when a 2D or 3D trigger exits this object.")]
        public bool playOnTriggerExit;
        [Tooltip("Play when a 2D or 3D collision starts on this object.")]
        public bool playOnCollisionEnter;

        [Header("Positioning")]
        [Tooltip("Choose whether playback ignores position or uses this GameObject's 3D position.")]
        [FormerlySerializedAs("use3DPosition")]
        public JamAudioPositionMode positionMode = JamAudioPositionMode.None;

        [Header("Debug")]
        [Tooltip("Controls how the runtime-generated AudioSource appears in the Inspector.")]
        [InspectorName("Show Audio Source")]
        public JamAudioSourceDebugView audioSourceDebugView = JamAudioSourceDebugView.Off;

        private AudioSource audioSource;
        private bool audioSourceWasGenerated;

        private void Awake()
        {
            if (playOnAwake)
            {
                Play();
            }
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

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

        private void OnValidate()
        {
            ApplyGeneratedAudioSourceVisibility();
        }

        /// <summary>
        /// Plays the assigned audio event using this GameObject's AudioSource.
        /// </summary>
        [ContextMenu("Play")]
        public void Play()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Jam Audio Player playback runs in Play Mode. Editor preview is coming in a later tooling step.", this);
                return;
            }

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
            if (audioSource == null)
            {
                return;
            }

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
                audioSourceWasGenerated = true;
            }

            audioSource.playOnAwake = false;
            ApplyGeneratedAudioSourceVisibility();
        }

        private void ConfigureAudioSource(AudioClip clip)
        {
            ApplyGeneratedAudioSourceVisibility();

            audioSource.clip = audioEvent.loop ? clip : null;
            audioSource.loop = audioEvent.loop;
            audioSource.volume = audioEvent.GetVolume();
            audioSource.pitch = audioEvent.GetPitch();
            audioSource.spatialBlend = positionMode == JamAudioPositionMode.Position3D ? 1f : 0f;
            audioSource.outputAudioMixerGroup = audioEvent.outputMixerGroup;
        }

        private void ApplyGeneratedAudioSourceVisibility()
        {
            if (!audioSourceWasGenerated || audioSource == null)
            {
                return;
            }

            audioSource.hideFlags = audioSourceDebugView switch
            {
                JamAudioSourceDebugView.Off => HideFlags.HideInInspector,
                JamAudioSourceDebugView.EditableAtRuntime => HideFlags.None,
                _ => HideFlags.NotEditable
            };
        }
    }
}

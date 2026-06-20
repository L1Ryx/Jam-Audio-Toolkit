using UnityEngine;
using UnityEngine.Serialization;

namespace JamAudioToolkit
{
    /// <summary>
    /// Plays a JamSoundEvent from a GameObject using Unity lifecycle and physics callbacks.
    /// </summary>
    [AddComponentMenu("Jam Audio Toolkit/Jam Audio Player")]
    [DisallowMultipleComponent]
    public class JamAudioPlayer : MonoBehaviour
    {
        [Tooltip("The sound event this component will play.")]
        [FormerlySerializedAs("audioEvent")]
        public JamSoundEvent soundEvent;

        [Tooltip("Play during Awake, before most other scene objects have started.")]
        public bool playOnAwake;
        [Tooltip("Play during Start, after scene objects have awakened.")]
        public bool playOnStart;
        [Tooltip("Play each time this GameObject or component is enabled.")]
        public bool playOnEnable;

        [Tooltip("Play when a 2D or 3D trigger enters this object.")]
        public bool playOnTriggerEnter;
        [Tooltip("Play when a 2D or 3D trigger exits this object.")]
        public bool playOnTriggerExit;
        [Tooltip("Play when a 2D or 3D collision starts on this object.")]
        public bool playOnCollisionEnter;

        [Tooltip("Use a positioning mode on this component instead of the assigned Sound Event's default.")]
        [InspectorName("Override Sound Event")]
        public bool overrideSoundEventPositioning;

        [Tooltip("Choose whether playback ignores position or uses this GameObject's 3D position.")]
        [FormerlySerializedAs("use3DPosition")]
        public JamAudioPositionMode positionMode = JamAudioPositionMode.None;

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
        /// Plays the assigned sound event using this GameObject's AudioSource.
        /// </summary>
        [ContextMenu("Play")]
        public void Play()
        {
            Play(soundEvent);
        }

        /// <summary>
        /// Plays a specific sound event using this GameObject's AudioSource.
        /// </summary>
        public void Play(JamSoundEvent soundEventToPlay)
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Jam Audio Player playback runs in Play Mode.", this);
                return;
            }

            if (soundEventToPlay == null)
            {
                Debug.LogWarning($"{name} has no Jam Sound Event assigned.", this);
                return;
            }

            AudioClip clip = soundEventToPlay.GetClip();
            if (clip == null)
            {
                return;
            }

            EnsureAudioSource();
            ConfigureAudioSource(soundEventToPlay, clip);

            if (soundEventToPlay.loop)
            {
                audioSource.clip = clip;
                audioSource.Play();
                return;
            }

            audioSource.PlayOneShot(clip);
        }

        /// <summary>
        /// Plays a specific sound event. This named wrapper is convenient for UnityEvent wiring.
        /// </summary>
        public void PlaySound(JamSoundEvent soundEventToPlay)
        {
            Play(soundEventToPlay);
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

        private void ConfigureAudioSource(JamSoundEvent soundEventToPlay, AudioClip clip)
        {
            ApplyGeneratedAudioSourceVisibility();

            audioSource.clip = soundEventToPlay.loop ? clip : null;
            audioSource.loop = soundEventToPlay.loop;
            audioSource.volume = soundEventToPlay.GetVolume();
            audioSource.pitch = soundEventToPlay.GetPitch();
            audioSource.spatialBlend = GetPositionMode(soundEventToPlay) == JamAudioPositionMode.Position3D ? 1f : 0f;
            audioSource.outputAudioMixerGroup = soundEventToPlay.outputMixerGroup;
            JamAudioFilterUtility.Apply(
                audioSource,
                soundEventToPlay.GetLowPassFilterAmount(),
                soundEventToPlay.GetHighPassFilterAmount());
            ApplyGeneratedAudioSourceVisibility();
        }

        private JamAudioPositionMode GetPositionMode(JamSoundEvent soundEventToPlay)
        {
            return overrideSoundEventPositioning ? positionMode : soundEventToPlay.positionMode;
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

            if (audioSource.TryGetComponent(out AudioLowPassFilter lowPassFilter))
            {
                lowPassFilter.hideFlags = audioSource.hideFlags;
            }

            if (audioSource.TryGetComponent(out AudioHighPassFilter highPassFilter))
            {
                highPassFilter.hideFlags = audioSource.hideFlags;
            }
        }
    }
}

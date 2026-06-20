using UnityEditor;
using UnityEngine;

namespace JamAudioToolkit.Editor
{
    [CustomEditor(typeof(JamAudioPlayer))]
    [CanEditMultipleObjects]
    internal sealed class JamAudioPlayerEditor : UnityEditor.Editor
    {
        private SerializedProperty soundEventProperty;
        private SerializedProperty playOnAwakeProperty;
        private SerializedProperty playOnStartProperty;
        private SerializedProperty playOnEnableProperty;
        private SerializedProperty playOnTriggerEnterProperty;
        private SerializedProperty playOnTriggerExitProperty;
        private SerializedProperty playOnCollisionEnterProperty;
        private SerializedProperty overrideSoundEventPositioningProperty;
        private SerializedProperty positionModeProperty;
        private SerializedProperty audioSourceDebugViewProperty;

        private const int PresetCustom = 0;
        private const int PresetOnStart = 1;
        private const int PresetOnEnable = 2;
        private const int PresetTriggerEnter = 3;
        private const int PresetCollisionEnter = 4;
        private const int PresetAwake = 5;
        private const int PresetCodeOrUnityEvent = 6;

        private readonly GUIContent codeGuidance = new GUIContent(
            "Jam Audio Player is optional. Use it when this GameObject should play from Unity callbacks. For gameplay code, you can skip this component and call JamAudio.Play(soundEvent) or JamAudio.Play(soundEvent, gameObject).");

        private readonly string[] playbackPresetLabels =
        {
            "Custom",
            "Play On Start",
            "Play On Enable",
            "Trigger Enter",
            "Collision Enter",
            "Awake",
            "Code or UnityEvent"
        };

        private bool showAdvanced;

        private void OnEnable()
        {
            soundEventProperty = serializedObject.FindProperty("soundEvent");
            playOnAwakeProperty = serializedObject.FindProperty("playOnAwake");
            playOnStartProperty = serializedObject.FindProperty("playOnStart");
            playOnEnableProperty = serializedObject.FindProperty("playOnEnable");
            playOnTriggerEnterProperty = serializedObject.FindProperty("playOnTriggerEnter");
            playOnTriggerExitProperty = serializedObject.FindProperty("playOnTriggerExit");
            playOnCollisionEnterProperty = serializedObject.FindProperty("playOnCollisionEnter");
            overrideSoundEventPositioningProperty = serializedObject.FindProperty("overrideSoundEventPositioning");
            positionModeProperty = serializedObject.FindProperty("positionMode");
            audioSourceDebugViewProperty = serializedObject.FindProperty("audioSourceDebugView");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawProperties();
            serializedObject.ApplyModifiedProperties();

            DrawWarnings();
            JamAudioEditorUtility.DrawAudioListenerWarning();
            DrawRuntimeButtons();
        }

        private void DrawProperties()
        {
            DrawScriptField();

            EditorGUILayout.HelpBox(codeGuidance.text, MessageType.Info);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Sound", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(soundEventProperty);
            DrawSoundEventActions();

            EditorGUILayout.Space(4f);
            DrawPlaybackPresetField();

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Automatic Unity Callbacks", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(playOnAwakeProperty, new GUIContent("Awake", "Play during Awake, before most other scene objects have started."));
            EditorGUILayout.PropertyField(playOnStartProperty, new GUIContent("Start", "Play during Start, after scene objects have awakened."));
            EditorGUILayout.PropertyField(playOnEnableProperty, new GUIContent("Enable", "Play each time this GameObject or component is enabled."));
            EditorGUILayout.PropertyField(playOnTriggerEnterProperty, new GUIContent("Trigger Enter", "Play when a 2D or 3D trigger enters this object."));
            EditorGUILayout.PropertyField(playOnTriggerExitProperty, new GUIContent("Trigger Exit", "Play when a 2D or 3D trigger exits this object."));
            EditorGUILayout.PropertyField(playOnCollisionEnterProperty, new GUIContent("Collision Enter", "Play when a 2D or 3D collision starts on this object."));

            EditorGUILayout.Space(4f);
            showAdvanced = EditorGUILayout.BeginFoldoutHeaderGroup(showAdvanced, "Advanced");
            if (showAdvanced)
            {
                DrawAdvancedProperties();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawSoundEventActions()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Create New Sound Event"))
                {
                    JamSoundEvent soundEvent = JamAudioEditorUtility.CreateSoundEventAsset();
                    AssignSoundEventToTargets(soundEvent);
                }

                using (new EditorGUI.DisabledScope(soundEventProperty.objectReferenceValue == null || targets.Length != 1))
                {
                    if (GUILayout.Button("Open Event"))
                    {
                        Selection.activeObject = soundEventProperty.objectReferenceValue;
                    }
                }
            }
        }

        private void DrawPlaybackPresetField()
        {
            int currentPreset = GetPlaybackPreset();
            int selectedPreset = EditorGUILayout.Popup(
                new GUIContent("Preset", "Choose a common callback setup. Use Custom when you want to tick the callback boxes yourself."),
                currentPreset,
                playbackPresetLabels);

            if (selectedPreset != currentPreset)
            {
                ApplyPlaybackPreset(selectedPreset);
            }
        }

        private int GetPlaybackPreset()
        {
            if (HasMixedCallbackValues())
            {
                return PresetCustom;
            }

            if (MatchesPlaybackPreset(false, true, false, false, false, false))
            {
                return PresetOnStart;
            }

            if (MatchesPlaybackPreset(false, false, true, false, false, false))
            {
                return PresetOnEnable;
            }

            if (MatchesPlaybackPreset(false, false, false, true, false, false))
            {
                return PresetTriggerEnter;
            }

            if (MatchesPlaybackPreset(false, false, false, false, false, true))
            {
                return PresetCollisionEnter;
            }

            if (MatchesPlaybackPreset(true, false, false, false, false, false))
            {
                return PresetAwake;
            }

            if (MatchesPlaybackPreset(false, false, false, false, false, false))
            {
                return PresetCodeOrUnityEvent;
            }

            return PresetCustom;
        }

        private bool HasMixedCallbackValues()
        {
            return playOnAwakeProperty.hasMultipleDifferentValues
                || playOnStartProperty.hasMultipleDifferentValues
                || playOnEnableProperty.hasMultipleDifferentValues
                || playOnTriggerEnterProperty.hasMultipleDifferentValues
                || playOnTriggerExitProperty.hasMultipleDifferentValues
                || playOnCollisionEnterProperty.hasMultipleDifferentValues;
        }

        private bool MatchesPlaybackPreset(
            bool playOnAwake,
            bool playOnStart,
            bool playOnEnable,
            bool playOnTriggerEnter,
            bool playOnTriggerExit,
            bool playOnCollisionEnter)
        {
            return playOnAwakeProperty.boolValue == playOnAwake
                && playOnStartProperty.boolValue == playOnStart
                && playOnEnableProperty.boolValue == playOnEnable
                && playOnTriggerEnterProperty.boolValue == playOnTriggerEnter
                && playOnTriggerExitProperty.boolValue == playOnTriggerExit
                && playOnCollisionEnterProperty.boolValue == playOnCollisionEnter;
        }

        private void ApplyPlaybackPreset(int preset)
        {
            switch (preset)
            {
                case PresetOnStart:
                    SetPlaybackPreset(false, true, false, false, false, false);
                    break;
                case PresetOnEnable:
                    SetPlaybackPreset(false, false, true, false, false, false);
                    break;
                case PresetTriggerEnter:
                    SetPlaybackPreset(false, false, false, true, false, false);
                    break;
                case PresetCollisionEnter:
                    SetPlaybackPreset(false, false, false, false, false, true);
                    break;
                case PresetAwake:
                    SetPlaybackPreset(true, false, false, false, false, false);
                    break;
                case PresetCodeOrUnityEvent:
                    SetPlaybackPreset(false, false, false, false, false, false);
                    break;
            }
        }

        private void DrawAdvancedProperties()
        {
            EditorGUILayout.LabelField("Positioning", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(overrideSoundEventPositioningProperty, new GUIContent("Override Sound Event", "Use a positioning mode on this component instead of the assigned Sound Event's default."));

            bool showPositionMode = overrideSoundEventPositioningProperty.boolValue
                || overrideSoundEventPositioningProperty.hasMultipleDifferentValues;

            using (new EditorGUI.DisabledScope(!showPositionMode))
            {
                EditorGUILayout.PropertyField(positionModeProperty);
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(audioSourceDebugViewProperty);
        }

        private void DrawScriptField()
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField(
                    "Script",
                    MonoScript.FromMonoBehaviour((JamAudioPlayer)target),
                    typeof(MonoScript),
                    false);
            }
        }

        private void DrawWarnings()
        {
            if (soundEventProperty.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Assign a Sound Event before playback.", MessageType.Warning);
            }

            if (!AnyAutomaticPlaybackSelected())
            {
                EditorGUILayout.HelpBox("No automatic Unity callback is selected. A UnityEvent can call Play() on this component, but normal gameplay code can call JamAudio.Play(soundEvent) without using Jam Audio Player.", MessageType.Info);
            }
        }

        private void DrawRuntimeButtons()
        {
            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Play"))
                {
                    foreach (UnityEngine.Object selectedTarget in targets)
                    {
                        ((JamAudioPlayer)selectedTarget).Play();
                    }
                }

                if (GUILayout.Button("Stop"))
                {
                    foreach (UnityEngine.Object selectedTarget in targets)
                    {
                        ((JamAudioPlayer)selectedTarget).Stop();
                    }
                }
            }

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Inspector test buttons are available in Play Mode.", MessageType.Info);
            }
        }

        private void SetPlaybackPreset(
            bool playOnAwake,
            bool playOnStart,
            bool playOnEnable,
            bool playOnTriggerEnter,
            bool playOnTriggerExit,
            bool playOnCollisionEnter)
        {
            playOnAwakeProperty.boolValue = playOnAwake;
            playOnStartProperty.boolValue = playOnStart;
            playOnEnableProperty.boolValue = playOnEnable;
            playOnTriggerEnterProperty.boolValue = playOnTriggerEnter;
            playOnTriggerExitProperty.boolValue = playOnTriggerExit;
            playOnCollisionEnterProperty.boolValue = playOnCollisionEnter;
        }

        private void AssignSoundEventToTargets(JamSoundEvent soundEvent)
        {
            if (soundEvent == null)
            {
                return;
            }

            serializedObject.ApplyModifiedProperties();
            Undo.RecordObjects(targets, "Assign Jam Sound Event");

            foreach (UnityEngine.Object selectedTarget in targets)
            {
                JamAudioPlayer player = (JamAudioPlayer)selectedTarget;
                player.soundEvent = soundEvent;
                EditorUtility.SetDirty(player);
            }

            serializedObject.Update();
        }

        private bool AnyAutomaticPlaybackSelected()
        {
            return playOnAwakeProperty.boolValue
                || playOnStartProperty.boolValue
                || playOnEnableProperty.boolValue
                || playOnTriggerEnterProperty.boolValue
                || playOnTriggerExitProperty.boolValue
                || playOnCollisionEnterProperty.boolValue
                || playOnAwakeProperty.hasMultipleDifferentValues
                || playOnStartProperty.hasMultipleDifferentValues
                || playOnEnableProperty.hasMultipleDifferentValues
                || playOnTriggerEnterProperty.hasMultipleDifferentValues
                || playOnTriggerExitProperty.hasMultipleDifferentValues
                || playOnCollisionEnterProperty.hasMultipleDifferentValues;
        }
    }
}

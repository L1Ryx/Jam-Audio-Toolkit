using UnityEditor;
using UnityEngine;

namespace JamAudioToolkit.Editor
{
    [CustomEditor(typeof(JamMusicPlayer))]
    [CanEditMultipleObjects]
    internal sealed class JamMusicPlayerEditor : UnityEditor.Editor
    {
        private SerializedProperty musicEventProperty;
        private SerializedProperty playOnStartProperty;

        private const int PresetOnStart = 0;
        private const int PresetCodeOrUnityEvent = 1;

        private readonly string[] playbackPresetLabels =
        {
            "Play On Start",
            "Code or UnityEvent"
        };

        private void OnEnable()
        {
            musicEventProperty = serializedObject.FindProperty("musicEvent");
            playOnStartProperty = serializedObject.FindProperty("playOnStart");
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

            EditorGUILayout.HelpBox(
                "Jam Music Player is optional. Use it when a scene object should request music automatically. For gameplay code, you can skip this component and call JamAudio.PlayMusic(musicEvent), JamAudio.StopMusic(), JamAudio.PauseMusic(), or JamAudio.ResumeMusic().",
                MessageType.Info);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Music", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(musicEventProperty);
            DrawMusicEventActions();

            EditorGUILayout.Space(4f);
            DrawPlaybackPresetField();

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Automatic Unity Callbacks", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(playOnStartProperty, new GUIContent("Start", "Play this music automatically when the scene starts."));
        }

        private void DrawScriptField()
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField(
                    "Script",
                    MonoScript.FromMonoBehaviour((JamMusicPlayer)target),
                    typeof(MonoScript),
                    false);
            }
        }

        private void DrawMusicEventActions()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Create New Music Event"))
                {
                    JamMusicEvent musicEvent = JamAudioEditorUtility.CreateMusicEventAsset();
                    AssignMusicEventToTargets(musicEvent);
                }

                using (new EditorGUI.DisabledScope(musicEventProperty.objectReferenceValue == null || targets.Length != 1))
                {
                    if (GUILayout.Button("Open Event"))
                    {
                        Selection.activeObject = musicEventProperty.objectReferenceValue;
                    }
                }
            }
        }

        private void DrawPlaybackPresetField()
        {
            int currentPreset = playOnStartProperty.boolValue ? PresetOnStart : PresetCodeOrUnityEvent;
            int selectedPreset = EditorGUILayout.Popup(
                new GUIContent("Preset", "Choose a common music playback setup."),
                currentPreset,
                playbackPresetLabels);

            if (selectedPreset != currentPreset)
            {
                playOnStartProperty.boolValue = selectedPreset == PresetOnStart;
            }
        }

        private void DrawWarnings()
        {
            if (musicEventProperty.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Assign a Music Event before playback.", MessageType.Warning);
            }

            if (!playOnStartProperty.boolValue && !playOnStartProperty.hasMultipleDifferentValues)
            {
                EditorGUILayout.HelpBox("No automatic Unity callback is selected. A UnityEvent can call Play() on this component, but normal gameplay code can call JamAudio.PlayMusic(musicEvent) without using Jam Music Player.", MessageType.Info);
            }
        }

        private void AssignMusicEventToTargets(JamMusicEvent musicEvent)
        {
            if (musicEvent == null)
            {
                return;
            }

            serializedObject.ApplyModifiedProperties();
            Undo.RecordObjects(targets, "Assign Jam Music Event");

            foreach (UnityEngine.Object selectedTarget in targets)
            {
                JamMusicPlayer player = (JamMusicPlayer)selectedTarget;
                player.musicEvent = musicEvent;
                EditorUtility.SetDirty(player);
            }

            serializedObject.Update();
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
                        ((JamMusicPlayer)selectedTarget).Play();
                    }
                }

                if (GUILayout.Button("Stop"))
                {
                    foreach (UnityEngine.Object selectedTarget in targets)
                    {
                        ((JamMusicPlayer)selectedTarget).Stop();
                    }
                }
            }

            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Pause"))
                {
                    foreach (UnityEngine.Object selectedTarget in targets)
                    {
                        ((JamMusicPlayer)selectedTarget).Pause();
                    }
                }

                if (GUILayout.Button("Resume"))
                {
                    foreach (UnityEngine.Object selectedTarget in targets)
                    {
                        ((JamMusicPlayer)selectedTarget).Resume();
                    }
                }
            }

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Inspector test buttons are available in Play Mode.", MessageType.Info);
            }
        }
    }
}

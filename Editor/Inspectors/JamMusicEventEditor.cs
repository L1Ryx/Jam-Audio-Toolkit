using UnityEditor;
using UnityEngine;

namespace JamAudioToolkit.Editor
{
    [CustomEditor(typeof(JamMusicEvent))]
    [CanEditMultipleObjects]
    internal sealed class JamMusicEventEditor : UnityEditor.Editor
    {
        private SerializedProperty musicClipProperty;
        private SerializedProperty volumeProperty;
        private SerializedProperty loopProperty;
        private SerializedProperty persistAcrossScenesProperty;
        private SerializedProperty fadeInDurationProperty;
        private SerializedProperty fadeOutDurationProperty;
        private SerializedProperty outputMixerGroupProperty;

        private void OnEnable()
        {
            musicClipProperty = serializedObject.FindProperty("musicClip");
            volumeProperty = serializedObject.FindProperty("volume");
            loopProperty = serializedObject.FindProperty("loop");
            persistAcrossScenesProperty = serializedObject.FindProperty("persistAcrossScenes");
            fadeInDurationProperty = serializedObject.FindProperty("fadeInDuration");
            fadeOutDurationProperty = serializedObject.FindProperty("fadeOutDuration");
            outputMixerGroupProperty = serializedObject.FindProperty("outputMixerGroup");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawProperties();
            serializedObject.ApplyModifiedProperties();

            DrawWarnings();
            DrawPreviewButtons();
        }

        private void DrawProperties()
        {
            DrawScriptField();

            EditorGUILayout.PropertyField(musicClipProperty);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Playback", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(volumeProperty, new GUIContent("Volume (0-1)", "Playback volume using Unity's linear 0-1 AudioSource volume scale. This is not dB."));
            EditorGUILayout.PropertyField(loopProperty);
            EditorGUILayout.PropertyField(persistAcrossScenesProperty);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Transition", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(fadeInDurationProperty, new GUIContent("Fade In (s)", "Seconds to fade this music in."));
            EditorGUILayout.PropertyField(fadeOutDurationProperty, new GUIContent("Fade Out (s)", "Seconds to fade the previous music out."));

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Routing", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(outputMixerGroupProperty);
        }

        private void DrawScriptField()
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField(
                    "Script",
                    MonoScript.FromScriptableObject((ScriptableObject)target),
                    typeof(MonoScript),
                    false);
            }
        }

        private void DrawWarnings()
        {
            foreach (UnityEngine.Object selectedTarget in targets)
            {
                JamMusicEvent musicEvent = (JamMusicEvent)selectedTarget;

                if (musicEvent.musicClip == null)
                {
                    EditorGUILayout.HelpBox($"{musicEvent.name} has no music clip assigned.", MessageType.Warning);
                }

                if (musicEvent.fadeInDuration < 0f || musicEvent.fadeOutDuration < 0f)
                {
                    EditorGUILayout.HelpBox("Fade durations below zero will be clamped at runtime.", MessageType.Info);
                }
            }
        }

        private void DrawPreviewButtons()
        {
            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Preview"))
                {
                    JamAudioPreviewUtility.PreviewMusicEvent((JamMusicEvent)target);
                }

                if (GUILayout.Button("Stop Preview"))
                {
                    JamAudioPreviewUtility.StopPreview();
                }
            }
        }
    }
}

using UnityEditor;
using UnityEngine;

namespace JamAudioToolkit.Editor
{
    [CustomEditor(typeof(JamAudioPlayer))]
    [CanEditMultipleObjects]
    internal sealed class JamAudioPlayerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            DrawWarnings();
            DrawRuntimeButtons();
        }

        private void DrawWarnings()
        {
            SerializedProperty soundEventProperty = serializedObject.FindProperty("soundEvent");
            if (soundEventProperty.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Assign a Sound Event before playback.", MessageType.Warning);
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
                EditorGUILayout.HelpBox("Manual playback buttons are available in Play Mode.", MessageType.Info);
            }
        }
    }
}

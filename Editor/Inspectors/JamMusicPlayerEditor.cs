using UnityEditor;
using UnityEngine;

namespace JamAudioToolkit.Editor
{
    [CustomEditor(typeof(JamMusicPlayer))]
    [CanEditMultipleObjects]
    internal sealed class JamMusicPlayerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            DrawWarnings();
            DrawRuntimeButtons();
        }

        private void DrawWarnings()
        {
            SerializedProperty musicEventProperty = serializedObject.FindProperty("musicEvent");
            if (musicEventProperty.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Assign a Music Event before playback.", MessageType.Warning);
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

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Manual playback buttons are available in Play Mode.", MessageType.Info);
            }
        }
    }
}

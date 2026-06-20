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
        private SerializedProperty lowPassFilterAmountProperty;
        private SerializedProperty highPassFilterAmountProperty;
        private SerializedProperty fadeInDurationProperty;
        private SerializedProperty fadeOutDurationProperty;
        private SerializedProperty outputMixerGroupProperty;

        private bool showFilters = true;
        private bool showTransitions = true;
        private bool showAdvanced;

        private void OnEnable()
        {
            musicClipProperty = serializedObject.FindProperty("musicClip");
            volumeProperty = serializedObject.FindProperty("volume");
            loopProperty = serializedObject.FindProperty("loop");
            persistAcrossScenesProperty = serializedObject.FindProperty("persistAcrossScenes");
            lowPassFilterAmountProperty = serializedObject.FindProperty("lowPassFilterAmount");
            highPassFilterAmountProperty = serializedObject.FindProperty("highPassFilterAmount");
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
        }

        private void DrawProperties()
        {
            DrawScriptField();

            EditorGUILayout.PropertyField(musicClipProperty);
            DrawClipActions();

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Playback", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(volumeProperty, new GUIContent("Volume (%)", "Playback volume shown as 0-100%. This is converted to Unity's linear 0-1 AudioSource volume scale, not dB."));
            EditorGUILayout.PropertyField(loopProperty);
            EditorGUILayout.PropertyField(persistAcrossScenesProperty);

            EditorGUILayout.Space(4f);
            showFilters = EditorGUILayout.BeginFoldoutHeaderGroup(showFilters, "Filters");
            if (showFilters)
            {
                EditorGUILayout.PropertyField(lowPassFilterAmountProperty, new GUIContent("Low-Pass Filter (%)", "0% is clear and unfiltered. Higher values remove more high frequencies, making the music darker or more muffled."));
                EditorGUILayout.PropertyField(highPassFilterAmountProperty, new GUIContent("High-Pass Filter (%)", "0% is full and unfiltered. Higher values remove more low frequencies, making the music thinner or more radio-like."));
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.Space(4f);
            showTransitions = EditorGUILayout.BeginFoldoutHeaderGroup(showTransitions, "Transitions");
            if (showTransitions)
            {
                EditorGUILayout.PropertyField(fadeInDurationProperty, new GUIContent("Fade In (Seconds)", "Seconds to fade this music in."));
                EditorGUILayout.PropertyField(fadeOutDurationProperty, new GUIContent("Fade Out (Seconds)", "Seconds to fade the previous music out."));
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.Space(4f);
            showAdvanced = EditorGUILayout.BeginFoldoutHeaderGroup(showAdvanced, "Advanced");
            if (showAdvanced)
            {
                EditorGUILayout.PropertyField(outputMixerGroupProperty);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawClipActions()
        {
            AudioClip[] selectedClips = JamAudioEditorUtility.GetSelectedAudioClips();

            using (new EditorGUI.DisabledScope(selectedClips.Length != 1))
            {
                if (GUILayout.Button("Use Selected Clip"))
                {
                    musicClipProperty.objectReferenceValue = selectedClips[0];
                }
            }
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

                if (musicEvent.lowPassFilterAmount >= 0.75f && musicEvent.highPassFilterAmount >= 0.75f)
                {
                    EditorGUILayout.HelpBox("Heavy low-pass and high-pass filtering together can make this music very quiet or narrow.", MessageType.Info);
                }
            }
        }

    }
}

using UnityEditor;
using UnityEngine;

namespace JamAudioToolkit.Editor
{
    [CustomEditor(typeof(JamSoundEvent))]
    [CanEditMultipleObjects]
    internal sealed class JamSoundEventEditor : UnityEditor.Editor
    {
        private SerializedProperty clipsProperty;
        private SerializedProperty volumeProperty;
        private SerializedProperty pitchProperty;
        private SerializedProperty loopProperty;
        private SerializedProperty volumeRandomRangeProperty;
        private SerializedProperty pitchRandomRangeProperty;
        private SerializedProperty randomizeClipProperty;
        private SerializedProperty avoidRepeatingLastClipsProperty;
        private SerializedProperty outputMixerGroupProperty;

        private void OnEnable()
        {
            clipsProperty = serializedObject.FindProperty("clips");
            volumeProperty = serializedObject.FindProperty("volume");
            pitchProperty = serializedObject.FindProperty("pitch");
            loopProperty = serializedObject.FindProperty("loop");
            volumeRandomRangeProperty = serializedObject.FindProperty("volumeRandomRange");
            pitchRandomRangeProperty = serializedObject.FindProperty("pitchRandomRange");
            randomizeClipProperty = serializedObject.FindProperty("randomizeClip");
            avoidRepeatingLastClipsProperty = serializedObject.FindProperty("avoidRepeatingLastClips");
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

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Clips", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(clipsProperty, true);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Playback", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(volumeProperty, new GUIContent("Volume (0-1)", "Base playback volume using Unity's linear 0-1 AudioSource volume scale. This is not dB."));
            EditorGUILayout.PropertyField(pitchProperty, new GUIContent("Pitch (x)", "Base playback pitch using Unity's AudioSource pitch multiplier. 1 is normal pitch/speed."));
            EditorGUILayout.PropertyField(loopProperty);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Randomization", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(volumeRandomRangeProperty, new GUIContent("Volume Variation (+/- 0-1)", "Random linear volume offset added to Volume. Example: -0.1 to 0.1 varies Unity volume by +/- 0.1."));
            EditorGUILayout.PropertyField(pitchRandomRangeProperty, new GUIContent("Pitch Variation (+/- x)", "Random Unity pitch multiplier offset added to Pitch. Example: -0.05 to 0.05 varies pitch around 1 by +/- 0.05."));
            EditorGUILayout.PropertyField(randomizeClipProperty);
            EditorGUILayout.PropertyField(avoidRepeatingLastClipsProperty, new GUIContent("Avoid Last N Clips", "Avoid choosing any of the last N played clips when possible. Set to 0 to allow immediate repeats."));

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
                JamSoundEvent soundEvent = (JamSoundEvent)selectedTarget;

                if (soundEvent.clips == null || soundEvent.clips.Length == 0)
                {
                    EditorGUILayout.HelpBox($"{soundEvent.name} has no clips assigned.", MessageType.Warning);
                }
                else if (HasEmptyClipSlot(soundEvent.clips))
                {
                    EditorGUILayout.HelpBox($"{soundEvent.name} has one or more empty clip slots.", MessageType.Warning);
                }

                Vector2 volumeRange = GetOrderedRange(soundEvent.volumeRandomRange);
                if (soundEvent.volume + volumeRange.x < 0f || soundEvent.volume + volumeRange.y > 1f)
                {
                    EditorGUILayout.HelpBox("Volume randomization can exceed Unity's linear 0-1 range. Final volume will be clamped.", MessageType.Info);
                }

                Vector2 pitchRange = GetOrderedRange(soundEvent.pitchRandomRange);
                if (soundEvent.pitch + pitchRange.x < -3f || soundEvent.pitch + pitchRange.y > 3f)
                {
                    EditorGUILayout.HelpBox("Pitch randomization can exceed Unity's supported -3 to 3 multiplier range. Final pitch will be clamped.", MessageType.Info);
                }

                int playableClipCount = GetPlayableClipCount(soundEvent.clips);
                if (soundEvent.randomizeClip && playableClipCount > 1 && soundEvent.avoidRepeatingLastClips >= playableClipCount)
                {
                    EditorGUILayout.HelpBox("Avoid Last N Clips is higher than the playable clip count. Runtime playback will use as much history as possible.", MessageType.Info);
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
                    JamAudioPreviewUtility.PreviewSoundEvent((JamSoundEvent)target);
                }

                if (GUILayout.Button("Stop Preview"))
                {
                    JamAudioPreviewUtility.StopPreview();
                }
            }
        }

        private static bool HasEmptyClipSlot(AudioClip[] clips)
        {
            foreach (AudioClip clip in clips)
            {
                if (clip == null)
                {
                    return true;
                }
            }

            return false;
        }

        private static int GetPlayableClipCount(AudioClip[] clips)
        {
            int count = 0;

            if (clips == null)
            {
                return count;
            }

            foreach (AudioClip clip in clips)
            {
                if (clip != null)
                {
                    count++;
                }
            }

            return count;
        }

        private static Vector2 GetOrderedRange(Vector2 range)
        {
            return new Vector2(Mathf.Min(range.x, range.y), Mathf.Max(range.x, range.y));
        }
    }
}

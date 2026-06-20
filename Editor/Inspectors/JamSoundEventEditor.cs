using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
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
        private SerializedProperty lowPassFilterAmountProperty;
        private SerializedProperty highPassFilterAmountProperty;
        private SerializedProperty positionModeProperty;
        private SerializedProperty volumeRandomRangeProperty;
        private SerializedProperty pitchRandomRangeProperty;
        private SerializedProperty randomizeClipProperty;
        private SerializedProperty avoidRepeatingLastClipsProperty;
        private SerializedProperty outputMixerGroupProperty;

        private ReorderableList clipsList;

        private bool showRandomization = true;
        private bool showFilters = true;
        private bool showPositioning = true;
        private bool showAdvanced;

        private void OnEnable()
        {
            clipsProperty = serializedObject.FindProperty("clips");
            volumeProperty = serializedObject.FindProperty("volume");
            pitchProperty = serializedObject.FindProperty("pitch");
            loopProperty = serializedObject.FindProperty("loop");
            lowPassFilterAmountProperty = serializedObject.FindProperty("lowPassFilterAmount");
            highPassFilterAmountProperty = serializedObject.FindProperty("highPassFilterAmount");
            positionModeProperty = serializedObject.FindProperty("positionMode");
            volumeRandomRangeProperty = serializedObject.FindProperty("volumeRandomRange");
            pitchRandomRangeProperty = serializedObject.FindProperty("pitchRandomRange");
            randomizeClipProperty = serializedObject.FindProperty("randomizeClip");
            avoidRepeatingLastClipsProperty = serializedObject.FindProperty("avoidRepeatingLastClips");
            outputMixerGroupProperty = serializedObject.FindProperty("outputMixerGroup");

            clipsList = new ReorderableList(serializedObject, clipsProperty, true, true, true, true);
            clipsList.drawHeaderCallback = DrawClipsHeader;
            clipsList.drawElementCallback = DrawClipElement;
            clipsList.onAddCallback = AddClipSlot;
            clipsList.elementHeight = EditorGUIUtility.singleLineHeight + 4f;
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

            EditorGUILayout.Space(4f);
            DrawClipList();

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Playback", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(volumeProperty, new GUIContent("Volume (%)", "Base playback volume shown as 0-100%. This is converted to Unity's linear 0-1 AudioSource volume scale, not dB."));
            EditorGUILayout.PropertyField(pitchProperty, new GUIContent("Pitch (%)", "Base playback pitch shown as a percentage. 100% is normal pitch/speed."));
            EditorGUILayout.PropertyField(loopProperty);

            EditorGUILayout.Space(4f);
            showFilters = EditorGUILayout.BeginFoldoutHeaderGroup(showFilters, "Filters");
            if (showFilters)
            {
                EditorGUILayout.PropertyField(lowPassFilterAmountProperty, new GUIContent("Low-Pass Filter (%)", "0% is clear and unfiltered. Higher values remove more high frequencies, making the sound darker or more muffled."));
                EditorGUILayout.PropertyField(highPassFilterAmountProperty, new GUIContent("High-Pass Filter (%)", "0% is full and unfiltered. Higher values remove more low frequencies, making the sound thinner or more radio-like."));
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.Space(4f);
            showPositioning = EditorGUILayout.BeginFoldoutHeaderGroup(showPositioning, "Positioning");
            if (showPositioning)
            {
                EditorGUILayout.PropertyField(positionModeProperty, new GUIContent("Default Mode", "Default positioning for this sound when it is played."));
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.Space(4f);
            showRandomization = EditorGUILayout.BeginFoldoutHeaderGroup(showRandomization, "Randomization");
            if (showRandomization)
            {
                EditorGUILayout.PropertyField(volumeRandomRangeProperty, new GUIContent("Volume Variation (%)", "Random percentage variation around Volume. Min lowers volume, Max raises volume."));
                EditorGUILayout.PropertyField(pitchRandomRangeProperty, new GUIContent("Pitch Variation (%)", "Random percentage variation around Pitch. Min lowers pitch, Max raises pitch."));
                EditorGUILayout.PropertyField(randomizeClipProperty);
                EditorGUILayout.PropertyField(avoidRepeatingLastClipsProperty, new GUIContent("Recent Clips To Avoid", "How many recently played clips should be skipped when possible. Set to 0 to allow immediate repeats."));
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

        private void DrawClipList()
        {
            if (clipsProperty.hasMultipleDifferentValues)
            {
                EditorGUILayout.PropertyField(clipsProperty, true);
            }
            else
            {
                clipsList.DoLayoutList();
            }

            DrawClipDropZone();
            DrawClipListActions();
        }

        private void DrawClipsHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "Clip(s)");
        }

        private void DrawClipElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            SerializedProperty clipProperty = clipsProperty.GetArrayElementAtIndex(index);
            AudioClip clip = clipProperty.objectReferenceValue as AudioClip;

            rect.y += 2f;
            rect.height = EditorGUIUtility.singleLineHeight;

            const float lengthWidth = 42f;
            const float gap = 4f;

            Rect fieldRect = new Rect(
                rect.x,
                rect.y,
                Mathf.Max(60f, rect.width - lengthWidth - gap),
                rect.height);

            Rect lengthRect = new Rect(
                fieldRect.xMax + gap,
                rect.y,
                lengthWidth,
                rect.height);

            EditorGUI.PropertyField(fieldRect, clipProperty, GUIContent.none);
            EditorGUI.LabelField(lengthRect, JamAudioEditorUtility.FormatClipLength(clip), EditorStyles.miniLabel);
        }

        private void AddClipSlot(ReorderableList list)
        {
            int index = clipsProperty.arraySize;
            clipsProperty.InsertArrayElementAtIndex(index);
            clipsProperty.GetArrayElementAtIndex(index).objectReferenceValue = null;
        }

        private void DrawClipDropZone()
        {
            Rect dropRect = GUILayoutUtility.GetRect(0f, 30f, GUILayout.ExpandWidth(true));
            GUI.Box(dropRect, "Drop AudioClip(s) here", EditorStyles.helpBox);

            Event currentEvent = Event.current;
            if (!dropRect.Contains(currentEvent.mousePosition))
            {
                return;
            }

            AudioClip[] draggedClips = JamAudioEditorUtility.GetDraggedAudioClips();
            if (currentEvent.type != EventType.DragUpdated && currentEvent.type != EventType.DragPerform)
            {
                return;
            }

            DragAndDrop.visualMode = draggedClips.Length > 0
                ? DragAndDropVisualMode.Copy
                : DragAndDropVisualMode.Rejected;

            if (currentEvent.type == EventType.DragPerform && draggedClips.Length > 0)
            {
                DragAndDrop.AcceptDrag();
                AddClipsToTargets(draggedClips);
            }

            currentEvent.Use();
        }

        private void DrawClipListActions()
        {
            AudioClip[] selectedClips = JamAudioEditorUtility.GetSelectedAudioClips();

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(selectedClips.Length == 0))
                {
                    if (GUILayout.Button("Add Selected Clip(s)"))
                    {
                        AddClipsToTargets(selectedClips);
                    }
                }

                using (new EditorGUI.DisabledScope(!AnyTargetHasEmptyClipSlots()))
                {
                    if (GUILayout.Button("Clear Empty Slots"))
                    {
                        ClearEmptyClipSlots();
                    }
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
                JamSoundEvent soundEvent = (JamSoundEvent)selectedTarget;

                if (soundEvent.clips == null || soundEvent.clips.Length == 0)
                {
                    EditorGUILayout.HelpBox($"{soundEvent.name} has no clip assigned.", MessageType.Warning);
                }
                else if (HasEmptyClipSlot(soundEvent.clips))
                {
                    EditorGUILayout.HelpBox($"{soundEvent.name} has one or more empty clip slots.", MessageType.Warning);
                    if (GUILayout.Button("Clear Empty Slots"))
                    {
                        ClearEmptyClipSlots();
                    }
                }

                Vector2 volumeRange = GetOrderedRange(soundEvent.volumeRandomRange);
                if (soundEvent.volume + volumeRange.x < 0f || soundEvent.volume + volumeRange.y > 1f)
                {
                    EditorGUILayout.HelpBox("Volume variation can exceed the 0-100% volume range. Final volume will be clamped.", MessageType.Info);
                }

                Vector2 pitchRange = GetOrderedRange(soundEvent.pitchRandomRange);
                if (soundEvent.pitch + pitchRange.x < 0f || soundEvent.pitch + pitchRange.y > 3f)
                {
                    EditorGUILayout.HelpBox("Pitch variation can exceed Unity's supported 0-300% pitch range. Final pitch will be clamped.", MessageType.Info);
                }

                int playableClipCount = GetPlayableClipCount(soundEvent.clips);
                if (soundEvent.randomizeClip && playableClipCount > 1 && soundEvent.avoidRepeatingLastClips >= playableClipCount)
                {
                    EditorGUILayout.HelpBox("Recent Clips To Avoid is higher than the playable clip count. Runtime playback will use as much history as possible.", MessageType.Info);
                }

                if (soundEvent.lowPassFilterAmount >= 0.75f && soundEvent.highPassFilterAmount >= 0.75f)
                {
                    EditorGUILayout.HelpBox("Heavy low-pass and high-pass filtering together can make this sound very quiet or narrow.", MessageType.Info);
                }
            }
        }

        private void AddClipsToTargets(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0)
            {
                return;
            }

            serializedObject.ApplyModifiedProperties();
            Undo.RecordObjects(targets, "Add Audio Clip(s)");

            foreach (UnityEngine.Object selectedTarget in targets)
            {
                JamSoundEvent soundEvent = (JamSoundEvent)selectedTarget;
                List<AudioClip> existingClips = new List<AudioClip>(soundEvent.clips ?? new AudioClip[0]);
                existingClips.AddRange(clips);
                soundEvent.clips = existingClips.ToArray();
                EditorUtility.SetDirty(soundEvent);
            }

            serializedObject.Update();
        }

        private void ClearEmptyClipSlots()
        {
            serializedObject.ApplyModifiedProperties();
            Undo.RecordObjects(targets, "Clear Empty Clip Slots");

            foreach (UnityEngine.Object selectedTarget in targets)
            {
                JamSoundEvent soundEvent = (JamSoundEvent)selectedTarget;
                if (soundEvent.clips == null)
                {
                    continue;
                }

                List<AudioClip> playableClips = new List<AudioClip>();
                foreach (AudioClip clip in soundEvent.clips)
                {
                    if (clip != null)
                    {
                        playableClips.Add(clip);
                    }
                }

                soundEvent.clips = playableClips.ToArray();
                EditorUtility.SetDirty(soundEvent);
            }

            serializedObject.Update();
        }

        private bool AnyTargetHasEmptyClipSlots()
        {
            foreach (UnityEngine.Object selectedTarget in targets)
            {
                JamSoundEvent soundEvent = (JamSoundEvent)selectedTarget;
                if (HasEmptyClipSlot(soundEvent.clips))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasEmptyClipSlot(AudioClip[] clips)
        {
            if (clips == null)
            {
                return false;
            }

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

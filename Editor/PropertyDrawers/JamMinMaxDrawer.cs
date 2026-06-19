using JamAudioToolkit;
using UnityEditor;
using UnityEngine;

namespace JamAudioToolkit.Editor
{
    [CustomPropertyDrawer(typeof(JamMinMaxAttribute))]
    internal sealed class JamMinMaxDrawer : PropertyDrawer
    {
        private const float LabelWidth = 28f;
        private const float Gap = 4f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.Vector2)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            EditorGUI.BeginProperty(position, label, property);

            Rect contentPosition = EditorGUI.PrefixLabel(
                position,
                GUIUtility.GetControlID(FocusType.Passive),
                label);

            int previousIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            float fieldWidth = Mathf.Max(
                40f,
                (contentPosition.width - (LabelWidth * 2f) - (Gap * 3f)) * 0.5f);

            Rect minLabelPosition = new Rect(
                contentPosition.x,
                contentPosition.y,
                LabelWidth,
                contentPosition.height);

            Rect minFieldPosition = new Rect(
                minLabelPosition.xMax + Gap,
                contentPosition.y,
                fieldWidth,
                contentPosition.height);

            Rect maxLabelPosition = new Rect(
                minFieldPosition.xMax + Gap,
                contentPosition.y,
                LabelWidth,
                contentPosition.height);

            Rect maxFieldPosition = new Rect(
                maxLabelPosition.xMax + Gap,
                contentPosition.y,
                fieldWidth,
                contentPosition.height);

            Vector2 value = property.vector2Value;

            EditorGUI.LabelField(minLabelPosition, "Min");
            value.x = EditorGUI.FloatField(minFieldPosition, value.x);

            EditorGUI.LabelField(maxLabelPosition, "Max");
            value.y = EditorGUI.FloatField(maxFieldPosition, value.y);

            property.vector2Value = value;
            EditorGUI.indentLevel = previousIndent;

            EditorGUI.EndProperty();
        }
    }
}

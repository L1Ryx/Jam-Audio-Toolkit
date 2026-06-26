using JamAudioToolkit;
using UnityEditor;
using UnityEngine;

namespace JamAudioToolkit.Editor
{
    [CustomPropertyDrawer(typeof(JamMinMaxAttribute))]
    internal sealed class JamMinMaxDrawer : PropertyDrawer
    {
        private const float DefaultLabelWidth = 28f;
        private const float PercentVariationLabelWidth = 58f;
        private const float PercentScale = 100f;
        private const float Gap = 4f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.Vector2)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            JamMinMaxAttribute minMaxAttribute = (JamMinMaxAttribute)attribute;

            EditorGUI.BeginProperty(position, label, property);

            Rect contentPosition = EditorGUI.PrefixLabel(
                position,
                GUIUtility.GetControlID(FocusType.Passive),
                label);

            int previousIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            float labelWidth = minMaxAttribute.DisplayAsPercentVariation
                ? PercentVariationLabelWidth
                : DefaultLabelWidth;

            float fieldWidth = Mathf.Max(
                40f,
                (contentPosition.width - (labelWidth * 2f) - (Gap * 3f)) * 0.5f);

            Rect minLabelPosition = new Rect(
                contentPosition.x,
                contentPosition.y,
                labelWidth,
                contentPosition.height);

            Rect minFieldPosition = new Rect(
                minLabelPosition.xMax + Gap,
                contentPosition.y,
                fieldWidth,
                contentPosition.height);

            Rect maxLabelPosition = new Rect(
                minFieldPosition.xMax + Gap,
                contentPosition.y,
                labelWidth,
                contentPosition.height);

            Rect maxFieldPosition = new Rect(
                maxLabelPosition.xMax + Gap,
                contentPosition.y,
                fieldWidth,
                contentPosition.height);

            Vector2 value = property.vector2Value;

            bool previousMixedValue = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = property.hasMultipleDifferentValues;

            EditorGUI.BeginChangeCheck();

            if (minMaxAttribute.DisplayAsPercentVariation)
            {
                DrawPercentVariationFields(
                    minLabelPosition,
                    minFieldPosition,
                    maxLabelPosition,
                    maxFieldPosition,
                    ref value);
            }
            else
            {
                EditorGUI.LabelField(minLabelPosition, "Min");
                value.x = EditorGUI.FloatField(minFieldPosition, value.x);

                EditorGUI.LabelField(maxLabelPosition, "Max");
                value.y = EditorGUI.FloatField(maxFieldPosition, value.y);
            }

            EditorGUI.showMixedValue = previousMixedValue;

            if (EditorGUI.EndChangeCheck())
            {
                property.vector2Value = value;
            }

            EditorGUI.indentLevel = previousIndent;

            EditorGUI.EndProperty();
        }

        private static void DrawPercentVariationFields(
            Rect minLabelPosition,
            Rect minFieldPosition,
            Rect maxLabelPosition,
            Rect maxFieldPosition,
            ref Vector2 value)
        {
            Vector2 orderedValue = new Vector2(
                Mathf.Min(value.x, value.y),
                Mathf.Max(value.x, value.y));

            float lowerPercent = Mathf.Max(0f, -orderedValue.x * PercentScale);
            float upperPercent = Mathf.Max(0f, orderedValue.y * PercentScale);
            lowerPercent = RoundPercent(lowerPercent);
            upperPercent = RoundPercent(upperPercent);

            EditorGUI.LabelField(minLabelPosition, "Min (-%)");
            lowerPercent = Mathf.Max(0f, EditorGUI.FloatField(minFieldPosition, lowerPercent));

            EditorGUI.LabelField(maxLabelPosition, "Max (+%)");
            upperPercent = Mathf.Max(0f, EditorGUI.FloatField(maxFieldPosition, upperPercent));

            value = new Vector2(
                -RoundPercent(lowerPercent) / PercentScale,
                RoundPercent(upperPercent) / PercentScale);
        }

        private static float RoundPercent(float value)
        {
            return Mathf.Round(value * 1000f) / 1000f;
        }
    }

    [CustomPropertyDrawer(typeof(JamPercentAttribute))]
    internal sealed class JamPercentDrawer : PropertyDrawer
    {
        private const float PercentScale = 100f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.Float)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            JamPercentAttribute percentAttribute = (JamPercentAttribute)attribute;

            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.BeginChangeCheck();

            bool previousMixedValue = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = property.hasMultipleDifferentValues;

            float percentValue = EditorGUI.Slider(
                position,
                label,
                property.floatValue * PercentScale,
                percentAttribute.MinPercent,
                percentAttribute.MaxPercent);

            EditorGUI.showMixedValue = previousMixedValue;

            if (EditorGUI.EndChangeCheck())
            {
                property.floatValue = percentValue / PercentScale;
            }

            EditorGUI.EndProperty();
        }
    }
}

using UnityEngine;

namespace JamAudioToolkit
{
    /// <summary>
    /// Draws a Vector2 field as a Min/Max pair in the Unity Inspector.
    /// </summary>
    public sealed class JamMinMaxAttribute : PropertyAttribute
    {
        public JamMinMaxAttribute(bool displayAsPercentVariation = false)
        {
            DisplayAsPercentVariation = displayAsPercentVariation;
        }

        public bool DisplayAsPercentVariation { get; }
    }

    /// <summary>
    /// Draws a serialized multiplier value as a percentage in the Unity Inspector.
    /// </summary>
    public sealed class JamPercentAttribute : PropertyAttribute
    {
        public JamPercentAttribute(float minPercent = 0f, float maxPercent = 100f)
        {
            MinPercent = Mathf.Min(minPercent, maxPercent);
            MaxPercent = Mathf.Max(minPercent, maxPercent);
        }

        public float MinPercent { get; }
        public float MaxPercent { get; }
    }
}

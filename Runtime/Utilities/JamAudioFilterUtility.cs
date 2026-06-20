using UnityEngine;

namespace JamAudioToolkit
{
    internal static class JamAudioFilterUtility
    {
        private const float FilterOffThreshold = 0.0001f;
        private const float MaxLowPassCutoff = 22000f;
        private const float MinLowPassCutoff = 500f;
        private const float MinHighPassCutoff = 10f;
        private const float MaxHighPassCutoff = 8000f;
        private const float GentleResonance = 1f;

        public static void Apply(AudioSource source, float lowPassAmount, float highPassAmount)
        {
            if (source == null)
            {
                return;
            }

            ApplyLowPass(source, lowPassAmount);
            ApplyHighPass(source, highPassAmount);
        }

        public static void Clear(AudioSource source)
        {
            Apply(source, 0f, 0f);
        }

        public static bool IsHeavyCombination(float lowPassAmount, float highPassAmount)
        {
            return Mathf.Clamp01(lowPassAmount) >= 0.75f && Mathf.Clamp01(highPassAmount) >= 0.75f;
        }

        private static void ApplyLowPass(AudioSource source, float amount)
        {
            AudioLowPassFilter filter = source.GetComponent<AudioLowPassFilter>();
            amount = Mathf.Clamp01(amount);

            if (amount <= FilterOffThreshold)
            {
                if (filter != null)
                {
                    filter.cutoffFrequency = MaxLowPassCutoff;
                    filter.enabled = false;
                }

                return;
            }

            if (filter == null)
            {
                filter = source.gameObject.AddComponent<AudioLowPassFilter>();
            }

            filter.enabled = true;
            filter.cutoffFrequency = LogLerp(MaxLowPassCutoff, MinLowPassCutoff, amount);
            filter.lowpassResonanceQ = GentleResonance;
        }

        private static void ApplyHighPass(AudioSource source, float amount)
        {
            AudioHighPassFilter filter = source.GetComponent<AudioHighPassFilter>();
            amount = Mathf.Clamp01(amount);

            if (amount <= FilterOffThreshold)
            {
                if (filter != null)
                {
                    filter.cutoffFrequency = MinHighPassCutoff;
                    filter.enabled = false;
                }

                return;
            }

            if (filter == null)
            {
                filter = source.gameObject.AddComponent<AudioHighPassFilter>();
            }

            filter.enabled = true;
            filter.cutoffFrequency = LogLerp(MinHighPassCutoff, MaxHighPassCutoff, amount);
            filter.highpassResonanceQ = GentleResonance;
        }

        private static float LogLerp(float from, float to, float amount)
        {
            float t = Mathf.Clamp01(amount);
            return Mathf.Exp(Mathf.Lerp(Mathf.Log(from), Mathf.Log(to), t));
        }
    }
}

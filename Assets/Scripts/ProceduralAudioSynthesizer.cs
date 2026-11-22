using UnityEngine;

namespace TheHouseIsAlive
{
    /// <summary>
    /// Generates runtime procedural audio clips for physical wall knocks,
    /// skittering claws, and atmospheric horror drones without requiring
    /// external WAV/MP3 asset files.
    /// </summary>
    public static class ProceduralAudioSynthesizer
    {
        private const int SampleRate = 44100;

        /// <summary>
        /// Generates a sharp, muffled impact sound simulating a hard knock on wood/drywall.
        /// </summary>
        public static AudioClip CreateKnockClip(float duration = 0.22f, float baseFreq = 95f)
        {
            int totalSamples = Mathf.CeilToInt(SampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float envelope = Mathf.Exp(-t * 28f); // Fast decay

                // Low-frequency damped knock transient + tiny noise rattle
                float sine = Mathf.Sin(2f * Mathf.PI * baseFreq * (1f - t * 2f) * t);
                float noise = (Random.value * 2f - 1f) * 0.15f * Mathf.Exp(-t * 45f);

                samples[i] = Mathf.Clamp(sine + noise, -1f, 1f) * envelope;
            }

            AudioClip clip = AudioClip.Create("Procedural_Knock", totalSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// Generates rapid skittering/scratching sound simulating claws on hard floor.
        /// </summary>
        public static AudioClip CreateSkitterClip(float duration = 0.65f)
        {
            int totalSamples = Mathf.CeilToInt(SampleRate * duration);
            float[] samples = new float[totalSamples];

            int taps = Mathf.RoundToInt(duration * 18f);
            float tapInterval = duration / taps;

            for (int tap = 0; tap < taps; tap++)
            {
                int tapStart = Mathf.RoundToInt(tap * tapInterval * SampleRate);
                int tapLength = Mathf.RoundToInt(0.015f * SampleRate);

                for (int i = 0; i < tapLength && (tapStart + i) < totalSamples; i++)
                {
                    float t = (float)i / SampleRate;
                    float env = Mathf.Exp(-t * 90f);
                    float click = (Random.value * 2f - 1f) * env * 0.6f;
                    samples[tapStart + i] += click;
                }
            }

            AudioClip clip = AudioClip.Create("Procedural_Skitter", totalSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// Generates a sinister low-frequency horror drone.
        /// </summary>
        public static AudioClip CreateDroneClip(float duration = 4.0f, float droneFreq = 55f)
        {
            int totalSamples = Mathf.CeilToInt(SampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float lfo = 1f + 0.15f * Mathf.Sin(2f * Mathf.PI * 0.4f * t);
                float s1 = Mathf.Sin(2f * Mathf.PI * droneFreq * lfo * t);
                float s2 = Mathf.Sin(2f * Mathf.PI * (droneFreq * 1.51f) * t) * 0.4f;

                // Soft fade in / out
                float fadeIn = Mathf.Clamp01(t / 0.5f);
                float fadeOut = Mathf.Clamp01((duration - t) / 0.5f);

                samples[i] = (s1 + s2) * 0.35f * fadeIn * fadeOut;
            }

            AudioClip clip = AudioClip.Create("Procedural_Drone", totalSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// Generates slow, wet breathing: low-passed noise with inhale/exhale envelopes.
        /// </summary>
        public static AudioClip CreateBreathClip(float duration = 3.2f, float breathsPerSecond = 0.6f)
        {
            int totalSamples = Mathf.CeilToInt(SampleRate * duration);
            float[] samples = new float[totalSamples];
            float lowPassed = 0f;

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float cycle = Mathf.Sin(Mathf.PI * breathsPerSecond * t);
                float env = cycle * cycle; // two swells per period: inhale, exhale
                float noise = Random.value * 2f - 1f;
                lowPassed += 0.08f * (noise - lowPassed);
                float fade = Mathf.Clamp01(t / 0.3f) * Mathf.Clamp01((duration - t) / 0.3f);
                samples[i] = lowPassed * env * fade * 1.8f;
            }

            AudioClip clip = AudioClip.Create("Procedural_Breath", totalSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}

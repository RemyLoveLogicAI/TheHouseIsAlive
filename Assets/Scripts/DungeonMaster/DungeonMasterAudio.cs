using UnityEngine;

namespace TheHouseIsAlive.DungeonMaster
{
    public enum DungeonSoundEffect
    {
        DiceRattle,
        SwordClash,
        ArrowHit,
        FireballImpact,
        CriticalFanfare,
        MonsterRoar
    }

    /// <summary>
    /// Procedurally synthesizes and plays D&D combat audio cues.
    /// </summary>
    public class DungeonMasterAudio : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;
        private const int SampleRate = 44100;

        private void Awake()
        {
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 0.5f;
            }
        }

        public void PlaySound(DungeonSoundEffect sfx)
        {
            AudioClip clip = GenerateClip(sfx);
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        public static AudioClip GenerateClip(DungeonSoundEffect sfx)
        {
            switch (sfx)
            {
                case DungeonSoundEffect.DiceRattle:
                    return CreateDiceRattleClip();
                case DungeonSoundEffect.SwordClash:
                    return CreateSwordClashClip();
                case DungeonSoundEffect.FireballImpact:
                    return CreateFireballClip();
                case DungeonSoundEffect.CriticalFanfare:
                    return CreateCriticalFanfareClip();
                default:
                    return CreateDiceRattleClip();
            }
        }

        private static AudioClip CreateDiceRattleClip()
        {
            float duration = 0.4f;
            int totalSamples = Mathf.CeilToInt(SampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int click = 0; click < 5; click++)
            {
                int start = click * (totalSamples / 5);
                for (int i = 0; i < 400 && (start + i) < totalSamples; i++)
                {
                    float t = (float)i / SampleRate;
                    samples[start + i] += (Random.value * 2f - 1f) * Mathf.Exp(-t * 200f) * 0.5f;
                }
            }

            AudioClip clip = AudioClip.Create("DiceRattle", totalSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateSwordClashClip()
        {
            float duration = 0.5f;
            int totalSamples = Mathf.CeilToInt(SampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float ring1 = Mathf.Sin(2f * Mathf.PI * 2400f * t);
                float ring2 = Mathf.Sin(2f * Mathf.PI * 3600f * t);
                float noise = (Random.value * 2f - 1f) * Mathf.Exp(-t * 50f);
                float env = Mathf.Exp(-t * 8f);

                samples[i] = (noise * 0.4f + (ring1 + ring2) * 0.3f) * env;
            }

            AudioClip clip = AudioClip.Create("SwordClash", totalSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateFireballClip()
        {
            float duration = 0.8f;
            int totalSamples = Mathf.CeilToInt(SampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float rumble = Mathf.Sin(2f * Mathf.PI * (60f + 30f * (1f - t)) * t);
                float hiss = (Random.value * 2f - 1f) * Mathf.Exp(-t * 4f);
                float env = Mathf.Exp(-t * 3f);

                samples[i] = (rumble * 0.6f + hiss * 0.3f) * env;
            }

            AudioClip clip = AudioClip.Create("FireballImpact", totalSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateCriticalFanfareClip()
        {
            float duration = 0.6f;
            int totalSamples = Mathf.CeilToInt(SampleRate * duration);
            float[] samples = new float[totalSamples];

            float[] chord = { 523.25f, 659.25f, 783.99f, 1046.50f }; // C major arpeggio
            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float sum = 0f;
                foreach (float freq in chord)
                {
                    sum += Mathf.Sin(2f * Mathf.PI * freq * t);
                }
                samples[i] = (sum / chord.Length) * Mathf.Exp(-t * 4f) * 0.7f;
            }

            AudioClip clip = AudioClip.Create("CritFanfare", totalSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}

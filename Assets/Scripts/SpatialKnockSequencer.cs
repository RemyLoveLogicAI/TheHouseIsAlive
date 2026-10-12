using System.Collections;
using UnityEngine;

namespace TheHouseIsAlive
{
    /// <summary>
    /// Spatially sequences knocking, scratching, and muffled impact sounds
    /// originating from behind physical room boundaries (walls, doors, floors).
    /// </summary>
    public class SpatialKnockSequencer : MonoBehaviour
    {
        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip knockClip;
        [SerializeField] private AudioClip scratchClip;

        [Header("Tuning")]
        [Tooltip("Distance behind wall plane in meters to place the audio source.")]
        [SerializeField] private float wallRecessOffset = 0.15f;

        [SerializeField] private Vector2 pitchRange = new Vector2(0.85f, 1.15f);
        [SerializeField] private Vector2 knockIntervalRange = new Vector2(0.25f, 0.45f);

        private void Awake()
        {
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1.0f; // Full 3D spatialization
                audioSource.spatialize = true;
                audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
                audioSource.minDistance = 0.5f;
                audioSource.maxDistance = 10f;
            }
        }

        /// <summary>
        /// Plays a sequence of knocks behind a specific wall or door surface.
        /// </summary>
        public void TriggerKnockBurst(Vector3 surfaceCenter, Vector3 surfaceNormal, int count = 3)
        {
            // Position source behind the wall
            Vector3 soundPosition = surfaceCenter - (surfaceNormal.normalized * wallRecessOffset);
            transform.position = soundPosition;

            StopAllCoroutines();
            StartCoroutine(KnockRoutine(count));
        }

        private IEnumerator KnockRoutine(int count)
        {
            for (int i = 0; i < count; i++)
            {
                PlaySingleImpact(knockClip, 1.0f);
                float interval = Random.Range(knockIntervalRange.x, knockIntervalRange.y);
                yield return new WaitForSeconds(interval);
            }
        }

        public void TriggerScratch(Vector3 surfaceCenter, Vector3 surfaceNormal, float duration = 1.5f)
        {
            Vector3 soundPosition = surfaceCenter - (surfaceNormal.normalized * wallRecessOffset);
            transform.position = soundPosition;

            if (scratchClip != null)
            {
                PlaySingleImpact(scratchClip, 0.8f);
            }
        }

        private void PlaySingleImpact(AudioClip clip, float volumeScale)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
                audioSource.PlayOneShot(clip, volumeScale);
            }
            else
            {
                // Fallback synth beep if no audio asset assigned yet
                Debug.Log($"[SpatialKnock] Impact simulated at position {transform.position}");
            }
        }
    }
}

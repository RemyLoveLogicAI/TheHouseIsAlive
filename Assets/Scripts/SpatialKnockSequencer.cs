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
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            }

            audioSource.spatialBlend = 1f;
            audioSource.spatialize = true;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.minDistance = 0.5f;
            audioSource.maxDistance = 10f;

            if (knockClip == null) knockClip = ProceduralAudioSynthesizer.CreateKnockClip();
            if (scratchClip == null) scratchClip = ProceduralAudioSynthesizer.CreateSkitterClip();
        }

        /// <summary>
        /// Plays a sequence of knocks behind a specific wall or door surface.
        /// </summary>
        public void TriggerKnockBurst(Vector3 surfaceCenter, Vector3 surfaceNormal, int count = 3)
        {
            Vector3 normal = surfaceNormal.sqrMagnitude > 0.0001f ? surfaceNormal.normalized : Vector3.forward;
            transform.position = surfaceCenter - normal * wallRecessOffset;

            StopAllCoroutines();
            StartCoroutine(KnockRoutine(Mathf.Max(0, count)));
        }

        private IEnumerator KnockRoutine(int count)
        {
            for (int i = 0; i < count; i++)
            {
                PlaySingleImpact(knockClip, 1f);
                float interval = Random.Range(knockIntervalRange.x, knockIntervalRange.y);
                yield return new WaitForSeconds(interval);
            }
        }

        public void TriggerScratch(Vector3 surfaceCenter, Vector3 surfaceNormal, float duration = 1.5f)
        {
            Vector3 normal = surfaceNormal.sqrMagnitude > 0.0001f ? surfaceNormal.normalized : Vector3.forward;
            transform.position = surfaceCenter - normal * wallRecessOffset;
            PlaySingleImpact(scratchClip, 0.8f);
        }

        private void PlaySingleImpact(AudioClip clip, float volumeScale)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
                audioSource.PlayOneShot(clip, volumeScale);
                TriggerHaptics(0.45f * volumeScale, 0.08f);
            }
        }

        private void TriggerHaptics(float amplitude, float duration)
        {
            try
            {
                OVRInput.SetControllerVibration(amplitude, amplitude, OVRInput.Controller.All);
                StartCoroutine(StopHapticsRoutine(duration));
            }
            catch { /* non-VR / editor fallback */ }
        }

        private IEnumerator StopHapticsRoutine(float duration)
        {
            yield return new WaitForSeconds(duration);
            try
            {
                OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.All);
            }
            catch { }
        }
    }
}

using System.Collections;
using UnityEngine;

namespace TheHouseIsAlive
{
    /// <summary>
    /// Modulates virtual lighting and screen-space passthrough exposure
    /// to simulate physical room lighting fluctuations and blackouts.
    /// </summary>
    public class FlickerLightingEffect : MonoBehaviour
    {
        [Header("Light References")]
        [SerializeField] private Light virtualRoomLight;
        [SerializeField] private MeshRenderer overlayQuad;

        [Header("Flicker Parameters")]
        [SerializeField] private float baseIntensity = 1.0f;
        [SerializeField] private float minIntensity = 0.05f;
        [SerializeField] private int flickerFlashes = 6;
        [SerializeField] private float flashDuration = 0.04f;

        private Coroutine _flickerCoroutine;

        private void Awake()
        {
            if (virtualRoomLight == null)
            {
                virtualRoomLight = GetComponent<Light>();
            }
        }

        public void TriggerFlickerSequence(float totalDuration = 1.2f)
        {
            if (_flickerCoroutine != null) StopCoroutine(_flickerCoroutine);
            _flickerCoroutine = StartCoroutine(FlickerRoutine(totalDuration));
        }

        private IEnumerator FlickerRoutine(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                for (int i = 0; i < flickerFlashes; i++)
                {
                    SetLightState(minIntensity);
                    yield return new WaitForSeconds(Random.Range(flashDuration * 0.5f, flashDuration * 1.5f));
                    SetLightState(baseIntensity * Random.Range(0.6f, 1.0f));
                    yield return new WaitForSeconds(Random.Range(flashDuration * 0.5f, flashDuration * 2f));
                }
                elapsed += flashDuration * flickerFlashes * 2f;
            }

            SetLightState(baseIntensity);
            _flickerCoroutine = null;
        }

        private void SetLightState(float intensity)
        {
            if (virtualRoomLight != null)
            {
                virtualRoomLight.intensity = intensity;
            }

            if (overlayQuad != null)
            {
                // Control opacity of dark vignette overlay
                var col = overlayQuad.material.color;
                col.a = Mathf.Clamp01(1.0f - (intensity / baseIntensity));
                overlayQuad.material.color = col;
            }
        }
    }
}

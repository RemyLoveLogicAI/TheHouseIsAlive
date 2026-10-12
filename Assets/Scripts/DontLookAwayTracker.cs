using System;
using UnityEngine;

namespace TheHouseIsAlive
{
    /// <summary>
    /// Tracks player head orientation and field-of-view against target entities.
    /// Powers the core "Don't Look Away" mechanic where entities only advance
    /// or reposition when unobserved.
    /// </summary>
    public class DontLookAwayTracker : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The main center-eye camera transform representing the player's head.")]
        [SerializeField] private Transform headTransform;

        [Header("FOV Configuration")]
        [Tooltip("Total horizontal FOV angle in degrees considered directly visible.")]
        [SerializeField] private float directGazeAngle = 60f;

        [Tooltip("Outer peripheral FOV angle in degrees. Beyond this is completely unobserved.")]
        [SerializeField] private float peripheralGazeAngle = 100f;

        [Tooltip("Max line-of-sight distance in meters.")]
        [SerializeField] private float maxDetectionDistance = 15f;

        [Header("Smoothing")]
        [SerializeField] private float stateChangeDebounceSeconds = 0.15f;

        public bool IsDirectlyObserved { get; private set; }
        public bool IsInPeriphery { get; private set; }
        public bool IsCompletelyUnobserved => !IsDirectlyObserved && !IsInPeriphery;
        public float GazeDotProduct { get; private set; }

        public event Action OnLookedAway;
        public event Action OnDirectGaze;

        private float _timeSinceLastStateChange;
        private bool _wasObservedLastFrame;

        private void Awake()
        {
            if (headTransform == null)
            {
                var mainCam = Camera.main;
                if (mainCam != null) headTransform = mainCam.transform;
            }
        }

        /// <summary>
        /// Evaluates visibility for a specific world position.
        /// </summary>
        public bool CheckVisibility(Vector3 targetPosition, out float dotProduct, out float distance)
        {
            if (headTransform == null)
            {
                var mainCam = Camera.main;
                if (mainCam != null) headTransform = mainCam.transform;
                if (headTransform == null)
                {
                    dotProduct = 0f;
                    distance = 0f;
                    return false;
                }
            }

            Vector3 toTarget = targetPosition - headTransform.position;
            distance = toTarget.magnitude;

            if (distance > maxDetectionDistance || distance < 0.05f)
            {
                dotProduct = -1f;
                return false;
            }

            Vector3 directionToTarget = toTarget / distance;
            dotProduct = Vector3.Dot(headTransform.forward, directionToTarget);

            float directCos = Mathf.Cos(directGazeAngle * 0.5f * Mathf.Deg2Rad);
            return dotProduct >= directCos;
        }

        /// <summary>
        /// Ticks visibility check against a target transform.
        /// </summary>
        public void UpdateTargetVisibility(Transform target)
        {
            if (target == null) return;

            bool observed = CheckVisibility(target.position, out float dot, out float dist);
            GazeDotProduct = dot;

            float peripheralCos = Mathf.Cos(peripheralGazeAngle * 0.5f * Mathf.Deg2Rad);
            IsInPeriphery = !observed && dot >= peripheralCos && dist <= maxDetectionDistance;
            IsDirectlyObserved = observed;

            _timeSinceLastStateChange += Time.deltaTime;

            if (IsDirectlyObserved != _wasObservedLastFrame && _timeSinceLastStateChange >= stateChangeDebounceSeconds)
            {
                _timeSinceLastStateChange = 0f;
                _wasObservedLastFrame = IsDirectlyObserved;

                if (IsDirectlyObserved)
                {
                    OnDirectGaze?.Invoke();
                }
                else
                {
                    OnLookedAway?.Invoke();
                }
            }
        }
    }
}

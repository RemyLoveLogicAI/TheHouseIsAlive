using System.Collections;
using UnityEngine;

namespace TheHouseIsAlive
{
    public enum EntityState
    {
        Dormant,
        Hiding,
        Scurrying,
        Peeking,
        Stalking,
        Frozen
    }

    /// <summary>
    /// Controls behavior and physical/occluded movements of the entity.
    /// Responds to observation states from DontLookAwayTracker.
    /// </summary>
    public class ShadowEntityController : MonoBehaviour
    {
        [Header("State")]
        [SerializeField] private EntityState currentState = EntityState.Dormant;

        [Header("Movement Settings")]
        [SerializeField] private float creepSpeed = 0.6f;
        [SerializeField] private float scurrySpeed = 3.5f;
        [SerializeField] private float minStalkDistance = 0.8f;

        [Header("Visual Representation")]
        [SerializeField] private Renderer entityMeshRenderer;
        [SerializeField] private Transform peekPivot;

        private DontLookAwayTracker _tracker;
        private Transform _playerTransform;
        private Coroutine _movementRoutine;
        private Vector3 _currentDestination;

        public EntityState State => currentState;

        public void Initialize(DontLookAwayTracker tracker, Transform playerTransform)
        {
            _tracker = tracker;
            _playerTransform = playerTransform;

            if (_tracker != null)
            {
                _tracker.OnDirectGaze += HandleGazeDirect;
                _tracker.OnLookedAway += HandleGazeLookedAway;
            }

            SetState(EntityState.Hiding);
        }

        private void OnDestroy()
        {
            if (_tracker != null)
            {
                _tracker.OnDirectGaze -= HandleGazeDirect;
                _tracker.OnLookedAway -= HandleGazeLookedAway;
            }
        }

        private void Update()
        {
            if (_tracker != null)
            {
                _tracker.UpdateTargetVisibility(transform);
            }

            if (currentState == EntityState.Stalking && _playerTransform != null)
            {
                // If player is looking directly at entity while stalking, immediately freeze
                if (_tracker != null && _tracker.IsDirectlyObserved)
                {
                    SetState(EntityState.Frozen);
                    return;
                }

                // Advance towards player if unobserved
                float distanceToPlayer = Vector3.Distance(transform.position, _playerTransform.position);
                if (distanceToPlayer > minStalkDistance)
                {
                    Vector3 moveDir = (_playerTransform.position - transform.position).normalized;
                    moveDir.y = 0; // Ground plane clamp
                    transform.position += moveDir * (creepSpeed * Time.deltaTime);
                    transform.rotation = Quaternion.LookRotation(moveDir);
                }
            }
        }

        public void ScurryBetweenPoints(Vector3 startPoint, Vector3 endPoint, System.Action onComplete = null)
        {
            transform.position = startPoint;
            _currentDestination = endPoint;
            SetState(EntityState.Scurrying);

            if (_movementRoutine != null) StopCoroutine(_movementRoutine);
            _movementRoutine = StartCoroutine(ScurryRoutine(startPoint, endPoint, onComplete));
        }

        private IEnumerator ScurryRoutine(Vector3 start, Vector3 end, System.Action onComplete)
        {
            float journeyLength = Vector3.Distance(start, end);
            float startTime = Time.time;

            while (Vector3.Distance(transform.position, end) > 0.05f)
            {
                float distCovered = (Time.time - startTime) * scurrySpeed;
                float fraction = Mathf.Clamp01(distCovered / journeyLength);

                transform.position = Vector3.Lerp(start, end, fraction);
                Vector3 dir = (end - start).normalized;
                if (dir != Vector3.zero) transform.rotation = Quaternion.LookRotation(dir);

                yield return null;
            }

            transform.position = end;
            SetState(EntityState.Hiding);
            onComplete?.Invoke();
        }

        public void PeekFromAnchor(Vector3 anchorEdge, Vector3 viewVector)
        {
            transform.position = anchorEdge;
            transform.rotation = Quaternion.LookRotation(viewVector);
            SetState(EntityState.Peeking);
        }

        private void HandleGazeDirect()
        {
            if (currentState == EntityState.Stalking)
            {
                SetState(EntityState.Frozen);
            }
            else if (currentState == EntityState.Peeking)
            {
                // Scurry back into hiding upon direct contact
                SetState(EntityState.Hiding);
            }
        }

        private void HandleGazeLookedAway()
        {
            if (currentState == EntityState.Frozen)
            {
                // Resume stalk when look away occurs
                SetState(EntityState.Stalking);
            }
        }

        private void SetState(EntityState newState)
        {
            currentState = newState;
            Debug.Log($"[ShadowEntity] State transition: {currentState}");

            if (entityMeshRenderer != null)
            {
                // Only hide mesh when completely dormant
                entityMeshRenderer.enabled = (currentState != EntityState.Dormant);
            }
        }
    }
}

using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace TheHouseIsAlive
{
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
        private NavMeshAgent _agent;
        private Coroutine _movementRoutine;

        public EntityState State => currentState;

        private void Start()
        {
            if (GetComponentInChildren<MeshFilter>() == null)
            {
                GameObject creature = ProceduralCreatureMesh.CreateCreatureGameObject();
                creature.transform.SetParent(transform, false);
            }

            if (entityMeshRenderer == null)
            {
                entityMeshRenderer = GetComponentInChildren<Renderer>();
            }

            _agent = GetComponent<NavMeshAgent>();
        }

        public void Initialize(DontLookAwayTracker tracker, Transform playerTransform)
        {
            if (_tracker != null)
            {
                _tracker.OnDirectGaze -= HandleGazeDirect;
                _tracker.OnLookedAway -= HandleGazeLookedAway;
            }

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
            if (_tracker == null) return;

            // Raw per-frame gaze (no debounce) so the freeze lands on the same frame.
            _tracker.UpdateTargetVisibility(peekPivot != null ? peekPivot : transform);
            bool observed = _tracker.IsDirectlyObserved;

            EntityState next = CreatureBrain.Next(currentState, observed);
            if (next != currentState)
            {
                if (next == EntityState.Frozen && _playerTransform != null)
                {
                    float dist = Vector3.Distance(transform.position, _playerTransform.position);
                    if (dist < 2.5f)
                    {
                        try { OVRInput.SetControllerVibration(0.2f, 0.2f, OVRInput.Controller.All); } catch { }
                    }
                }

                if (next == EntityState.Hiding && _movementRoutine != null)
                {
                    StopCoroutine(_movementRoutine);
                    _movementRoutine = null;
                }
                SetState(next);
            }

            if (currentState == EntityState.Stalking && _playerTransform != null
                && CreatureBrain.CanMove(currentState, observed))
            {
                Vector3 destination = _playerTransform.position;
                destination.y = transform.position.y;
                if (Vector3.Distance(transform.position, destination) > minStalkDistance)
                {
                    MoveTowards(destination, creepSpeed);
                }
            }
            else if (currentState == EntityState.Frozen && IsAgentOnNavMesh())
            {
                _agent.isStopped = true;
            }
        }

        /// <summary>Begin the final stalk: the creature only advances while unobserved.</summary>
        public void BeginStalk()
        {
            if (_movementRoutine != null) StopCoroutine(_movementRoutine);
            _movementRoutine = null;
            SetState(EntityState.Stalking);
        }

        public float DistanceTo(Vector3 point) => Vector3.Distance(transform.position, point);

        public void ScurryBetweenPoints(Vector3 startPoint, Vector3 endPoint, System.Action onComplete = null)
        {
            if (_movementRoutine != null) StopCoroutine(_movementRoutine);
            transform.position = startPoint;
            SetState(EntityState.Scurrying);
            _movementRoutine = StartCoroutine(ScurryRoutine(endPoint, onComplete));
        }

        private IEnumerator ScurryRoutine(Vector3 end, System.Action onComplete)
        {
            while (Vector3.Distance(transform.position, end) > 0.05f)
            {
                if (_tracker != null && _tracker.IsDirectlyObserved)
                {
                    SetState(EntityState.Hiding);
                    _movementRoutine = null;
                    yield break;
                }

                MoveTowards(end, scurrySpeed);
                yield return null;
            }

            MoveTo(end);
            SetState(EntityState.Hiding);
            _movementRoutine = null;
            onComplete?.Invoke();
        }

        public void PeekFromAnchor(Vector3 anchorEdge, Vector3 viewVector)
        {
            transform.position = anchorEdge;
            if (viewVector.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(viewVector);
            }
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
                if (_movementRoutine != null) StopCoroutine(_movementRoutine);
                SetState(EntityState.Hiding);
            }
            else if (currentState == EntityState.Scurrying)
            {
                if (_movementRoutine != null) StopCoroutine(_movementRoutine);
                _movementRoutine = null;
                SetState(EntityState.Hiding);
            }
        }

        private void HandleGazeLookedAway()
        {
            if (currentState == EntityState.Frozen)
            {
                SetState(EntityState.Stalking);
            }
            else if (currentState == EntityState.Hiding)
            {
                SetState(EntityState.Peeking);
            }
            else if (currentState == EntityState.Peeking)
            {
                SetState(EntityState.Stalking);
            }
        }

        private void MoveTowards(Vector3 destination, float speed)
        {
            if (IsAgentOnNavMesh())
            {
                _agent.isStopped = false;
                _agent.speed = speed;
                _agent.SetDestination(destination);
                return;
            }

            Vector3 direction = destination - transform.position;
            direction.y = 0f;
            transform.position = Vector3.MoveTowards(transform.position, destination, speed * Time.deltaTime);
            if (direction.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }

        private void MoveTo(Vector3 destination)
        {
            if (IsAgentOnNavMesh())
            {
                _agent.Warp(destination);
                _agent.isStopped = true;
            }
            else
            {
                transform.position = destination;
            }
        }

        private bool IsAgentOnNavMesh()
        {
            return _agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh;
        }

        private void SetState(EntityState newState)
        {
            currentState = newState;
            if (IsAgentOnNavMesh())
            {
                _agent.isStopped = newState == EntityState.Frozen || newState == EntityState.Hiding || newState == EntityState.Peeking;
            }

            if (entityMeshRenderer != null)
            {
                entityMeshRenderer.enabled = currentState != EntityState.Dormant && currentState != EntityState.Hiding;
            }
        }
    }
}

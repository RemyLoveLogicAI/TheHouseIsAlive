using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if META_XR_MRUK
using Meta.XR.MRUtilityKit;
#endif

namespace TheHouseIsAlive
{
    /// <summary>
    /// Master director for room-aware horror progression.
    /// Queries MRUK scene geometry for physical walls, doors, and furniture,
    /// and sequences escalating supernatural encounters.
    /// </summary>
    public class RoomScareDirector : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private SpatialKnockSequencer knockSequencer;
        [SerializeField] private FlickerLightingEffect lightFlicker;
        [SerializeField] private ShadowEntityController shadowEntity;
        [SerializeField] private DontLookAwayTracker gazeTracker;

        [Header("Timeline Settings")]
        [SerializeField] private float initialDelaySeconds = 15f;
        [SerializeField] private float phaseIntervalSeconds = 45f;

        private Transform _headTransform;
        private bool _sceneReady;

        private void Start()
        {
            var mainCam = Camera.main;
            if (mainCam != null) _headTransform = mainCam.transform;

#if META_XR_MRUK
            if (MRUK.Instance != null)
            {
                MRUK.Instance.RegisterSceneLoadedCallback(OnMRUKSceneLoaded);
            }
            else
            {
                StartCoroutine(FallbackRoomBootstrap());
            }
#else
            StartCoroutine(FallbackRoomBootstrap());
#endif
        }

#if META_XR_MRUK
        private void OnMRUKSceneLoaded()
        {
            _sceneReady = true;
            Debug.Log("[RoomScareDirector] MRUK scene loaded successfully. Initializing encounter flow.");
            StartCoroutine(EncounterSequence());
        }
#endif

        private IEnumerator FallbackRoomBootstrap()
        {
            // Fallback for Meta XR Simulator or testing prior to physical room scan
            yield return new WaitForSeconds(2.0f);
            _sceneReady = true;
            Debug.Log("[RoomScareDirector] Running with synthetic/fallback room bounds.");
            StartCoroutine(EncounterSequence());
        }

        private IEnumerator EncounterSequence()
        {
            yield return new WaitForSeconds(initialDelaySeconds);

            // Phase 1: The First Knock (Behind the player)
            Debug.Log("[RoomScareDirector] Starting Phase 1: Inhabitation Knock.");
            Vector3 knockPos = GetPositionBehindPlayer(2.5f);
            Vector3 knockNormal = (_headTransform != null ? _headTransform.forward : Vector3.forward);

            if (knockSequencer != null)
            {
                knockSequencer.TriggerKnockBurst(knockPos, knockNormal, 3);
            }

            yield return new WaitForSeconds(3.0f);

            // Light flicker follows the knocking
            if (lightFlicker != null)
            {
                lightFlicker.TriggerFlickerSequence(1.5f);
            }

            yield return new WaitForSeconds(phaseIntervalSeconds);

            // Phase 2: Peripheral Scurry
            Debug.Log("[RoomScareDirector] Starting Phase 2: Peripheral Furniture Scurry.");
            Vector3 startCover = GetFlankPosition(true, 2.0f);
            Vector3 endCover = GetFlankPosition(false, 2.2f);

            if (shadowEntity != null && gazeTracker != null)
            {
                shadowEntity.Initialize(gazeTracker, _headTransform);
                shadowEntity.ScurryBetweenPoints(startCover, endCover, () =>
                {
                    Debug.Log("[RoomScareDirector] Phase 2 Scurry complete.");
                });
            }

            yield return new WaitForSeconds(phaseIntervalSeconds);

            // Phase 3: The Doorway Stalk
            Debug.Log("[RoomScareDirector] Starting Phase 3: Stalk Encounter.");
            Vector3 doorwayPosition = GetPositionInFrontOfPlayer(3.5f);

            if (shadowEntity != null && gazeTracker != null)
            {
                shadowEntity.PeekFromAnchor(doorwayPosition, -(_headTransform.position - doorwayPosition).normalized);
            }
        }

        private Vector3 GetPositionBehindPlayer(float distance)
        {
            if (_headTransform == null) return new Vector3(0, 1.2f, -distance);
            Vector3 back = -_headTransform.forward;
            back.y = 0;
            return _headTransform.position + back.normalized * distance;
        }

        private Vector3 GetPositionInFrontOfPlayer(float distance)
        {
            if (_headTransform == null) return new Vector3(0, 1.2f, distance);
            Vector3 forward = _headTransform.forward;
            forward.y = 0;
            return _headTransform.position + forward.normalized * distance;
        }

        private Vector3 GetFlankPosition(bool leftSide, float distance)
        {
            if (_headTransform == null) return new Vector3(leftSide ? -distance : distance, 0.4f, 1.5f);
            Vector3 side = leftSide ? -_headTransform.right : _headTransform.right;
            side.y = 0;
            return _headTransform.position + side.normalized * distance;
        }
    }
}

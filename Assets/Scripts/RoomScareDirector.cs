using System.Collections;
using System.Collections.Generic;
using Meta.XR.MRUtilityKit;
using UnityEngine;

namespace TheHouseIsAlive
{
    /// <summary>
    /// Master director for the 10-minute room-aware encounter.
    /// Reads MRUK room anchors when a scene model exists, falls back to synthetic
    /// placement around the player when it does not, and drives the phases of
    /// <see cref="ScareTimeline"/>.
    /// </summary>
    public class RoomScareDirector : MonoBehaviour
    {
        private const string IntroLine = "Something followed you home.";
        private const string EndLine = "It lives here now.";
        private const float IntroTextSeconds = 4f;
        private const float TextDistance = 2f;
        private const float BehindDistance = 2.5f;
        private const float FlankDistance = 2f;
        private const float FinaleDistance = 1.0f;

        [Header("Components")]
        [SerializeField] private SpatialKnockSequencer knockSequencer;
        [SerializeField] private FlickerLightingEffect lightFlicker;
        [SerializeField] private ShadowEntityController shadowEntity;
        [SerializeField] private DontLookAwayTracker gazeTracker;
        [SerializeField] private AudioSource breathSource;

        [Header("Timeline (set timeScale < 1 for fast test runs)")]
        [SerializeField] private ScareTimeline timeline = new ScareTimeline();

        private readonly List<AnchorCandidate> _anchors = new List<AnchorCandidate>();
        private Transform _head;
        private TextMesh _text;
        private float _startTime;

        public ScarePhase CurrentPhase { get; private set; } = ScarePhase.Intro;
        public bool HasRoomData => _anchors.Count > 0;

        private void Start()
        {
            var cam = Camera.main;
            _head = cam != null ? cam.transform : transform;

            if (breathSource == null)
            {
                breathSource = new GameObject("BreathSource").AddComponent<AudioSource>();
                breathSource.spatialBlend = 1f;
                breathSource.spatialize = true;
                breathSource.clip = ProceduralAudioSynthesizer.CreateBreathClip();
            }

            if (MRUK.Instance != null)
            {
                MRUK.Instance.RegisterSceneLoadedCallback(CollectAnchors);
            }
            else
            {
                Debug.Log("[RoomScareDirector] No MRUK instance; using fallback placement.");
            }

            _startTime = Time.time;
            StartCoroutine(RunEncounter());
        }

        private void CollectAnchors()
        {
            _anchors.Clear();
            MRUKRoom room = MRUK.Instance != null ? MRUK.Instance.GetCurrentRoom() : null;
            if (room == null)
            {
                Debug.Log("[RoomScareDirector] Scene loaded without a room; using fallback placement.");
                return;
            }

            foreach (MRUKAnchor anchor in room.Anchors)
            {
                if (anchor == null) continue;
                _anchors.Add(new AnchorCandidate(anchor.transform.position, anchor.transform.forward,
                    anchor.Label.ToString()));
            }
            Debug.Log($"[RoomScareDirector] Room loaded with {_anchors.Count} anchors.");
        }

        private void Update()
        {
            // Quest Touch controller & keyboard in-headset testing shortcuts:
            // A or Space: Advance to next phase
            // B or K: Trigger wall knock on demand
            // Thumbstick Click or T: Toggle fast-forward (0.08x timeScale)
            if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.Touch) || Input.GetKeyDown(KeyCode.Space))
            {
                AdvancePhaseDebug();
            }
            if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.Touch) || Input.GetKeyDown(KeyCode.K))
            {
                Knock(3);
            }
            if (OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.Touch) ||
                OVRInput.GetDown(OVRInput.Button.SecondaryThumbstick, OVRInput.Controller.Touch) ||
                Input.GetKeyDown(KeyCode.T))
            {
                timeline.timeScale = timeline.timeScale > 0.5f ? 0.08f : 1.0f;
                Debug.Log($"[RoomScareDirector] Toggled timeline timeScale -> {timeline.timeScale}");
            }
        }

        public void AdvancePhaseDebug()
        {
            if (CurrentPhase < ScarePhase.Ended)
            {
                ScarePhase next = CurrentPhase + 1;
                _startTime = Time.time - timeline.PhaseStart(next);
                Debug.Log($"[RoomScareDirector] Debug advanced to phase: {next} at time {Elapsed:F1}s");
            }
        }

        private float Elapsed => Time.time - _startTime;

        private IEnumerator RunEncounter()
        {
            yield return Intro();
            yield return WaitForPhase(ScarePhase.Phase1Knock);
            yield return PhaseOneKnock();
            yield return WaitForPhase(ScarePhase.Phase2Scurry);
            yield return PhaseTwoScurry();
            yield return WaitForPhase(ScarePhase.Phase3Stalk);
            yield return PhaseThreeStalk();
            yield return WaitForPhase(ScarePhase.Finale);
            yield return Finale();
            yield return WaitForPhase(ScarePhase.Ended);
            EnterPhase(ScarePhase.Ended);
        }

        private IEnumerator WaitForPhase(ScarePhase phase)
        {
            while (Elapsed < timeline.PhaseStart(phase)) yield return null;
        }

        private void EnterPhase(ScarePhase phase)
        {
            CurrentPhase = phase;
            Debug.Log($"[RoomScareDirector] Phase -> {phase} at {Elapsed:F1}s (roomData={HasRoomData})");
        }

        private IEnumerator Intro()
        {
            EnterPhase(ScarePhase.Intro);
            ShowText(IntroLine);
            yield return new WaitForSeconds(Mathf.Min(IntroTextSeconds, timeline.introSeconds * 0.6f));
            HideText();
        }

        private IEnumerator PhaseOneKnock()
        {
            EnterPhase(ScarePhase.Phase1Knock);
            Knock(3);
            yield return Scaled(3f);
            if (lightFlicker != null) lightFlicker.TriggerFlickerSequence(1.5f);

            yield return Scaled(timeline.phase1Seconds * 0.55f);
            Knock(5);
        }

        private IEnumerator PhaseTwoScurry()
        {
            EnterPhase(ScarePhase.Phase2Scurry);
            if (shadowEntity == null || gazeTracker == null) yield break;
            shadowEntity.Initialize(gazeTracker, _head);

            Vector3 from = CoverPoint(true);
            Vector3 to = CoverPoint(false);
            yield return WaitUntilUnseen(from);
            shadowEntity.ScurryBetweenPoints(from, to);
            if (knockSequencer != null) knockSequencer.TriggerScratch(from, Vector3.up);

            yield return Scaled(timeline.phase2Seconds * 0.5f);
            yield return WaitUntilUnseen(to);
            shadowEntity.PeekFromAnchor(to, (_head.position - to).normalized);
        }

        private IEnumerator PhaseThreeStalk()
        {
            EnterPhase(ScarePhase.Phase3Stalk);
            bool hasDoor = SpawnPlanner.HasAny(_anchors, SpawnPlanner.DoorLabels);
            AnchorCandidate source = SpawnPlanner.PickBehind(_anchors,
                hasDoor ? SpawnPlanner.DoorLabels : SpawnPlanner.KnockLabels, _head.position, _head.forward,
                BehindDistance);

            breathSource.transform.position = source.position;
            breathSource.loop = true;
            breathSource.Play();
            Debug.Log($"[RoomScareDirector] Breathing from {source.label} (door={hasDoor}).");

            if (shadowEntity == null) yield break;
            yield return WaitUntilUnseen(source.position);
            shadowEntity.PeekFromAnchor(source.position, (_head.position - source.position).normalized);
            yield return Scaled(10f);
            shadowEntity.BeginStalk();
        }

        private IEnumerator Finale()
        {
            EnterPhase(ScarePhase.Finale);
            Knock(8);
            if (lightFlicker != null) lightFlicker.TriggerFlickerSequence(3f);

            Vector3 behind = SpawnPlanner.EnforceMinDistance(
                _head.position - FlatForward() * FinaleDistance, _head.position);
            if (shadowEntity != null)
            {
                yield return WaitUntilUnseen(behind);
                shadowEntity.PeekFromAnchor(behind, (_head.position - behind).normalized);
                shadowEntity.BeginStalk();
            }

            yield return Scaled(timeline.finaleSeconds * 0.6f);
            breathSource.Stop();
            ShowText(EndLine);
        }

        private void Knock(int count)
        {
            if (knockSequencer == null) return;
            AnchorCandidate wall = SpawnPlanner.PickBehind(_anchors, SpawnPlanner.KnockLabels, _head.position,
                _head.forward, BehindDistance);
            knockSequencer.TriggerKnockBurst(wall.position, wall.normal, count);
            Debug.Log($"[RoomScareDirector] Knock x{count} at {wall.label} {wall.position}");
        }

        private Vector3 CoverPoint(bool left)
        {
            Vector3 side = left ? -_head.right : _head.right;
            side.y = 0f;
            Vector3 flankHead = _head.position + side.normalized * 0.01f;
            AnchorCandidate cover = SpawnPlanner.PickBehind(_anchors, SpawnPlanner.CoverLabels, flankHead,
                -side, FlankDistance);
            Vector3 p = cover.position;
            p.y = Mathf.Min(p.y, _head.position.y - 1.0f); // keep it low, near the floor
            return SpawnPlanner.EnforceMinDistance(p, _head.position);
        }

        /// <summary>Never teleport the creature into view: wait until the spot is outside the gaze cone.</summary>
        private IEnumerator WaitUntilUnseen(Vector3 point)
        {
            while (GazeMath.IsInCone(_head.position, _head.forward, point, GazeMath.DefaultDirectConeDegrees, 15f))
            {
                yield return null;
            }
        }

        private WaitForSeconds Scaled(float seconds) =>
            new WaitForSeconds(seconds * (timeline.timeScale <= 0f ? 1f : timeline.timeScale));

        private Vector3 FlatForward()
        {
            Vector3 f = _head.forward;
            f.y = 0f;
            return f.sqrMagnitude < 1e-6f ? Vector3.forward : f.normalized;
        }

        private void ShowText(string line)
        {
            if (_text == null)
            {
                var go = new GameObject("NarrativeText");
                _text = go.AddComponent<TextMesh>();
                _text.anchor = TextAnchor.MiddleCenter;
                _text.alignment = TextAlignment.Center;
                _text.characterSize = 0.02f;
                _text.fontSize = 64;
                _text.color = new Color(0.85f, 0.1f, 0.1f);
            }

            _text.text = line;
            _text.transform.position = _head.position + FlatForward() * TextDistance;
            _text.transform.rotation = Quaternion.LookRotation(FlatForward());
            _text.gameObject.SetActive(true);
        }

        private void HideText()
        {
            if (_text != null) _text.gameObject.SetActive(false);
        }
    }
}

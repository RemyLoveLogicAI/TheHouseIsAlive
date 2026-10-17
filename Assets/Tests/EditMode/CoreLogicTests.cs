using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace TheHouseIsAlive.Tests
{
    public class GazeMathTests
    {
        private static readonly Vector3 Head = new Vector3(0f, 1.6f, 0f);

        [Test]
        public void TargetStraightAhead_IsInCone()
        {
            Assert.IsTrue(GazeMath.IsInCone(Head, Vector3.forward, Head + Vector3.forward * 3f, 60f, 15f));
        }

        [TestCase(29f, true)]
        [TestCase(31f, false)]
        [TestCase(90f, false)]
        [TestCase(180f, false)]
        public void ConeBoundary_IsHalfOfConeAngle(float yawDegrees, bool expected)
        {
            Vector3 target = Head + Quaternion.Euler(0f, yawDegrees, 0f) * Vector3.forward * 3f;
            Assert.AreEqual(expected, GazeMath.IsInCone(Head, Vector3.forward, target, 60f, 15f));
        }

        [Test]
        public void BeyondMaxDistance_IsNotObserved()
        {
            Assert.IsFalse(GazeMath.IsInCone(Head, Vector3.forward, Head + Vector3.forward * 20f, 60f, 15f));
        }

        [Test]
        public void ZeroForward_IsNotObserved()
        {
            Assert.IsFalse(GazeMath.IsInCone(Head, Vector3.zero, Head + Vector3.forward, 60f, 15f));
        }

        [Test]
        public void EntityEyeLevel_DirectGaze_Observed()
        {
            Vector3 head = new Vector3(0f, 1.6f, 0f);
            Vector3 lookDir = Vector3.forward;
            // Entity floor root at (0, 0, 2), eye level at (0, 1.1, 2)
            Vector3 eyePos = new Vector3(0f, 1.1f, 2f);
            Assert.IsTrue(GazeMath.IsInCone(head, lookDir, eyePos, 60f, 15f));
        }
    }

    public class CreatureBrainTests
    {
        [Test]
        public void StalkingCreature_FreezesTheFrameItIsSeen()
        {
            Assert.AreEqual(EntityState.Frozen, CreatureBrain.Next(EntityState.Stalking, true));
        }

        [Test]
        public void FrozenCreature_ResumesStalkingWhenUnobserved()
        {
            Assert.AreEqual(EntityState.Stalking, CreatureBrain.Next(EntityState.Frozen, false));
        }

        [TestCase(EntityState.Peeking)]
        [TestCase(EntityState.Scurrying)]
        public void SeenWhilePeekingOrScurrying_Hides(EntityState state)
        {
            Assert.AreEqual(EntityState.Hiding, CreatureBrain.Next(state, true));
        }

        [Test]
        public void NeverMovesWhileObserved()
        {
            foreach (EntityState s in System.Enum.GetValues(typeof(EntityState)))
            {
                Assert.IsFalse(CreatureBrain.CanMove(s, true), s.ToString());
            }
        }

        [Test]
        public void MovesOnlyWhenStalkingOrScurrying()
        {
            Assert.IsTrue(CreatureBrain.CanMove(EntityState.Stalking, false));
            Assert.IsTrue(CreatureBrain.CanMove(EntityState.Scurrying, false));
            Assert.IsFalse(CreatureBrain.CanMove(EntityState.Frozen, false));
            Assert.IsFalse(CreatureBrain.CanMove(EntityState.Hiding, false));
        }
    }

    public class ScareTimelineTests
    {
        [Test]
        public void DefaultSession_IsAboutTenMinutes()
        {
            var t = new ScareTimeline();
            Assert.That(t.TotalSeconds, Is.InRange(540f, 660f));
        }

        [Test]
        public void FirstKnock_StartsWithinTenSeconds()
        {
            var t = new ScareTimeline();
            Assert.LessOrEqual(t.PhaseStart(ScarePhase.Phase1Knock), 10f);
        }

        [Test]
        public void PhasesAdvanceInOrder()
        {
            var t = new ScareTimeline();
            ScarePhase last = ScarePhase.Intro;
            for (float s = 0f; s <= t.TotalSeconds + 1f; s += 0.5f)
            {
                ScarePhase p = t.PhaseAt(s);
                Assert.GreaterOrEqual((int)p, (int)last, "phase went backwards at " + s);
                Assert.LessOrEqual((int)p - (int)last, 1, "phase skipped at " + s);
                last = p;
            }
            Assert.AreEqual(ScarePhase.Ended, last);
        }

        [Test]
        public void TimeScale_ShrinksSessionForTesting()
        {
            var t = new ScareTimeline { timeScale = 0.1f };
            Assert.AreEqual(ScarePhase.Phase2Scurry, t.PhaseAt(t.PhaseStart(ScarePhase.Phase2Scurry) + 0.01f));
            Assert.That(t.TotalSeconds, Is.LessThan(70f));
        }
    }

    public class SpawnPlannerTests
    {
        private static readonly Vector3 Head = new Vector3(0f, 1.6f, 0f);

        [Test]
        public void PrefersWallBehindPlayer()
        {
            var anchors = new List<AnchorCandidate>
            {
                new AnchorCandidate(new Vector3(0f, 1.5f, 3f), Vector3.back, "WALL_FACE"),
                new AnchorCandidate(new Vector3(0f, 1.5f, -3f), Vector3.forward, "WALL_FACE"),
                new AnchorCandidate(new Vector3(0f, 0.5f, -1f), Vector3.up, "TABLE"),
            };
            var pick = SpawnPlanner.PickBehind(anchors, SpawnPlanner.KnockLabels, Head, Vector3.forward, 2.5f);
            Assert.AreEqual("WALL_FACE", pick.label);
            Assert.Less(pick.position.z, 0f);
        }

        [Test]
        public void NoSceneData_FallsBackBehindPlayer()
        {
            var pick = SpawnPlanner.PickBehind(null, SpawnPlanner.KnockLabels, Head, Vector3.forward, 2.5f);
            Assert.AreEqual(SpawnPlanner.Fallback, pick.label);
            Assert.AreEqual(-2.5f, pick.position.z, 1e-3f);
        }

        [Test]
        public void SpawnsNeverInsideMinHeadDistance()
        {
            var anchors = new List<AnchorCandidate>
            {
                new AnchorCandidate(Head + new Vector3(0f, 0f, -0.1f), Vector3.forward, "WALL_FACE"),
            };
            var pick = SpawnPlanner.PickBehind(anchors, SpawnPlanner.KnockLabels, Head, Vector3.forward, 2.5f);
            Assert.GreaterOrEqual(Vector3.Distance(pick.position, Head), SpawnPlanner.MinHeadDistance - 1e-4f);
        }

        [Test]
        public void NoDoor_IsDetected()
        {
            var anchors = new List<AnchorCandidate> { new AnchorCandidate(Vector3.one, Vector3.up, "WALL_FACE") };
            Assert.IsFalse(SpawnPlanner.HasAny(anchors, SpawnPlanner.DoorLabels));
        }
    }
}

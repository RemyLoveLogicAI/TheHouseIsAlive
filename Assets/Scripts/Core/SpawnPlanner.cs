using System.Collections.Generic;
using UnityEngine;

namespace TheHouseIsAlive
{
    /// <summary>A room surface the director may use (wall, door frame, furniture).</summary>
    public struct AnchorCandidate
    {
        public Vector3 position;
        public Vector3 normal;
        public string label;

        public AnchorCandidate(Vector3 position, Vector3 normal, string label)
        {
            this.position = position;
            this.normal = normal;
            this.label = label;
        }
    }

    /// <summary>
    /// Pure placement rules: prefers real room anchors, falls back to synthetic
    /// positions around the player, and never spawns closer than MinHeadDistance.
    /// </summary>
    public static class SpawnPlanner
    {
        public const float MinHeadDistance = 0.5f;
        public const string Fallback = "FALLBACK";

        public static readonly string[] KnockLabels = { "WALL_FACE", "DOOR_FRAME" };
        public static readonly string[] CoverLabels = { "TABLE", "COUCH", "BED", "STORAGE", "OTHER" };
        public static readonly string[] DoorLabels = { "DOOR_FRAME" };

        /// <summary>Best anchor with one of the labels, preferring those most behind the player.</summary>
        public static AnchorCandidate PickBehind(IList<AnchorCandidate> candidates, string[] labels,
            Vector3 head, Vector3 headForward, float fallbackDistance)
        {
            Vector3 flatForward = Flatten(headForward);
            bool found = false;
            AnchorCandidate best = default;
            float bestScore = float.MaxValue;

            if (candidates != null)
            {
                foreach (var c in candidates)
                {
                    if (!HasLabel(c.label, labels)) continue;
                    Vector3 to = Flatten(c.position - head);
                    if (to.sqrMagnitude < 1e-6f) continue;
                    float score = Vector3.Dot(flatForward, to.normalized); // -1 = directly behind
                    if (score < bestScore)
                    {
                        bestScore = score;
                        best = c;
                        found = true;
                    }
                }
            }

            if (!found)
            {
                Vector3 pos = head - flatForward * fallbackDistance;
                best = new AnchorCandidate(pos, flatForward, Fallback);
            }

            best.position = EnforceMinDistance(best.position, head);
            return best;
        }

        public static bool HasAny(IList<AnchorCandidate> candidates, string[] labels)
        {
            if (candidates == null) return false;
            foreach (var c in candidates)
            {
                if (HasLabel(c.label, labels)) return true;
            }
            return false;
        }

        public static Vector3 EnforceMinDistance(Vector3 position, Vector3 head)
        {
            Vector3 offset = position - head;
            float d = offset.magnitude;
            if (d >= MinHeadDistance) return position;
            Vector3 dir = d > 1e-4f ? offset / d : Vector3.back;
            return head + dir * MinHeadDistance;
        }

        private static bool HasLabel(string label, string[] labels)
        {
            if (string.IsNullOrEmpty(label)) return false;
            foreach (var l in labels)
            {
                if (label.Contains(l)) return true;
            }
            return false;
        }

        private static Vector3 Flatten(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude < 1e-6f ? Vector3.forward : v.normalized;
        }
    }
}

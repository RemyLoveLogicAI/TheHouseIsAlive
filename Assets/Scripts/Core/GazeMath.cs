using UnityEngine;

namespace TheHouseIsAlive
{
    /// <summary>Pure gaze-cone math, shared by runtime and EditMode tests.</summary>
    public static class GazeMath
    {
        public const float DefaultDirectConeDegrees = 60f;
        public const float MinTrackedDistance = 0.05f;

        /// <summary>True when target lies inside the full cone angle (degrees) around headForward.</summary>
        public static bool IsInCone(Vector3 headPosition, Vector3 headForward, Vector3 target,
            float coneDegrees, float maxDistance)
        {
            Vector3 toTarget = target - headPosition;
            float distance = toTarget.magnitude;
            if (distance < MinTrackedDistance || distance > maxDistance) return false;
            if (headForward.sqrMagnitude < 1e-6f) return false;

            float dot = Vector3.Dot(headForward.normalized, toTarget / distance);
            return dot >= Mathf.Cos(coneDegrees * 0.5f * Mathf.Deg2Rad);
        }
    }
}

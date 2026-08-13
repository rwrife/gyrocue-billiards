using UnityEngine;

namespace GyroCue.Core
{
    /// <summary>
    /// Small deterministic helpers for billiards-style velocity behavior.
    /// Kept separate from MonoBehaviours so we can exercise logic in EditMode tests.
    /// </summary>
    public static class PoolPhysicsMath
    {
        public static Vector2 ComputeCushionBounce(
            Vector2 incomingVelocity,
            Vector2 cushionNormal,
            float restitution,
            float minimumBounceSpeed,
            float stopSpeedThreshold)
        {
            var stopThreshold = Mathf.Max(0f, stopSpeedThreshold);
            var incomingSpeed = incomingVelocity.magnitude;
            if (incomingSpeed <= stopThreshold)
            {
                return Vector2.zero;
            }

            var safeNormal = cushionNormal.sqrMagnitude > 0.0001f ? cushionNormal.normalized : Vector2.up;
            var reflectedDirection = Vector2.Reflect(incomingVelocity.normalized, safeNormal);
            if (reflectedDirection.sqrMagnitude <= 0.0001f)
            {
                reflectedDirection = -incomingVelocity.normalized;
            }

            var clampedRestitution = Mathf.Clamp01(restitution);
            var minimumSpeed = Mathf.Max(0f, minimumBounceSpeed);
            var targetSpeed = incomingSpeed * clampedRestitution;

            // Prevent shallow/low-energy impacts from numerically "sticking" to cushions.
            if (targetSpeed > stopThreshold && targetSpeed < minimumSpeed)
            {
                targetSpeed = minimumSpeed;
            }

            if (targetSpeed <= stopThreshold)
            {
                return Vector2.zero;
            }

            return reflectedDirection * targetSpeed;
        }

        public static Vector2 ClampToRest(Vector2 velocity, float stopSpeedThreshold)
        {
            var stopThreshold = Mathf.Max(0f, stopSpeedThreshold);
            return velocity.magnitude <= stopThreshold ? Vector2.zero : velocity;
        }
    }
}

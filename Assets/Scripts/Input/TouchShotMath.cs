using UnityEngine;

namespace GyroCue.Input
{
    /// <summary>
    /// Reusable touch shot math helpers so behavior can be tested without a running scene.
    /// </summary>
    public static class TouchShotMath
    {
        public static Vector2 ResolveAimDirection(Vector2 dragDelta, Vector2 fallbackDirection, float deadzonePixels)
        {
            var deadzone = Mathf.Max(0f, deadzonePixels);
            if (dragDelta.sqrMagnitude <= deadzone * deadzone)
            {
                return NormalizeOrFallback(fallbackDirection, Vector2.right);
            }

            return dragDelta.normalized;
        }

        public static float CalculatePullDistancePixels(Vector2 gestureStart, Vector2 currentScreenPosition, Vector2 aimDirection)
        {
            var safeAimDirection = NormalizeOrFallback(aimDirection, Vector2.right);
            var pullVector = gestureStart - currentScreenPosition;
            return Mathf.Max(0f, Vector2.Dot(pullVector, safeAimDirection));
        }

        public static float NormalizePullDistance(float pullDistancePixels, float maxPullDistancePixels)
        {
            var safeMax = Mathf.Max(1f, maxPullDistancePixels);
            return Mathf.Clamp01(Mathf.Max(0f, pullDistancePixels) / safeMax);
        }

        public static float EvaluatePower(AnimationCurve powerCurve, float normalizedPullDistance)
        {
            var pull = Mathf.Clamp01(normalizedPullDistance);
            if (powerCurve == null || powerCurve.length == 0)
            {
                return pull;
            }

            return Mathf.Clamp01(powerCurve.Evaluate(pull));
        }

        private static Vector2 NormalizeOrFallback(Vector2 vector, Vector2 fallback)
        {
            return vector.sqrMagnitude > 0.0001f ? vector.normalized : fallback;
        }
    }
}

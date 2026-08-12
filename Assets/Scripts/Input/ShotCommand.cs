using UnityEngine;

namespace GyroCue.Input
{
    /// <summary>
    /// Normalized shot intent produced by touch input.
    /// </summary>
    public readonly struct ShotCommand
    {
        public ShotCommand(Vector2 aimDirection, float normalizedPower)
        {
            AimDirection = aimDirection.sqrMagnitude > 0.0001f ? aimDirection.normalized : Vector2.right;
            NormalizedPower = Mathf.Clamp01(normalizedPower);
        }

        public Vector2 AimDirection { get; }

        public float NormalizedPower { get; }
    }
}

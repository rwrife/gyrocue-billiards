using UnityEngine;

namespace GyroCue.Core
{
    /// <summary>
    /// Shared world-space layout defaults for the main mobile table scene.
    /// </summary>
    public static class TableLayoutConstants
    {
        public const float TableWidthWorldUnits = 20f;
        public const float TableHeightWorldUnits = 10f;
        public const float CushionThickness = 0.6f;
        public const float PocketRadius = 0.55f;
        public const float CameraPadding = 1.2f;

        // Portrait baseline for modern phones (~9:16).
        public const float TargetPhoneAspect = 9f / 16f;

        /// <summary>
        /// Computes an orthographic size that fits the full table + padding at a given aspect ratio.
        /// </summary>
        public static float CalculateOrthoSize(float viewportAspect)
        {
            var safeAspect = Mathf.Max(0.01f, viewportAspect);
            var halfHeight = (TableHeightWorldUnits * 0.5f) + CameraPadding;
            var halfWidth = (TableWidthWorldUnits * 0.5f) + CameraPadding;
            var aspectConstrainedHeight = halfWidth / safeAspect;
            return Mathf.Max(halfHeight, aspectConstrainedHeight);
        }
    }
}

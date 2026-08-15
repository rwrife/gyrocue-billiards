using UnityEngine;

namespace GyroCue.UI
{
    /// <summary>
    /// Keeps HUD text readable across common mobile resolutions by scaling from
    /// the shortest screen edge.
    /// </summary>
    public static class HudScaleUtility
    {
        private const float BaselineShortEdgePixels = 1080f;

        public static float ResolveScaleFactor(
            float screenWidthPixels,
            float screenHeightPixels,
            float minScale = 0.85f,
            float maxScale = 1.3f)
        {
            if (screenWidthPixels <= 0f || screenHeightPixels <= 0f)
            {
                return 1f;
            }

            var shortestEdge = Mathf.Min(Mathf.Abs(screenWidthPixels), Mathf.Abs(screenHeightPixels));
            var normalizedScale = shortestEdge / BaselineShortEdgePixels;
            var clampedMin = Mathf.Max(0.1f, minScale);
            var clampedMax = Mathf.Max(clampedMin, maxScale);
            return Mathf.Clamp(normalizedScale, clampedMin, clampedMax);
        }

        public static int ResolveScaledFontSize(int baseFontSize, float scaleFactor)
        {
            var safeBase = Mathf.Max(1, baseFontSize);
            var safeScale = Mathf.Max(0.1f, scaleFactor);
            return Mathf.Max(1, Mathf.RoundToInt(safeBase * safeScale));
        }
    }
}

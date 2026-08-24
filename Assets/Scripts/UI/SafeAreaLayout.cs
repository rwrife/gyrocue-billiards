using UnityEngine;

namespace GyroCue.UI
{
    /// <summary>Pure safe-area math shared by runtime canvases and touch hit regions.</summary>
    public static class SafeAreaLayout
    {
        public static Rect Normalize(Rect safeAreaPixels, Vector2 screenPixels)
        {
            if (screenPixels.x <= 0f || screenPixels.y <= 0f)
            {
                return new Rect(0f, 0f, 1f, 1f);
            }

            var xMin = Mathf.Clamp01(safeAreaPixels.xMin / screenPixels.x);
            var yMin = Mathf.Clamp01(safeAreaPixels.yMin / screenPixels.y);
            var xMax = Mathf.Clamp01(safeAreaPixels.xMax / screenPixels.x);
            var yMax = Mathf.Clamp01(safeAreaPixels.yMax / screenPixels.y);

            if (xMax <= xMin || yMax <= yMin)
            {
                return new Rect(0f, 0f, 1f, 1f);
            }

            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        public static Rect MapLocalViewportRect(
            Rect localViewportRect,
            Rect safeAreaPixels,
            Vector2 screenPixels)
        {
            var safe = Normalize(safeAreaPixels, screenPixels);
            var localXMin = Mathf.Clamp01(Mathf.Min(localViewportRect.xMin, localViewportRect.xMax));
            var localYMin = Mathf.Clamp01(Mathf.Min(localViewportRect.yMin, localViewportRect.yMax));
            var localXMax = Mathf.Clamp01(Mathf.Max(localViewportRect.xMin, localViewportRect.xMax));
            var localYMax = Mathf.Clamp01(Mathf.Max(localViewportRect.yMin, localViewportRect.yMax));

            return Rect.MinMaxRect(
                safe.xMin + (localXMin * safe.width),
                safe.yMin + (localYMin * safe.height),
                safe.xMin + (localXMax * safe.width),
                safe.yMin + (localYMax * safe.height));
        }
    }
}

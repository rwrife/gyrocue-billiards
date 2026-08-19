using UnityEngine;

namespace GyroCue.Practice
{
    /// <summary>
    /// Screen regions for the practice controls, in viewport coordinates so the HUD
    /// and the input router cannot drift apart.
    /// </summary>
    public static class PracticeControlLayout
    {
        /// <summary>Ball-face widget: draw down inside it, then stroke up.</summary>
        public static Rect StrokeWidget => new Rect(0.30f, 0.02f, 0.40f, 0.34f);

        /// <summary>Vertical strip for cue elevation.</summary>
        public static Rect ElevationStrip => new Rect(0.88f, 0.15f, 0.10f, 0.55f);

        public const float MaximumElevationDegrees = 70f;

        /// <summary>Face y at the bottom of the stroke widget: backswing room.</summary>
        public const float FaceBottom = -2f;

        /// <summary>Face y at the top of the stroke widget: the top edge of the ball.</summary>
        public const float FaceTop = 1f;

        public static bool Contains(Rect viewportRect, Vector2 screenPosition)
        {
            var viewport = ToViewport(screenPosition);
            return viewportRect.Contains(viewport);
        }

        public static Vector2 ToViewport(Vector2 screenPosition)
        {
            return new Vector2(
                screenPosition.x / Mathf.Max(1, Screen.width),
                screenPosition.y / Mathf.Max(1, Screen.height));
        }

        /// <summary>
        /// Maps a screen point inside the stroke widget onto ball-face coordinates,
        /// where (0,0) is the centre of the cue ball and y = -1 is its bottom edge.
        /// </summary>
        public static Vector2 ToFacePosition(Vector2 screenPosition)
        {
            var rect = StrokeWidget;
            var viewport = ToViewport(screenPosition);
            var normalizedX = Mathf.InverseLerp(rect.xMin, rect.xMax, viewport.x);
            var normalizedY = Mathf.InverseLerp(rect.yMin, rect.yMax, viewport.y);

            return new Vector2(
                Mathf.Lerp(-1f, 1f, normalizedX),
                Mathf.Lerp(FaceBottom, FaceTop, normalizedY));
        }

        /// <summary>Maps a screen point inside the elevation strip to degrees.</summary>
        public static float ToElevationDegrees(Vector2 screenPosition)
        {
            var rect = ElevationStrip;
            var viewport = ToViewport(screenPosition);
            var normalized = Mathf.Clamp01(Mathf.InverseLerp(rect.yMin, rect.yMax, viewport.y));

            return normalized * MaximumElevationDegrees;
        }
    }
}

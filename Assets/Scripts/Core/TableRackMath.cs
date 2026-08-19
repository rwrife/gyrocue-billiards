using System.Collections.Generic;
using UnityEngine;

namespace GyroCue.Core
{
    /// <summary>
    /// Deterministic world-space layout for the playable table: cushion segments,
    /// pocket mouths, the cue-ball spot, and the opening rack triangle.
    /// Kept free of MonoBehaviour state so layout stays testable in edit mode.
    /// </summary>
    public static class TableRackMath
    {
        public const int PocketCount = 6;
        public const int RackBallCount = 15;
        public const int RackRowCount = 5;

        public static float PlayableHalfWidth =>
            (TableLayoutConstants.TableWidthWorldUnits * 0.5f) - TableLayoutConstants.CushionThickness;

        public static float PlayableHalfHeight =>
            (TableLayoutConstants.TableHeightWorldUnits * 0.5f) - TableLayoutConstants.CushionThickness;

        /// <summary>
        /// Corner and side pocket centers, ordered top-left, top-center, top-right,
        /// bottom-left, bottom-center, bottom-right to match the scene placeholders.
        /// </summary>
        public static Vector2[] ResolvePocketPositions()
        {
            var halfWidth = PlayableHalfWidth;
            var halfHeight = PlayableHalfHeight;

            return new[]
            {
                new Vector2(-halfWidth, halfHeight),
                new Vector2(0f, halfHeight),
                new Vector2(halfWidth, halfHeight),
                new Vector2(-halfWidth, -halfHeight),
                new Vector2(0f, -halfHeight),
                new Vector2(halfWidth, -halfHeight)
            };
        }

        /// <summary>
        /// Cushion rails as (center, size) pairs, split so each pocket mouth stays open.
        /// </summary>
        public static IReadOnlyList<(Vector2 Center, Vector2 Size)> ResolveCushionSegments()
        {
            var thickness = TableLayoutConstants.CushionThickness;
            var pocketGap = TableLayoutConstants.PocketRadius;
            var halfWidth = PlayableHalfWidth;
            var halfHeight = PlayableHalfHeight;

            var railCenterY = halfHeight + (thickness * 0.5f);
            var railCenterX = halfWidth + (thickness * 0.5f);

            // Horizontal rails run from a corner pocket to the side pocket.
            var horizontalSpan = Mathf.Max(0.01f, halfWidth - (pocketGap * 2f));
            var horizontalOffsetX = pocketGap + (horizontalSpan * 0.5f);

            // Vertical rails run corner pocket to corner pocket.
            var verticalSpan = Mathf.Max(0.01f, (halfHeight * 2f) - (pocketGap * 2f));

            return new[]
            {
                (new Vector2(-horizontalOffsetX, railCenterY), new Vector2(horizontalSpan, thickness)),
                (new Vector2(horizontalOffsetX, railCenterY), new Vector2(horizontalSpan, thickness)),
                (new Vector2(-horizontalOffsetX, -railCenterY), new Vector2(horizontalSpan, thickness)),
                (new Vector2(horizontalOffsetX, -railCenterY), new Vector2(horizontalSpan, thickness)),
                (new Vector2(-railCenterX, 0f), new Vector2(thickness, verticalSpan)),
                (new Vector2(railCenterX, 0f), new Vector2(thickness, verticalSpan))
            };
        }

        /// <summary>
        /// Head-spot equivalent: a quarter of the playable length from the left cushion.
        /// </summary>
        public static Vector2 ResolveCueBallSpot()
        {
            return new Vector2(-PlayableHalfWidth * 0.5f, 0f);
        }

        public static Vector2 ResolveRackApex()
        {
            return new Vector2(PlayableHalfWidth * 0.5f, 0f);
        }

        /// <summary>
        /// Standard five-row triangle with its apex facing the cue ball.
        /// </summary>
        public static Vector2[] ResolveRackPositions(Vector2 apex, float ballRadius, float ballGap = 0.01f)
        {
            var spacing = Mathf.Max(0.01f, (ballRadius * 2f) + Mathf.Max(0f, ballGap));
            var rowSpacing = spacing * Mathf.Sqrt(3f) * 0.5f;
            var positions = new Vector2[RackBallCount];

            var index = 0;
            for (var row = 0; row < RackRowCount; row++)
            {
                var rowX = apex.x + (row * rowSpacing);
                for (var slot = 0; slot <= row; slot++)
                {
                    var rowY = (slot - (row * 0.5f)) * spacing;
                    positions[index++] = new Vector2(rowX, rowY);
                }
            }

            return positions;
        }
    }
}

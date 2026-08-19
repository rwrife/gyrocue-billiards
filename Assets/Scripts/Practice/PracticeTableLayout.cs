using UnityEngine;

namespace GyroCue.Practice
{
    /// <summary>
    /// Real-world 9-foot table dimensions, in metres, laid out on the XZ plane with
    /// the cloth surface at y = 0. Real units keep Unity's default gravity correct
    /// for jump shots without any scaling fudge.
    /// </summary>
    public static class PracticeTableLayout
    {
        public const float PlayfieldLengthMetres = 2.54f;
        public const float PlayfieldWidthMetres = 1.27f;
        public const float BallRadiusMetres = 0.028575f;
        public const float BallMassKg = 0.17f;
        public const float CushionHeightMetres = 0.0375f;
        public const float CushionThicknessMetres = 0.05f;
        public const float CornerPocketRadiusMetres = 0.06f;
        public const float SidePocketRadiusMetres = 0.065f;

        public const int RackBallCount = 15;
        public const int RackRowCount = 5;

        public static float HalfLength => PlayfieldLengthMetres * 0.5f;

        public static float HalfWidth => PlayfieldWidthMetres * 0.5f;

        /// <summary>Ball centre height when resting on the cloth.</summary>
        public static float BallRestHeight => BallRadiusMetres;

        /// <summary>Head spot: a quarter of the table length from the head cushion.</summary>
        public static Vector3 CueBallSpot =>
            new Vector3(-HalfLength * 0.5f, BallRestHeight, 0f);

        /// <summary>Foot spot: the apex ball of the rack.</summary>
        public static Vector3 RackApex =>
            new Vector3(HalfLength * 0.5f, BallRestHeight, 0f);

        /// <summary>
        /// Corner and side pocket centres, on the cloth plane.
        /// </summary>
        public static Vector3[] PocketCentres()
        {
            return new[]
            {
                new Vector3(-HalfLength, 0f, -HalfWidth),
                new Vector3(-HalfLength, 0f, HalfWidth),
                new Vector3(0f, 0f, -HalfWidth),
                new Vector3(0f, 0f, HalfWidth),
                new Vector3(HalfLength, 0f, -HalfWidth),
                new Vector3(HalfLength, 0f, HalfWidth)
            };
        }

        public static float PocketRadiusAt(Vector3 pocketCentre)
        {
            return Mathf.Abs(pocketCentre.x) < 0.001f
                ? SidePocketRadiusMetres
                : CornerPocketRadiusMetres;
        }

        /// <summary>
        /// Standard five-row triangle, apex facing the cue ball down the -X axis.
        /// </summary>
        public static Vector3[] RackPositions(Vector3 apex, float ballRadius, float gapMetres = 0.0002f)
        {
            var spacing = (ballRadius * 2f) + Mathf.Max(0f, gapMetres);
            var rowSpacing = spacing * Mathf.Sqrt(3f) * 0.5f;
            var positions = new Vector3[RackBallCount];

            var index = 0;
            for (var row = 0; row < RackRowCount; row++)
            {
                var rowX = apex.x + (row * rowSpacing);
                for (var slot = 0; slot <= row; slot++)
                {
                    var rowZ = (slot - (row * 0.5f)) * spacing;
                    positions[index++] = new Vector3(rowX, apex.y, rowZ);
                }
            }

            return positions;
        }

        /// <summary>
        /// Clamps a cue-ball placement to the cloth, inside the cushions.
        /// </summary>
        public static Vector3 ClampToPlayfield(Vector3 desired, float ballRadius)
        {
            var limitX = Mathf.Max(0f, HalfLength - ballRadius);
            var limitZ = Mathf.Max(0f, HalfWidth - ballRadius);

            return new Vector3(
                Mathf.Clamp(desired.x, -limitX, limitX),
                BallRestHeight,
                Mathf.Clamp(desired.z, -limitZ, limitZ));
        }
    }
}

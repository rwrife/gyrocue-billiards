using UnityEngine;

namespace GyroCue.Practice
{
    /// <summary>
    /// A resolved stroke: where on the ball face the tip lands, how hard, and at
    /// what cue elevation.
    /// </summary>
    public readonly struct CueStrike
    {
        public CueStrike(Vector3 aimDirection, float power01, Vector2 strikeOffset, float elevationDegrees)
        {
            var flatAim = new Vector3(aimDirection.x, 0f, aimDirection.z);
            AimDirection = flatAim.sqrMagnitude > 0.000001f ? flatAim.normalized : Vector3.forward;
            Power01 = Mathf.Clamp01(power01);
            StrikeOffset = Vector2.ClampMagnitude(strikeOffset, 1f);
            ElevationDegrees = Mathf.Clamp(elevationDegrees, 0f, 90f);
        }

        /// <summary>Horizontal aim on the cloth plane.</summary>
        public Vector3 AimDirection { get; }

        public float Power01 { get; }

        /// <summary>
        /// Tip contact point on the ball face in ball radii: x is left/right for side
        /// spin, y is low/high for draw and follow. Magnitude 1 is the ball's edge.
        /// </summary>
        public Vector2 StrikeOffset { get; }

        /// <summary>Butt-end elevation. 0 is a level cue, 90 points straight down.</summary>
        public float ElevationDegrees { get; }
    }

    /// <summary>Velocities to hand to the cue ball's rigidbody.</summary>
    public readonly struct CueStrikeResult
    {
        public CueStrikeResult(Vector3 linearVelocity, Vector3 angularVelocity, bool isMiscue)
        {
            LinearVelocity = linearVelocity;
            AngularVelocity = angularVelocity;
            IsMiscue = isMiscue;
        }

        public Vector3 LinearVelocity { get; }

        public Vector3 AngularVelocity { get; }

        public bool IsMiscue { get; }

        public bool IsAirborne => LinearVelocity.y > 0.01f;
    }

    /// <summary>
    /// Converts a stroke into cue-ball motion: speed from power, spin from the tip's
    /// offset on the ball face, and hop from an elevated cue striking above centre.
    ///
    /// This is a playable approximation rather than a full contact simulation, but the
    /// relationships it encodes are the real ones: high tip gives follow, low tip gives
    /// draw, off-centre gives side, and elevation plus a high tip plus power makes the
    /// ball leave the cloth.
    /// </summary>
    public static class CueStrikeMath
    {
        /// <summary>Beyond half a ball radius the tip slides off the ball.</summary>
        public const float MiscueOffsetLimit = 0.5f;

        public const float MiscuePowerRetained = 0.35f;

        /// <summary>Share of downward tip speed that comes back as upward hop.</summary>
        public const float JumpEfficiency = 0.5f;

        public static CueStrikeResult Resolve(
            CueStrike strike,
            float ballRadiusMetres,
            float maxBallSpeedMetresPerSecond)
        {
            var radius = Mathf.Max(0.0001f, ballRadiusMetres);
            var maxSpeed = Mathf.Max(0f, maxBallSpeedMetresPerSecond);
            var isMiscue = strike.StrikeOffset.magnitude > MiscueOffsetLimit;

            var effectivePower = isMiscue ? strike.Power01 * MiscuePowerRetained : strike.Power01;
            var ballSpeed = effectivePower * maxSpeed;

            if (ballSpeed <= 0f)
            {
                return new CueStrikeResult(Vector3.zero, Vector3.zero, isMiscue);
            }

            var elevationRadians = strike.ElevationDegrees * Mathf.Deg2Rad;
            var horizontalSpeed = ballSpeed * Mathf.Cos(elevationRadians);

            // An elevated cue drives the ball down into the slate; it rebounds as a hop.
            // Striking at or below centre scoops instead of jumping, so no lift there.
            var liftFactor = Mathf.Clamp01(strike.StrikeOffset.y);
            var verticalSpeed = isMiscue
                ? 0f
                : ballSpeed * Mathf.Sin(elevationRadians) * JumpEfficiency * liftFactor;

            var linearVelocity = (strike.AimDirection * horizontalSpeed) + (Vector3.up * verticalSpeed);

            if (isMiscue)
            {
                return new CueStrikeResult(linearVelocity, Vector3.zero, isMiscue: true);
            }

            return new CueStrikeResult(linearVelocity, ResolveAngularVelocity(strike, horizontalSpeed, radius), false);
        }

        /// <summary>
        /// Angular velocity from an off-centre hit: w = (p x v) * 5 / (2 r^2), the
        /// solid-sphere result with the tip impulse applied at contact point p.
        /// </summary>
        private static Vector3 ResolveAngularVelocity(CueStrike strike, float horizontalSpeed, float radius)
        {
            var right = Vector3.Cross(Vector3.up, strike.AimDirection).normalized;
            var contactPoint = ((right * strike.StrikeOffset.x) + (Vector3.up * strike.StrikeOffset.y)) * radius;
            var impulseDirection = strike.AimDirection * horizontalSpeed;

            return Vector3.Cross(contactPoint, impulseDirection) * (5f / (2f * radius * radius));
        }

        /// <summary>
        /// Natural rolling angular speed for a ball travelling at <paramref name="speed"/>.
        /// Useful for classifying a hit as follow, stun, or draw.
        /// </summary>
        public static float NaturalRollAngularSpeed(float speed, float ballRadiusMetres)
        {
            return speed / Mathf.Max(0.0001f, ballRadiusMetres);
        }
    }
}

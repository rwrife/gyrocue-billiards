using UnityEngine;

namespace GyroCue.Core
{
    /// <summary>
    /// Centralized, inspector-configurable pool physics constants for ball tuning and cushion response.
    /// </summary>
    [CreateAssetMenu(
        fileName = "PoolPhysicsTuningProfile",
        menuName = "GyroCue/Physics/Pool Physics Tuning Profile")]
    public sealed class PoolPhysicsTuningProfile : ScriptableObject
    {
        [Header("Ball Rigidbody2D")]
        [SerializeField, Min(0.01f)]
        private float ballMassKg = 0.17f;

        [SerializeField, Min(0f)]
        private float linearDrag = 1.4f;

        [SerializeField, Min(0f)]
        private float angularDrag = 0.9f;

        [Header("Ball/Cushion Contact")]
        [SerializeField, Range(0f, 1f)]
        private float restitution = 0.92f;

        [SerializeField, Min(0f)]
        private float friction = 0.05f;

        [SerializeField, Min(0f)]
        private float minimumCushionBounceSpeed = 0.5f;

        [SerializeField, Min(0f)]
        private float stopSpeedThreshold = 0.06f;

        public float BallMassKg => ballMassKg;

        public float LinearDrag => linearDrag;

        public float AngularDrag => angularDrag;

        public float Restitution => restitution;

        public float Friction => friction;

        public float MinimumCushionBounceSpeed => minimumCushionBounceSpeed;

        public float StopSpeedThreshold => stopSpeedThreshold;

        /// <summary>
        /// Applies tuned defaults to a live ball body/collider pair.
        /// </summary>
        public void ApplyTo(Rigidbody2D body, Collider2D collider = null)
        {
            if (body == null)
            {
                return;
            }

            body.mass = ballMassKg;
            body.drag = linearDrag;
            body.angularDrag = angularDrag;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            if (collider == null)
            {
                return;
            }

            var material = collider.sharedMaterial;
            if (material == null)
            {
                material = new PhysicsMaterial2D("PoolBallRuntimeMaterial");
            }

            material.friction = friction;
            material.bounciness = restitution;
            collider.sharedMaterial = material;
        }

        public Vector2 ResolveCushionBounce(Vector2 incomingVelocity, Vector2 cushionNormal)
        {
            var bounced = PoolPhysicsMath.ComputeCushionBounce(
                incomingVelocity,
                cushionNormal,
                restitution,
                minimumCushionBounceSpeed,
                stopSpeedThreshold);

            return PoolPhysicsMath.ClampToRest(bounced, stopSpeedThreshold);
        }

        public Vector2 ClampVelocityToRest(Vector2 velocity)
        {
            return PoolPhysicsMath.ClampToRest(velocity, stopSpeedThreshold);
        }
    }
}

using UnityEngine;

namespace GyroCue.Practice
{
    /// <summary>
    /// Cloth interaction for a ball resting on the slate.
    ///
    /// PhysX alone will happily roll a frictionless sphere forever and ignores the
    /// spin the cue put on the ball, so the interesting behaviour is applied here:
    /// while the contact patch is sliding, friction acts on both the ball's velocity
    /// and its spin, which is what turns backspin into draw and topspin into follow.
    /// Once the contact patch stops sliding the ball rolls and only rolling
    /// resistance remains.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class ClothContactMotion : MonoBehaviour
    {
        private const float Gravity = 9.81f;

        [SerializeField, Min(0.0001f)]
        private float ballRadius = PracticeTableLayout.BallRadiusMetres;

        [SerializeField, Min(0f)]
        private float slidingFriction = 0.2f;

        [SerializeField, Min(0f)]
        private float rollingResistance = 0.012f;

        [SerializeField, Min(0f)]
        private float spinDecay = 0.35f;

        [SerializeField, Min(0f)]
        private float stopSpeed = 0.012f;

        [SerializeField, Min(0f)]
        private float stopAngularSpeed = 0.35f;

        private Rigidbody body;

        public bool IsGrounded { get; private set; }

        public bool IsSliding { get; private set; }

        public bool IsAtRest =>
            body != null && body.velocity.magnitude <= stopSpeed && body.angularVelocity.magnitude <= stopAngularSpeed;

        public void Configure(float radius)
        {
            ballRadius = Mathf.Max(0.0001f, radius);
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            if (body == null || body.isKinematic)
            {
                return;
            }

            var deltaTime = Time.fixedDeltaTime;
            IsGrounded = transform.position.y <= ballRadius * 1.05f && Mathf.Abs(body.velocity.y) < 0.35f;

            if (!IsGrounded)
            {
                // Airborne: only spin decays, gravity is PhysX's job.
                IsSliding = false;
                body.angularVelocity *= Mathf.Clamp01(1f - (spinDecay * deltaTime));
                return;
            }

            ApplyClothContact(deltaTime);
            ClampToRest();
        }

        private void ApplyClothContact(float deltaTime)
        {
            var velocity = body.velocity;
            var angularVelocity = body.angularVelocity;

            // Velocity of the material point at the bottom of the ball.
            var contactOffset = Vector3.down * ballRadius;
            var contactVelocity = velocity + Vector3.Cross(angularVelocity, contactOffset);
            contactVelocity.y = 0f;

            if (contactVelocity.magnitude > stopSpeed)
            {
                IsSliding = true;

                var slideDirection = contactVelocity.normalized;
                var deceleration = slidingFriction * Gravity * deltaTime;

                // Friction opposes the sliding contact patch, slowing the ball and
                // spinning it toward a natural roll at the same time.
                velocity -= slideDirection * deceleration;
                angularVelocity += Vector3.Cross(Vector3.up, slideDirection) *
                                   (5f * slidingFriction * Gravity * deltaTime / (2f * ballRadius));
            }
            else
            {
                IsSliding = false;

                var horizontal = new Vector3(velocity.x, 0f, velocity.z);
                if (horizontal.magnitude > stopSpeed)
                {
                    velocity -= horizontal.normalized * (rollingResistance * Gravity * deltaTime);
                    horizontal = new Vector3(velocity.x, 0f, velocity.z);
                }

                // Hold a true roll so the ball tracks straight once it settles into one.
                var rollAxis = Vector3.Cross(Vector3.up, horizontal.normalized);
                var rollingSpin = horizontal.magnitude / ballRadius;
                var targetAngular = horizontal.sqrMagnitude > 0.000001f
                    ? rollAxis * rollingSpin
                    : Vector3.zero;

                // Preserve spin about the vertical axis: that is english, not roll.
                targetAngular.y = angularVelocity.y * Mathf.Clamp01(1f - (spinDecay * deltaTime));
                angularVelocity = targetAngular;
            }

            body.velocity = velocity;
            body.angularVelocity = angularVelocity;
        }

        private void ClampToRest()
        {
            if (body.velocity.magnitude <= stopSpeed)
            {
                body.velocity = Vector3.zero;
            }

            if (body.angularVelocity.magnitude <= stopAngularSpeed)
            {
                body.angularVelocity = Vector3.zero;
            }
        }
    }
}

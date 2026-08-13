using GyroCue.Core;
using NUnit.Framework;
using UnityEngine;

namespace GyroCue.Tests.EditMode
{
    public sealed class PoolPhysicsTuningProfileTests
    {
        [Test]
        public void ApplyTo_AssignsConfiguredMassDragAndContactMaterial()
        {
            var profile = ScriptableObject.CreateInstance<PoolPhysicsTuningProfile>();
            var root = new GameObject("pool-physics-profile-test");

            try
            {
                var body = root.AddComponent<Rigidbody2D>();
                var collider = root.AddComponent<CircleCollider2D>();

                profile.ApplyTo(body, collider);

                Assert.That(body.mass, Is.EqualTo(profile.BallMassKg).Within(0.0001f));
                Assert.That(body.drag, Is.EqualTo(profile.LinearDrag).Within(0.0001f));
                Assert.That(body.angularDrag, Is.EqualTo(profile.AngularDrag).Within(0.0001f));
                Assert.That(body.collisionDetectionMode, Is.EqualTo(CollisionDetectionMode2D.Continuous));

                Assert.That(collider.sharedMaterial, Is.Not.Null);
                Assert.That(collider.sharedMaterial.friction, Is.EqualTo(profile.Friction).Within(0.0001f));
                Assert.That(collider.sharedMaterial.bounciness, Is.EqualTo(profile.Restitution).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void ResolveCushionBounce_StopsNearRestVelocities()
        {
            var profile = ScriptableObject.CreateInstance<PoolPhysicsTuningProfile>();

            try
            {
                var bounced = profile.ResolveCushionBounce(new Vector2(0.02f, 0f), Vector2.left);
                Assert.That(bounced, Is.EqualTo(Vector2.zero));
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }
    }
}

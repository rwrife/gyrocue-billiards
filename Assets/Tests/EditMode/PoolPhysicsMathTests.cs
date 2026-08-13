using GyroCue.Core;
using NUnit.Framework;
using UnityEngine;

namespace GyroCue.Tests.EditMode
{
    public sealed class PoolPhysicsMathTests
    {
        [Test]
        public void ComputeCushionBounce_ReflectsDirectionAndAppliesRestitution()
        {
            var incoming = new Vector2(3f, 0f);
            var normal = Vector2.left;

            var bounced = PoolPhysicsMath.ComputeCushionBounce(
                incoming,
                normal,
                restitution: 0.9f,
                minimumBounceSpeed: 0.1f,
                stopSpeedThreshold: 0.01f);

            Assert.That(bounced.x, Is.LessThan(0f));
            Assert.That(bounced.y, Is.EqualTo(0f).Within(0.001f));
            Assert.That(bounced.magnitude, Is.EqualTo(2.7f).Within(0.001f));
        }

        [Test]
        public void ComputeCushionBounce_UsesMinimumBounceSpeedForLowEnergyContact()
        {
            var incoming = new Vector2(0.4f, 0f);
            var normal = Vector2.left;

            var bounced = PoolPhysicsMath.ComputeCushionBounce(
                incoming,
                normal,
                restitution: 0.6f,
                minimumBounceSpeed: 0.5f,
                stopSpeedThreshold: 0.05f);

            Assert.That(bounced.x, Is.LessThan(0f));
            Assert.That(bounced.magnitude, Is.EqualTo(0.5f).Within(0.001f));
        }

        [Test]
        public void ClampToRest_ZeroesVelocitiesBelowThreshold()
        {
            var resting = PoolPhysicsMath.ClampToRest(new Vector2(0.03f, 0f), stopSpeedThreshold: 0.05f);
            var moving = PoolPhysicsMath.ClampToRest(new Vector2(0.2f, 0f), stopSpeedThreshold: 0.05f);

            Assert.That(resting, Is.EqualTo(Vector2.zero));
            Assert.That(moving, Is.EqualTo(new Vector2(0.2f, 0f)));
        }
    }
}

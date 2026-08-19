using GyroCue.Practice;
using NUnit.Framework;
using UnityEngine;

namespace GyroCue.Tests.EditMode
{
    public sealed class CueStrikeMathTests
    {
        private const float Radius = PracticeTableLayout.BallRadiusMetres;
        private const float MaxSpeed = 5f;

        private static CueStrikeResult Resolve(Vector2 offset, float power = 1f, float elevation = 0f)
        {
            return CueStrikeMath.Resolve(
                new CueStrike(Vector3.forward, power, offset, elevation),
                Radius,
                MaxSpeed);
        }

        [Test]
        public void CentreHit_SendsBallDownAimWithNoSpin()
        {
            var result = Resolve(Vector2.zero);

            Assert.That(result.LinearVelocity.normalized.z, Is.EqualTo(1f).Within(0.001f));
            Assert.That(result.LinearVelocity.magnitude, Is.EqualTo(MaxSpeed).Within(0.001f));
            Assert.That(result.AngularVelocity.magnitude, Is.LessThan(0.001f));
            Assert.That(result.IsAirborne, Is.False);
        }

        [Test]
        public void HighTip_ProducesOverspinAndLowTipReversesIt()
        {
            var follow = Resolve(new Vector2(0f, 0.4f));
            var draw = Resolve(new Vector2(0f, -0.4f));

            // Travelling along +Z, a natural roll turns about +X, so follow adds to
            // that axis and draw reverses it.
            Assert.That(follow.AngularVelocity.x, Is.GreaterThan(0f));
            Assert.That(draw.AngularVelocity.x, Is.LessThan(0f));
            Assert.That(follow.AngularVelocity.x, Is.EqualTo(-draw.AngularVelocity.x).Within(0.001f));
        }

        [Test]
        public void HighTip_SpinsFasterThanNaturalRoll()
        {
            var result = Resolve(new Vector2(0f, 0.5f));
            var naturalRoll = CueStrikeMath.NaturalRollAngularSpeed(result.LinearVelocity.magnitude, Radius);

            Assert.That(result.AngularVelocity.magnitude, Is.GreaterThan(naturalRoll));
        }

        [Test]
        public void SideTip_SpinsAboutTheVerticalAxis()
        {
            var right = Resolve(new Vector2(0.4f, 0f));

            Assert.That(Mathf.Abs(right.AngularVelocity.y), Is.GreaterThan(1f));
            Assert.That(Mathf.Abs(right.AngularVelocity.x), Is.LessThan(0.001f));
        }

        [Test]
        public void ElevatedCueThroughTheTop_LiftsTheBallOffTheCloth()
        {
            var jump = Resolve(new Vector2(0f, 0.45f), power: 1f, elevation: 45f);

            Assert.That(jump.IsAirborne, Is.True);
            Assert.That(jump.LinearVelocity.y, Is.GreaterThan(0.5f));
        }

        [Test]
        public void ElevatedCueBelowCentre_ScoopsInsteadOfJumping()
        {
            var scoop = Resolve(new Vector2(0f, -0.45f), power: 1f, elevation: 45f);

            Assert.That(scoop.IsAirborne, Is.False);
            Assert.That(scoop.LinearVelocity.y, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void JumpHeightScalesWithPowerAndElevation()
        {
            var soft = Resolve(new Vector2(0f, 0.5f), power: 0.4f, elevation: 45f);
            var hard = Resolve(new Vector2(0f, 0.5f), power: 1f, elevation: 45f);
            var steeper = Resolve(new Vector2(0f, 0.5f), power: 1f, elevation: 65f);

            Assert.That(hard.LinearVelocity.y, Is.GreaterThan(soft.LinearVelocity.y));
            Assert.That(steeper.LinearVelocity.y, Is.GreaterThan(hard.LinearVelocity.y));
        }

        [Test]
        public void ElevationTradesHorizontalSpeedForHeight()
        {
            var level = Resolve(new Vector2(0f, 0.5f), power: 1f, elevation: 0f);
            var elevated = Resolve(new Vector2(0f, 0.5f), power: 1f, elevation: 60f);

            var levelHorizontal = new Vector2(level.LinearVelocity.x, level.LinearVelocity.z).magnitude;
            var elevatedHorizontal = new Vector2(elevated.LinearVelocity.x, elevated.LinearVelocity.z).magnitude;

            Assert.That(elevatedHorizontal, Is.LessThan(levelHorizontal));
        }

        [Test]
        public void TipBeyondHalfARadius_Miscues()
        {
            var clean = Resolve(new Vector2(0f, 0.5f));
            var miscue = Resolve(new Vector2(0f, 0.9f));

            Assert.That(clean.IsMiscue, Is.False);
            Assert.That(miscue.IsMiscue, Is.True);
            Assert.That(miscue.LinearVelocity.magnitude, Is.LessThan(clean.LinearVelocity.magnitude));
            Assert.That(miscue.AngularVelocity.magnitude, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void ZeroPower_LeavesTheBallStill()
        {
            var result = Resolve(Vector2.zero, power: 0f);

            Assert.That(result.LinearVelocity, Is.EqualTo(Vector3.zero));
            Assert.That(result.AngularVelocity, Is.EqualTo(Vector3.zero));
        }
    }
}

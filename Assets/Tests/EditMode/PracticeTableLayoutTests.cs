using GyroCue.Practice;
using NUnit.Framework;
using UnityEngine;

namespace GyroCue.Tests.EditMode
{
    public sealed class PracticeTableLayoutTests
    {
        [Test]
        public void PocketCentres_SitOnTheRailsAtSixPoints()
        {
            var pockets = PracticeTableLayout.PocketCentres();

            Assert.That(pockets.Length, Is.EqualTo(6));
            foreach (var pocket in pockets)
            {
                Assert.That(Mathf.Abs(pocket.z), Is.EqualTo(PracticeTableLayout.HalfWidth).Within(0.0001f));
                Assert.That(pocket.y, Is.EqualTo(0f).Within(0.0001f));
            }
        }

        [Test]
        public void SidePockets_AreWiderThanCornerPockets()
        {
            var side = PracticeTableLayout.PocketRadiusAt(new Vector3(0f, 0f, PracticeTableLayout.HalfWidth));
            var corner = PracticeTableLayout.PocketRadiusAt(
                new Vector3(PracticeTableLayout.HalfLength, 0f, PracticeTableLayout.HalfWidth));

            Assert.That(side, Is.GreaterThan(corner));
        }

        [Test]
        public void RackPositions_FitInsideTheFootHalfWithoutOverlapping()
        {
            var radius = PracticeTableLayout.BallRadiusMetres;
            var rack = PracticeTableLayout.RackPositions(PracticeTableLayout.RackApex, radius);

            Assert.That(rack.Length, Is.EqualTo(PracticeTableLayout.RackBallCount));

            foreach (var position in rack)
            {
                Assert.That(position.x, Is.GreaterThan(0f));
                Assert.That(position.x, Is.LessThan(PracticeTableLayout.HalfLength - radius));
                Assert.That(Mathf.Abs(position.z), Is.LessThan(PracticeTableLayout.HalfWidth - radius));
                Assert.That(position.y, Is.EqualTo(PracticeTableLayout.BallRestHeight).Within(0.0001f));
            }

            for (var i = 0; i < rack.Length; i++)
            {
                for (var j = i + 1; j < rack.Length; j++)
                {
                    Assert.That(Vector3.Distance(rack[i], rack[j]), Is.GreaterThanOrEqualTo(radius * 2f));
                }
            }
        }

        [Test]
        public void CueBallSpot_SitsOnTheHeadHalfAtRestHeight()
        {
            var spot = PracticeTableLayout.CueBallSpot;

            Assert.That(spot.x, Is.LessThan(0f));
            Assert.That(spot.y, Is.EqualTo(PracticeTableLayout.BallRestHeight).Within(0.0001f));
        }

        [Test]
        public void ClampToPlayfield_KeepsBallsOnTheCloth()
        {
            var radius = PracticeTableLayout.BallRadiusMetres;
            var clamped = PracticeTableLayout.ClampToPlayfield(new Vector3(99f, 5f, -99f), radius);

            Assert.That(clamped.x, Is.EqualTo(PracticeTableLayout.HalfLength - radius).Within(0.0001f));
            Assert.That(clamped.z, Is.EqualTo(-(PracticeTableLayout.HalfWidth - radius)).Within(0.0001f));
            Assert.That(clamped.y, Is.EqualTo(PracticeTableLayout.BallRestHeight).Within(0.0001f));
        }
    }
}

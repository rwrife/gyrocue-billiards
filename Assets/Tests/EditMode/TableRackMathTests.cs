using GyroCue.Core;
using NUnit.Framework;
using UnityEngine;

namespace GyroCue.Tests.EditMode
{
    public sealed class TableRackMathTests
    {
        [Test]
        public void ResolvePocketPositions_PlacesSixMouthsInsideCushions()
        {
            var pockets = TableRackMath.ResolvePocketPositions();

            Assert.That(pockets.Length, Is.EqualTo(TableRackMath.PocketCount));
            foreach (var pocket in pockets)
            {
                Assert.That(Mathf.Abs(pocket.x), Is.LessThanOrEqualTo(TableRackMath.PlayableHalfWidth + 0.001f));
                Assert.That(Mathf.Abs(pocket.y), Is.EqualTo(TableRackMath.PlayableHalfHeight).Within(0.001f));
            }
        }

        [Test]
        public void ResolveCushionSegments_LeaveGapsWideEnoughForEveryPocket()
        {
            var segments = TableRackMath.ResolveCushionSegments();
            var pockets = TableRackMath.ResolvePocketPositions();

            foreach (var pocket in pockets)
            {
                foreach (var segment in segments)
                {
                    var halfSize = segment.Size * 0.5f;
                    var insideX = Mathf.Abs(pocket.x - segment.Center.x) < halfSize.x;
                    var insideY = Mathf.Abs(pocket.y - segment.Center.y) < halfSize.y;

                    Assert.That(insideX && insideY, Is.False, $"Cushion at {segment.Center} blocks pocket at {pocket}.");
                }
            }
        }

        [Test]
        public void ResolveRackPositions_BuildsFifteenBallTriangleClearOfCushions()
        {
            const float ballRadius = 0.26f;
            var positions = TableRackMath.ResolveRackPositions(TableRackMath.ResolveRackApex(), ballRadius);

            Assert.That(positions.Length, Is.EqualTo(TableRackMath.RackBallCount));

            foreach (var position in positions)
            {
                Assert.That(
                    Mathf.Abs(position.x),
                    Is.LessThan(TableRackMath.PlayableHalfWidth - ballRadius),
                    $"Rack ball at {position} overlaps a side cushion.");
                Assert.That(
                    Mathf.Abs(position.y),
                    Is.LessThan(TableRackMath.PlayableHalfHeight - ballRadius),
                    $"Rack ball at {position} overlaps a top/bottom cushion.");
            }
        }

        [Test]
        public void ResolveRackPositions_KeepsBallsFromOverlappingEachOther()
        {
            const float ballRadius = 0.26f;
            var positions = TableRackMath.ResolveRackPositions(TableRackMath.ResolveRackApex(), ballRadius);

            for (var i = 0; i < positions.Length; i++)
            {
                for (var j = i + 1; j < positions.Length; j++)
                {
                    var separation = Vector2.Distance(positions[i], positions[j]);
                    Assert.That(separation, Is.GreaterThanOrEqualTo(ballRadius * 2f), $"Balls {i} and {j} overlap.");
                }
            }
        }

        [Test]
        public void ResolveCueBallSpot_SitsOnTheOppositeHalfFromTheRack()
        {
            var cueSpot = TableRackMath.ResolveCueBallSpot();
            var apex = TableRackMath.ResolveRackApex();

            Assert.That(cueSpot.x, Is.LessThan(0f));
            Assert.That(apex.x, Is.GreaterThan(0f));
            Assert.That(cueSpot.y, Is.EqualTo(0f).Within(0.001f));
        }
    }
}

using GyroCue.Core;
using NUnit.Framework;
using UnityEngine;

namespace GyroCue.Tests.EditMode
{
    public sealed class PocketTableControllerTests
    {
        [Test]
        public void TryPocketBody_RegularBall_DisablesSimulationAndRaisesPocketEvent()
        {
            var tableObject = new GameObject("table");
            var ballObject = new GameObject("ball");

            try
            {
                var table = tableObject.AddComponent<PocketTableController>();
                var ballBody = ballObject.AddComponent<Rigidbody2D>();
                ballBody.velocity = new Vector2(2f, 1f);
                ballBody.angularVelocity = 35f;

                PocketedBallEvent observed = default;
                var eventCount = 0;
                table.BallPocketed += evt =>
                {
                    observed = evt;
                    eventCount++;
                };

                var pocketed = table.TryPocketBody(ballBody, new Vector2(4f, -1f));

                Assert.That(pocketed, Is.True);
                Assert.That(eventCount, Is.EqualTo(1));
                Assert.That(observed.Body, Is.EqualTo(ballBody));
                Assert.That(observed.IsCueBall, Is.False);
                Assert.That(observed.PocketPosition, Is.EqualTo(new Vector2(4f, -1f)));
                Assert.That(ballBody.simulated, Is.False);
                Assert.That(ballBody.velocity, Is.EqualTo(Vector2.zero));
                Assert.That(ballBody.angularVelocity, Is.EqualTo(0f));
                Assert.That(ballObject.activeSelf, Is.False);
                Assert.That(table.ScratchOccurredThisTurn, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(tableObject);
                Object.DestroyImmediate(ballObject);
            }
        }

        [Test]
        public void TryPocketBody_CueBall_SetsScratchFlagAndRaisesScratchEvent()
        {
            var tableObject = new GameObject("table");
            var cueBallObject = new GameObject("cue-ball");

            try
            {
                var table = tableObject.AddComponent<PocketTableController>();
                var cueBallBody = cueBallObject.AddComponent<Rigidbody2D>();
                table.CueBallBody = cueBallBody;

                PocketedBallEvent scratchEvent = default;
                var scratchCount = 0;
                table.CueBallScratched += evt =>
                {
                    scratchEvent = evt;
                    scratchCount++;
                };

                var pocketed = table.TryPocketBody(cueBallBody, new Vector2(0f, 0f));

                Assert.That(pocketed, Is.True);
                Assert.That(table.ScratchOccurredThisTurn, Is.True);
                Assert.That(scratchCount, Is.EqualTo(1));
                Assert.That(scratchEvent.IsCueBall, Is.True);
                Assert.That(table.WasPocketed(cueBallBody), Is.True);

                table.BeginTurnPocketTracking();
                Assert.That(table.ScratchOccurredThisTurn, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(tableObject);
                Object.DestroyImmediate(cueBallObject);
            }
        }

        [Test]
        public void TryPocketBody_DuplicateTriggerContacts_AreIgnoredAfterFirstPocket()
        {
            var tableObject = new GameObject("table");
            var ballObject = new GameObject("ball");

            try
            {
                var table = tableObject.AddComponent<PocketTableController>();
                var ballBody = ballObject.AddComponent<Rigidbody2D>();

                var first = table.TryPocketBody(ballBody, Vector2.zero);
                var second = table.TryPocketBody(ballBody, Vector2.one);

                Assert.That(first, Is.True);
                Assert.That(second, Is.False);
                Assert.That(table.PocketedBallCount, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(tableObject);
                Object.DestroyImmediate(ballObject);
            }
        }
    }
}

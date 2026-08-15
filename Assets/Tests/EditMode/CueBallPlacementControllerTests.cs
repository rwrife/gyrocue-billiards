using GyroCue.Core;
using NUnit.Framework;
using UnityEngine;

namespace GyroCue.Tests.EditMode
{
    public sealed class CueBallPlacementControllerTests
    {
        [Test]
        public void BeginCueBallInHand_ReturnsFalseWhenCueBallMissing()
        {
            var tableObject = new GameObject("table");

            try
            {
                var controller = tableObject.AddComponent<CueBallPlacementController>();
                Assert.That(controller.BeginCueBallInHand(), Is.False);
                Assert.That(controller.IsPlacementModeActive, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(tableObject);
            }
        }

        [Test]
        public void BeginCueBallInHand_ReactivatesCueBallAndDisablesSimulation()
        {
            var tableObject = new GameObject("table");
            var cueBallObject = new GameObject("cue-ball");

            try
            {
                var controller = tableObject.AddComponent<CueBallPlacementController>();
                var cueBallBody = cueBallObject.AddComponent<Rigidbody2D>();
                cueBallBody.velocity = new Vector2(3f, -1f);
                cueBallBody.angularVelocity = 22f;
                cueBallBody.gameObject.SetActive(false);
                cueBallBody.simulated = true;

                controller.CueBallBody = cueBallBody;

                var began = controller.BeginCueBallInHand();

                Assert.That(began, Is.True);
                Assert.That(controller.IsPlacementModeActive, Is.True);
                Assert.That(cueBallBody.gameObject.activeSelf, Is.True);
                Assert.That(cueBallBody.simulated, Is.False);
                Assert.That(cueBallBody.velocity, Is.EqualTo(Vector2.zero));
                Assert.That(cueBallBody.angularVelocity, Is.EqualTo(0f));
            }
            finally
            {
                Object.DestroyImmediate(tableObject);
                Object.DestroyImmediate(cueBallObject);
            }
        }

        [Test]
        public void TryPlaceCueBall_ReturnsFalseUntilPlacementModeBegins()
        {
            var tableObject = new GameObject("table");
            var cueBallObject = new GameObject("cue-ball");

            try
            {
                var controller = tableObject.AddComponent<CueBallPlacementController>();
                var cueBallBody = cueBallObject.AddComponent<Rigidbody2D>();
                controller.CueBallBody = cueBallBody;

                var placed = controller.TryPlaceCueBall(new Vector2(1f, 1f), out var finalPosition);

                Assert.That(placed, Is.False);
                Assert.That(finalPosition, Is.EqualTo(default(Vector2)));
            }
            finally
            {
                Object.DestroyImmediate(tableObject);
                Object.DestroyImmediate(cueBallObject);
            }
        }

        [Test]
        public void TryPlaceCueBall_ClampsToPlayableTableBounds()
        {
            var tableObject = new GameObject("table");
            var cueBallObject = new GameObject("cue-ball");

            try
            {
                var controller = tableObject.AddComponent<CueBallPlacementController>();
                var cueBallBody = cueBallObject.AddComponent<Rigidbody2D>();
                var cueBallCollider = cueBallObject.AddComponent<CircleCollider2D>();
                cueBallCollider.radius = 0.25f;

                controller.CueBallBody = cueBallBody;
                Assert.That(controller.BeginCueBallInHand(), Is.True);

                var placed = controller.TryPlaceCueBall(new Vector2(50f, -50f), out var finalPosition);

                Assert.That(placed, Is.True);
                Assert.That(controller.IsPlacementModeActive, Is.False);
                Assert.That(cueBallBody.simulated, Is.True);

                var expectedHalfWidth = (TableLayoutConstants.TableWidthWorldUnits * 0.5f)
                                        - TableLayoutConstants.CushionThickness
                                        - cueBallCollider.radius
                                        - 0.02f;
                var expectedHalfHeight = (TableLayoutConstants.TableHeightWorldUnits * 0.5f)
                                         - TableLayoutConstants.CushionThickness
                                         - cueBallCollider.radius
                                         - 0.02f;

                Assert.That(finalPosition.x, Is.EqualTo(expectedHalfWidth).Within(0.0001f));
                Assert.That(finalPosition.y, Is.EqualTo(-expectedHalfHeight).Within(0.0001f));
                Assert.That(cueBallBody.position, Is.EqualTo(finalPosition));
                Assert.That(cueBallBody.velocity, Is.EqualTo(Vector2.zero));
                Assert.That(cueBallBody.angularVelocity, Is.EqualTo(0f));
            }
            finally
            {
                Object.DestroyImmediate(tableObject);
                Object.DestroyImmediate(cueBallObject);
            }
        }
    }
}

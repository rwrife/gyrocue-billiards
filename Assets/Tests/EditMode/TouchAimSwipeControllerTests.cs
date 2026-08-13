using GyroCue.Input;
using NUnit.Framework;
using UnityEngine;

namespace GyroCue.Tests.EditMode
{
    public sealed class TouchAimSwipeControllerTests
    {
        [Test]
        public void ProcessTouchPhase_ReleasesShotAfterAimThenPullBack()
        {
            var root = new GameObject("touch-controller-test");

            try
            {
                var controller = root.AddComponent<TouchAimSwipeController>();

                var didRelease = controller.ProcessTouchPhase(TouchPhase.Began, new Vector2(100f, 100f), 1, out _);
                Assert.That(didRelease, Is.False);

                controller.ProcessTouchPhase(TouchPhase.Moved, new Vector2(220f, 100f), 1, out _);
                didRelease = controller.ProcessTouchPhase(TouchPhase.Ended, new Vector2(20f, 100f), 1, out var shot);

                Assert.That(didRelease, Is.True);
                Assert.That(shot.AimDirection.x, Is.GreaterThan(0.95f));
                Assert.That(shot.NormalizedPower, Is.GreaterThan(0.15f));
                Assert.That(shot.NormalizedPower, Is.LessThanOrEqualTo(1f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SetBallsMoving_LocksInputAndSuppressesShotRelease()
        {
            var root = new GameObject("touch-controller-lock-test");

            try
            {
                var controller = root.AddComponent<TouchAimSwipeController>();

                controller.ProcessTouchPhase(TouchPhase.Began, new Vector2(100f, 100f), 2, out _);
                controller.ProcessTouchPhase(TouchPhase.Moved, new Vector2(220f, 100f), 2, out _);

                controller.SetBallsMoving(true);
                var didRelease = controller.ProcessTouchPhase(TouchPhase.Ended, new Vector2(20f, 100f), 2, out _);

                Assert.That(controller.InputLocked, Is.True);
                Assert.That(didRelease, Is.False);

                controller.SetBallsMoving(false);
                Assert.That(controller.InputLocked, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SetTouchInputEnabled_FalseSuppressesTouchesUntilReEnabled()
        {
            var root = new GameObject("touch-controller-enable-test");

            try
            {
                var controller = root.AddComponent<TouchAimSwipeController>();

                controller.SetTouchInputEnabled(false);
                Assert.That(controller.TouchInputEnabled, Is.False);

                var didRelease = controller.ProcessTouchPhase(TouchPhase.Began, new Vector2(100f, 100f), 3, out _);
                Assert.That(didRelease, Is.False);

                controller.SetTouchInputEnabled(true);
                Assert.That(controller.TouchInputEnabled, Is.True);

                controller.ProcessTouchPhase(TouchPhase.Began, new Vector2(100f, 100f), 3, out _);
                controller.ProcessTouchPhase(TouchPhase.Moved, new Vector2(220f, 100f), 3, out _);
                didRelease = controller.ProcessTouchPhase(TouchPhase.Ended, new Vector2(20f, 100f), 3, out _);
                Assert.That(didRelease, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}

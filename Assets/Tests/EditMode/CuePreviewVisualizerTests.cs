using GyroCue.Input;
using NUnit.Framework;
using UnityEngine;

namespace GyroCue.Tests.EditMode
{
    public sealed class CuePreviewVisualizerTests
    {
        [Test]
        public void RefreshVisuals_WithTouchAim_ShowsCueIndicatorAndPreviewLine()
        {
            var root = new GameObject("cue-preview-touch-test");
            var cueBall = new GameObject("cue-ball");
            var cueIndicator = new GameObject("cue-indicator");

            try
            {
                cueBall.transform.position = new Vector3(2f, 0f, -3f);
                cueIndicator.SetActive(false);

                var touchController = root.AddComponent<TouchAimSwipeController>();
                var lineRenderer = root.AddComponent<LineRenderer>();
                var visualizer = root.AddComponent<CuePreviewVisualizer>();

                visualizer.SetCueBallAnchor(cueBall.transform);
                visualizer.SetCueIndicator(cueIndicator.transform);

                touchController.ProcessTouchPhase(TouchPhase.Began, new Vector2(100f, 100f), 1, out _);
                touchController.ProcessTouchPhase(TouchPhase.Moved, new Vector2(260f, 100f), 1, out _);

                visualizer.RefreshVisuals();

                Assert.That(lineRenderer.enabled, Is.True);
                Assert.That(lineRenderer.positionCount, Is.EqualTo(2));

                var lineStart = lineRenderer.GetPosition(0);
                var lineEnd = lineRenderer.GetPosition(1);

                Assert.That(Vector3.Distance(lineStart, cueBall.transform.position), Is.LessThan(0.001f));
                Assert.That(lineEnd.x, Is.GreaterThan(lineStart.x));
                Assert.That(cueIndicator.activeSelf, Is.True);
                Assert.That(Vector3.Distance(cueIndicator.transform.position, cueBall.transform.position), Is.LessThan(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(cueBall);
                Object.DestroyImmediate(cueIndicator);
            }
        }

        [Test]
        public void RefreshVisuals_WithFreshRemoteFrames_PrefersRemoteAimDirection()
        {
            var root = new GameObject("cue-preview-remote-test");

            try
            {
                var now = 10f;
                var touchController = root.AddComponent<TouchAimSwipeController>();
                var remoteAdapter = root.AddComponent<RemoteSensorInputAdapter>();
                var lineRenderer = root.AddComponent<LineRenderer>();
                var visualizer = root.AddComponent<CuePreviewVisualizer>();

                remoteAdapter.SetTimeProviderForTests(() => now);

                // Touch aim up.
                touchController.ProcessTouchPhase(TouchPhase.Began, new Vector2(100f, 100f), 7, out _);
                touchController.ProcessTouchPhase(TouchPhase.Moved, new Vector2(100f, 260f), 7, out _);

                // Remote aim right (fresh frame should take precedence).
                remoteAdapter.ProcessSensorFrame(CreateFrame(0, Quaternion.Euler(0f, 90f, 0f), Vector3.zero), out _);

                visualizer.RefreshVisuals();

                var lineStart = lineRenderer.GetPosition(0);
                var lineEnd = lineRenderer.GetPosition(1);
                var direction = (lineEnd - lineStart).normalized;

                Assert.That(lineRenderer.enabled, Is.True);
                Assert.That(direction.x, Is.GreaterThan(0.95f));
                Assert.That(Mathf.Abs(direction.z), Is.LessThan(0.2f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SetBallsMoving_HidesPreviewUntilShotStateClears()
        {
            var root = new GameObject("cue-preview-lock-test");
            var cueIndicator = new GameObject("cue-indicator");

            try
            {
                cueIndicator.SetActive(false);

                var touchController = root.AddComponent<TouchAimSwipeController>();
                var lineRenderer = root.AddComponent<LineRenderer>();
                var visualizer = root.AddComponent<CuePreviewVisualizer>();

                visualizer.SetCueIndicator(cueIndicator.transform);

                touchController.ProcessTouchPhase(TouchPhase.Began, new Vector2(100f, 100f), 3, out _);
                touchController.ProcessTouchPhase(TouchPhase.Moved, new Vector2(260f, 100f), 3, out _);
                visualizer.RefreshVisuals();

                Assert.That(lineRenderer.enabled, Is.True);
                Assert.That(cueIndicator.activeSelf, Is.True);

                visualizer.SetBallsMoving(true);
                Assert.That(lineRenderer.enabled, Is.False);
                Assert.That(cueIndicator.activeSelf, Is.False);

                visualizer.SetBallsMoving(false);
                visualizer.RefreshVisuals();

                Assert.That(lineRenderer.enabled, Is.True);
                Assert.That(cueIndicator.activeSelf, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(cueIndicator);
            }
        }

        private static RemoteCueSensorFrame CreateFrame(long sequence, Quaternion orientation, Vector3 accelerationMps2)
        {
            return new RemoteCueSensorFrame(
                RemoteCueProtocol.SchemaVersionV1,
                timestampUnixMs: 10_000 + sequence,
                sequence: sequence,
                orientation,
                accelerationMps2,
                angularVelocityRadPerSec: Vector3.zero);
        }
    }
}

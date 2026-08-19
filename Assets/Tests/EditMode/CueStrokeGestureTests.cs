using GyroCue.Practice;
using NUnit.Framework;
using UnityEngine;

namespace GyroCue.Tests.EditMode
{
    public sealed class CueStrokeGestureTests
    {
        private static CueStrokeGesture NewGesture() => new CueStrokeGesture();

        [Test]
        public void DrawDownThenStrokeUp_ReleasesAShot()
        {
            var gesture = NewGesture();

            gesture.Begin(new Vector2(0f, -0.5f), 0f);
            gesture.Update(new Vector2(0f, -1.6f), 0.30f);
            Assert.That(gesture.Phase, Is.EqualTo(CueStrokePhase.DrawingBack));
            Assert.That(gesture.HasUsableBackswing, Is.True);

            var released = gesture.TryRelease(new Vector2(0f, 0.2f), 0.45f, out var sample);

            Assert.That(released, Is.True);
            Assert.That(sample.Power01, Is.GreaterThan(0.5f));
            Assert.That(sample.StrikeOffset.y, Is.EqualTo(0.2f).Within(0.001f));
            Assert.That(gesture.Phase, Is.EqualTo(CueStrokePhase.Idle));
        }

        [Test]
        public void FasterStroke_OverTheSameDistance_HitsHarder()
        {
            var slow = NewGesture();
            slow.Begin(new Vector2(0f, -0.5f), 0f);
            slow.Update(new Vector2(0f, -1.6f), 0.3f);
            slow.TryRelease(new Vector2(0f, 0f), 1.1f, out var slowSample);

            var fast = NewGesture();
            fast.Begin(new Vector2(0f, -0.5f), 0f);
            fast.Update(new Vector2(0f, -1.6f), 0.3f);
            fast.TryRelease(new Vector2(0f, 0f), 0.42f, out var fastSample);

            Assert.That(fastSample.Power01, Is.GreaterThan(slowSample.Power01));
        }

        [Test]
        public void WhereTheStrokeStops_SetsTheStrikeHeight()
        {
            var high = NewGesture();
            high.Begin(new Vector2(0f, -0.5f), 0f);
            high.Update(new Vector2(0f, -1.6f), 0.2f);
            high.TryRelease(new Vector2(0f, 0.6f), 0.35f, out var highSample);

            var low = NewGesture();
            low.Begin(new Vector2(0f, -0.5f), 0f);
            low.Update(new Vector2(0f, -1.6f), 0.2f);
            low.TryRelease(new Vector2(0f, -0.6f), 0.35f, out var lowSample);

            Assert.That(highSample.StrikeOffset.y, Is.GreaterThan(0.5f));
            Assert.That(lowSample.StrikeOffset.y, Is.LessThan(-0.5f));
        }

        [Test]
        public void StrikeOffsetIsClampedToTheBallFace()
        {
            var gesture = NewGesture();
            gesture.Begin(new Vector2(0f, -0.5f), 0f);
            gesture.Update(new Vector2(2f, -1.8f), 0.2f);

            Assert.That(gesture.StrikeOffset.x, Is.EqualTo(1f).Within(0.001f));
            Assert.That(gesture.StrikeOffset.y, Is.EqualTo(-1f).Within(0.001f));
        }

        [Test]
        public void WithoutEnoughBackswing_NoShotIsReleased()
        {
            var gesture = NewGesture();
            gesture.Begin(new Vector2(0f, -0.9f), 0f);
            gesture.Update(new Vector2(0f, -1.1f), 0.05f);

            var released = gesture.TryRelease(new Vector2(0f, 0.5f), 0.12f, out _);

            Assert.That(released, Is.False);
        }

        [Test]
        public void APullBackAlone_DoesNotFire()
        {
            var gesture = NewGesture();
            gesture.Begin(new Vector2(0f, -0.5f), 0f);
            gesture.Update(new Vector2(0f, -1.9f), 0.4f);

            var released = gesture.TryRelease(new Vector2(0f, -1.9f), 0.5f, out _);

            Assert.That(released, Is.False);
        }

        [Test]
        public void StutterDuringTheDraw_DoesNotCountAsTheDelivery()
        {
            var gesture = NewGesture();
            gesture.Begin(new Vector2(0f, -0.5f), 0f);
            gesture.Update(new Vector2(0f, -1.2f), 0.10f);
            gesture.Update(new Vector2(0f, -1.9f), 0.60f);

            // The delivery is timed from the deepest point, not from the first touch.
            var released = gesture.TryRelease(new Vector2(0f, 0.1f), 0.75f, out var sample);

            Assert.That(released, Is.True);
            Assert.That(sample.Power01, Is.GreaterThan(0.9f));
        }

        [Test]
        public void Cancel_ResetsTheGesture()
        {
            var gesture = NewGesture();
            gesture.Begin(new Vector2(0f, -0.5f), 0f);
            gesture.Update(new Vector2(0f, -1.8f), 0.2f);
            gesture.Cancel();

            Assert.That(gesture.Phase, Is.EqualTo(CueStrokePhase.Idle));
            Assert.That(gesture.Backswing01, Is.EqualTo(0f));
            Assert.That(gesture.TryRelease(new Vector2(0f, 0.5f), 0.3f, out _), Is.False);
        }
    }
}

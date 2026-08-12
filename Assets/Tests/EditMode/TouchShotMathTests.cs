using GyroCue.Input;
using NUnit.Framework;
using UnityEngine;

namespace GyroCue.Tests.EditMode
{
    public sealed class TouchShotMathTests
    {
        [Test]
        public void ResolveAimDirection_UsesFallbackInsideDeadzone()
        {
            var fallback = Vector2.up;
            var resolved = TouchShotMath.ResolveAimDirection(new Vector2(2f, 1f), fallback, deadzonePixels: 6f);

            Assert.That(resolved, Is.EqualTo(fallback));
        }

        [Test]
        public void ResolveAimDirection_UsesDragDirectionOutsideDeadzone()
        {
            var resolved = TouchShotMath.ResolveAimDirection(new Vector2(30f, 0f), Vector2.up, deadzonePixels: 6f);
            Assert.That(resolved, Is.EqualTo(Vector2.right));
        }

        [Test]
        public void CalculatePullDistancePixels_OnlyCountsOpposingMotionAlongAim()
        {
            var gestureStart = new Vector2(100f, 100f);
            var aimDirection = Vector2.right;

            var pullingBack = TouchShotMath.CalculatePullDistancePixels(gestureStart, new Vector2(40f, 100f), aimDirection);
            var pushingForward = TouchShotMath.CalculatePullDistancePixels(gestureStart, new Vector2(180f, 100f), aimDirection);

            Assert.That(pullingBack, Is.EqualTo(60f).Within(0.001f));
            Assert.That(pushingForward, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void EvaluatePower_ClampsAndFollowsCurve()
        {
            var curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            var power = TouchShotMath.EvaluatePower(curve, 0.5f);

            Assert.That(power, Is.GreaterThan(0f));
            Assert.That(power, Is.LessThan(1f));
            Assert.That(TouchShotMath.EvaluatePower(curve, 2f), Is.EqualTo(1f).Within(0.001f));
        }
    }
}

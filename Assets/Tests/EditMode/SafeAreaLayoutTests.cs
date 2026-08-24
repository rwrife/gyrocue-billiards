using GyroCue.UI;
using NUnit.Framework;
using UnityEngine;

namespace GyroCue.Tests.EditMode
{
    public sealed class SafeAreaLayoutTests
    {
        [TestCase(1179f, 2556f, 0f, 102f, 1179f, 2328f)]
        [TestCase(1080f, 2400f, 0f, 80f, 1080f, 2240f)]
        public void PracticeControls_StayInsideRepresentativePhoneSafeAreas(
            float width,
            float height,
            float safeX,
            float safeY,
            float safeWidth,
            float safeHeight)
        {
            var screen = new Vector2(width, height);
            var safeArea = new Rect(safeX, safeY, safeWidth, safeHeight);
            var normalizedSafeArea = SafeAreaLayout.Normalize(safeArea, screen);
            var stroke = SafeAreaLayout.MapLocalViewportRect(
                Practice.PracticeControlLayout.StrokeWidgetInSafeArea,
                safeArea,
                screen);
            var elevation = SafeAreaLayout.MapLocalViewportRect(
                Practice.PracticeControlLayout.ElevationStripInSafeArea,
                safeArea,
                screen);

            Assert.That(Contains(normalizedSafeArea, stroke), Is.True);
            Assert.That(Contains(normalizedSafeArea, elevation), Is.True);
            Assert.That(stroke.Overlaps(elevation), Is.False);
        }

        [Test]
        public void MapLocalViewportRect_ClampsOversizedRectToSafeArea()
        {
            var screen = new Vector2(1000f, 2000f);
            var safeArea = new Rect(0f, 100f, 1000f, 1800f);
            var normalizedSafeArea = SafeAreaLayout.Normalize(safeArea, screen);
            var mapped = SafeAreaLayout.MapLocalViewportRect(
                new Rect(0.9f, 0.8f, 0.5f, 0.6f),
                safeArea,
                screen);

            Assert.That(Contains(normalizedSafeArea, mapped), Is.True);
            Assert.That(mapped.width, Is.EqualTo(normalizedSafeArea.width * 0.1f).Within(0.0001f));
            Assert.That(mapped.height, Is.EqualTo(normalizedSafeArea.height * 0.2f).Within(0.0001f));
        }

        [Test]
        public void Normalize_InvalidScreenSize_FallsBackToFullViewport()
        {
            Assert.That(
                SafeAreaLayout.Normalize(new Rect(5f, 5f, 1f, 1f), Vector2.zero),
                Is.EqualTo(new Rect(0f, 0f, 1f, 1f)));
        }

        private static bool Contains(Rect outer, Rect inner)
        {
            const float tolerance = 0.0001f;
            return inner.xMin >= outer.xMin - tolerance &&
                   inner.yMin >= outer.yMin - tolerance &&
                   inner.xMax <= outer.xMax + tolerance &&
                   inner.yMax <= outer.yMax + tolerance;
        }
    }
}

using GyroCue.Practice;
using NUnit.Framework;
using UnityEngine;

namespace GyroCue.Tests.EditMode
{
    public sealed class PracticeControlLayoutTests
    {
        [Test]
        public void StrokeWidgetAndElevationStrip_DoNotOverlap()
        {
            Assert.That(
                PracticeControlLayout.StrokeWidget.Overlaps(PracticeControlLayout.ElevationStrip),
                Is.False,
                "A drag would be ambiguous where the two widgets overlap.");
        }

        [Test]
        public void TopOfTheStrokeWidget_MapsToTheTopOfTheBall()
        {
            var top = new Vector2(
                Screen.width * PracticeControlLayout.StrokeWidget.center.x,
                Screen.height * PracticeControlLayout.StrokeWidget.yMax);

            var face = PracticeControlLayout.ToFacePosition(top);

            Assert.That(face.y, Is.EqualTo(PracticeControlLayout.FaceTop).Within(0.02f));
            Assert.That(face.x, Is.EqualTo(0f).Within(0.02f));
        }

        [Test]
        public void BottomOfTheStrokeWidget_IsBackswingRoomBelowTheBall()
        {
            var bottom = new Vector2(
                Screen.width * PracticeControlLayout.StrokeWidget.center.x,
                Screen.height * PracticeControlLayout.StrokeWidget.yMin);

            var face = PracticeControlLayout.ToFacePosition(bottom);

            Assert.That(face.y, Is.EqualTo(PracticeControlLayout.FaceBottom).Within(0.02f));
            Assert.That(face.y, Is.LessThan(-1f), "There must be room below the ball to draw the cue back.");
        }

        [Test]
        public void ElevationStrip_RunsFromLevelCueToTheMaximum()
        {
            var strip = PracticeControlLayout.ElevationStrip;
            var low = new Vector2(Screen.width * strip.center.x, Screen.height * strip.yMin);
            var high = new Vector2(Screen.width * strip.center.x, Screen.height * strip.yMax);

            Assert.That(PracticeControlLayout.ToElevationDegrees(low), Is.EqualTo(0f).Within(0.5f));
            Assert.That(
                PracticeControlLayout.ToElevationDegrees(high),
                Is.EqualTo(PracticeControlLayout.MaximumElevationDegrees).Within(0.5f));
        }
    }
}

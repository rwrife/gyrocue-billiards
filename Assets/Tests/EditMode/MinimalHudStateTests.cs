using GyroCue.UI;
using NUnit.Framework;

namespace GyroCue.Tests.EditMode
{
    public sealed class MinimalHudStateTests
    {
        [Test]
        public void ResetMatch_PlayerOneStart_UsesBaselineTurnAndStatus()
        {
            var hudState = new MinimalHudState();

            hudState.ResetMatch(playerOneStarts: true);
            var snapshot = hudState.Snapshot;

            Assert.That(snapshot.TurnText, Is.EqualTo("Turn 1 • Player 1"));
            Assert.That(snapshot.PowerText, Is.EqualTo("Power 0%"));
            Assert.That(snapshot.StatusText, Is.EqualTo("Line up your shot."));
            Assert.That(snapshot.StatusTone, Is.EqualTo(HudStatusTone.Info));
            Assert.That(snapshot.IsTerminal, Is.False);
        }

        [Test]
        public void ApplyTurnResolution_FoulWithCueBallInHand_ShowsWarningStatus()
        {
            var hudState = new MinimalHudState();
            hudState.ResetMatch(playerOneStarts: true);

            hudState.ApplyTurnResolution(
                keepTurn: false,
                committedFoul: true,
                wonMatch: false,
                lostMatch: false,
                requiresCueBallPlacement: true,
                resultingPlayerIndex: 1,
                resultingTurnNumber: 2);

            var snapshot = hudState.Snapshot;
            Assert.That(snapshot.TurnText, Is.EqualTo("Turn 2 • Player 2"));
            Assert.That(snapshot.StatusText, Is.EqualTo("Foul. Player 2 places cue ball."));
            Assert.That(snapshot.StatusTone, Is.EqualTo(HudStatusTone.Warning));
            Assert.That(snapshot.IsTerminal, Is.False);
        }

        [Test]
        public void ApplyTurnResolution_Win_ShowsTerminalSuccessAndResetsPower()
        {
            var hudState = new MinimalHudState();
            hudState.ResetMatch(playerOneStarts: true);
            hudState.SetPreviewPower(0.73f);

            hudState.ApplyTurnResolution(
                keepTurn: false,
                committedFoul: false,
                wonMatch: true,
                lostMatch: false,
                requiresCueBallPlacement: false,
                resultingPlayerIndex: 0,
                resultingTurnNumber: 3);

            var snapshot = hudState.Snapshot;
            Assert.That(snapshot.StatusText, Is.EqualTo("Player 1 wins the rack."));
            Assert.That(snapshot.StatusTone, Is.EqualTo(HudStatusTone.Success));
            Assert.That(snapshot.PowerText, Is.EqualTo("Power 0%"));
            Assert.That(snapshot.IsTerminal, Is.True);
        }

        [Test]
        public void ResolveScaleFactor_ClampsAcrossCommonPhoneSizes()
        {
            var compactPhoneScale = HudScaleUtility.ResolveScaleFactor(720f, 1280f);
            var baselinePhoneScale = HudScaleUtility.ResolveScaleFactor(1080f, 1920f);
            var highDensityPhoneScale = HudScaleUtility.ResolveScaleFactor(1440f, 3200f);

            Assert.That(compactPhoneScale, Is.EqualTo(0.85f).Within(0.0001f));
            Assert.That(baselinePhoneScale, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(highDensityPhoneScale, Is.EqualTo(1.3f).Within(0.0001f));
            Assert.That(HudScaleUtility.ResolveScaledFontSize(30, compactPhoneScale), Is.EqualTo(26));
            Assert.That(HudScaleUtility.ResolveScaledFontSize(30, highDensityPhoneScale), Is.EqualTo(39));
        }
    }
}

using GyroCue.UI;
using NUnit.Framework;

namespace GyroCue.Tests.EditMode
{
    public sealed class GameFlowStateTests
    {
        [Test]
        public void PracticeMode_StartPauseResumeAndReturn_FollowValidTransitions()
        {
            var flow = new GameFlowState();

            Assert.That(flow.SelectedMode, Is.EqualTo(GameMode.Practice));
            Assert.That(flow.Phase, Is.EqualTo(GameFlowPhase.Title));
            Assert.That(flow.StartSelectedMode(), Is.True);
            Assert.That(flow.Phase, Is.EqualTo(GameFlowPhase.Playing));
            Assert.That(flow.Pause(), Is.True);
            Assert.That(flow.Phase, Is.EqualTo(GameFlowPhase.Paused));
            Assert.That(flow.Resume(), Is.True);
            Assert.That(flow.Phase, Is.EqualTo(GameFlowPhase.Playing));
            Assert.That(flow.ReturnToTitle(), Is.True);
            Assert.That(flow.Phase, Is.EqualTo(GameFlowPhase.Title));
        }

        [Test]
        public void Restart_FromPause_ResumesAndAdvancesSessionRevision()
        {
            var flow = new GameFlowState();
            flow.StartSelectedMode();
            flow.Pause();

            Assert.That(flow.Restart(), Is.True);
            Assert.That(flow.Phase, Is.EqualTo(GameFlowPhase.Playing));
            Assert.That(flow.SessionRevision, Is.EqualTo(1));
        }

        [Test]
        public void InvalidTransitions_AreRejectedWithoutChangingState()
        {
            var flow = new GameFlowState();

            Assert.That(flow.Pause(), Is.False);
            Assert.That(flow.Resume(), Is.False);
            Assert.That(flow.Restart(), Is.False);
            Assert.That(flow.Phase, Is.EqualTo(GameFlowPhase.Title));
        }

        [Test]
        public void ModeSelection_OnlyOffersImplementedPracticeMode()
        {
            var flow = new GameFlowState();

            Assert.That(flow.SelectMode(GameMode.Practice), Is.True);
            Assert.That(GameModeCatalog.IsAvailable(GameMode.Practice), Is.True);
            Assert.That(GameModeCatalog.SceneName(GameMode.Practice), Is.EqualTo("Practice"));
            Assert.That(GameModeCatalog.DisplayName(GameMode.Practice), Is.EqualTo("PRACTICE"));
            Assert.That(GameModeCatalog.FromPersistedValue(999), Is.EqualTo(GameMode.Practice));
        }
    }
}

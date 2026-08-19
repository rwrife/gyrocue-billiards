using GyroCue.UI;
using NUnit.Framework;

namespace GyroCue.Tests.EditMode
{
    public sealed class TitleScreenCopyTests
    {
        [Test]
        public void ResolveStartPrompt_SceneAvailable_NamesGameplayScene()
        {
            var prompt = TitleScreenCopy.ResolveStartPrompt(gameplaySceneAvailable: true, "MainTable");

            Assert.That(prompt, Is.EqualTo("Tap PLAY to open MainTable."));
        }

        [Test]
        public void ResolveStartPrompt_SceneMissing_ExplainsBuildSettingsGap()
        {
            var prompt = TitleScreenCopy.ResolveStartPrompt(gameplaySceneAvailable: false, "MainTable");

            Assert.That(prompt, Is.EqualTo(TitleScreenCopy.MissingSceneHint));
        }

        [Test]
        public void ResolveStartPrompt_BlankSceneName_FallsBackToGenericLabel()
        {
            var prompt = TitleScreenCopy.ResolveStartPrompt(gameplaySceneAvailable: true, "   ");

            Assert.That(prompt, Is.EqualTo("Tap PLAY to open the table."));
        }
    }
}

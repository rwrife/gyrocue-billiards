using GyroCue.Practice;
using NUnit.Framework;
using UnityEngine;

namespace GyroCue.Tests.EditMode
{
    public sealed class PracticeFeedbackSettingsTests
    {
        [TearDown]
        public void ClearPreferences()
        {
            PlayerPrefs.DeleteKey(PracticeFeedbackPreferences.SoundPreferenceKey);
            PlayerPrefs.DeleteKey(PracticeFeedbackPreferences.HapticsPreferenceKey);
            PlayerPrefs.DeleteKey(PracticeFeedbackPreferences.ReducedFeedbackPreferenceKey);
        }

        [Test]
        public void ReducedFeedback_DisablesHapticsWithoutForgettingThePreference()
        {
            var options = new PracticeFeedbackOptions(
                soundEnabled: true,
                hapticsEnabled: true,
                reducedFeedback: true);

            Assert.That(options.HapticsEnabled, Is.True);
            Assert.That(options.HapticsAllowed, Is.False);
            Assert.That(options.NonessentialVisualFeedbackAllowed, Is.False);
            Assert.That(options.WithReducedFeedback(false).HapticsAllowed, Is.True);
        }

        [Test]
        public void Preferences_RoundTripAllThreeToggles()
        {
            var expected = new PracticeFeedbackOptions(
                soundEnabled: false,
                hapticsEnabled: true,
                reducedFeedback: true);

            PracticeFeedbackPreferences.Save(expected);
            var loaded = PracticeFeedbackPreferences.Load();

            Assert.That(loaded.SoundEnabled, Is.EqualTo(expected.SoundEnabled));
            Assert.That(loaded.HapticsEnabled, Is.EqualTo(expected.HapticsEnabled));
            Assert.That(loaded.ReducedFeedback, Is.EqualTo(expected.ReducedFeedback));
            Assert.That(loaded.HapticsAllowed, Is.False);
        }

        [Test]
        public void Mix_ScalesStrikeWithPowerAndKeepsContactFeedbackQuieter()
        {
            var softStrike = PracticeFeedbackMix.ResolveVolume(PracticeFeedbackCue.CueStrike, 0.1f);
            var hardStrike = PracticeFeedbackMix.ResolveVolume(PracticeFeedbackCue.CueStrike, 1f);
            var ballContact = PracticeFeedbackMix.ResolveVolume(PracticeFeedbackCue.BallCollision, 1f);
            var cushion = PracticeFeedbackMix.ResolveVolume(PracticeFeedbackCue.CushionHit, 1f);

            Assert.That(hardStrike, Is.GreaterThan(softStrike));
            Assert.That(hardStrike, Is.LessThanOrEqualTo(0.8f));
            Assert.That(ballContact, Is.LessThan(hardStrike));
            Assert.That(cushion, Is.LessThan(hardStrike));
        }

        [Test]
        public void Debouncer_RejectsBreakSpamButAllowsLaterContacts()
        {
            var debouncer = new PracticeFeedbackDebouncer();

            Assert.That(debouncer.TryAccept(PracticeFeedbackCue.BallCollision, 10f), Is.True);
            Assert.That(debouncer.TryAccept(PracticeFeedbackCue.BallCollision, 10.01f), Is.False);
            Assert.That(debouncer.TryAccept(PracticeFeedbackCue.CushionHit, 10.01f), Is.True);
            Assert.That(debouncer.TryAccept(PracticeFeedbackCue.BallCollision, 10.08f), Is.True);
        }

        [Test]
        public void HapticDebouncer_RejectsRapidPocketBursts()
        {
            var debouncer = new PracticeFeedbackDebouncer();

            Assert.That(debouncer.TryAcceptHaptic(20f), Is.True);
            Assert.That(debouncer.TryAcceptHaptic(20.05f), Is.False);
            Assert.That(debouncer.TryAcceptHaptic(20.20f), Is.True);
        }

        [Test]
        public void LandingState_ReportsOnlyAnAirborneToGroundedTransition()
        {
            var state = new LandingFeedbackState();

            Assert.That(state.Observe(isGrounded: true, verticalVelocity: 0f, out _), Is.False);
            Assert.That(state.Observe(isGrounded: false, verticalVelocity: -1.5f, out _), Is.False);
            Assert.That(state.Observe(isGrounded: false, verticalVelocity: -2.5f, out _), Is.False);
            Assert.That(state.Observe(isGrounded: true, verticalVelocity: -0.1f, out var intensity), Is.True);
            Assert.That(intensity, Is.GreaterThan(0.2f));
            Assert.That(state.Observe(isGrounded: true, verticalVelocity: 0f, out _), Is.False);
        }

        [TestCase(true, "Ball7", PracticeFeedbackCue.BallCollision, true)]
        [TestCase(false, "RailLongFarA", PracticeFeedbackCue.CushionHit, true)]
        [TestCase(false, "Slate", PracticeFeedbackCue.CushionHit, false)]
        public void ContactClassifier_DistinguishesBallsAndCushions(
            bool otherIsBall,
            string otherName,
            PracticeFeedbackCue expected,
            bool shouldClassify)
        {
            var classified = PracticeContactClassifier.TryClassify(otherIsBall, otherName, out var cue);

            Assert.That(classified, Is.EqualTo(shouldClassify));
            if (shouldClassify)
            {
                Assert.That(cue, Is.EqualTo(expected));
            }
        }
    }
}

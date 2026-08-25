using System;
using UnityEngine;

namespace GyroCue.Practice
{
    public enum PracticeFeedbackCue
    {
        CueStrike = 0,
        BallCollision = 1,
        CushionHit = 2,
        Pocket = 3,
        Miscue = 4,
        Landing = 5
    }

    /// <summary>Locally persisted, non-sensitive practice feedback choices.</summary>
    public readonly struct PracticeFeedbackOptions
    {
        public PracticeFeedbackOptions(bool soundEnabled, bool hapticsEnabled, bool reducedFeedback)
        {
            SoundEnabled = soundEnabled;
            HapticsEnabled = hapticsEnabled;
            ReducedFeedback = reducedFeedback;
        }

        public bool SoundEnabled { get; }

        /// <summary>The user's stored haptics preference, retained while reduced feedback is active.</summary>
        public bool HapticsEnabled { get; }

        public bool ReducedFeedback { get; }

        public bool HapticsAllowed => HapticsEnabled && !ReducedFeedback;

        public bool NonessentialVisualFeedbackAllowed => !ReducedFeedback;

        public PracticeFeedbackOptions WithSound(bool enabled)
        {
            return new PracticeFeedbackOptions(enabled, HapticsEnabled, ReducedFeedback);
        }

        public PracticeFeedbackOptions WithHaptics(bool enabled)
        {
            return new PracticeFeedbackOptions(SoundEnabled, enabled, ReducedFeedback);
        }

        public PracticeFeedbackOptions WithReducedFeedback(bool enabled)
        {
            return new PracticeFeedbackOptions(SoundEnabled, HapticsEnabled, enabled);
        }
    }

    public static class PracticeFeedbackPreferences
    {
        public const string SoundPreferenceKey = "gyrocue.feedback.sound";
        public const string HapticsPreferenceKey = "gyrocue.feedback.haptics";
        public const string ReducedFeedbackPreferenceKey = "gyrocue.feedback.reduced";

        public static PracticeFeedbackOptions Load()
        {
            return new PracticeFeedbackOptions(
                PlayerPrefs.GetInt(SoundPreferenceKey, 1) != 0,
                PlayerPrefs.GetInt(HapticsPreferenceKey, 1) != 0,
                PlayerPrefs.GetInt(ReducedFeedbackPreferenceKey, 0) != 0);
        }

        public static void Save(PracticeFeedbackOptions options)
        {
            PlayerPrefs.SetInt(SoundPreferenceKey, options.SoundEnabled ? 1 : 0);
            PlayerPrefs.SetInt(HapticsPreferenceKey, options.HapticsEnabled ? 1 : 0);
            PlayerPrefs.SetInt(ReducedFeedbackPreferenceKey, options.ReducedFeedback ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    /// <summary>Volume balancing shared by runtime playback and deterministic tests.</summary>
    public static class PracticeFeedbackMix
    {
        public static float ResolveVolume(PracticeFeedbackCue cue, float intensity01)
        {
            var intensity = Mathf.Clamp01(intensity01);
            switch (cue)
            {
                case PracticeFeedbackCue.CueStrike:
                    return Mathf.Lerp(0.18f, 0.78f, intensity);
                case PracticeFeedbackCue.BallCollision:
                    return Mathf.Lerp(0.08f, 0.42f, intensity);
                case PracticeFeedbackCue.CushionHit:
                    return Mathf.Lerp(0.06f, 0.34f, intensity);
                case PracticeFeedbackCue.Pocket:
                    return Mathf.Lerp(0.34f, 0.56f, intensity);
                case PracticeFeedbackCue.Miscue:
                    return Mathf.Lerp(0.32f, 0.52f, intensity);
                case PracticeFeedbackCue.Landing:
                    return Mathf.Lerp(0.06f, 0.28f, intensity);
                default:
                    return 0f;
            }
        }
    }

    /// <summary>Per-cue debounce prevents dense breaks from becoming audio spam.</summary>
    public sealed class PracticeFeedbackDebouncer
    {
        private readonly float[] lastAcceptedTimes = new float[6];
        private readonly bool[] hasAccepted = new bool[6];
        private float lastAcceptedHapticTime;
        private bool hasAcceptedHaptic;

        public bool TryAccept(PracticeFeedbackCue cue, float timeSeconds)
        {
            var index = (int)cue;
            if (index < 0 || index >= lastAcceptedTimes.Length)
            {
                return false;
            }

            var interval = MinimumInterval(cue);
            if (hasAccepted[index] && timeSeconds - lastAcceptedTimes[index] < interval)
            {
                return false;
            }

            hasAccepted[index] = true;
            lastAcceptedTimes[index] = timeSeconds;
            return true;
        }

        public bool TryAcceptHaptic(float timeSeconds)
        {
            const float minimumHapticInterval = 0.15f;
            if (hasAcceptedHaptic && timeSeconds - lastAcceptedHapticTime < minimumHapticInterval)
            {
                return false;
            }

            hasAcceptedHaptic = true;
            lastAcceptedHapticTime = timeSeconds;
            return true;
        }

        private static float MinimumInterval(PracticeFeedbackCue cue)
        {
            switch (cue)
            {
                case PracticeFeedbackCue.BallCollision:
                    return 0.06f;
                case PracticeFeedbackCue.CushionHit:
                    return 0.08f;
                case PracticeFeedbackCue.Landing:
                    return 0.10f;
                default:
                    return 0.02f;
            }
        }
    }

    /// <summary>Tracks a real airborne interval and converts its fall speed into landing intensity.</summary>
    public sealed class LandingFeedbackState
    {
        private bool initialized;
        private bool wasGrounded;
        private float peakFallSpeed;

        public bool Observe(bool isGrounded, float verticalVelocity, out float intensity01)
        {
            intensity01 = 0f;
            if (!initialized)
            {
                initialized = true;
                wasGrounded = isGrounded;
                return false;
            }

            if (!isGrounded)
            {
                peakFallSpeed = Mathf.Max(peakFallSpeed, Mathf.Max(0f, -verticalVelocity));
                wasGrounded = false;
                return false;
            }

            if (wasGrounded)
            {
                return false;
            }

            wasGrounded = true;
            intensity01 = Mathf.Clamp01(peakFallSpeed / 3f);
            peakFallSpeed = 0f;
            return intensity01 > 0.05f;
        }
    }

    public static class PracticeContactClassifier
    {
        public static bool TryClassify(bool otherIsBall, string otherName, out PracticeFeedbackCue cue)
        {
            if (otherIsBall)
            {
                cue = PracticeFeedbackCue.BallCollision;
                return true;
            }

            if (!string.IsNullOrEmpty(otherName) && otherName.StartsWith("Rail", StringComparison.Ordinal))
            {
                cue = PracticeFeedbackCue.CushionHit;
                return true;
            }

            cue = default;
            return false;
        }
    }
}

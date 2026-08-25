using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GyroCue.Practice
{
    /// <summary>
    /// Mobile-friendly practice feedback. Short procedural clips keep the feature
    /// source-controlled, while the settings remain optional and safe on unsupported
    /// platforms.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PracticeFeedbackController : MonoBehaviour
    {
        private const int SampleRate = 22050;

        private readonly Dictionary<PracticeFeedbackCue, AudioClip> clips =
            new Dictionary<PracticeFeedbackCue, AudioClip>();
        private readonly List<PracticeCollisionFeedbackReporter> reporters =
            new List<PracticeCollisionFeedbackReporter>();
        private readonly List<ClothContactMotion> motions = new List<ClothContactMotion>();
        private readonly List<PracticePocket> pockets = new List<PracticePocket>();
        private readonly PracticeFeedbackDebouncer debouncer = new PracticeFeedbackDebouncer();
        private readonly PracticeFeedbackDebouncer visualDebouncer = new PracticeFeedbackDebouncer();

        private PracticeSessionController session;
        private AudioSource audioSource;
        private Image flashImage;
        private Color flashColor = Color.white;

        public event Action OptionsChanged;

        public PracticeFeedbackOptions Options { get; private set; }

        public int AvailableCueCount => clips.Count;

        public float CurrentVisualIntensity { get; private set; }

        private void Awake()
        {
            Options = PracticeFeedbackPreferences.Load();
            audioSource = gameObject.GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
            BuildProceduralClips();
            BuildVisualOverlay();
        }

        public void Configure(
            PracticeSessionController sessionController,
            IReadOnlyList<Rigidbody> balls,
            IReadOnlyList<PracticePocket> practicePockets)
        {
            Unsubscribe();
            session = sessionController;
            if (session != null)
            {
                session.ShotFeedbackRequested += HandleShotFeedback;
            }

            if (balls != null)
            {
                for (var i = 0; i < balls.Count; i++)
                {
                    var body = balls[i];
                    if (body == null)
                    {
                        continue;
                    }

                    var reporter = body.GetComponent<PracticeCollisionFeedbackReporter>();
                    if (reporter != null)
                    {
                        reporter.ContactFeedback += HandleContactFeedback;
                        reporters.Add(reporter);
                    }

                    var motion = body.GetComponent<ClothContactMotion>();
                    if (motion != null)
                    {
                        motion.Landed += HandleLanding;
                        motions.Add(motion);
                    }
                }
            }

            if (practicePockets == null)
            {
                return;
            }

            for (var i = 0; i < practicePockets.Count; i++)
            {
                var pocket = practicePockets[i];
                if (pocket == null)
                {
                    continue;
                }

                pocket.BallEntered += HandlePocket;
                pockets.Add(pocket);
            }
        }

        public void SetSoundEnabled(bool enabled)
        {
            UpdateOptions(Options.WithSound(enabled));
        }

        public void SetHapticsEnabled(bool enabled)
        {
            UpdateOptions(Options.WithHaptics(enabled));
        }

        public void SetReducedFeedback(bool enabled)
        {
            UpdateOptions(Options.WithReducedFeedback(enabled));
        }

        private void UpdateOptions(PracticeFeedbackOptions updated)
        {
            Options = updated;
            if (!Options.NonessentialVisualFeedbackAllowed)
            {
                ClearVisualPulse();
            }

            PracticeFeedbackPreferences.Save(Options);
            OptionsChanged?.Invoke();
        }

        private void HandleShotFeedback(CueStrikeResult result, float power01)
        {
            PlaySound(PracticeFeedbackCue.CueStrike, power01);
            TryPulseVisual(
                result.IsMiscue ? PracticeFeedbackCue.Miscue : PracticeFeedbackCue.CueStrike,
                power01);
            if (result.IsMiscue)
            {
                PlaySound(PracticeFeedbackCue.Miscue, Mathf.Max(0.35f, power01));
                VibrateIfAllowed();
                return;
            }

            VibrateIfAllowed();
        }

        private void HandleContactFeedback(PracticeFeedbackCue cue, float intensity01)
        {
            PlaySound(cue, intensity01);
            TryPulseVisual(cue, intensity01);
        }

        private void HandlePocket(Rigidbody body, PracticePocket pocket)
        {
            if (body == null)
            {
                return;
            }

            PlaySound(PracticeFeedbackCue.Pocket, 1f);
            TryPulseVisual(PracticeFeedbackCue.Pocket, 1f);
            VibrateIfAllowed();
        }

        private void HandleLanding(float intensity01)
        {
            PlaySound(PracticeFeedbackCue.Landing, intensity01);
            TryPulseVisual(PracticeFeedbackCue.Landing, intensity01);
        }

        private void PlaySound(PracticeFeedbackCue cue, float intensity01)
        {
            if (!Options.SoundEnabled || audioSource == null ||
                !clips.TryGetValue(cue, out var clip) ||
                !debouncer.TryAccept(cue, Time.unscaledTime))
            {
                return;
            }

            audioSource.PlayOneShot(clip, PracticeFeedbackMix.ResolveVolume(cue, intensity01));
        }

        public bool TryPulseVisual(PracticeFeedbackCue cue, float intensity01)
        {
            if (!Options.NonessentialVisualFeedbackAllowed || flashImage == null ||
                !visualDebouncer.TryAccept(cue, Time.unscaledTime))
            {
                return false;
            }

            flashColor = ResolveFlashColor(cue);
            CurrentVisualIntensity = Mathf.Max(
                CurrentVisualIntensity,
                Mathf.Lerp(0.025f, 0.14f, Mathf.Clamp01(intensity01)));
            ApplyVisualPulse();
            return true;
        }

        private void Update()
        {
            if (CurrentVisualIntensity <= 0f)
            {
                return;
            }

            CurrentVisualIntensity = Mathf.Max(
                0f,
                CurrentVisualIntensity - (Time.unscaledDeltaTime * 0.8f));
            ApplyVisualPulse();
        }

        private void ClearVisualPulse()
        {
            CurrentVisualIntensity = 0f;
            ApplyVisualPulse();
        }

        private void ApplyVisualPulse()
        {
            if (flashImage != null)
            {
                flashImage.color = new Color(
                    flashColor.r,
                    flashColor.g,
                    flashColor.b,
                    CurrentVisualIntensity);
            }
        }

        private void VibrateIfAllowed()
        {
            if (!Options.HapticsAllowed || !debouncer.TryAcceptHaptic(Time.unscaledTime))
            {
                return;
            }

#if UNITY_IOS || UNITY_ANDROID
            if (Application.isMobilePlatform)
            {
                Handheld.Vibrate();
            }
#endif
        }

        private void BuildVisualOverlay()
        {
            var canvasObject = new GameObject(
                "PracticeFeedbackVisual",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, worldPositionStays: false);

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = -100;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            var flashObject = new GameObject("FeedbackFlash", typeof(RectTransform), typeof(Image));
            flashObject.transform.SetParent(canvasObject.transform, worldPositionStays: false);
            var flashRect = (RectTransform)flashObject.transform;
            flashRect.anchorMin = Vector2.zero;
            flashRect.anchorMax = Vector2.one;
            flashRect.offsetMin = Vector2.zero;
            flashRect.offsetMax = Vector2.zero;

            flashImage = flashObject.GetComponent<Image>();
            flashImage.raycastTarget = false;
            ApplyVisualPulse();
        }

        private static Color ResolveFlashColor(PracticeFeedbackCue cue)
        {
            switch (cue)
            {
                case PracticeFeedbackCue.BallCollision:
                    return new Color(0.55f, 0.85f, 1f);
                case PracticeFeedbackCue.CushionHit:
                    return new Color(0.48f, 0.90f, 0.62f);
                case PracticeFeedbackCue.Pocket:
                    return new Color(1f, 0.72f, 0.25f);
                case PracticeFeedbackCue.Miscue:
                    return new Color(1f, 0.28f, 0.22f);
                case PracticeFeedbackCue.Landing:
                    return new Color(0.72f, 0.80f, 1f);
                default:
                    return Color.white;
            }
        }

        private void BuildProceduralClips()
        {
            clips[PracticeFeedbackCue.CueStrike] = CreateTone("CueStrike", 860f, 0.055f, 0.15f);
            clips[PracticeFeedbackCue.BallCollision] = CreateTone("BallCollision", 1320f, 0.035f, 0.05f);
            clips[PracticeFeedbackCue.CushionHit] = CreateTone("CushionHit", 320f, 0.075f, 0.30f);
            clips[PracticeFeedbackCue.Pocket] = CreateTone("Pocket", 170f, 0.16f, 0.55f);
            clips[PracticeFeedbackCue.Miscue] = CreateTone("Miscue", 115f, 0.13f, 0.85f);
            clips[PracticeFeedbackCue.Landing] = CreateTone("Landing", 230f, 0.08f, 0.45f);
        }

        private static AudioClip CreateTone(string name, float frequency, float duration, float roughness)
        {
            var sampleCount = Mathf.Max(1, Mathf.RoundToInt(SampleRate * duration));
            var samples = new float[sampleCount];
            var phase = 0f;
            var phaseStep = (Mathf.PI * 2f * frequency) / SampleRate;

            for (var i = 0; i < sampleCount; i++)
            {
                var progress = i / (float)sampleCount;
                var envelope = (1f - progress) * (1f - progress);
                var harmonic = Mathf.Sin(phase) + (Mathf.Sin(phase * 1.73f) * roughness);
                samples[i] = harmonic * envelope * 0.45f;
                phase += phaseStep;
            }

            var clip = AudioClip.Create(name, sampleCount, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void OnDestroy()
        {
            Unsubscribe();
            foreach (var clip in clips.Values)
            {
                if (clip != null)
                {
                    Destroy(clip);
                }
            }
        }

        private void Unsubscribe()
        {
            if (session != null)
            {
                session.ShotFeedbackRequested -= HandleShotFeedback;
                session = null;
            }

            for (var i = 0; i < reporters.Count; i++)
            {
                if (reporters[i] != null)
                {
                    reporters[i].ContactFeedback -= HandleContactFeedback;
                }
            }
            reporters.Clear();

            for (var i = 0; i < motions.Count; i++)
            {
                if (motions[i] != null)
                {
                    motions[i].Landed -= HandleLanding;
                }
            }
            motions.Clear();

            for (var i = 0; i < pockets.Count; i++)
            {
                if (pockets[i] != null)
                {
                    pockets[i].BallEntered -= HandlePocket;
                }
            }
            pockets.Clear();
        }
    }
}

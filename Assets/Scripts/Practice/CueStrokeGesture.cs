using UnityEngine;

namespace GyroCue.Practice
{
    public enum CueStrokePhase
    {
        Idle = 0,
        DrawingBack = 1,
        StrokingForward = 2
    }

    /// <summary>
    /// A completed stroke: how hard, and where on the ball face the tip stopped.
    /// </summary>
    public readonly struct CueStrokeSample
    {
        public CueStrokeSample(float power01, Vector2 strikeOffset, float strokeSpeed)
        {
            Power01 = Mathf.Clamp01(power01);
            StrikeOffset = strikeOffset;
            StrokeSpeed = strokeSpeed;
        }

        public float Power01 { get; }

        public Vector2 StrikeOffset { get; }

        /// <summary>Raw forward speed in ball-face units per second, before normalising.</summary>
        public float StrokeSpeed { get; }
    }

    /// <summary>
    /// The stroke gesture, driven in ball-face coordinates: the drag area is the cue
    /// ball itself, where (0,0) is dead centre and y = +1/-1 are the top and bottom
    /// edges. Below the ball is backswing room.
    ///
    /// Draw down to pull the cue back, then slide up to stroke. How fast you slide
    /// sets the power; where you stop sets the strike height, so a short jab low on
    /// the ball draws and a fast stroke through the top follows.
    ///
    /// Pure and frame-rate independent so stroke feel can be tuned under test.
    /// </summary>
    public sealed class CueStrokeGesture
    {
        private readonly float minimumBackswing;
        private readonly float referenceStrokeSpeed;
        private readonly float minimumPower01;

        private Vector2 currentFacePosition;
        private float deepestBackswingY;
        private float forwardStartY;
        private float forwardStartTime;
        private bool hasForwardMotion;

        public CueStrokeGesture(
            float minimumBackswing = 0.35f,
            float referenceStrokeSpeed = 9f,
            float minimumPower01 = 0.05f)
        {
            this.minimumBackswing = Mathf.Max(0.01f, minimumBackswing);
            this.referenceStrokeSpeed = Mathf.Max(0.01f, referenceStrokeSpeed);
            this.minimumPower01 = Mathf.Clamp01(minimumPower01);
        }

        public CueStrokePhase Phase { get; private set; } = CueStrokePhase.Idle;

        /// <summary>How far the cue is drawn back, 0 to 1, for the cue stick view.</summary>
        public float Backswing01 { get; private set; }

        /// <summary>Live power estimate during the forward stroke, for the HUD.</summary>
        public float PreviewPower01 { get; private set; }

        /// <summary>Where the tip currently sits on the ball face.</summary>
        public Vector2 StrikeOffset => new Vector2(
            Mathf.Clamp(currentFacePosition.x, -1f, 1f),
            Mathf.Clamp(currentFacePosition.y, -1f, 1f));

        public bool HasUsableBackswing => Backswing01 >= minimumBackswing;

        public void Begin(Vector2 facePosition, float timeSeconds)
        {
            Phase = CueStrokePhase.DrawingBack;
            currentFacePosition = facePosition;
            deepestBackswingY = facePosition.y;
            forwardStartY = facePosition.y;
            forwardStartTime = timeSeconds;
            hasForwardMotion = false;
            Backswing01 = 0f;
            PreviewPower01 = 0f;
        }

        public void Update(Vector2 facePosition, float timeSeconds)
        {
            if (Phase == CueStrokePhase.Idle)
            {
                return;
            }

            currentFacePosition = facePosition;

            if (facePosition.y <= deepestBackswingY)
            {
                // Still drawing back. Reset the forward stroke origin so a stutter
                // mid-pull does not count as the start of the delivery.
                deepestBackswingY = facePosition.y;
                forwardStartY = facePosition.y;
                forwardStartTime = timeSeconds;
                hasForwardMotion = false;
                Phase = CueStrokePhase.DrawingBack;
                Backswing01 = ResolveBackswing(deepestBackswingY);
                PreviewPower01 = 0f;
                return;
            }

            hasForwardMotion = true;
            Phase = CueStrokePhase.StrokingForward;
            PreviewPower01 = ResolvePower(facePosition.y, timeSeconds);
        }

        public bool TryRelease(Vector2 facePosition, float timeSeconds, out CueStrokeSample sample)
        {
            sample = default;

            if (Phase == CueStrokePhase.Idle)
            {
                return false;
            }

            Update(facePosition, timeSeconds);

            var strokeSpeed = ResolveStrokeSpeed(facePosition.y, timeSeconds);
            var power = ResolvePower(facePosition.y, timeSeconds);
            var releasedCleanly = hasForwardMotion && HasUsableBackswing && power >= minimumPower01;

            if (releasedCleanly)
            {
                sample = new CueStrokeSample(power, StrikeOffset, strokeSpeed);
            }

            Cancel();
            return releasedCleanly;
        }

        public void Cancel()
        {
            Phase = CueStrokePhase.Idle;
            Backswing01 = 0f;
            PreviewPower01 = 0f;
            hasForwardMotion = false;
        }

        private float ResolveBackswing(float deepestY)
        {
            // The backswing zone runs from the bottom of the ball downward.
            return Mathf.Clamp01(-1f - deepestY);
        }

        private float ResolveStrokeSpeed(float releaseY, float timeSeconds)
        {
            var elapsed = timeSeconds - forwardStartTime;
            if (!hasForwardMotion || elapsed <= 0f)
            {
                return 0f;
            }

            var travelled = releaseY - forwardStartY;
            return travelled <= 0f ? 0f : travelled / elapsed;
        }

        private float ResolvePower(float releaseY, float timeSeconds)
        {
            return Mathf.Clamp01(ResolveStrokeSpeed(releaseY, timeSeconds) / referenceStrokeSpeed);
        }
    }
}

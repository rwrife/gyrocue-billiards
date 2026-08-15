using System;
using UnityEngine;

namespace GyroCue.Input
{
    public enum RemoteCueCalibrationState
    {
        NotCalibrated = 0,
        Calibrating = 1,
        Calibrated = 2,
        TimedOut = 3
    }

    /// <summary>
    /// Maps validated second-device sensor frames to aiming vectors and shot releases.
    /// Supports a quick calibration pass so stacked-phone alignment offsets can be removed.
    /// </summary>
    public sealed class RemoteSensorInputAdapter : MonoBehaviour
    {
        [SerializeField]
        private bool remoteInputEnabled = true;

        [SerializeField]
        private float frameTimeoutSeconds = 0.35f;

        [SerializeField, Range(0f, 45f)]
        private float aimDeadzoneDegrees = 1.5f;

        [SerializeField]
        private float aimSensitivity = 1f;

        [SerializeField, Range(0f, 1f)]
        private float aimSmoothingFactor = 0.35f;

        [SerializeField, Range(0f, 45f)]
        private float maxAimStepDegreesPerFrame = 12f;

        [SerializeField, Min(0f)]
        private float stationaryAngularVelocityThresholdRadPerSec = 0.12f;

        [SerializeField, Min(0f)]
        private float stationaryForwardAccelerationThresholdMps2 = 0.35f;

        [SerializeField, Range(0f, 20f)]
        private float stationaryAimDriftClampDegrees = 3f;

        [SerializeField]
        private float shotTriggerAccelerationMps2 = 2.2f;

        [SerializeField]
        private float shotTriggerRearmAccelerationMps2 = 1.2f;

        [SerializeField]
        private float shotPowerSensitivity = 0.2f;

        [SerializeField, Range(0f, 1f)]
        private float forwardAccelerationSmoothingFactor = 0.8f;

        [SerializeField, Range(0f, 1f)]
        private float forwardAccelerationDriftCorrectionFactor = 0.15f;

        [SerializeField, Range(0f, 1f)]
        private float minimumLaunchPower = 0.08f;

        [SerializeField, Min(1)]
        private int calibrationSampleTarget = 24;

        [SerializeField]
        private float calibrationMaxDurationSeconds = 8f;

        private Vector2 aimDirection = Vector2.up;
        private float previewPower01;
        private float lastFrameReceivedRealtime = float.NegativeInfinity;
        private bool shotTriggerArmed = true;
        private long lastSequence = -1;
        private Func<float> timeProvider = () => Time.unscaledTime;

        private Quaternion calibrationOffset = Quaternion.identity;
        private bool hasCalibrationOffset;
        private bool calibrationInProgress;
        private float calibrationStartedRealtime = float.NegativeInfinity;
        private int calibrationSamplesCollected;
        private Vector3 calibrationForwardAccumulator;
        private Vector3 calibrationUpAccumulator;
        private RemoteCueCalibrationState calibrationState = RemoteCueCalibrationState.NotCalibrated;
        private bool hasForwardAccelerationBias;
        private float forwardAccelerationBiasMps2;
        private bool hasSmoothedForwardAcceleration;
        private float smoothedForwardAccelerationMps2;
        private bool stationaryAimAnchorActive;
        private Vector2 stationaryAimAnchor = Vector2.up;

        public event Action<ShotCommand> ShotReleased;

        public Vector2 AimDirection => aimDirection;

        public float PreviewPower01 => previewPower01;

        public bool RemoteInputEnabled => remoteInputEnabled;

        public RemoteCueCalibrationState CalibrationState => calibrationState;

        public bool IsCalibrationInProgress => calibrationInProgress;

        public bool HasCalibrationOffset => hasCalibrationOffset;

        public int CalibrationSamplesCollected => calibrationSamplesCollected;

        public float CalibrationElapsedSeconds =>
            calibrationInProgress
                ? Mathf.Max(0f, NowSeconds - calibrationStartedRealtime)
                : 0f;

        public bool IsRemoteControlActive =>
            remoteInputEnabled &&
            !calibrationInProgress &&
            (NowSeconds - lastFrameReceivedRealtime) <= Mathf.Max(0.01f, frameTimeoutSeconds);

        /// <summary>
        /// Allows deterministic edit-mode tests to control the runtime clock.
        /// </summary>
        public void SetTimeProviderForTests(Func<float> provider)
        {
            timeProvider = provider ?? (() => Time.unscaledTime);
        }

        /// <summary>
        /// Test-only hook for deterministic tuning checks.
        /// </summary>
        public void ConfigureStabilityFilterForTests(
            float aimSmoothing,
            float maxAimStepDegrees,
            float stationaryAngularVelocityThreshold,
            float stationaryForwardAccelerationThreshold,
            float stationaryDriftClampDegrees,
            float forwardAccelerationSmoothing,
            float forwardAccelerationDriftCorrection)
        {
            aimSmoothingFactor = Mathf.Clamp01(aimSmoothing);
            maxAimStepDegreesPerFrame = Mathf.Clamp(maxAimStepDegrees, 0f, 45f);
            stationaryAngularVelocityThresholdRadPerSec = Mathf.Max(0f, stationaryAngularVelocityThreshold);
            stationaryForwardAccelerationThresholdMps2 = Mathf.Max(0f, stationaryForwardAccelerationThreshold);
            stationaryAimDriftClampDegrees = Mathf.Clamp(stationaryDriftClampDegrees, 0f, 20f);
            forwardAccelerationSmoothingFactor = Mathf.Clamp01(forwardAccelerationSmoothing);
            forwardAccelerationDriftCorrectionFactor = Mathf.Clamp01(forwardAccelerationDriftCorrection);
            ResetStabilityFilterState();
        }

        public void SetRemoteInputEnabled(bool enabled)
        {
            remoteInputEnabled = enabled;

            if (!enabled)
            {
                previewPower01 = 0f;
                lastFrameReceivedRealtime = float.NegativeInfinity;
                shotTriggerArmed = true;
                calibrationInProgress = false;
                ResetCalibrationAccumulators();
                ResetStabilityFilterState();
            }
        }

        /// <summary>
        /// Starts a short calibration pass while the cue phone is stacked/aligned on top of the game phone.
        /// Existing calibration remains active until a new pass completes successfully.
        /// </summary>
        public void BeginCalibration()
        {
            calibrationInProgress = true;
            calibrationStartedRealtime = NowSeconds;
            calibrationState = RemoteCueCalibrationState.Calibrating;
            previewPower01 = 0f;
            shotTriggerArmed = true;
            ResetCalibrationAccumulators();
            ResetStabilityFilterState();
        }

        public void CancelCalibration()
        {
            if (!calibrationInProgress)
            {
                return;
            }

            calibrationInProgress = false;
            calibrationStartedRealtime = float.NegativeInfinity;
            calibrationState = hasCalibrationOffset
                ? RemoteCueCalibrationState.Calibrated
                : RemoteCueCalibrationState.NotCalibrated;
            ResetCalibrationAccumulators();
            ResetStabilityFilterState();
        }

        public void ClearCalibration()
        {
            hasCalibrationOffset = false;
            calibrationOffset = Quaternion.identity;
            calibrationState = calibrationInProgress
                ? RemoteCueCalibrationState.Calibrating
                : RemoteCueCalibrationState.NotCalibrated;
        }

        public bool ProcessSensorFrame(RemoteCueSensorFrame frame, out ShotCommand shotCommand)
        {
            shotCommand = default;

            if (!remoteInputEnabled || !frame.IsValid)
            {
                return false;
            }

            if (frame.Sequence < lastSequence)
            {
                return false;
            }

            lastSequence = frame.Sequence;
            lastFrameReceivedRealtime = NowSeconds;

            if (calibrationInProgress)
            {
                if (CalibrationElapsedSeconds > Mathf.Max(0.25f, calibrationMaxDurationSeconds))
                {
                    FailCalibrationTimeout();
                    return false;
                }

                AccumulateCalibrationSample(frame.Orientation);
                if (calibrationSamplesCollected >= Mathf.Max(1, calibrationSampleTarget))
                {
                    CompleteCalibration();
                }

                return false;
            }

            var calibratedOrientation = ApplyCalibration(frame.Orientation);
            var calibratedAcceleration = ApplyCalibration(frame.AccelerationMps2);
            var calibratedAngularVelocity = ApplyCalibration(frame.AngularVelocityRadPerSec);

            var cueForward = calibratedOrientation * Vector3.forward;
            var rawForwardAcceleration = Vector3.Dot(calibratedAcceleration, cueForward);
            var isStationary =
                calibratedAngularVelocity.magnitude <= stationaryAngularVelocityThresholdRadPerSec &&
                Mathf.Abs(rawForwardAcceleration) <= stationaryForwardAccelerationThresholdMps2;

            UpdateAimDirection(calibratedOrientation, isStationary);

            var forwardAcceleration = ApplyForwardAccelerationFilter(rawForwardAcceleration, isStationary);

            var triggerThreshold = Mathf.Max(0f, shotTriggerAccelerationMps2);
            var rearmThreshold = Mathf.Min(triggerThreshold, Mathf.Max(0f, shotTriggerRearmAccelerationMps2));

            if (forwardAcceleration <= rearmThreshold)
            {
                shotTriggerArmed = true;
            }

            var triggerStrength = Mathf.Max(0f, forwardAcceleration - triggerThreshold);
            previewPower01 = Mathf.Clamp01(triggerStrength * Mathf.Max(0.001f, shotPowerSensitivity));

            if (!shotTriggerArmed || forwardAcceleration < triggerThreshold)
            {
                return false;
            }

            shotTriggerArmed = false;

            var releasePower = Mathf.Max(minimumLaunchPower, previewPower01);
            shotCommand = new ShotCommand(aimDirection, releasePower);
            ShotReleased?.Invoke(shotCommand);
            return true;
        }

        public void MarkRemoteStreamIdle()
        {
            lastFrameReceivedRealtime = float.NegativeInfinity;
            previewPower01 = 0f;
            shotTriggerArmed = true;
            ResetStabilityFilterState();
        }

        private float NowSeconds => timeProvider();

        private void AccumulateCalibrationSample(Quaternion orientation)
        {
            calibrationForwardAccumulator += (orientation * Vector3.forward).normalized;
            calibrationUpAccumulator += (orientation * Vector3.up).normalized;
            calibrationSamplesCollected++;
        }

        private void CompleteCalibration()
        {
            var referenceForward = NormalizeOrFallback(calibrationForwardAccumulator, Vector3.forward);
            var referenceUp = NormalizeOrFallback(calibrationUpAccumulator, Vector3.up);

            if (Mathf.Abs(Vector3.Dot(referenceForward, referenceUp)) > 0.98f)
            {
                referenceUp = Vector3.up;
            }

            var referenceOrientation = Quaternion.LookRotation(referenceForward, referenceUp);
            calibrationOffset = Quaternion.Inverse(referenceOrientation);
            hasCalibrationOffset = true;
            calibrationInProgress = false;
            calibrationStartedRealtime = float.NegativeInfinity;
            calibrationState = RemoteCueCalibrationState.Calibrated;
            ResetCalibrationAccumulators();
            ResetStabilityFilterState();
        }

        private void FailCalibrationTimeout()
        {
            calibrationInProgress = false;
            calibrationStartedRealtime = float.NegativeInfinity;
            calibrationState = RemoteCueCalibrationState.TimedOut;
            previewPower01 = 0f;
            shotTriggerArmed = true;
            ResetCalibrationAccumulators();
            ResetStabilityFilterState();
        }

        private void ResetCalibrationAccumulators()
        {
            calibrationSamplesCollected = 0;
            calibrationForwardAccumulator = Vector3.zero;
            calibrationUpAccumulator = Vector3.zero;
        }

        private Quaternion ApplyCalibration(Quaternion orientation)
        {
            return hasCalibrationOffset ? calibrationOffset * orientation : orientation;
        }

        private Vector3 ApplyCalibration(Vector3 sensorVector)
        {
            return hasCalibrationOffset ? calibrationOffset * sensorVector : sensorVector;
        }

        private void UpdateAimDirection(Quaternion orientation, bool isStationary)
        {
            var forward = orientation * Vector3.forward;
            var candidate = new Vector2(forward.x * Mathf.Max(0.01f, aimSensitivity), forward.z);

            if (candidate.sqrMagnitude < 0.0001f)
            {
                return;
            }

            candidate.Normalize();

            if (isStationary)
            {
                if (!stationaryAimAnchorActive)
                {
                    stationaryAimAnchor = aimDirection;
                    stationaryAimAnchorActive = true;
                }

                candidate = ClampAimStep(stationaryAimAnchor, candidate, stationaryAimDriftClampDegrees);
            }
            else
            {
                stationaryAimAnchorActive = false;
            }

            candidate = ClampAimStep(aimDirection, candidate, maxAimStepDegreesPerFrame);

            if (Vector2.Angle(aimDirection, candidate) < aimDeadzoneDegrees)
            {
                return;
            }

            var blendedDirection = BlendAimDirection(aimDirection, candidate, aimSmoothingFactor);
            aimDirection = NormalizeOrFallback2D(blendedDirection, aimDirection);
        }

        private float ApplyForwardAccelerationFilter(float rawForwardAccelerationMps2, bool isStationary)
        {
            if (!hasForwardAccelerationBias)
            {
                forwardAccelerationBiasMps2 = isStationary ? rawForwardAccelerationMps2 : 0f;
                hasForwardAccelerationBias = true;
            }

            if (isStationary)
            {
                forwardAccelerationBiasMps2 = Mathf.Lerp(
                    forwardAccelerationBiasMps2,
                    rawForwardAccelerationMps2,
                    forwardAccelerationDriftCorrectionFactor);
            }

            var correctedForwardAcceleration = rawForwardAccelerationMps2 - forwardAccelerationBiasMps2;

            if (!hasSmoothedForwardAcceleration)
            {
                smoothedForwardAccelerationMps2 = correctedForwardAcceleration;
                hasSmoothedForwardAcceleration = true;
            }
            else
            {
                smoothedForwardAccelerationMps2 = Mathf.Lerp(
                    smoothedForwardAccelerationMps2,
                    correctedForwardAcceleration,
                    forwardAccelerationSmoothingFactor);
            }

            return smoothedForwardAccelerationMps2;
        }

        private void ResetStabilityFilterState()
        {
            hasForwardAccelerationBias = false;
            forwardAccelerationBiasMps2 = 0f;
            hasSmoothedForwardAcceleration = false;
            smoothedForwardAccelerationMps2 = 0f;
            stationaryAimAnchorActive = false;
            stationaryAimAnchor = aimDirection;
        }

        private static Vector2 ClampAimStep(Vector2 from, Vector2 to, float maxStepDegrees)
        {
            var normalizedFrom = NormalizeOrFallback2D(from, Vector2.up);
            var normalizedTo = NormalizeOrFallback2D(to, normalizedFrom);
            var allowedStep = Mathf.Max(0f, maxStepDegrees);
            var deltaDegrees = Vector2.SignedAngle(normalizedFrom, normalizedTo);

            if (Mathf.Abs(deltaDegrees) <= allowedStep)
            {
                return normalizedTo;
            }

            var clampedStep = Mathf.Clamp(deltaDegrees, -allowedStep, allowedStep);
            var rotated = (Vector2)(Quaternion.Euler(0f, 0f, clampedStep) * normalizedFrom);
            return NormalizeOrFallback2D(rotated, normalizedFrom);
        }

        private static Vector2 BlendAimDirection(Vector2 current, Vector2 target, float smoothingFactor)
        {
            var t = Mathf.Clamp01(smoothingFactor);
            var normalizedCurrent = NormalizeOrFallback2D(current, Vector2.up);
            var normalizedTarget = NormalizeOrFallback2D(target, normalizedCurrent);

            if (t <= 0f)
            {
                return normalizedCurrent;
            }

            if (t >= 1f)
            {
                return normalizedTarget;
            }

            return NormalizeOrFallback2D(
                (normalizedCurrent * (1f - t)) + (normalizedTarget * t),
                normalizedTarget);
        }

        private static Vector2 NormalizeOrFallback2D(Vector2 value, Vector2 fallback)
        {
            if (value.sqrMagnitude < 0.0001f)
            {
                return fallback.sqrMagnitude < 0.0001f ? Vector2.up : fallback.normalized;
            }

            return value.normalized;
        }

        private static Vector3 NormalizeOrFallback(Vector3 value, Vector3 fallback)
        {
            if (value.sqrMagnitude < 0.0001f)
            {
                return fallback.normalized;
            }

            return value.normalized;
        }

        private void OnValidate()
        {
            if (shotTriggerRearmAccelerationMps2 > shotTriggerAccelerationMps2)
            {
                shotTriggerRearmAccelerationMps2 = shotTriggerAccelerationMps2;
            }

            if (frameTimeoutSeconds < 0.01f)
            {
                frameTimeoutSeconds = 0.01f;
            }

            if (calibrationSampleTarget < 1)
            {
                calibrationSampleTarget = 1;
            }

            if (calibrationMaxDurationSeconds < 0.25f)
            {
                calibrationMaxDurationSeconds = 0.25f;
            }

            if (calibrationMaxDurationSeconds > 9.5f)
            {
                calibrationMaxDurationSeconds = 9.5f;
            }

            aimSmoothingFactor = Mathf.Clamp01(aimSmoothingFactor);
            maxAimStepDegreesPerFrame = Mathf.Clamp(maxAimStepDegreesPerFrame, 0f, 45f);
            stationaryAngularVelocityThresholdRadPerSec = Mathf.Max(0f, stationaryAngularVelocityThresholdRadPerSec);
            stationaryForwardAccelerationThresholdMps2 = Mathf.Max(0f, stationaryForwardAccelerationThresholdMps2);
            stationaryAimDriftClampDegrees = Mathf.Clamp(stationaryAimDriftClampDegrees, 0f, 20f);
            forwardAccelerationSmoothingFactor = Mathf.Clamp01(forwardAccelerationSmoothingFactor);
            forwardAccelerationDriftCorrectionFactor = Mathf.Clamp01(forwardAccelerationDriftCorrectionFactor);
        }
    }
}

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

        [SerializeField]
        private float shotTriggerAccelerationMps2 = 2.2f;

        [SerializeField]
        private float shotTriggerRearmAccelerationMps2 = 1.2f;

        [SerializeField]
        private float shotPowerSensitivity = 0.2f;

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

            UpdateAimDirection(calibratedOrientation);

            var cueForward = calibratedOrientation * Vector3.forward;
            var forwardAcceleration = Vector3.Dot(calibratedAcceleration, cueForward);

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
        }

        private void FailCalibrationTimeout()
        {
            calibrationInProgress = false;
            calibrationStartedRealtime = float.NegativeInfinity;
            calibrationState = RemoteCueCalibrationState.TimedOut;
            previewPower01 = 0f;
            shotTriggerArmed = true;
            ResetCalibrationAccumulators();
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

        private void UpdateAimDirection(Quaternion orientation)
        {
            var forward = orientation * Vector3.forward;
            var candidate = new Vector2(forward.x * Mathf.Max(0.01f, aimSensitivity), forward.z);

            if (candidate.sqrMagnitude < 0.0001f)
            {
                return;
            }

            candidate.Normalize();

            if (Vector2.Angle(aimDirection, candidate) < aimDeadzoneDegrees)
            {
                return;
            }

            aimDirection = candidate;
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
        }
    }
}

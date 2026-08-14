using System;
using UnityEngine;

namespace GyroCue.Input
{
    /// <summary>
    /// Maps validated second-device sensor frames to aiming vectors and shot releases.
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

        private Vector2 aimDirection = Vector2.up;
        private float previewPower01;
        private float lastFrameReceivedRealtime = float.NegativeInfinity;
        private bool shotTriggerArmed = true;
        private long lastSequence = -1;
        private Func<float> timeProvider = () => Time.unscaledTime;

        public event Action<ShotCommand> ShotReleased;

        public Vector2 AimDirection => aimDirection;

        public float PreviewPower01 => previewPower01;

        public bool RemoteInputEnabled => remoteInputEnabled;

        public bool IsRemoteControlActive =>
            remoteInputEnabled &&
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
            }
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

            UpdateAimDirection(frame.Orientation);

            var cueForward = frame.Orientation * Vector3.forward;
            var forwardAcceleration = Vector3.Dot(frame.AccelerationMps2, cueForward);

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
        }
    }
}

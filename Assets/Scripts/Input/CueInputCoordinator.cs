using System;
using UnityEngine;

namespace GyroCue.Input
{
    /// <summary>
    /// Coordinates touch and optional remote-cue inputs, preferring remote while frames are fresh.
    /// </summary>
    public sealed class CueInputCoordinator : MonoBehaviour
    {
        [SerializeField]
        private TouchAimSwipeController touchAimSwipeController;

        [SerializeField]
        private RemoteSensorInputAdapter remoteSensorInputAdapter;

        [SerializeField]
        private bool lockTouchWhileRemoteActive = true;

        public event Action<ShotCommand> ShotReleased;

        public bool IsRemoteCalibrationInProgress =>
            remoteSensorInputAdapter != null && remoteSensorInputAdapter.IsCalibrationInProgress;

        public RemoteCueCalibrationState RemoteCalibrationState =>
            remoteSensorInputAdapter != null
                ? remoteSensorInputAdapter.CalibrationState
                : RemoteCueCalibrationState.NotCalibrated;

        public void BeginRemoteCalibration()
        {
            ResolveReferences();

            if (remoteSensorInputAdapter == null)
            {
                return;
            }

            remoteSensorInputAdapter.BeginCalibration();
            RefreshInputLocks();
        }

        public void CancelRemoteCalibration()
        {
            ResolveReferences();

            if (remoteSensorInputAdapter == null)
            {
                return;
            }

            remoteSensorInputAdapter.CancelCalibration();
            RefreshInputLocks();
        }

        public void RefreshInputLocks()
        {
            ResolveReferences();

            if (!lockTouchWhileRemoteActive || touchAimSwipeController == null || remoteSensorInputAdapter == null)
            {
                return;
            }

            touchAimSwipeController.SetTouchInputEnabled(!remoteSensorInputAdapter.IsRemoteControlActive);
        }

        public bool TryProcessRemoteSensorFrame(RemoteCueSensorFrame frame, out ShotCommand shotCommand)
        {
            shotCommand = default;

            ResolveReferences();
            if (remoteSensorInputAdapter == null)
            {
                return false;
            }

            var didRelease = remoteSensorInputAdapter.ProcessSensorFrame(frame, out shotCommand);
            RefreshInputLocks();
            return didRelease;
        }

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();

            if (touchAimSwipeController != null)
            {
                touchAimSwipeController.ShotReleased += HandleTouchShotReleased;
            }

            if (remoteSensorInputAdapter != null)
            {
                remoteSensorInputAdapter.ShotReleased += HandleRemoteShotReleased;
            }
        }

        private void OnDisable()
        {
            if (touchAimSwipeController != null)
            {
                touchAimSwipeController.ShotReleased -= HandleTouchShotReleased;
            }

            if (remoteSensorInputAdapter != null)
            {
                remoteSensorInputAdapter.ShotReleased -= HandleRemoteShotReleased;
            }
        }

        private void Update()
        {
            RefreshInputLocks();
        }

        private void HandleTouchShotReleased(ShotCommand shotCommand)
        {
            if (remoteSensorInputAdapter != null && remoteSensorInputAdapter.IsRemoteControlActive)
            {
                return;
            }

            ShotReleased?.Invoke(shotCommand);
        }

        private void HandleRemoteShotReleased(ShotCommand shotCommand)
        {
            ShotReleased?.Invoke(shotCommand);
        }

        private void ResolveReferences()
        {
            if (touchAimSwipeController == null)
            {
                touchAimSwipeController = GetComponent<TouchAimSwipeController>();
            }

            if (remoteSensorInputAdapter == null)
            {
                remoteSensorInputAdapter = GetComponent<RemoteSensorInputAdapter>();
            }
        }
    }
}

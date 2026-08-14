using GyroCue.Input;
using NUnit.Framework;
using UnityEngine;

namespace GyroCue.Tests.EditMode
{
    public sealed class RemoteSensorInputAdapterTests
    {
        [Test]
        public void ProcessSensorFrame_UpdatesAimAndReleasesShot()
        {
            var root = new GameObject("remote-adapter-shot-test");

            try
            {
                var now = 10f;
                var adapter = root.AddComponent<RemoteSensorInputAdapter>();
                adapter.SetTimeProviderForTests(() => now);

                var didRelease = adapter.ProcessSensorFrame(
                    CreateFrame(0, Quaternion.Euler(0f, 90f, 0f), Vector3.zero),
                    out _);

                Assert.That(didRelease, Is.False);
                Assert.That(adapter.AimDirection.x, Is.GreaterThan(0.95f));

                now += 0.02f;
                didRelease = adapter.ProcessSensorFrame(
                    CreateFrame(1, Quaternion.Euler(0f, 90f, 0f), new Vector3(5f, 0f, 0f)),
                    out var shotCommand);

                Assert.That(didRelease, Is.True);
                Assert.That(shotCommand.AimDirection.x, Is.GreaterThan(0.95f));
                Assert.That(shotCommand.NormalizedPower, Is.GreaterThan(0.08f));
                Assert.That(shotCommand.NormalizedPower, Is.LessThanOrEqualTo(1f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ProcessSensorFrame_RequiresRearmBeforeSecondShot()
        {
            var root = new GameObject("remote-adapter-rearm-test");

            try
            {
                var now = 20f;
                var adapter = root.AddComponent<RemoteSensorInputAdapter>();
                adapter.SetTimeProviderForTests(() => now);

                var firstShot = adapter.ProcessSensorFrame(
                    CreateFrame(0, Quaternion.identity, new Vector3(0f, 0f, 5f)),
                    out _);

                now += 0.02f;
                var immediateSecondShot = adapter.ProcessSensorFrame(
                    CreateFrame(1, Quaternion.identity, new Vector3(0f, 0f, 5f)),
                    out _);

                now += 0.02f;
                var rearmFrameShot = adapter.ProcessSensorFrame(
                    CreateFrame(2, Quaternion.identity, Vector3.zero),
                    out _);

                now += 0.02f;
                var secondShotAfterRearm = adapter.ProcessSensorFrame(
                    CreateFrame(3, Quaternion.identity, new Vector3(0f, 0f, 5f)),
                    out _);

                Assert.That(firstShot, Is.True);
                Assert.That(immediateSecondShot, Is.False);
                Assert.That(rearmFrameShot, Is.False);
                Assert.That(secondShotAfterRearm, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void IsRemoteControlActive_ExpiresAfterFrameTimeout()
        {
            var root = new GameObject("remote-adapter-timeout-test");

            try
            {
                var now = 30f;
                var adapter = root.AddComponent<RemoteSensorInputAdapter>();
                adapter.SetTimeProviderForTests(() => now);

                adapter.ProcessSensorFrame(CreateFrame(0, Quaternion.identity, Vector3.zero), out _);
                Assert.That(adapter.IsRemoteControlActive, Is.True);

                now += 1f;
                Assert.That(adapter.IsRemoteControlActive, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void BeginCalibration_AlignsStackedPhoneOffsetWithinSampleWindow()
        {
            var root = new GameObject("remote-adapter-calibration-success-test");

            try
            {
                var now = 35f;
                var adapter = root.AddComponent<RemoteSensorInputAdapter>();
                adapter.SetTimeProviderForTests(() => now);

                adapter.BeginCalibration();
                Assert.That(adapter.CalibrationState, Is.EqualTo(RemoteCueCalibrationState.Calibrating));

                for (var i = 0; i < 24; i++)
                {
                    var didRelease = adapter.ProcessSensorFrame(
                        CreateFrame(i, Quaternion.Euler(0f, 90f, 0f), Vector3.zero),
                        out _);
                    Assert.That(didRelease, Is.False);
                    now += 0.01f;
                }

                Assert.That(adapter.IsCalibrationInProgress, Is.False);
                Assert.That(adapter.CalibrationState, Is.EqualTo(RemoteCueCalibrationState.Calibrated));
                Assert.That(adapter.HasCalibrationOffset, Is.True);

                var shotReleased = adapter.ProcessSensorFrame(
                    CreateFrame(30, Quaternion.Euler(0f, 90f, 0f), new Vector3(5f, 0f, 0f)),
                    out var shotCommand);

                Assert.That(shotReleased, Is.True);
                Assert.That(adapter.AimDirection.y, Is.GreaterThan(0.95f));
                Assert.That(shotCommand.AimDirection.y, Is.GreaterThan(0.95f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void BeginCalibration_TimesOutWhenCalibrationWindowExceeded()
        {
            var root = new GameObject("remote-adapter-calibration-timeout-test");

            try
            {
                var now = 40f;
                var adapter = root.AddComponent<RemoteSensorInputAdapter>();
                adapter.SetTimeProviderForTests(() => now);
                adapter.BeginCalibration();

                adapter.ProcessSensorFrame(CreateFrame(0, Quaternion.identity, Vector3.zero), out _);
                now += 9f;

                var didRelease = adapter.ProcessSensorFrame(
                    CreateFrame(1, Quaternion.identity, Vector3.zero),
                    out _);

                Assert.That(didRelease, Is.False);
                Assert.That(adapter.IsCalibrationInProgress, Is.False);
                Assert.That(adapter.CalibrationState, Is.EqualTo(RemoteCueCalibrationState.TimedOut));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void CueInputCoordinator_LocksTouchWhileRemoteIsFreshAndRestoresOnTimeout()
        {
            var root = new GameObject("cue-input-coordinator-test");

            try
            {
                var now = 50f;
                var touchController = root.AddComponent<TouchAimSwipeController>();
                var remoteAdapter = root.AddComponent<RemoteSensorInputAdapter>();
                var coordinator = root.AddComponent<CueInputCoordinator>();
                remoteAdapter.SetTimeProviderForTests(() => now);

                coordinator.RefreshInputLocks();
                Assert.That(touchController.TouchInputEnabled, Is.True);

                remoteAdapter.ProcessSensorFrame(CreateFrame(0, Quaternion.identity, Vector3.zero), out _);
                coordinator.RefreshInputLocks();
                Assert.That(touchController.TouchInputEnabled, Is.False);

                now += 1f;
                coordinator.RefreshInputLocks();
                Assert.That(touchController.TouchInputEnabled, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void CueInputCoordinator_BeginRemoteCalibration_UnlocksTouchDuringCalibration()
        {
            var root = new GameObject("cue-input-coordinator-calibration-test");

            try
            {
                var now = 60f;
                var touchController = root.AddComponent<TouchAimSwipeController>();
                var remoteAdapter = root.AddComponent<RemoteSensorInputAdapter>();
                var coordinator = root.AddComponent<CueInputCoordinator>();
                remoteAdapter.SetTimeProviderForTests(() => now);

                remoteAdapter.ProcessSensorFrame(CreateFrame(0, Quaternion.identity, Vector3.zero), out _);
                coordinator.RefreshInputLocks();
                Assert.That(touchController.TouchInputEnabled, Is.False);

                coordinator.BeginRemoteCalibration();
                coordinator.RefreshInputLocks();

                Assert.That(coordinator.IsRemoteCalibrationInProgress, Is.True);
                Assert.That(coordinator.RemoteCalibrationState, Is.EqualTo(RemoteCueCalibrationState.Calibrating));
                Assert.That(touchController.TouchInputEnabled, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static RemoteCueSensorFrame CreateFrame(long sequence, Quaternion orientation, Vector3 accelerationMps2)
        {
            return new RemoteCueSensorFrame(
                RemoteCueProtocol.SchemaVersionV1,
                timestampUnixMs: 1_000 + sequence,
                sequence: sequence,
                orientation,
                accelerationMps2,
                angularVelocityRadPerSec: Vector3.zero);
        }
    }
}

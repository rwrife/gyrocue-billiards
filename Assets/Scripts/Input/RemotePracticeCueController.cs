using GyroCue.Practice;
using UnityEngine;

namespace GyroCue.Input
{
    /// <summary>
    /// Bridges game-phone receiver frames into the 3D practice controls. Fresh remote
    /// frames own aim/stroke; stale/disconnected sessions immediately release touch.
    /// Tip offset and elevation intentionally stay on the table phone touch widgets.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RemotePracticeCueController : MonoBehaviour
    {
        private RemoteCueUdpReceiver receiver;
        private RemoteSensorInputAdapter adapter;
        private OrbitAimController orbitAim;
        private PracticeInputRouter inputRouter;
        private PracticeSessionController session;

        public void Configure(
            RemoteCueUdpReceiver udpReceiver,
            RemoteSensorInputAdapter inputAdapter,
            OrbitAimController orbitAimController,
            PracticeInputRouter router,
            PracticeSessionController sessionController)
        {
            Unsubscribe();
            receiver = udpReceiver;
            adapter = inputAdapter;
            orbitAim = orbitAimController;
            inputRouter = router;
            session = sessionController;
            Subscribe();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
            inputRouter?.SetRemoteInputActive(false);
        }

        private void Update()
        {
            if (receiver == null || adapter == null)
            {
                inputRouter?.SetRemoteInputActive(false);
                return;
            }

            var connected = receiver.Snapshot.State == RemoteCueReceiverState.Connected &&
                            adapter.IsRemoteControlActive &&
                            !adapter.IsCalibrationInProgress;
            inputRouter?.SetRemoteInputActive(connected);

            if (!connected)
            {
                adapter.MarkRemoteStreamIdle();
            }
        }

        public static bool CanProcessFrame(PracticeSessionController practiceSession)
        {
            return practiceSession != null &&
                   !practiceSession.IsPaused &&
                   practiceSession.Phase == PracticePhase.Aiming;
        }

        private void HandleFrameReceived(RemoteCueSensorFrame frame)
        {
            if (adapter == null || !CanProcessFrame(session))
            {
                return;
            }

            var released = adapter.ProcessSensorFrame(frame, out var shot);
            orbitAim?.SetAimDirection(adapter.AimDirection3D);

            if (!released)
            {
                return;
            }

            var strikeOffset = inputRouter != null
                ? inputRouter.StrokeGesture.StrikeOffset
                : Vector2.zero;
            session.TryTakeShot(RemotePracticeCueMapping.ToPracticeStroke(shot, strikeOffset));
        }

        private void Subscribe()
        {
            if (isActiveAndEnabled && receiver != null)
            {
                receiver.FrameReceived -= HandleFrameReceived;
                receiver.FrameReceived += HandleFrameReceived;
            }
        }

        private void Unsubscribe()
        {
            if (receiver != null)
            {
                receiver.FrameReceived -= HandleFrameReceived;
            }
        }
    }
}

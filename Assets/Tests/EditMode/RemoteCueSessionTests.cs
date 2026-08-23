using GyroCue.Input;
using NUnit.Framework;
using UnityEngine;

namespace GyroCue.Tests.EditMode
{
    public sealed class RemoteCueSessionTests
    {
        [Test]
        public void AcceptDatagram_LocksToFirstSenderAndRejectsDuplicateSequence()
        {
            var session = new RemoteCueSession(queueCapacity: 4, staleTimeoutSeconds: 0.5);
            session.StartListening(RemoteCueProtocol.UdpPort);

            Assert.That(session.TryAcceptDatagram(ValidPayload(0), "192.168.1.20:51000", 10.0), Is.True);
            Assert.That(session.TryAcceptDatagram(ValidPayload(0), "192.168.1.20:51000", 10.1), Is.False);
            Assert.That(session.TryAcceptDatagram(ValidPayload(1), "192.168.1.21:51000", 10.2), Is.False);

            var snapshot = session.GetSnapshot(10.2);
            Assert.That(snapshot.State, Is.EqualTo(RemoteCueReceiverState.Connected));
            Assert.That(snapshot.Sender, Is.EqualTo("192.168.1.20:51000"));
            Assert.That(snapshot.AcceptedFrames, Is.EqualTo(1));
            Assert.That(snapshot.RejectedFrames, Is.EqualTo(2));
        }

        [Test]
        public void Queue_DropsOldestFrameAndDequeuesLatest()
        {
            var session = new RemoteCueSession(queueCapacity: 2, staleTimeoutSeconds: 0.5);
            session.StartListening(RemoteCueProtocol.UdpPort);

            Assert.That(session.TryAcceptDatagram(ValidPayload(0), "sender", 20.0), Is.True);
            Assert.That(session.TryAcceptDatagram(ValidPayload(1), "sender", 20.1), Is.True);
            Assert.That(session.TryAcceptDatagram(ValidPayload(2), "sender", 20.2), Is.True);

            Assert.That(session.TryDequeueLatest(out var latest), Is.True);
            Assert.That(latest.Sequence, Is.EqualTo(2));

            var snapshot = session.GetSnapshot(20.2);
            Assert.That(snapshot.QueuedFrames, Is.EqualTo(0));
            Assert.That(snapshot.DroppedFrames, Is.EqualTo(1));
        }

        [Test]
        public void Snapshot_TransitionsFromConnectedToStaleAndTouchFallback()
        {
            var session = new RemoteCueSession(queueCapacity: 4, staleTimeoutSeconds: 0.35);
            session.StartListening(RemoteCueProtocol.UdpPort);
            session.TryAcceptDatagram(ValidPayload(0), "sender", 30.0);

            Assert.That(session.GetSnapshot(30.2).State, Is.EqualTo(RemoteCueReceiverState.Connected));
            Assert.That(session.GetSnapshot(30.2).TouchFallbackAvailable, Is.False);
            Assert.That(session.GetSnapshot(30.5).State, Is.EqualTo(RemoteCueReceiverState.Stale));
            Assert.That(session.GetSnapshot(30.5).TouchFallbackAvailable, Is.True);
        }

        [Test]
        public void StopThenStart_AllowsNewSenderAndSequenceRestart()
        {
            var session = new RemoteCueSession(queueCapacity: 4, staleTimeoutSeconds: 0.5);
            session.StartListening(RemoteCueProtocol.UdpPort);
            Assert.That(session.TryAcceptDatagram(ValidPayload(8), "sender-a", 40.0), Is.True);

            session.StopListening();
            Assert.That(session.GetSnapshot(40.1).State, Is.EqualTo(RemoteCueReceiverState.Disconnected));
            Assert.That(session.GetSnapshot(40.1).TouchFallbackAvailable, Is.True);

            session.StartListening(RemoteCueProtocol.UdpPort);
            Assert.That(session.TryAcceptDatagram(ValidPayload(0), "sender-b", 40.2), Is.True);
            Assert.That(session.GetSnapshot(40.2).Sender, Is.EqualTo("sender-b"));
        }

        [Test]
        public void AcceptDatagram_RejectsMalformedAndUnsupportedPayloadsWithoutPoisoningSession()
        {
            var session = new RemoteCueSession(queueCapacity: 4, staleTimeoutSeconds: 0.5);
            session.StartListening(RemoteCueProtocol.UdpPort);

            Assert.That(session.TryAcceptDatagram("not-json", "sender", 50.0), Is.False);
            Assert.That(session.TryAcceptDatagram(ValidPayload(0).Replace(RemoteCueProtocol.SchemaVersionV1, "gyrocue.sensor.v0"), "sender", 50.1), Is.False);
            Assert.That(session.TryAcceptDatagram(ValidPayload(0), "sender", 50.2), Is.True);

            var snapshot = session.GetSnapshot(50.2);
            Assert.That(snapshot.AcceptedFrames, Is.EqualTo(1));
            Assert.That(snapshot.RejectedFrames, Is.EqualTo(2));
            Assert.That(snapshot.LastError, Is.Empty);
        }

        [Test]
        public void AimMapping_UsesXzPlaneAndPreservesTouchTipAndElevationControls()
        {
            var aim = RemotePracticeCueMapping.ToTableAim(new Vector2(1f, 0f));
            var stroke = RemotePracticeCueMapping.ToPracticeStroke(
                new ShotCommand(Vector2.right, 0.7f),
                new Vector2(-0.25f, 0.5f));

            Assert.That(aim, Is.EqualTo(Vector3.right));
            Assert.That(stroke.Power01, Is.EqualTo(0.7f).Within(0.001f));
            Assert.That(stroke.StrikeOffset, Is.EqualTo(new Vector2(-0.25f, 0.5f)));
        }

        private static string ValidPayload(long sequence)
        {
            var frame = new RemoteCueSensorFrame(
                RemoteCueProtocol.SchemaVersionV1,
                timestampUnixMs: 1723600000000 + sequence,
                sequence,
                Quaternion.identity,
                Vector3.zero,
                Vector3.zero);
            return RemoteCueSensorFrameJson.ToJson(frame);
        }
    }
}

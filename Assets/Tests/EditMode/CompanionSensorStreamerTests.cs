using System.Collections.Generic;
using GyroCue.Input;
using NUnit.Framework;
using UnityEngine;

namespace GyroCue.Tests.EditMode
{
    public sealed class CompanionSensorStreamerTests
    {
        [Test]
        public void ConnectStartStopDisconnect_TransitionsStateAndStatusText()
        {
            var root = new GameObject("companion-streamer-state-test");

            try
            {
                var streamer = root.AddComponent<CompanionSensorStreamer>();
                var fakeTransport = new FakeCompanionSensorTransport();
                streamer.SetTransportForTests(fakeTransport);

                Assert.That(streamer.StreamState, Is.EqualTo(CompanionSensorStreamState.Disconnected));
                Assert.That(streamer.SessionStatusText, Is.EqualTo("Disconnected"));

                var didConnect = streamer.ConnectToTarget();
                Assert.That(didConnect, Is.True);
                Assert.That(streamer.StreamState, Is.EqualTo(CompanionSensorStreamState.Connected));
                Assert.That(streamer.SessionStatusText, Does.Contain("Connected to"));
                Assert.That(fakeTransport.ConnectCalls, Is.EqualTo(1));

                var didStart = streamer.StartStreaming();
                Assert.That(didStart, Is.True);
                Assert.That(streamer.StreamState, Is.EqualTo(CompanionSensorStreamState.Streaming));
                Assert.That(streamer.SessionStatusText, Does.Contain("Streaming to"));

                streamer.StopStreaming();
                Assert.That(streamer.StreamState, Is.EqualTo(CompanionSensorStreamState.Connected));

                streamer.DisconnectFromTarget();
                Assert.That(streamer.StreamState, Is.EqualTo(CompanionSensorStreamState.Disconnected));
                Assert.That(fakeTransport.DisconnectCalls, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void StartStreaming_ReturnsFalseUntilConnected()
        {
            var root = new GameObject("companion-streamer-connect-gate-test");

            try
            {
                var streamer = root.AddComponent<CompanionSensorStreamer>();
                var fakeTransport = new FakeCompanionSensorTransport();
                streamer.SetTransportForTests(fakeTransport);

                var didStartWhileDisconnected = streamer.StartStreaming();
                Assert.That(didStartWhileDisconnected, Is.False);
                Assert.That(streamer.StreamState, Is.EqualTo(CompanionSensorStreamState.Disconnected));

                Assert.That(streamer.ConnectToTarget(), Is.True);
                Assert.That(streamer.StartStreaming(), Is.True);
                Assert.That(streamer.StreamState, Is.EqualTo(CompanionSensorStreamState.Streaming));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void StreamFrame_ThrottlesToConfiguredRateAndSendsValidPayloads()
        {
            var root = new GameObject("companion-streamer-rate-test");

            try
            {
                var now = 100f;
                var streamer = root.AddComponent<CompanionSensorStreamer>();
                var fakeTransport = new FakeCompanionSensorTransport();

                streamer.SetTransportForTests(fakeTransport);
                streamer.SetTimeProviderForTests(() => now);
                streamer.SetStreamRateForTests(60f);

                Assert.That(streamer.ConnectToTarget(), Is.True);
                Assert.That(streamer.StartStreaming(), Is.True);

                var firstSend = streamer.StreamFrame(Quaternion.identity, new Vector3(0f, -9.8f, 0f), Vector3.zero, 1_000);
                var secondSendSameTick = streamer.StreamFrame(Quaternion.identity, new Vector3(0f, -9.8f, 0f), Vector3.zero, 1_001);

                now += 0.005f;
                var thirdSendTooSoon = streamer.StreamFrame(Quaternion.identity, new Vector3(0f, -9.8f, 0f), Vector3.zero, 1_002);

                now += 0.012f;
                var fourthSendAfterInterval = streamer.StreamFrame(Quaternion.identity, new Vector3(0f, -9.8f, 0f), Vector3.zero, 1_003);

                Assert.That(firstSend, Is.True);
                Assert.That(secondSendSameTick, Is.False);
                Assert.That(thirdSendTooSoon, Is.False);
                Assert.That(fourthSendAfterInterval, Is.True);
                Assert.That(fakeTransport.SentPayloads.Count, Is.EqualTo(2));

                Assert.That(RemoteCueSensorFrameJson.TryParse(fakeTransport.SentPayloads[0], out var frame0), Is.True);
                Assert.That(RemoteCueSensorFrameJson.TryParse(fakeTransport.SentPayloads[1], out var frame1), Is.True);
                Assert.That(frame0.Sequence, Is.EqualTo(0));
                Assert.That(frame1.Sequence, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void StreamFrame_SendFailureStopsStreamingButKeepsConnectionReady()
        {
            var root = new GameObject("companion-streamer-send-failure-test");

            try
            {
                var now = 50f;
                var streamer = root.AddComponent<CompanionSensorStreamer>();
                var fakeTransport = new FakeCompanionSensorTransport
                {
                    SendResult = false
                };

                streamer.SetTransportForTests(fakeTransport);
                streamer.SetTimeProviderForTests(() => now);

                Assert.That(streamer.ConnectToTarget(), Is.True);
                Assert.That(streamer.StartStreaming(), Is.True);

                var didSend = streamer.StreamFrame(Quaternion.identity, new Vector3(0f, -9.8f, 0f), Vector3.zero, 900);

                Assert.That(didSend, Is.False);
                Assert.That(streamer.StreamState, Is.EqualTo(CompanionSensorStreamState.Connected));
                Assert.That(streamer.SessionStatusText, Does.Contain("Connected to"));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private sealed class FakeCompanionSensorTransport : ICompanionSensorTransport
        {
            public bool ConnectResult { get; set; } = true;

            public bool SendResult { get; set; } = true;

            public int ConnectCalls { get; private set; }

            public int DisconnectCalls { get; private set; }

            public List<string> SentPayloads { get; } = new List<string>();

            public bool Connect(string host, int port)
            {
                ConnectCalls++;
                return ConnectResult;
            }

            public void Disconnect()
            {
                DisconnectCalls++;
            }

            public bool Send(string payload)
            {
                if (SendResult)
                {
                    SentPayloads.Add(payload);
                }

                return SendResult;
            }
        }
    }
}

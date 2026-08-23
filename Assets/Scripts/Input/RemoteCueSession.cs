using System;
using System.Collections.Generic;

namespace GyroCue.Input
{
    public enum RemoteCueReceiverState
    {
        Disconnected = 0,
        Listening = 1,
        Connected = 2,
        Stale = 3,
        Error = 4
    }

    public readonly struct RemoteCueSessionSnapshot
    {
        public RemoteCueSessionSnapshot(
            RemoteCueReceiverState state,
            int port,
            string sender,
            double lastFrameAgeSeconds,
            int queuedFrames,
            long acceptedFrames,
            long rejectedFrames,
            long droppedFrames,
            string lastError)
        {
            State = state;
            Port = port;
            Sender = sender ?? string.Empty;
            LastFrameAgeSeconds = lastFrameAgeSeconds;
            QueuedFrames = queuedFrames;
            AcceptedFrames = acceptedFrames;
            RejectedFrames = rejectedFrames;
            DroppedFrames = droppedFrames;
            LastError = lastError ?? string.Empty;
        }

        public RemoteCueReceiverState State { get; }
        public int Port { get; }
        public string Sender { get; }
        public double LastFrameAgeSeconds { get; }
        public int QueuedFrames { get; }
        public long AcceptedFrames { get; }
        public long RejectedFrames { get; }
        public long DroppedFrames { get; }
        public string LastError { get; }
        public bool TouchFallbackAvailable => State != RemoteCueReceiverState.Connected;
    }

    /// <summary>
    /// Thread-safe receiver state and bounded hand-off queue. UDP parsing calls this
    /// from a worker thread; Unity's main thread drains only the newest frame.
    /// </summary>
    public sealed class RemoteCueSession
    {
        private readonly object sync = new object();
        private readonly Queue<RemoteCueSensorFrame> frames = new Queue<RemoteCueSensorFrame>();
        private readonly int queueCapacity;
        private readonly double staleTimeoutSeconds;

        private bool listening;
        private int port;
        private string sender = string.Empty;
        private long lastSequence = -1;
        private double lastFrameReceivedSeconds = double.NegativeInfinity;
        private long acceptedFrames;
        private long rejectedFrames;
        private long droppedFrames;
        private string transportError = string.Empty;
        private string lastError = string.Empty;

        public RemoteCueSession(int queueCapacity, double staleTimeoutSeconds)
        {
            this.queueCapacity = Math.Max(1, queueCapacity);
            this.staleTimeoutSeconds = Math.Max(0.01, staleTimeoutSeconds);
        }

        public void StartListening(int listenPort)
        {
            lock (sync)
            {
                listening = true;
                port = Math.Max(1, listenPort);
                sender = string.Empty;
                lastSequence = -1;
                lastFrameReceivedSeconds = double.NegativeInfinity;
                acceptedFrames = 0;
                rejectedFrames = 0;
                droppedFrames = 0;
                transportError = string.Empty;
                lastError = string.Empty;
                frames.Clear();
            }
        }

        public void StopListening()
        {
            lock (sync)
            {
                listening = false;
                sender = string.Empty;
                lastSequence = -1;
                lastFrameReceivedSeconds = double.NegativeInfinity;
                transportError = string.Empty;
                lastError = string.Empty;
                frames.Clear();
            }
        }

        public void MarkTransportError(string message)
        {
            lock (sync)
            {
                transportError = string.IsNullOrWhiteSpace(message)
                    ? "UDP receiver stopped unexpectedly."
                    : message.Trim();
                lastError = transportError;
            }
        }

        public bool TryAcceptDatagram(string payload, string senderId, double receivedRealtimeSeconds)
        {
            lock (sync)
            {
                if (!listening)
                {
                    return Reject("Start listening before pairing the cue phone.");
                }

                if (string.IsNullOrWhiteSpace(senderId))
                {
                    return Reject("Packet sender was not identified.");
                }

                if (!RemoteCueSensorFrameJson.TryParse(payload, out var frame))
                {
                    return Reject("Ignored malformed or unsupported cue packet.");
                }

                if (sender.Length > 0 && !string.Equals(sender, senderId, StringComparison.Ordinal))
                {
                    return Reject($"Ignored packet from {senderId}; paired with {sender}.");
                }

                if (sender.Length > 0 && frame.Sequence <= lastSequence)
                {
                    return Reject("Ignored duplicate or out-of-order cue packet.");
                }

                if (sender.Length == 0)
                {
                    sender = senderId;
                }

                lastSequence = frame.Sequence;
                lastFrameReceivedSeconds = receivedRealtimeSeconds;
                transportError = string.Empty;
                lastError = string.Empty;
                acceptedFrames++;

                if (frames.Count >= queueCapacity)
                {
                    frames.Dequeue();
                    droppedFrames++;
                }

                frames.Enqueue(frame);
                return true;
            }
        }

        public bool TryDequeueLatest(out RemoteCueSensorFrame frame)
        {
            lock (sync)
            {
                frame = default;
                if (frames.Count == 0)
                {
                    return false;
                }

                while (frames.Count > 0)
                {
                    frame = frames.Dequeue();
                }

                return true;
            }
        }

        public RemoteCueSessionSnapshot GetSnapshot(double nowRealtimeSeconds)
        {
            lock (sync)
            {
                var age = double.IsNegativeInfinity(lastFrameReceivedSeconds)
                    ? -1.0
                    : Math.Max(0.0, nowRealtimeSeconds - lastFrameReceivedSeconds);

                RemoteCueReceiverState state;
                if (!listening)
                {
                    state = RemoteCueReceiverState.Disconnected;
                }
                else if (transportError.Length > 0)
                {
                    state = RemoteCueReceiverState.Error;
                }
                else if (lastFrameReceivedSeconds == double.NegativeInfinity)
                {
                    state = RemoteCueReceiverState.Listening;
                }
                else
                {
                    state = age <= staleTimeoutSeconds
                        ? RemoteCueReceiverState.Connected
                        : RemoteCueReceiverState.Stale;
                }

                return new RemoteCueSessionSnapshot(
                    state,
                    port,
                    sender,
                    age,
                    frames.Count,
                    acceptedFrames,
                    rejectedFrames,
                    droppedFrames,
                    lastError);
            }
        }

        private bool Reject(string message)
        {
            rejectedFrames++;
            lastError = message;
            return false;
        }
    }
}

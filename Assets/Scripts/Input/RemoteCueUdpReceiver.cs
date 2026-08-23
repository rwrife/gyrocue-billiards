using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace GyroCue.Input
{
    /// <summary>
    /// Game-phone UDP listener. Datagrams are parsed and filtered on a worker thread,
    /// then the newest accepted frame is dispatched from Update on Unity's main thread.
    /// Closing the socket cancels the blocking receive during pause/disable.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RemoteCueUdpReceiver : MonoBehaviour
    {
        [SerializeField]
        private int listenPort = RemoteCueProtocol.UdpPort;

        [SerializeField, Min(1)]
        private int queueCapacity = 8;

        [SerializeField, Min(0.05f)]
        private float staleTimeoutSeconds = 0.35f;

        [SerializeField]
        private bool listenOnEnable;

        private readonly object transportSync = new object();
        private RemoteCueSession session;
        private UdpClient udpClient;
        private CancellationTokenSource cancellation;
        private Task receiveTask;
        private bool resumeAfterPause;

        public event Action<RemoteCueSensorFrame> FrameReceived;

        public bool IsListening
        {
            get
            {
                var snapshot = Snapshot;
                return snapshot.State != RemoteCueReceiverState.Disconnected &&
                       snapshot.State != RemoteCueReceiverState.Error;
            }
        }

        public int ListenPort => listenPort;

        public string ListeningAddress => ResolveLocalIpv4Address();

        public RemoteCueSessionSnapshot Snapshot => EnsureSession().GetSnapshot(RealtimeClock.Seconds);

        public bool StartListening()
        {
            lock (transportSync)
            {
                StopTransportLocked(updateSession: false);

                try
                {
                    listenPort = Mathf.Clamp(listenPort, 1, 65535);
                    udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, listenPort));
                    cancellation = new CancellationTokenSource();
                    EnsureSession().StartListening(listenPort);
                    var client = udpClient;
                    var token = cancellation.Token;
                    receiveTask = Task.Run(() => ReceiveLoop(client, token), token);
                    return true;
                }
                catch (Exception exception) when (
                    exception is SocketException ||
                    exception is ObjectDisposedException ||
                    exception is ArgumentOutOfRangeException)
                {
                    StopTransportLocked(updateSession: true);
                    EnsureSession().StartListening(listenPort);
                    EnsureSession().MarkTransportError($"Could not listen on UDP {listenPort}: {exception.Message}");
                    return false;
                }
            }
        }

        public void StopListening()
        {
            lock (transportSync)
            {
                StopTransportLocked(updateSession: true);
            }
        }

        public void Configure(int port, int capacity, float staleSeconds)
        {
            if (IsListening)
            {
                throw new InvalidOperationException("Disconnect before changing receiver settings.");
            }

            listenPort = Mathf.Clamp(port, 1, 65535);
            queueCapacity = Mathf.Max(1, capacity);
            staleTimeoutSeconds = Mathf.Max(0.05f, staleSeconds);
            session = new RemoteCueSession(queueCapacity, staleTimeoutSeconds);
        }

        private void Awake()
        {
            EnsureSession();
        }

        private void OnEnable()
        {
            if (listenOnEnable)
            {
                StartListening();
            }
        }

        private void OnDisable()
        {
            StopListening();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                resumeAfterPause = IsListening;
                StopListening();
            }
            else if (resumeAfterPause && isActiveAndEnabled)
            {
                resumeAfterPause = false;
                StartListening();
            }
        }

        private void Update()
        {
            if (EnsureSession().TryDequeueLatest(out var latest))
            {
                FrameReceived?.Invoke(latest);
            }
        }

        private void ReceiveLoop(UdpClient client, CancellationToken token)
        {
            var remote = new IPEndPoint(IPAddress.Any, 0);

            while (!token.IsCancellationRequested)
            {
                try
                {
                    var bytes = client.Receive(ref remote);
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    var payload = Encoding.UTF8.GetString(bytes);
                    EnsureSession().TryAcceptDatagram(
                        payload,
                        remote.ToString(),
                        RealtimeClock.Seconds);
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (SocketException exception)
                {
                    if (!token.IsCancellationRequested)
                    {
                        EnsureSession().MarkTransportError($"UDP receive failed: {exception.Message}");
                    }

                    return;
                }
                catch (Exception exception)
                {
                    EnsureSession().MarkTransportError($"Cue packet processing failed: {exception.Message}");
                }
            }
        }

        private RemoteCueSession EnsureSession()
        {
            if (session == null)
            {
                session = new RemoteCueSession(queueCapacity, staleTimeoutSeconds);
            }

            return session;
        }

        private void StopTransportLocked(bool updateSession)
        {
            cancellation?.Cancel();
            udpClient?.Close();
            udpClient = null;

            if (receiveTask != null && !receiveTask.IsCompleted)
            {
                try
                {
                    receiveTask.Wait(100);
                }
                catch (AggregateException)
                {
                    // Socket close/cancellation is the expected shutdown path.
                }
            }

            receiveTask = null;
            cancellation?.Dispose();
            cancellation = null;

            if (updateSession)
            {
                EnsureSession().StopListening();
            }
        }

        private static string ResolveLocalIpv4Address()
        {
            try
            {
                var addresses = Dns.GetHostEntry(Dns.GetHostName()).AddressList;
                for (var i = 0; i < addresses.Length; i++)
                {
                    if (addresses[i].AddressFamily == AddressFamily.InterNetwork &&
                        !IPAddress.IsLoopback(addresses[i]))
                    {
                        return addresses[i].ToString();
                    }
                }
            }
            catch (SocketException)
            {
                // UI falls back to the wildcard address with actionable copy.
            }

            return "0.0.0.0";
        }

        private static class RealtimeClock
        {
            private static readonly System.Diagnostics.Stopwatch Stopwatch =
                System.Diagnostics.Stopwatch.StartNew();

            public static double Seconds => Stopwatch.Elapsed.TotalSeconds;
        }

        private void OnValidate()
        {
            listenPort = Mathf.Clamp(listenPort, 1, 65535);
            queueCapacity = Mathf.Max(1, queueCapacity);
            staleTimeoutSeconds = Mathf.Max(0.05f, staleTimeoutSeconds);
        }
    }
}

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace GyroCue.Input
{
    public enum CompanionSensorStreamState
    {
        Disconnected,
        Connected,
        Streaming
    }

    public interface ICompanionSensorTransport
    {
        bool Connect(string host, int port);

        void Disconnect();

        bool Send(string payload);
    }

    /// <summary>
    /// UDP transport implementation for companion-sensor frame streaming.
    /// </summary>
    public sealed class UdpCompanionSensorTransport : ICompanionSensorTransport, IDisposable
    {
        private UdpClient udpClient;
        private IPEndPoint remoteEndPoint;

        public bool Connect(string host, int port)
        {
            Disconnect();

            if (string.IsNullOrWhiteSpace(host) || port < 1 || port > 65535)
            {
                return false;
            }

            if (!TryResolveHost(host, out var ipAddress))
            {
                return false;
            }

            try
            {
                udpClient = new UdpClient();
                remoteEndPoint = new IPEndPoint(ipAddress, port);
                return true;
            }
            catch (SocketException)
            {
                Disconnect();
                return false;
            }
        }

        public void Disconnect()
        {
            remoteEndPoint = null;

            if (udpClient == null)
            {
                return;
            }

            udpClient.Close();
            udpClient = null;
        }

        public bool Send(string payload)
        {
            if (udpClient == null || remoteEndPoint == null || string.IsNullOrWhiteSpace(payload))
            {
                return false;
            }

            try
            {
                var bytes = Encoding.UTF8.GetBytes(payload);
                udpClient.Send(bytes, bytes.Length, remoteEndPoint);
                return true;
            }
            catch (SocketException)
            {
                return false;
            }
        }

        public void Dispose()
        {
            Disconnect();
        }

        private static bool TryResolveHost(string host, out IPAddress ipAddress)
        {
            if (IPAddress.TryParse(host, out ipAddress))
            {
                return true;
            }

            try
            {
                var addresses = Dns.GetHostAddresses(host);
                for (var index = 0; index < addresses.Length; index++)
                {
                    var candidate = addresses[index];
                    if (candidate.AddressFamily == AddressFamily.InterNetwork ||
                        candidate.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        ipAddress = candidate;
                        return true;
                    }
                }
            }
            catch (SocketException)
            {
                // Host resolution failed.
            }

            ipAddress = null;
            return false;
        }
    }

    /// <summary>
    /// Prototype companion-device sensor streaming controller for dual-phone cue mode.
    /// </summary>
    public sealed class CompanionSensorStreamer : MonoBehaviour
    {
        private const float GravityMps2 = 9.80665f;
        private const float SendScheduleToleranceSeconds = 0.0001f;

        [SerializeField]
        private string targetHost = "127.0.0.1";

        [SerializeField]
        private int targetPort = RemoteCueProtocol.UdpPort;

        [SerializeField, Min(1f)]
        private float streamRateHz = 60f;

        [SerializeField]
        private bool autoEnableGyroOnConnect = true;

        [SerializeField]
        private bool autoCaptureDeviceSensors = true;

        private ICompanionSensorTransport transport;
        private Func<float> timeProvider = () => Time.unscaledTime;
        private CompanionSensorStreamState streamState = CompanionSensorStreamState.Disconnected;
        private float nextSendRealtime = float.NegativeInfinity;
        private long nextSequence;

        public event Action<CompanionSensorStreamState> StreamStateChanged;

        public CompanionSensorStreamState StreamState => streamState;

        public string TargetHost => targetHost;

        public int TargetPort => targetPort;

        public float StreamRateHz => EffectiveStreamRateHz;

        public string SessionStatusText => BuildSessionStatusText();

        public void ConfigureTarget(string host, int port)
        {
            targetHost = host ?? string.Empty;
            targetPort = port;
        }

        public bool ConnectToTarget()
        {
            if (streamState != CompanionSensorStreamState.Disconnected)
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(targetHost) || targetPort < 1 || targetPort > 65535)
            {
                return false;
            }

            if (!EnsureTransport().Connect(targetHost, targetPort))
            {
                return false;
            }

            nextSequence = 0;
            nextSendRealtime = float.NegativeInfinity;
            SetStreamState(CompanionSensorStreamState.Connected);

            if (autoEnableGyroOnConnect)
            {
                UnityEngine.Input.gyro.enabled = true;
            }

            return true;
        }

        public void DisconnectFromTarget()
        {
            transport?.Disconnect();
            nextSequence = 0;
            nextSendRealtime = float.NegativeInfinity;
            SetStreamState(CompanionSensorStreamState.Disconnected);
        }

        public bool StartStreaming()
        {
            if (streamState == CompanionSensorStreamState.Disconnected)
            {
                return false;
            }

            if (streamState == CompanionSensorStreamState.Streaming)
            {
                return true;
            }

            nextSendRealtime = float.NegativeInfinity;
            SetStreamState(CompanionSensorStreamState.Streaming);
            return true;
        }

        public void StopStreaming()
        {
            if (streamState != CompanionSensorStreamState.Streaming)
            {
                return;
            }

            nextSendRealtime = float.NegativeInfinity;
            SetStreamState(CompanionSensorStreamState.Connected);
        }

        public bool StreamFrame(
            Quaternion orientation,
            Vector3 accelerationMps2,
            Vector3 angularVelocityRadPerSec,
            long timestampUnixMs)
        {
            if (streamState != CompanionSensorStreamState.Streaming)
            {
                return false;
            }

            var sendIntervalSeconds = 1f / EffectiveStreamRateHz;
            var now = NowSeconds;
            if (!ShouldSendAtCurrentTime(now, sendIntervalSeconds))
            {
                return false;
            }

            if (timestampUnixMs <= 0)
            {
                timestampUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }

            var frame = new RemoteCueSensorFrame(
                RemoteCueProtocol.SchemaVersionV1,
                timestampUnixMs,
                nextSequence,
                orientation,
                accelerationMps2,
                angularVelocityRadPerSec);

            if (!frame.IsValid)
            {
                return false;
            }

            var payload = RemoteCueSensorFrameJson.ToJson(frame);
            if (!EnsureTransport().Send(payload))
            {
                StopStreaming();
                return false;
            }

            nextSequence++;
            return true;
        }

        public void SetTransportForTests(ICompanionSensorTransport customTransport)
        {
            transport = customTransport;
        }

        public void SetTimeProviderForTests(Func<float> provider)
        {
            timeProvider = provider ?? (() => Time.unscaledTime);
        }

        public void SetStreamRateForTests(float rateHz)
        {
            streamRateHz = Mathf.Max(1f, rateHz);
        }

        private float NowSeconds => timeProvider();

        private float EffectiveStreamRateHz => Mathf.Max(1f, streamRateHz);

        private ICompanionSensorTransport EnsureTransport()
        {
            if (transport == null)
            {
                transport = new UdpCompanionSensorTransport();
            }

            return transport;
        }

        private bool ShouldSendAtCurrentTime(float now, float sendIntervalSeconds)
        {
            if (float.IsNegativeInfinity(nextSendRealtime))
            {
                nextSendRealtime = now;
            }

            if (now + SendScheduleToleranceSeconds < nextSendRealtime)
            {
                return false;
            }

            do
            {
                nextSendRealtime += sendIntervalSeconds;
            }
            while (nextSendRealtime <= now);

            return true;
        }

        private string BuildSessionStatusText()
        {
            if (streamState == CompanionSensorStreamState.Disconnected)
            {
                return "Disconnected";
            }

            if (streamState == CompanionSensorStreamState.Streaming)
            {
                return $"Streaming to {targetHost}:{targetPort} at {Mathf.RoundToInt(EffectiveStreamRateHz)} Hz";
            }

            return $"Connected to {targetHost}:{targetPort} (ready)";
        }

        private void SetStreamState(CompanionSensorStreamState nextState)
        {
            if (streamState == nextState)
            {
                return;
            }

            streamState = nextState;
            StreamStateChanged?.Invoke(streamState);
        }

        private void Update()
        {
            if (!autoCaptureDeviceSensors || streamState != CompanionSensorStreamState.Streaming)
            {
                return;
            }

            var orientation = SystemInfo.supportsGyroscope ? UnityEngine.Input.gyro.attitude : Quaternion.identity;
            var angularVelocity = SystemInfo.supportsGyroscope ? UnityEngine.Input.gyro.rotationRateUnbiased : Vector3.zero;
            var acceleration = UnityEngine.Input.acceleration * GravityMps2;

            StreamFrame(
                orientation,
                acceleration,
                angularVelocity,
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        }

        private void OnDisable()
        {
            DisconnectFromTarget();
        }

        private void OnDestroy()
        {
            if (transport is IDisposable disposableTransport)
            {
                disposableTransport.Dispose();
            }
            else
            {
                transport?.Disconnect();
            }

            transport = null;
        }

        private void OnValidate()
        {
            streamRateHz = Mathf.Max(1f, streamRateHz);
            targetPort = Mathf.Clamp(targetPort, 1, 65535);
        }
    }
}

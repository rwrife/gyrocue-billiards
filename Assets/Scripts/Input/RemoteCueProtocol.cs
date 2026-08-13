using System;

namespace GyroCue.Input
{
    /// <summary>
    /// Shared protocol constants for dual-phone cue sensor streaming.
    /// </summary>
    public static class RemoteCueProtocol
    {
        public const string SchemaVersionV1 = "gyrocue.sensor.v1";

        // UDP is the default for low-latency LAN streaming.
        public const int UdpPort = 28745;

        // WebSocket fallback for easier debugging and network tooling interoperability.
        public const int WebSocketPort = 28746;
        public const string WebSocketPath = "/gyrocue/v1/sensor";

        public static bool IsSupportedSchema(string schemaVersion)
        {
            return string.Equals(schemaVersion, SchemaVersionV1, StringComparison.Ordinal);
        }
    }
}

using System;
using UnityEngine;

namespace GyroCue.Input
{
    /// <summary>
    /// JSON parser/serializer for remote cue sensor frames.
    /// </summary>
    public static class RemoteCueSensorFrameJson
    {
        public static bool TryParse(string json, out RemoteCueSensorFrame frame)
        {
            frame = default;
            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            RemoteCueSensorFrameWire wire;
            try
            {
                wire = JsonUtility.FromJson<RemoteCueSensorFrameWire>(json);
            }
            catch (ArgumentException)
            {
                return false;
            }

            if (wire == null)
            {
                return false;
            }

            frame = new RemoteCueSensorFrame(
                wire.schemaVersion,
                wire.timestampUnixMs,
                wire.sequence,
                wire.orientation,
                wire.accelerationMps2,
                wire.angularVelocityRadPerSec);

            return frame.IsValid;
        }

        public static string ToJson(RemoteCueSensorFrame frame, bool prettyPrint = false)
        {
            var wire = new RemoteCueSensorFrameWire
            {
                schemaVersion = frame.SchemaVersion,
                timestampUnixMs = frame.TimestampUnixMs,
                sequence = frame.Sequence,
                orientation = frame.Orientation,
                accelerationMps2 = frame.AccelerationMps2,
                angularVelocityRadPerSec = frame.AngularVelocityRadPerSec
            };

            return JsonUtility.ToJson(wire, prettyPrint);
        }

        [Serializable]
        private sealed class RemoteCueSensorFrameWire
        {
            public string schemaVersion = string.Empty;
            public long timestampUnixMs;
            public long sequence;
            public Quaternion orientation = Quaternion.identity;
            public Vector3 accelerationMps2 = Vector3.zero;
            public Vector3 angularVelocityRadPerSec = Vector3.zero;
        }
    }
}

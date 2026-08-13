using UnityEngine;

namespace GyroCue.Input
{
    /// <summary>
    /// Normalized sensor frame from the optional second-phone cue device.
    /// </summary>
    public readonly struct RemoteCueSensorFrame
    {
        public RemoteCueSensorFrame(
            string schemaVersion,
            long timestampUnixMs,
            long sequence,
            Quaternion orientation,
            Vector3 accelerationMps2,
            Vector3 angularVelocityRadPerSec)
        {
            SchemaVersion = schemaVersion ?? string.Empty;
            TimestampUnixMs = timestampUnixMs;
            Sequence = sequence;
            Orientation = orientation;
            AccelerationMps2 = accelerationMps2;
            AngularVelocityRadPerSec = angularVelocityRadPerSec;
        }

        public string SchemaVersion { get; }

        public long TimestampUnixMs { get; }

        public long Sequence { get; }

        public Quaternion Orientation { get; }

        public Vector3 AccelerationMps2 { get; }

        public Vector3 AngularVelocityRadPerSec { get; }

        public bool IsSchemaSupported => RemoteCueProtocol.IsSupportedSchema(SchemaVersion);

        public bool IsValid =>
            IsSchemaSupported &&
            TimestampUnixMs > 0 &&
            Sequence >= 0 &&
            HasFiniteValues;

        private bool HasFiniteValues =>
            IsFinite(Orientation.x) &&
            IsFinite(Orientation.y) &&
            IsFinite(Orientation.z) &&
            IsFinite(Orientation.w) &&
            IsFinite(AccelerationMps2.x) &&
            IsFinite(AccelerationMps2.y) &&
            IsFinite(AccelerationMps2.z) &&
            IsFinite(AngularVelocityRadPerSec.x) &&
            IsFinite(AngularVelocityRadPerSec.y) &&
            IsFinite(AngularVelocityRadPerSec.z);

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}

using GyroCue.Input;
using NUnit.Framework;
using UnityEngine;

namespace GyroCue.Tests.EditMode
{
    public sealed class RemoteCueSensorFrameTests
    {
        [Test]
        public void IsValid_TrueForSupportedSchemaWithFiniteSensorValues()
        {
            var frame = new RemoteCueSensorFrame(
                RemoteCueProtocol.SchemaVersionV1,
                timestampUnixMs: 1723600000123,
                sequence: 14,
                orientation: Quaternion.Euler(2f, 15f, 0f),
                accelerationMps2: new Vector3(0.2f, -9.6f, 0.1f),
                angularVelocityRadPerSec: new Vector3(0.01f, 0.2f, -0.1f));

            Assert.That(frame.IsSchemaSupported, Is.True);
            Assert.That(frame.IsValid, Is.True);
        }

        [Test]
        public void IsValid_FalseWhenSchemaVersionIsUnsupported()
        {
            var frame = new RemoteCueSensorFrame(
                "gyrocue.sensor.v0",
                timestampUnixMs: 1723600000123,
                sequence: 0,
                orientation: Quaternion.identity,
                accelerationMps2: Vector3.zero,
                angularVelocityRadPerSec: Vector3.zero);

            Assert.That(frame.IsSchemaSupported, Is.False);
            Assert.That(frame.IsValid, Is.False);
        }

        [Test]
        public void TryParse_ParsesExpectedWireFields()
        {
            var payload = "{\"schemaVersion\":\"gyrocue.sensor.v1\",\"timestampUnixMs\":1723600000123,\"sequence\":7,\"orientation\":{\"x\":0.0,\"y\":0.25881904,\"z\":0.0,\"w\":0.9659258},\"accelerationMps2\":{\"x\":0.10,\"y\":-9.72,\"z\":0.02},\"angularVelocityRadPerSec\":{\"x\":0.03,\"y\":0.11,\"z\":-0.04}}";

            var didParse = RemoteCueSensorFrameJson.TryParse(payload, out var frame);

            Assert.That(didParse, Is.True);
            Assert.That(frame.Sequence, Is.EqualTo(7));
            Assert.That(frame.TimestampUnixMs, Is.EqualTo(1723600000123));
            Assert.That(frame.AccelerationMps2.y, Is.EqualTo(-9.72f).Within(0.001f));
            Assert.That(frame.AngularVelocityRadPerSec.z, Is.EqualTo(-0.04f).Within(0.001f));
            Assert.That(frame.IsValid, Is.True);
        }

        [Test]
        public void TryParse_ReturnsFalseWhenRequiredFieldsAreMissing()
        {
            var payload = "{\"timestampUnixMs\":1723600000123,\"sequence\":4}";

            var didParse = RemoteCueSensorFrameJson.TryParse(payload, out _);

            Assert.That(didParse, Is.False);
        }
    }
}

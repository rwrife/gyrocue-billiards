# Dual-phone cue sensor protocol (Issues #10 and #36)

The 3D `Practice` table now includes an opt-in UDP receiver and setup panel. The wire
protocol remains `gyrocue.sensor.v1`; fresh remote frames steer the table on the XZ
plane and trigger strokes, while stale/disconnected sessions immediately restore the
normal touch controls.

The input mapping is deliberately hybrid for a casual two-phone setup:

- cue-phone heading sets horizontal table aim;
- forward acceleration sets stroke power/release;
- the game phone's ball-face and elevation widgets retain tip offset and cue elevation.

A stationary cue phone **does** set aim. Its first valid heading is applied immediately;
subsequent stationary movement is drift-clamped. This resolves the former 90-degree
mapping failure without making a held phone unresponsive.

This document defines the wire contract between the optional companion phone (sensor source) and the game phone (Unity table app).

## Versioning

- **Schema version:** `gyrocue.sensor.v1`
- Packets with unknown schema versions must be ignored by the receiver.

## Transport choice

Primary + fallback transport options are supported:

1. **UDP over LAN (default)**
   - Lowest overhead/latency for high-rate motion streaming.
   - Best for continuous orientation/accel/gyro frames where occasional packet loss is acceptable.
   - **Port:** `28745`

2. **WebSocket over LAN (fallback/debug)**
   - Easier to inspect with standard tooling and browser/dev-server stacks.
   - Useful for debugging and early integration while preserving the same packet schema.
   - **Endpoint:** `ws://<host>:28746/gyrocue/v1/sensor`

## Packet schema (`gyrocue.sensor.v1`)

Each packet is a single JSON object:

| Field | Type | Units | Notes |
|---|---|---:|---|
| `schemaVersion` | string | n/a | Must be `gyrocue.sensor.v1` |
| `timestampUnixMs` | integer (int64) | milliseconds | Sender wall-clock timestamp (Unix epoch ms) |
| `sequence` | integer (int64) | count | Monotonic frame counter per session |
| `orientation` | object `{x,y,z,w}` | unit quaternion | Device attitude in sender device frame |
| `accelerationMps2` | object `{x,y,z}` | m/s² | Linear acceleration vector |
| `angularVelocityRadPerSec` | object `{x,y,z}` | rad/s | Gyroscope angular velocity |

## Example payloads

### UDP datagram payload example

```json
{
  "schemaVersion": "gyrocue.sensor.v1",
  "timestampUnixMs": 1723600000123,
  "sequence": 241,
  "orientation": { "x": 0.0000, "y": 0.2588, "z": 0.0000, "w": 0.9659 },
  "accelerationMps2": { "x": 0.12, "y": -9.71, "z": 0.06 },
  "angularVelocityRadPerSec": { "x": 0.04, "y": 0.10, "z": -0.03 }
}
```

### WebSocket text-frame payload example

```json
{
  "schemaVersion": "gyrocue.sensor.v1",
  "timestampUnixMs": 1723600000456,
  "sequence": 242,
  "orientation": { "x": 0.0021, "y": 0.2611, "z": -0.0017, "w": 0.9653 },
  "accelerationMps2": { "x": 0.08, "y": -9.74, "z": 0.01 },
  "angularVelocityRadPerSec": { "x": 0.02, "y": 0.07, "z": -0.01 }
}
```

## Receiver validation rules

A frame is accepted only when:

- `schemaVersion == "gyrocue.sensor.v1"`
- `timestampUnixMs > 0`
- `sequence >= 0`
- all numeric components are finite (not `NaN`/`Infinity`)

## Unity runtime mapping (Issue #13)

The game-side adapter that converts sensor frames into cue aim + shot commands is:

- `Assets/Scripts/Input/RemoteSensorInputAdapter.cs`

Key tunables are exposed as serialized fields for per-device balancing:

- `aimDeadzoneDegrees` — ignores tiny orientation jitter.
- `aimSensitivity` — scales lateral orientation response.
- `aimSmoothingFactor` — low-pass blend for orientation updates.
- `maxAimStepDegreesPerFrame` — clamps single-frame direction spikes.
- `stationaryAngularVelocityThresholdRadPerSec` — identifies low-motion drift windows.
- `stationaryForwardAccelerationThresholdMps2` — forward-axis stillness gate for drift logic.
- `stationaryAimDriftClampDegrees` — prevents runaway aim drift while device is stationary.
- `shotTriggerAccelerationMps2` — forward-acceleration threshold required to fire.
- `shotTriggerRearmAccelerationMps2` — release/rearm threshold to prevent repeated fire spam.
- `shotPowerSensitivity` — maps trigger-overdrive acceleration to normalized shot power.
- `forwardAccelerationSmoothingFactor` — configurable smoothing for shot-trigger spikes.
- `forwardAccelerationDriftCorrectionFactor` — baseline correction for forward-axis drift.
- `frameTimeoutSeconds` — marks remote control inactive when stream freshness is lost.
- `calibrationSampleTarget` — number of frames to average during stacked-phone calibration.
- `calibrationMaxDurationSeconds` — hard cap for calibration completion (clamped below 10s).

## Calibration flow (Issue #12)

The remote adapter now supports a quick alignment flow that compensates frame offsets between the cue phone and table phone.

1. Player opens pause/settings and taps **Recalibrate cue phone**.
2. UI calls `CueInputCoordinator.BeginRemoteCalibration()`.
3. While calibration is in progress, touch fallback remains enabled (remote lock is temporarily released).
4. After enough samples arrive before timeout, the adapter computes a calibration offset and marks state as `Calibrated`.
5. If calibration exceeds the max duration window, state becomes `TimedOut` and gameplay continues with the last successful offset (if any).

Touch fallback and calibration commands are coordinated by:

- `Assets/Scripts/Input/CueInputCoordinator.cs`

Reference runtime helpers implementing the wire contract:

- `Assets/Scripts/Input/RemoteCueProtocol.cs`
- `Assets/Scripts/Input/RemoteCueSensorFrame.cs`
- `Assets/Scripts/Input/RemoteCueSensorFrameJson.cs`

## Companion streamer prototype (Issue #11)

A first-pass companion-side controller now lives in:

- `Assets/Scripts/Input/CompanionSensorStreamer.cs`

It is designed for a second phone build that streams sensor frames to the game phone over LAN.

### Session UX states

`CompanionSensorStreamer` exposes explicit state transitions suitable for simple mobile UI buttons:

1. `Disconnected`
2. `Connected` (target selected + transport ready)
3. `Streaming` (actively sending frames)

The controller provides `SessionStatusText` so a companion UI can display clear status copy for connect/disconnect/start/stop actions.

### Runtime behavior

- `ConnectToTarget()` validates `targetHost:targetPort` and opens UDP transport.
- `StartStreaming()` only succeeds from `Connected` state.
- `StopStreaming()` returns to `Connected` without tearing down the session.
- `DisconnectFromTarget()` closes transport and resets sequence/timing state.
- `StreamFrame(...)` enforces a stable send cadence via `streamRateHz` (default 60 Hz), serializes `RemoteCueSensorFrame` JSON payloads, and increments sequence monotonically.

This keeps issue #11 scoped to a mergeable prototype while preserving compatibility with the receiver-side contract.

## Game-phone receiver and setup panel (Issue #36)

`RemoteCueUdpReceiver` binds UDP port `28745` only after the player taps **Listen**.
The blocking socket and JSON validation run on a worker task. Accepted frames enter a
bounded queue; the main thread drains only the newest frame, so a burst cannot make
Unity replay stale motion. The first valid sender owns the session until disconnect,
and duplicate/out-of-order sequence numbers and packets from other senders are ignored.

The setup panel shows:

- the game phone's listening IPv4 address and port;
- `OFF`, `LISTENING`, `CONNECTED`, `STALE`, or `ERROR` state;
- paired sender and last-frame age;
- actionable socket/malformed-packet guidance;
- **Listen/Retry**, **Disconnect**, **Calibrate**, and **Use touch** actions.

`RemoteCueUdpReceiver` closes the socket on disable or app pause. If it had been
listening, it starts a fresh session on resume, allowing the sender and sequence counter
to reconnect cleanly. A stale stream releases `PracticeInputRouter` immediately and
keeps the game playable with touch.

## Two-device setup walkthrough

1. Put both phones on the same trusted Wi-Fi network. Guest networks may isolate devices.
2. On the **game phone**, open `Practice` and tap **Listen** in the cue-phone panel.
3. Note the displayed address, for example `192.168.1.42:28745`.
4. On the **cue phone**, enter that host and UDP port in the companion screen, connect,
   and start streaming `gyrocue.sensor.v1` frames.
5. When the game phone says **CONNECTED**, stack/align the phones and tap **Calibrate**.
6. Lift the cue phone. Its heading aims; a forward stroke releases the shot. Set spin
   and elevation with the game phone's existing ball-face/right-edge widgets.
7. Tap **Use touch** at any time to close the listener and return to touch-only play.

If state becomes **STALE**, confirm both phones remain on the same LAN, disable VPN or
client isolation, and reconnect. Malformed packets and unexpected senders are ignored;
they do not freeze gameplay.

## Mobile local-network permissions

### iOS / iPadOS

The post-build hook at `Assets/Editor/LocalNetworkPermissionPostprocessor.cs` adds
`NSLocalNetworkUsageDescription` to the generated Xcode `Info.plist`. The first listen
may show Apple's Local Network prompt; allow it. If denied, enable **Settings → Privacy
& Security → Local Network → GyroCue** before retrying. No Bonjour service is advertised;
the player enters/reads the numeric LAN address directly.

### Android

`PlayerSettings` forces the normal `android.permission.INTERNET` permission so UDP LAN
traffic is available in builds. It is install-time and does not show a runtime prompt.
GyroCue does not scan SSIDs or nearby devices, so it does not request location or
`NEARBY_WIFI_DEVICES`. Vendor firewalls/VPNs can still block peer-to-peer UDP; keep both
phones on the same non-isolated Wi-Fi network.

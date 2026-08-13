# Scripts

Runtime gameplay scripts live here.

- `Core/` for foundational game services and bootstrap code.
  - `GameBootstrap.cs` sets mobile runtime defaults.
  - `TableLayoutConstants.cs` centralizes table dimensions and camera-fit math.
  - `TurnStateMachine.cs` provides a lightweight turn lifecycle + terminal win/loss flow for 8-ball-style matches.
  - `PoolPhysicsTuningProfile.cs` centralizes configurable ball mass/drag/restitution/friction defaults and applies them to 2D bodies/colliders.
  - `PoolPhysicsMath.cs` provides deterministic cushion-bounce/rest helpers used by physics tuning and edit-mode tests.
- `Input/` for player input adapters and shot-intent composition.
  - `TouchAimSwipeController.cs` handles drag-to-aim + pull/release shot gestures.
  - `TouchShotMath.cs` exposes testable aim/power helpers.
  - `ShotCommand.cs` defines normalized shot payloads for downstream systems.
- Planned follow-up adapters:
  - `RemoteSensorInputAdapter` for optional dual-phone cue mode.

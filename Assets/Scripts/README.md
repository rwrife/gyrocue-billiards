# Scripts

Runtime gameplay scripts live here.

- `Core/` for foundational game services and bootstrap code.
  - `GameBootstrap.cs` sets mobile runtime defaults.
  - `TableLayoutConstants.cs` centralizes table dimensions and camera-fit math.
  - `TurnStateMachine.cs` provides a lightweight turn lifecycle + terminal win/loss flow for 8-ball-style matches.
  - `PoolPhysicsTuningProfile.cs` centralizes configurable ball mass/drag/restitution/friction defaults and applies them to 2D bodies/colliders.
  - `PoolPhysicsMath.cs` provides deterministic cushion-bounce/rest helpers used by physics tuning and edit-mode tests.
  - `PocketTableController.cs` resolves pocket events, de-duplicates trigger contacts, removes pocketed balls from simulation, and exposes scratch events for the rules layer.
  - `PocketTriggerReporter.cs` forwards pocket trigger collisions to the shared pocket controller.
- `Input/` for player input adapters and shot-intent composition.
  - `TouchAimSwipeController.cs` handles drag-to-aim + pull/release shot gestures.
  - `TouchShotMath.cs` exposes testable aim/power helpers.
  - `ShotCommand.cs` defines normalized shot payloads for downstream systems.
  - `RemoteCueProtocol.cs` centralizes schema version + default LAN transport endpoints for dual-phone mode.
  - `RemoteCueSensorFrame.cs` defines validated timestamp/orientation/acceleration/gyro payloads.
  - `RemoteCueSensorFrameJson.cs` parses/serializes protocol JSON for network adapters.
  - `RemoteSensorInputAdapter.cs` maps second-phone orientation/acceleration frames into aim + shot commands.
  - `CueInputCoordinator.cs` gates touch-vs-remote control and keeps touch fallback instant when remote frames go stale.

# Scripts

Runtime gameplay scripts live here.

`Practice/` is the current, 3D stack. `Core/`, `Input/` and `UI/` are the earlier 2D
prototype, kept for reference until practice mode covers the same ground — see the
README section in the repository root for what is and is not 3D yet.

- `Practice/` for the 3D single-player practice table. See
  [practice-mode.md](../../docs/practice-mode.md).
  - `PracticeTableLayout.cs` holds real-world table geometry in metres, on the XZ plane
    with the cloth at `y = 0`, plus rack and pocket positions.
  - `PracticeTableBuilder.cs` builds the whole table from Unity primitives at runtime and
    wires the control rig. Primitives are placeholders for real art.
  - `CueStrikeMath.cs` converts a stroke into cue-ball motion: speed from power, spin from
    the tip's offset on the ball face, and hop from an elevated cue striking above centre.
  - `CueStrokeGesture.cs` is the pure draw-back/stroke-forward state machine, read in
    ball-face coordinates; slide speed sets power and the stopping point sets tip contact.
  - `ClothContactMotion.cs` applies cloth friction to both velocity and spin while the
    contact patch slides, which is what turns backspin into draw, then rolls the ball.
  - `PracticeControlLayout.cs` defines the on-screen widget regions shared by the HUD and
    the input router, and maps screen points to ball-face and elevation values.
  - `PracticeInputRouter.cs` routes one drag to one action based on where it starts:
    stroke widget, elevation strip, or camera aim. Mouse and touch share the same path.
  - `OrbitAimController.cs` swings the camera around the cue ball; where it looks is where
    the shot goes.
  - `CueStickView.cs` places a primitive cue behind the ball for the current aim, draw,
    tip offset, and elevation.
  - `PracticePocket.cs` reports balls whose centre drops into a pocket mouth.
  - `BallIdentity.cs` gives the cue and 15 object balls stable rule identities independent
    of GameObject names, colours, and rack-list order.
  - `CueBallContactTracker.cs` records the first numbered object ball touched by each 3D shot.
  - `EightBallRules.cs` owns the deterministic standard rack, open-table group assignment,
    scores/remaining balls, casual fouls, HUD-ready reasons, and 8-ball win/loss outcomes.
  - `PracticeSessionController.cs` runs the single-player loop: stroke to shot, settle
    detection, pocketing, scratch spotting, and re-rack.
  - `PracticeHud.cs` shows session stats, a live stroke readout, and the control widgets.
- `Core/` (2D prototype) for foundational game services and bootstrap code.
  - `GameBootstrap.cs` sets mobile runtime defaults.
  - `TableLayoutConstants.cs` and `TableRackMath.cs` centralize 2D table dimensions,
    cushion segments, pocket mouths, and the rack triangle.
  - `TableSceneBuilder.cs` builds the 2D `MainTable` scene at runtime.
  - `TurnStateMachine.cs` provides a lightweight turn lifecycle + terminal win/loss flow for 8-ball-style matches.
  - `PoolPhysicsTuningProfile.cs` centralizes configurable ball mass/drag/restitution/friction defaults and applies them to 2D bodies/colliders.
  - `PoolPhysicsMath.cs` provides deterministic cushion-bounce/rest helpers used by physics tuning and edit-mode tests.
  - `PocketTableController.cs` resolves pocket events, de-duplicates trigger contacts, removes pocketed balls from simulation, and exposes scratch events for the rules layer.
  - `PocketTriggerReporter.cs` forwards pocket trigger collisions to the shared pocket controller.
  - `CueBallPlacementController.cs` manages cue-ball-in-hand placement after fouls and clamps placement to playable table bounds.
- `Input/` (2D prototype) for player input adapters and shot-intent composition.
  - `TouchAimSwipeController.cs` handles drag-to-aim + pull/release shot gestures.
  - `TouchShotMath.cs` exposes testable aim/power helpers.
  - `ShotCommand.cs` defines normalized shot payloads for downstream systems.
  - `CuePreviewVisualizer.cs` renders cue indicator + trajectory preview, preferring fresh remote aim and hiding during active shot simulation.
  - `RemoteCueProtocol.cs` centralizes schema version + default LAN transport endpoints for dual-phone mode.
  - `RemoteCueSensorFrame.cs` defines validated timestamp/orientation/acceleration/gyro payloads.
  - `RemoteCueSensorFrameJson.cs` parses/serializes protocol JSON for network adapters.
  - `CompanionSensorStreamer.cs` provides a prototype companion-phone session controller (connect/disconnect/start/stop) plus UDP frame streaming at a stable send cadence.
  - `RemoteSensorInputAdapter.cs` maps second-phone orientation/acceleration frames into aim + shot commands and supports a quick stacked-phone calibration pass (target: <10s).
  - `CueInputCoordinator.cs` gates touch-vs-remote control, keeps touch fallback instant when remote frames go stale, and exposes calibration start/cancel hooks suitable for pause/settings UI buttons.
- `UI/` for the title screen, plus the 2D prototype's HUD state and rendering helpers.
  - `TitleScreenController.cs` builds the title screen canvas at runtime and opens
    practice mode.
  - `MinimalHudState.cs` is a testable formatter/state model for turn, power, foul, and win/loss messaging.
  - `HudScaleUtility.cs` scales HUD font sizes from short-edge screen resolution for phone readability.
  - `MinimalHudPresenter.cs` binds formatted HUD state to `UI.Text` labels, polls touch/remote preview power, and surfaces foul/terminal status colors.

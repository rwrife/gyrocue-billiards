# Minimal HUD (Issue #9)

> **Legacy — 2D prototype.** This describes `MinimalHudPresenter`, the HUD for the 2D
> `MainTable` scene. 3D practice mode has its own HUD (`PracticeHud`) showing session
> stats and a live stroke readout. Kept for reference until the 2D stack is retired.

This document covers the first-pass mobile HUD for turn, power, foul, and terminal match state.

## Added runtime pieces

- `Assets/Scripts/UI/MinimalHudState.cs`
  - Testable, headless-safe model/formatter for HUD strings.
  - Produces:
    - turn label (`Turn N • Player X`)
    - power label (`Power XX%`)
    - status text + tone (info/warning/success/danger)
- `Assets/Scripts/UI/HudScaleUtility.cs`
  - Computes a clamped HUD scale factor from shortest screen edge.
  - Baseline: `1080` short edge, clamp range default `0.85 .. 1.3`.
- `Assets/Scripts/UI/MinimalHudPresenter.cs`
  - Binds `MinimalHudState` output to `UnityEngine.UI.Text` labels.
  - Polls touch/remote input preview power each frame.
  - Applies tone-based status colors and dynamic font scaling.

## Integration pattern

Attach `MinimalHudPresenter` to a HUD root object (or existing gameplay root), then assign:

- `turnLabel` (`UI.Text`)
- `powerLabel` (`UI.Text`)
- `statusLabel` (`UI.Text`)

Optional input links:

- `TouchAimSwipeController`
- `RemoteSensorInputAdapter`
- `CueInputCoordinator`

## Driving gameplay state into HUD

Call from gameplay orchestration code:

- `ResetForNewMatch(playerOneStarts)` when a rack starts
- `SetTurnState(playerIndex, turnNumber, requiresCueBallPlacement)` when aiming phase begins
- `ApplyTurnResolution(...)` immediately after turn resolution/foul/win/loss decisions

This keeps status messaging aligned with turn/foul transitions while preserving touch + dual-phone power preview updates.

## Edit mode verification

`Assets/Tests/EditMode/MinimalHudStateTests.cs` validates:

- baseline turn/power/status formatting
- foul + cue-ball-in-hand warning messaging
- terminal win messaging + power reset
- short-edge scale clamping for common phone resolutions

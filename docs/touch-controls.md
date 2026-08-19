# Touch Controls (Issue #4)

> **Legacy — 2D prototype.** This describes `TouchAimSwipeController`, the drag-to-aim
> and pull-to-charge gesture used by the 2D `MainTable` scene. 3D practice mode uses a
> different scheme — camera-drag aiming plus a ball-face stroke widget — documented in
> [practice-mode.md](practice-mode.md). Kept for reference until the 2D stack is retired.

This document describes the first-pass single-phone control path implemented in `TouchAimSwipeController`.

## Gesture model

1. **Drag to aim:** moving the finger beyond the deadzone updates `AimDirection`.
2. **Pull to charge:** moving opposite the aim vector accumulates pull distance.
3. **Release to shoot:** ending the touch emits a `ShotCommand` when normalized power meets the minimum threshold.

## Tunables

- `aimDeadzonePixels`: suppresses accidental aim jitter on tiny movements.
- `maxPullDistancePixels`: pull distance that maps to full (`1.0`) normalized power.
- `powerCurve`: nonlinear power curve for casual/mobile feel tuning.
- `minimumLaunchPower`: prevents accidental micro-shots.

## Runtime lock behavior

`SetBallsMoving(true)` locks controls and cancels any active gesture so users cannot queue overlapping shots while balls are still moving.

## Cue visualization (Issue #5)

`CuePreviewVisualizer` provides the first-pass shot preview layer.

- Draws a `LineRenderer` trajectory preview from cue ball anchor in real time.
- Optionally shows a cue indicator transform aligned to the current aim direction.
- Prefers fresh remote-sensor aim vectors when dual-phone input is active, otherwise uses touch aim.
- Hides preview visuals while shot simulation is active (`SetBallsMoving(true)`), then restores once play returns to aiming.

## Follow-up integration

- Connect `ShotReleased` to cue-ball impulse application when physics issue work lands.
- Mirror shot intents into optional second-device cue pipeline so dual-phone mode can share the same shot command contract.
- Wire pause/settings UI buttons to `CueInputCoordinator.BeginRemoteCalibration()` / `CancelRemoteCalibration()` so stacked-phone alignment can be re-run mid-session.

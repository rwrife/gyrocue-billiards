# Touch Controls (Issue #4)

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

## Follow-up integration

- Connect `ShotReleased` to cue-ball impulse application when physics issue work lands.
- Mirror shot intents into optional second-device cue pipeline so dual-phone mode can share the same shot command contract.

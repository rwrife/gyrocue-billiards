# MainTable Scene Layout (Issue #2)

> **Legacy — 2D prototype.** This describes `MainTable.unity`, the original 2D
> `Rigidbody2D` table. The game has since moved to 3D; the live table layout is
> `PracticeTableLayout` and is documented in [practice-mode.md](practice-mode.md).
> Kept for reference until the 2D stack is retired.

This document captures the initial layout scaffolding for `Assets/Scenes/MainTable.unity`.

## Goals covered

- Main table scene created for the mobile-first play surface.
- Camera configured for portrait-friendly orthographic framing.
- Placeholder transforms added for:
  - table bounds
  - 4 cushions
  - 6 pockets

## World-space baseline

- Table width: `20`
- Table height: `10`
- Cushion thickness: `0.6`
- Pocket radius placeholder scale: `0.55`
- Camera orthographic size: `11.2`

## Placeholder hierarchy

- `TableRoot`
  - `TableBoundsPlaceholder`
  - `CushionTopPlaceholder`
  - `CushionBottomPlaceholder`
  - `CushionLeftPlaceholder`
  - `CushionRightPlaceholder`
  - `PocketTopLeftPlaceholder`
  - `PocketTopCenterPlaceholder`
  - `PocketTopRightPlaceholder`
  - `PocketBottomLeftPlaceholder`
  - `PocketBottomCenterPlaceholder`
  - `PocketBottomRightPlaceholder`

## Runtime shot lifecycle wiring (Issue #33)

Add a `GameSession` object (or use the existing gameplay/input root) and attach
`ShotLifecycleController` beside the input and presentation components. Wire:

- **Cue Input Coordinator**: the `CueInputCoordinator` that combines touch and remote cue input.
- **Touch Aim Swipe Controller** and **Cue Preview Visualizer**: locked/hidden during simulation.
- **HUD Presenter**: receives turn, foul, cue-ball-in-hand, and terminal outcomes.
- **Cue Ball Body**: the live cue-ball `Rigidbody2D` that receives the impulse.
- **Ball Bodies**: every active cue/object-ball `Rigidbody2D`; settle detection must observe the full table.
- **Pocket Table Controller**: resets shot-scoped pocket/scratch tracking before each impulse.
- **Cue Ball Placement Controller**: activated when rule resolution returns a foul.
- **Physics Profile**: the shared `PoolPhysicsTuningProfile`; its stop threshold drives settle detection.
- **Rule Resolver Behaviour** (optional): a component implementing `IShotRuleResolver`. If omitted,
  the scratch-only fallback passes turns on misses/scratches and keeps the turn after an object ball.

Recommended starting tuning is `Max Cue Ball Impulse = 8`, `Settle Debounce Seconds = 0.25`,
and `Angular Stop Threshold = 3`. Tune impulse strength in-device while keeping the profile stop
threshold high enough to eliminate long-running micro-drift.

The runtime order is deliberately fixed:

1. `CueInputCoordinator.ShotReleased` requests `TurnStateMachine.TryBeginShot()`.
2. Pocket tracking resets, input locks, the preview hides, and normalized aim/power applies an impulse.
3. Every configured ball remains below linear/angular thresholds for the full debounce window.
4. Residual velocity is clamped, simulation moves to turn resolution, then `IShotRuleResolver` runs.
5. The HUD and cue-ball placement mode receive the resolved state; input reopens only when legal.

This separation keeps the upcoming full 8-ball evaluator independent of touch, remote sensor,
and physics polling code.

## Follow-up implementation notes

- Convert placeholders into prefabs with colliders/visuals.
- Bind `TableLayoutConstants` values to generated runtime/table setup.
- Add touch-aim and swipe-shot controls on top of this scene skeleton.

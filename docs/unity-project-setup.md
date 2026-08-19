# Unity Project Setup (Mobile-First)

This repository now includes a Unity-ready baseline scaffold.

## Target Editor Version

- **Unity `2022.3.62f3`**
- Stored in `ProjectSettings/ProjectVersion.txt`

## Initial Folder Layout

- `Assets/Scenes`
- `Assets/Scripts/Practice` — the 3D practice stack
- `Assets/Scripts/Core`, `Assets/Scripts/Input`, `Assets/Scripts/UI` — the earlier 2D stack
- `Assets/Prefabs`
- `Assets/Tests/EditMode`
- `Assets/Tests/PlayMode`

## Scenes

All three scenes are registered in Build Settings, `Title` first.

- **`Title.unity`** — builds its canvas at runtime and opens `Practice`.
- **`Practice.unity`** — the current focus. `PracticeTableBuilder` builds the 3D table
  from Unity primitives at runtime and wires the whole control rig.
- **`MainTable.unity`** — the earlier 2D prototype, kept for reference.

No scene carries inspector wiring; each is constructed in code, so there are no
serialized references to drift.

## Practice Mode (3D)

The current focus, and the first fully 3D part of the game. Single player, no turns:
pocketed balls stay down, a scratch spots the cue ball, and clearing the rack re-racks it.

`PracticeTableBuilder` builds the table at runtime from Unity primitives, in real-world
metres on the XZ plane with the cloth at `y = 0`. Primitives are placeholders for real art.

Drag the table to aim, use the ball-face widget at the bottom to stroke (draw down, slide
up — speed sets power, stopping point sets the tip contact), and the right-hand strip to
raise the cue.

Full detail — table geometry, control mapping, the strike and cloth-contact physics, and
how to run the tests — is in **[practice-mode.md](practice-mode.md)**.

## Notes

- `GameBootstrap` sets baseline runtime defaults for mobile framerate behavior.
- `BootstrapTests` provides a starter edit-mode test using Unity Test Framework.
- `Packages/manifest.json` includes core mobile/input/test dependencies, and both
  `com.unity.modules.physics` (3D, used by practice mode) and `com.unity.modules.physics2d`
  (used by the legacy 2D scene).
- The `CueBall` tag is registered in `ProjectSettings/TagManager.asset`; the 2D pocket
  controller falls back to it when no cue-ball reference is set.

## Next Recommended Steps

1. Replace the practice-mode primitives with real 3D art (meshes and materials only — the
   layout is derived, not authored).
2. Add practice goals: drills, targets, or a shot clock, so the mode has something to
   practise against.
3. Port match rules to 3D, then retire the 2D `MainTable` stack.
4. Fix the `RemoteSensorInputAdapter` aim mapping before resuming dual-phone work.

## Recently Completed

- Pivoted to 3D: `Assets/Scenes/Practice.unity` plus `Assets/Scripts/Practice/` implement a
  single-player 3D practice table with stroke-based cue control and jump-shot physics.
- Editor upgraded to `2022.3.62f3`; generated `.meta` files and `ProjectSettings` committed.
- `Assets/Scenes/Title.unity` added; opens practice mode.
- `Assets/Scenes/MainTable.unity` scaffold added with camera + table placeholder hierarchy.
- `Assets/Scripts/Core/TableLayoutConstants.cs` added for shared layout sizing and aspect-fit camera math.
- `docs/main-table-scene-layout.md` documents initial coordinates/object layout.
- `Assets/Scripts/Input/TouchAimSwipeController.cs` added for drag-to-aim + swipe-shot controls.
- `Assets/Tests/EditMode/TouchAimSwipeControllerTests.cs` and `TouchShotMathTests.cs` added for touch input behavior coverage.

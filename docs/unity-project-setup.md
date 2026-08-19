# Unity Project Setup (Mobile-First)

This repository now includes a Unity-ready baseline scaffold.

## Target Editor Version

- **Unity `2022.3.62f3`**
- Stored in `ProjectSettings/ProjectVersion.txt`

## Initial Folder Layout

- `Assets/Scenes`
- `Assets/Scripts`
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

Single player, no turns. Pocketed balls stay down, a scratch just spots the cue ball,
and clearing the rack re-racks it.

Geometry is real-world metres on the XZ plane with the cloth at `y = 0`, driven by
`PracticeTableLayout`: a 2.54m x 1.27m playfield and 57mm balls. Real units mean Unity's
default gravity is correct for jump shots with no scaling fudge. The scene sets a 5ms
fixed timestep, since balls that small and fast otherwise tunnel through a 5cm rail.

Primitives are placeholders. Replacing them with real art is a matter of swapping meshes
and materials — the layout is derived, not authored.

### Controls

- **Aim** — drag anywhere on the table to swing the camera around the cue ball. Where
  the camera looks is where the shot goes.
- **Stroke** — the widget at the bottom is the cue ball itself. Draw *down* below it to
  pull the cue back, then slide *up* to deliver. How fast you slide sets the power; where
  you stop sets the tip contact point, so a fast stroke through the top follows and a
  stab low on the ball draws. Past half a ball radius from centre the tip miscues.
- **Elevation** — the strip on the right raises the butt of the cue, up to 70 degrees.

### Shot physics

`CueStrikeMath` turns a stroke into cue-ball motion:

- Tip height gives follow or draw as overspin or backspin, via the solid-sphere result
  `w = (p x v) * 5 / 2r^2`.
- Tip offset left or right gives english about the vertical axis.
- An elevated cue trades horizontal speed for lift, but only when striking *above*
  centre — high tip, steep cue, and power together make the ball leave the cloth, while
  the same cue below centre scoops instead.

`ClothContactMotion` does the part PhysX will not: while the contact patch is sliding it
applies friction to the ball's velocity *and* its spin, which is what turns backspin into
draw. Once the patch stops sliding the ball rolls under rolling resistance alone.

## Notes

- `GameBootstrap` sets baseline runtime defaults for mobile framerate behavior.
- `BootstrapTests` provides a starter edit-mode test using Unity Test Framework.
- `Packages/manifest.json` includes core mobile/input/test dependencies.

## Next Recommended Steps

1. Wire `TouchAimSwipeController.ShotReleased` into cue-ball force application once physics tuning lands.
2. Replace `MainTable` placeholders with colliders/sprites/materials for playable geometry.
3. Add deterministic gameplay logic tests for turn transitions and foul handling.

## Recently Completed

- `Assets/Scenes/MainTable.unity` scaffold added with camera + table placeholder hierarchy.
- `Assets/Scripts/Core/TableLayoutConstants.cs` added for shared layout sizing and aspect-fit camera math.
- `docs/main-table-scene-layout.md` documents initial coordinates/object layout.
- `Assets/Scripts/Input/TouchAimSwipeController.cs` added for drag-to-aim + swipe-shot controls.
- `Assets/Tests/EditMode/TouchAimSwipeControllerTests.cs` and `TouchShotMathTests.cs` added for touch input behavior coverage.

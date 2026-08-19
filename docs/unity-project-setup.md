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

Both scenes are registered in Build Settings, `Title` first.

- **`Title.unity`** — `TitleScreenController` builds its canvas at runtime and loads
  `MainTable` from the PLAY button.
- **`MainTable.unity`** — `TableSceneBuilder` builds the whole playable table at runtime:
  felt, six cushion rails, six pocket triggers, a cue ball plus a fifteen-ball rack, the
  input/rules/HUD rig, and camera framing.

Neither scene carries inspector wiring; both are constructed in code, so there are no
serialized references to drift. Layout comes from `TableLayoutConstants` and
`TableRackMath`.

## Controls

Drag away from the cue ball to aim, pull back past your start point to build power, then
release to shoot. `PointerShotInput` maps mouse input onto the same gesture pipeline that
`TouchAimSwipeController` uses for touch, so the table is playable in the editor.
After a scratch, click to place the cue ball.

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

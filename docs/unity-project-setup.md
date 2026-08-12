# Unity Project Setup (Mobile-First)

This repository now includes a Unity-ready baseline scaffold.

## Target Editor Version

- **Unity `2022.3.40f1`**
- Stored in `ProjectSettings/ProjectVersion.txt`

## Initial Folder Layout

- `Assets/Scenes`
- `Assets/Scripts`
- `Assets/Prefabs`
- `Assets/Tests/EditMode`

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

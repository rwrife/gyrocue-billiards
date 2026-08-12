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

1. Create `Assets/Scenes/MainTable.unity` and wire `GameBootstrap`.
2. Add first-pass touch aim + swipe shot scripts under `Assets/Scripts/Input`.
3. Add deterministic gameplay logic tests under `Assets/Tests/EditMode`.

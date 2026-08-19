# Scenes

All three scenes are registered in Build Settings, `Title` first. None of them carry
inspector wiring — each is built in code at runtime, so there are no serialized
references to drift.

## Included

- `Title.unity` — `TitleScreenController` builds its canvas at runtime and opens
  `Practice`.
- `Practice.unity` — **the current focus.** `PracticeTableBuilder` builds the 3D practice
  table from Unity primitives: slate, split rails, six pocket mouths, cue ball and rack,
  cue stick, camera and control rig. See [practice-mode.md](../../docs/practice-mode.md).
- `MainTable.unity` — the earlier **2D prototype**, driven by `TableSceneBuilder` and the
  `Rigidbody2D` stack. Kept for reference until practice mode covers the same ground.

## Notes

- 3D layout constants live in `Assets/Scripts/Practice/PracticeTableLayout.cs`, in
  real-world metres on the XZ plane with the cloth at `y = 0`.
- The 2D prototype's constants live in `Assets/Scripts/Core/TableLayoutConstants.cs` and
  `TableRackMath.cs`.
- `MainTable` still contains the original empty `TableRoot` placeholder transforms. They
  are superseded by `TableSceneBuilder` and render nothing.

# Scenes

All three scenes are registered in Build Settings, `Title` first. None of them carry
inspector wiring — each is built in code at runtime, so there are no serialized
references to drift.

## Included

- `Title.unity` — `TitleScreenController` builds its canvas at runtime and opens
  `Practice`.
- `Practice.unity` — **the current focus.** A man-cave room (brick, wood, bar, fireplace,
  neon, memorabilia) authored directly in the scene as editor-editable GameObjects under
  `ManCaveRoom`, with the billiard table at its centre. Gameplay surfaces (playfield,
  cushions, pockets, balls) are built at runtime by `PracticeTableBuilder`. See
  [practice-mode.md](../../docs/practice-mode.md) and
  [man-cave-asset-list.md](../../docs/man-cave-asset-list.md) for the decoration
  placeholders and what to purchase for each.
- `MainTable.unity` — the earlier **2D prototype**, driven by `TableSceneBuilder` and the
  `Rigidbody2D` stack. Kept for reference until practice mode covers the same ground.

## Notes

- 3D layout constants live in `Assets/Scripts/Practice/PracticeTableLayout.cs`, in
  real-world metres on the XZ plane with the cloth at `y = 0`.
- The 2D prototype's constants live in `Assets/Scripts/Core/TableLayoutConstants.cs` and
  `TableRackMath.cs`.
- `MainTable` still contains the original empty `TableRoot` placeholder transforms. They
  are superseded by `TableSceneBuilder` and render nothing.

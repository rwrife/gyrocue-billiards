# MainTable Scene Layout (Issue #2)

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

## Follow-up implementation notes

- Convert placeholders into prefabs with colliders/visuals.
- Bind `TableLayoutConstants` values to generated runtime/table setup.
- Add touch-aim and swipe-shot controls on top of this scene skeleton.

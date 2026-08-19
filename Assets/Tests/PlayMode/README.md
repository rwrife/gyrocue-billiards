# PlayMode Tests

Integration tests that load a real scene and drive it.

- `PracticeTablePlayTests` — the 3D practice table: construction, shot-to-settle with no
  ball escaping the table, a draw shot pulling the cue ball back behind the contact point,
  an elevated jump clearing a full ball radius, and scratch spotting.
- `TableSceneBuilderPlayTests` — the legacy 2D `MainTable` scene: construction,
  shot-to-settle, and pocket detection.

See [gameplay-test-harness.md](../../../docs/gameplay-test-harness.md) for how to run
these headlessly.

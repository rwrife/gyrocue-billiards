# Prefabs

Store reusable gameplay prefabs here (balls, cue visuals, HUD widgets).

Currently empty by design: both playable scenes build their contents in code at runtime
rather than from prefabs, so there is nothing to keep in sync. Practice mode's table is
made of Unity primitives positioned from `PracticeTableLayout`.

This is the natural home for the real 3D art that replaces those primitives — meshes and
materials only, since the layout is derived rather than authored.

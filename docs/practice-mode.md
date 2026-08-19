# Practice Mode (3D)

Practice mode is the project's current focus and the first fully 3D part of the game.
It is single player: there are no turns and no opponent.

`Assets/Scenes/Practice.unity` holds one component, `PracticeTableBuilder`, which
constructs the entire table at runtime. Nothing is wired in the inspector, so there are
no serialized references to drift.

## Graphics status

Everything on the table is a Unity primitive — cubes for the slate and rails, cylinders
for the pocket mouths and cue, spheres for the balls. **These are deliberate
placeholders.** Because every position is derived from `PracticeTableLayout` rather than
authored in the scene, replacing them with real art is a matter of swapping meshes and
materials; no layout has to be re-derived.

Materials resolve `Universal Render Pipeline/Lit` and fall back to `Standard`, so the
scene renders under either pipeline.

## Table geometry

Real-world metres, laid out on the **XZ plane with the cloth at `y = 0`**:

| | |
|---|---|
| Playfield | 2.54 m x 1.27 m (9-foot) |
| Ball | 57.15 mm diameter, 0.17 kg |
| Cushion | 50 mm thick, 37.5 mm tall |
| Pockets | 60 mm corners, 65 mm sides |

Real units matter: Unity's default gravity is then already correct for jump shots, with
no scaling fudge anywhere in the physics.

The scene sets a **5 ms fixed timestep**. Balls this small moving this fast will
otherwise tunnel straight through a 50 mm rail in a single 20 ms step.

Long rails are split at the side pockets and short rails span between the corners, so
every pocket mouth stays open.

## Controls

One drag does one thing, decided by where it starts (`PracticeInputRouter`). Mouse and
touch travel the same path, so the editor plays like the phone.

### Aim — drag the table

Dragging anywhere outside the two widgets swings the camera around the cue ball
(`OrbitAimController`). Where the camera looks is where the shot goes, so framing and
aiming are one gesture. Pitch is clamped to 8-72 degrees.

### Stroke — the widget at the bottom is the cue ball

`CueStrokeGesture` reads the widget in **ball-face coordinates**: `(0, 0)` is dead
centre of the cue ball, `y = +1` and `y = -1` are its top and bottom edges, and below
`y = -1` is backswing room.

1. **Draw down** below the ball to pull the cue back.
2. **Slide up** to deliver.

Two things are read from that upward slide:

- **How fast you slide sets the power.** Speed is measured from the deepest point of the
  draw, so a stutter mid-pull does not count as the start of the delivery.
- **Where you stop sets the tip contact point.** Stopping high on the face follows,
  stopping low draws, stopping off to one side puts english on the ball.

A stroke needs a real backswing and a real delivery to fire — a pull-back alone does
nothing, and neither does a slow nudge.

### Elevation — the strip on the right

Raises the butt of the cue from level to 70 degrees.

## Shot physics

`CueStrikeMath` converts a stroke into cue-ball motion. It is a playable approximation
rather than a full contact simulation, but the relationships it encodes are the real
ones.

**Spin from the tip offset.** The tip lands at point `p` on the ball face and the
resulting angular velocity is the solid-sphere result:

```
w = (p x v) * 5 / (2 * r^2)
```

A maximum-height hit therefore spins the ball *faster* than a natural roll — real
overspin — while a low hit spins it backwards against its direction of travel. Offset
left or right gives english about the vertical axis.

**Miscue.** Past half a ball radius from centre the tip slides off: power drops to 35%
and all spin is lost.

**Jump shots.** An elevated cue trades horizontal speed (`cos`) for downward tip speed
(`sin`). That downward component drives the ball into the slate, which rebounds it —
but only when striking *above* centre. High tip plus steep cue plus power lifts the ball
off the cloth; the same cue below centre scoops it forward with no lift at all, which is
what happens on a real table.

**Cloth contact.** `ClothContactMotion` supplies what PhysX will not. While the contact
patch at the bottom of the ball is sliding, friction acts on the ball's velocity *and*
its spin — that coupling is what actually turns backspin into draw rather than
decoration. Once the patch stops sliding the ball rolls under rolling resistance alone,
and spin about the vertical axis is preserved separately, because that is english, not
roll.

## Session loop

`PracticeSessionController`:

- Pocketed object balls stay down.
- A scratch increments a counter and spots the cue ball back on the head half. There is
  no ball-in-hand, because there is no opponent to give it to.
- Clearing the rack re-racks it.
- Input is locked while balls are moving and unlocks once the table settles.

## Tests

- **EditMode** — `CueStrikeMathTests`, `CueStrokeGestureTests`, `PracticeTableLayoutTests`,
  `PracticeControlLayoutTests` cover the pure strike physics, the stroke state machine,
  and the two layout tables.
- **PlayMode** — `PracticeTablePlayTests` loads the real scene and checks that it builds,
  that a shot settles without balls escaping the table, that a draw shot pulls the cue
  ball back behind the contact point, that an elevated high strike clears a full ball
  radius and lands again, and that a scratch spots the cue ball.

Run them headlessly (the editor holds a lock on the project, so use a copy):

```bash
cp -R Assets Packages ProjectSettings /tmp/gyrocue-test/
/Applications/Unity/Hub/Editor/2022.3.62f3/Unity.app/Contents/MacOS/Unity \
  -batchmode -runTests -testPlatform PlayMode \
  -projectPath /tmp/gyrocue-test \
  -testResults /tmp/gyrocue-test/results.xml \
  -logFile /tmp/gyrocue-test/log.txt -nographics
```

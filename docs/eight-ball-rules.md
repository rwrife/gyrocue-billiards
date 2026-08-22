# Lightweight 8-ball rules (3D)

The first 3D match-rules increment lives in `Assets/Scripts/Practice/` beside the table it
will govern. It is deliberately split into a pure rules core and thin Unity identity/contact
components so the evaluator does not depend on scene names, rendering, or PhysX timing.

Practice mode remains single player. These rules are the shared foundation for the local
two-player match surface; they do not silently add turns or terminal outcomes to practice.

## Ball identity and rack

`BallIdentity` gives every runtime-built ball a stable number:

- `0` cue ball
- `1`–`7` solids
- `8` eight ball
- `9`–`15` stripes

`PracticeTableBuilder` attaches those identities and builds `EightBallRack.StandardBallNumbers`.
The deterministic rack places the 8-ball at the centre of row three and opposite groups in
the two back corners. `PracticeSessionController.RackAgain` resolves each position by identity,
so renaming or reordering GameObjects cannot corrupt a reset.

`CueBallContactTracker` is the 3D first-contact hook. Match orchestration calls `BeginShot`
before applying a stroke and reads `FirstContactBallNumber` after the table settles. Only the
first numbered object-ball collision is retained.

## Evaluator contract

After a shot settles, feed `EightBallRules.ResolveShot`:

- shooting player index (`0` or `1`)
- first contacted ball number, or `EightBallShotRecord.NoBall`
- pocketed ball numbers in pocket-event order
- cue-ball scratch state
- any balls detected outside the table

The returned `EightBallShotResolution` contains the existing `TurnResolutionResult`, a typed
`EightBallFoulReason`, and concise `FoulMessage` copy ready for a match HUD. The rules instance
tracks group assignment, remaining solids/stripes, ball availability, and each assigned
player's pocketed-ball score. `ResetMatch` restores the complete rack and open table.

## Intentional casual-rule simplifications

This is a quick local mobile game, not a tournament referee:

- The table stays open until the first **legal** solid or stripe is pocketed.
- If a legal open-table shot pockets both groups, pocket-event order decides assignment;
  the first solid/stripe down becomes the shooter's group.
- A turn continues only when the shot is foul-free and pockets at least one ball from the
  shooter's assigned group. Pocketing only an opponent ball passes the turn but is not by
  itself a foul.
- No contact, a scratch, the wrong group (or early 8-ball) contacted first, and any ball
  leaving the table are fouls.
- The 8-ball wins only when the shooter's group was already clear before the stroke,
  the 8-ball is contacted first, and the shot has no scratch or other foul. Pocketing the
  final group ball and the 8-ball together is early; an early/illegal 8-ball or 8-ball
  scratch loses.
- There is no called shot, called pocket, kitchen restriction, rail-after-contact rule,
  break-specific win/loss rule, three-foul rule, or stalemate procedure.

## Balls leaving the 3D table

The current rail geometry and `PracticeTablePlayTests` expect balls to remain in bounds, but
jump shots make an escape physically possible after future geometry changes. The rules
contract therefore handles it explicitly instead of ignoring it:

- cue ball off table: scratch foul
- solid/stripe off table: foul, then re-spot it (it remains available in rules state)
- 8-ball off table: immediate loss

The future match orchestrator owns out-of-bounds detection and physical re-spot placement.

## Tests

`Assets/Tests/EditMode/EightBallRulesTests.cs` covers stable identity/rack placement, open-table
assignment, mixed pockets, scratches, no contact, wrong first contact, opponent-only pockets,
legal win, early 8-ball loss, 8-ball scratch, off-table handling, reset, and first-contact
capture.

Run in Unity 2022.3.62f3 through **Window → General → Test Runner → EditMode**, or headlessly:

```bash
Unity -batchmode -nographics -runTests -testPlatform EditMode \
  -projectPath "$PWD" -testResults /tmp/gyrocue-editmode-results.xml \
  -logFile /tmp/gyrocue-editmode.log -quit
```

# Gameplay test harness (Issue #16)

This repository now includes an **EditMode gameplay harness** focused on turn outcomes and lightweight rules transitions without requiring scene/UI wiring.

- Harness tests: `Assets/Tests/EditMode/GameplayTestHarnessTests.cs`
- Core systems exercised:
  - `Assets/Scripts/Core/TurnStateMachine.cs`
  - `Assets/Scripts/Core/PocketTableController.cs`

## Scenario coverage

The harness executes shot-resolution scenarios and asserts final turn state:

1. Object ball pocketed with legal contact keeps the same player's turn.
2. Cue-ball scratch is treated as a foul and passes turn to the opponent.
3. Legal eight-ball pocket resolves to `MatchWon`.
4. Eight-ball + scratch resolves to `MatchLost` (regression fixture).
5. Duplicate cue-ball pocket signals are ignored after first pocket (regression fixture).
6. No-contact shot (without scratch) is still treated as a foul (regression fixture).
7. Foul outcomes now raise a cue-ball-in-hand requirement before the next shot can begin.

## Why this matters

- Provides scenario-level test coverage for turns/fouls/win/loss transitions.
- Gives future gameplay/rules changes a stable regression baseline.
- Keeps tests independent of full in-scene UI and touch input flow.

## CI-ready execution path

The existing workflow at `.github/workflows/unity-ci.yml` auto-detects `Assets/Tests/EditMode/*.cs` and runs EditMode tests through `game-ci/unity-test-runner` when `UNITY_LICENSE` is configured.

On runners where Unity licensing is unavailable, CI still executes sanity jobs and explicit fallback notes. The harness remains source-controlled and ready for full Unity EditMode execution in licensed environments.

## PlayMode suites

PlayMode integration tests now exist alongside the EditMode harness, in
`Assets/Tests/PlayMode/`:

- `PracticeTablePlayTests` — the 3D practice table. Loads `Practice.unity` and asserts it
  builds a cue ball, a fifteen-ball rack and six pockets; that a shot settles without any
  ball escaping the table; that a draw shot pulls the cue ball back behind the contact
  point; that an elevated high strike clears a full ball radius and lands again; and that
  a scratch spots the cue ball.
- `TableSceneBuilderPlayTests` — the legacy 2D `MainTable` scene: construction,
  shot-to-settle, and pocket detection.

The 3D practice stack also has EditMode coverage for its pure parts: `CueStrikeMathTests`
(spin axes, miscue limit, jump conditions), `CueStrokeGestureTests` (the draw/deliver
state machine), `PracticeTableLayoutTests` and `PracticeControlLayoutTests`.

## Running headlessly

The editor holds a lock on the project, so run against a copy:

```bash
cp -R Assets Packages ProjectSettings /tmp/gyrocue-test/
/Applications/Unity/Hub/Editor/2022.3.62f3/Unity.app/Contents/MacOS/Unity \
  -batchmode -runTests -testPlatform EditMode \
  -projectPath /tmp/gyrocue-test \
  -testResults /tmp/gyrocue-test/results.xml \
  -logFile /tmp/gyrocue-test/log.txt -nographics
```

Swap `-testPlatform PlayMode` for the integration suites.

## Current status

EditMode 98/100, PlayMode 8/8 against `2022.3.62f3`. The two EditMode failures are the
pre-existing dual-phone remote-cue aim tests described in
[dual-phone-cue-protocol.md](dual-phone-cue-protocol.md).

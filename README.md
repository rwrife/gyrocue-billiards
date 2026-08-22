# GyroCue Billiards

A simple-but-realistic mobile billiards game inspired by popular touch pool games, with an optional **dual-phone cue mode**.

## Concept
- **Primary mode (single phone):** a 3D table viewed from a camera the player swings around the cue ball to aim. A ball-face widget controls the cue stroke: draw back, then slide forward, with stroke speed setting power and the stopping point setting where the tip strikes the ball.
- **Dual-phone mode (optional):** a second phone acts as a physical cue. The player briefly places the second phone on top of the game phone to align sensors, then uses accelerometer/gyro motion from the second phone to aim and shoot.
- **Tabletop play:** designed to be playable with the phone resting on a real table surface.

## MVP Direction
To move quickly, this project is planned as a **Unity-based mobile app** (iOS-first acceptable, Android optional).

### MVP goals

Single-player **practice mode** comes first; match rules and the dual-phone cue follow.
1. Realistic-enough 3D pool physics (balls, cushions, pockets, cloth friction, spin, jump shots).
2. Touch controls for aiming, stroke power, tip placement, and cue elevation.
3. Optional second-device sensor cue input via local wireless connection.
4. Basic game loop: break, turns, foul detection (lightweight), win condition.
5. CI build pipeline and automated tests for core gameplay logic.

## Architecture (initial)
- Unity project (URP optional)
- 3D rigidbody physics in real-world metres, with a cloth-contact model layered on top for spin and roll
- Input adapters:
  - TouchInputAdapter
  - RemoteSensorInputAdapter (second phone)
- Lightweight networking for sensor streaming (WebSocket or UDP over LAN)
- State machine for turns and rules

## Unity Baseline (Issue #1)
- **Unity editor version:** `2022.3.62f3` (documented in `ProjectSettings/ProjectVersion.txt`)
- **Core project folders:** `Assets/Scenes`, `Assets/Scripts`, `Assets/Prefabs`, `Assets/Tests`
- **Seed runtime script:** `Assets/Scripts/Core/GameBootstrap.cs`
- **Seed edit-mode test:** `Assets/Tests/EditMode/BootstrapTests.cs`
- **Packages manifest:** `Packages/manifest.json`

## Status
Unity mobile-first scaffold is in-repo, with initial `MainTable` scene layout, first-pass touch aim + swipe-shot input scripts, cue indicator + shot preview visualization, a foundational turn state machine, centralized pool physics tuning helpers, baseline pocket-detection/rules signaling scripts, bounded cue-ball-in-hand placement plus foul-gated turn progression, first-pass remote sensor cue integration (aim/shot mapping + instant touch fallback), quick stacked-phone calibration hooks (<10s target with recalibration entry points), a companion-phone streaming prototype (connect/disconnect/start/stop + stable UDP frame cadence), scenario-driven EditMode gameplay tests for turn/foul/win transitions, tabletop anti-jitter + drift-correction filters for remote cue control, a first-pass mobile HUD model/presenter, and a runtime shot lifecycle that locks input, applies cue-ball impulses, detects settled tables, resolves rules, and updates HUD/placement state.

The project has pivoted to **3D** and to a single-player **practice mode**.

`Practice.unity` builds a real-scale 3D table at runtime — slate, split rails, six pocket
mouths, cue ball and rack — and wires an aim/stroke/elevation control rig. Drag the table
to aim; the widget at the bottom is the cue ball, where you draw down and stroke up, with
stroke speed setting power and the stopping point setting the tip contact. Tip height
gives follow and draw, tip offset gives english, and an elevated cue struck above centre
jumps the ball. All geometry is Unity primitives standing in for real art.

See **[docs/practice-mode.md](docs/practice-mode.md)** for controls, table geometry, and
the physics model.

The 3D stack now also has a pure, casual **8-ball rules core**: numbered ball identities,
a legal standard rack/reset, open-table group assignment, remaining-ball scores, first-contact
and scratch/wrong-group fouls, concise HUD-ready reasons, and legal/illegal 8-ball outcomes.
Practice remains single player; the local two-player match shell will consume this core next.
See **[docs/eight-ball-rules.md](docs/eight-ball-rules.md)** for the contract and intentional
simplifications.

### What is and is not 3D yet

`Practice.unity` and everything under `Assets/Scripts/Practice/` are 3D. The earlier 2D
prototype has not been removed: `MainTable.unity` and the `Rigidbody2D` stack it drives
(`TableSceneBuilder`, `TableRackMath`, `PocketTableController`, `ShotLifecycleController`,
`CueBallPlacementController`, `TouchAimSwipeController`, `CuePreviewVisualizer`,
`PoolPhysicsMath`, `PoolPhysicsTuningProfile`) still compile, still have tests, and are
still reachable from Build Settings. They are kept for reference until practice mode
covers the same ground; the game is not yet *only* 3D.

`Title.unity` fronts the game and opens `Practice`.

Verified against 2022.3.62f3: EditMode 98/100, PlayMode 8/8. The two EditMode failures are
pre-existing and both in the dual-phone remote-cue path, which the pivot deprioritises:
`RemoteSensorInputAdapter` returns an aim direction roughly 90 degrees off what its tests
expect. Tracked in issue #36.

Next up: wire the 8-ball core into a local two-player match shell, add practice drills and
goals, replace gameplay primitives with real art, and retire the 2D stack.

## CI Pipeline (Issue #15)
A GitHub Actions workflow now lives at `.github/workflows/unity-ci.yml` and runs on push + pull request.

- **Project sanity checks** always run (manifest/structure/version validation).
- **Unity EditMode/PlayMode jobs** are configured via `game-ci/unity-test-runner` and run when `UNITY_LICENSE` is available.
- **iOS build check** is configured via `game-ci/unity-builder` (target platform `iOS`) and runs when `UNITY_LICENSE` is available.
- If Unity license secrets are missing on the repo, the workflow still runs with explicit fallback notes/sanity checks instead of silently passing.

Expected repository secrets for full Unity execution:
- `UNITY_LICENSE`
- `UNITY_EMAIL` (if your license flow requires credentials)
- `UNITY_PASSWORD` (if your license flow requires credentials)

## License
TBD

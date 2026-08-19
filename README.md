# GyroCue Billiards

A simple-but-realistic mobile billiards game inspired by popular touch pool games, with an optional **dual-phone cue mode**.

## Concept
- **Primary mode (single phone):** the phone screen is the billiards table. The player drags to aim and uses a pull/release swipe to control shot power.
- **Dual-phone mode (optional):** a second phone acts as a physical cue. The player briefly places the second phone on top of the game phone to align sensors, then uses accelerometer/gyro motion from the second phone to aim and shoot.
- **Tabletop play:** designed to be playable with the phone resting on a real table surface.

## MVP Direction
To move quickly, this project is planned as a **Unity-based mobile app** (iOS-first acceptable, Android optional).

### MVP goals
1. Realistic-enough 2D pool physics (balls, cushions, pockets, friction, spin-lite).
2. Touch controls for aiming and shot power.
3. Optional second-device sensor cue input via local wireless connection.
4. Basic game loop: break, turns, foul detection (lightweight), win condition.
5. CI build pipeline and automated tests for core gameplay logic.

## Architecture (initial)
- Unity project (URP optional)
- Deterministic-ish physics wrapper around Unity 2D physics
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

`MainTable` is now a playable rack: `TableSceneBuilder` constructs felt, six cushion rails, six pocket triggers, a cue ball and a fifteen-ball rack at runtime, wires the input/rules/HUD rig, and frames the camera. A `Title` scene fronts it. Drag to aim, pull back to charge, release to shoot; mouse input is mapped onto the same gesture pipeline so the table plays in the editor. PlayMode tests cover table construction, shot-to-settle, and pocketing.

Next up: 8-ball rules (`IShotRuleResolver` currently falls back to scratch-only detection) and correcting the remote-cue aim mapping, whose two EditMode tests still fail.

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

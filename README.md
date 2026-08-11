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
- **Unity editor version:** `2022.3.40f1` (documented in `ProjectSettings/ProjectVersion.txt`)
- **Core project folders:** `Assets/Scenes`, `Assets/Scripts`, `Assets/Prefabs`, `Assets/Tests`
- **Seed runtime script:** `Assets/Scripts/Core/GameBootstrap.cs`
- **Seed edit-mode test:** `Assets/Tests/EditMode/BootstrapTests.cs`
- **Packages manifest:** `Packages/manifest.json`

## Status
Initial Unity mobile-first scaffold is now in-repo; gameplay systems are next.

## License
TBD

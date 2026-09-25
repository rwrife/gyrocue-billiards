# Roadmap to Launch: Finish Line + AAA Polish + iPhone Duo

_Status snapshot taken 2026-09-25. This document is the living plan referenced from the
README; update it as phases complete instead of letting it drift out of sync with the code._

## 1. Where the project actually is

All 23 GitHub issues filed to date (`#1`-`#45`, some numbers are PRs) are **closed**, and
there are currently **zero open issues and zero open PRs**. That is good news read the right
way: the backlog isn't stalled, it's just been fully drained. The project has a working,
tested, single-player 3D practice mode:

- Real-world-scale 3D table, cushions, pockets, rack, physically-motivated cue strike
  (`CueStrikeMath`, `ClothContactMotion`) including spin, draw/follow, miscue, and jump shots.
- A full drag-to-aim / stroke-widget / elevation-strip control scheme that already routes
  through `Screen.safeArea`, so it degrades correctly on notches and home indicators.
- A pure, unit-tested 8-ball rules core (`EightBallRules`) with rack, fouls, group
  assignment, and win/loss — **built but not yet wired to a playable match**.
- A first-pass dual-phone remote cue input path (UDP protocol, calibration, companion
  streamer, receiver/setup UI) — functional but explicitly flagged in its own docs as
  needing the aim-mapping fix that shipped, plus more real-device hardening.
- A fixed, editor-authored "man-cave" room scene built entirely from Unity primitives and
  flat Standard-shader materials, with a documented shopping list for real assets
  (`docs/man-cave-asset-list.md`).
- A CI pipeline that runs project-sanity checks unconditionally and Unity EditMode/PlayMode/
  iOS-build jobs only when `UNITY_LICENSE` is present — i.e. **CI has probably never actually
  run a Unity test in this repo**, only the shell-level sanity fallback.
- A parallel, still-compiling **2D prototype** (`MainTable.unity` + the `Rigidbody2D` stack)
  that the README already marks for retirement once practice-mode parity is reached.

So "what needs to happen to make this a usable game" is not "resume a stalled project," it's
"start a new phase": the practice sandbox is solid, but there is currently **no way to play a
match against anyone** (human or AI), no non-programmer-art, and no store-shippable build
config. Below is the plan to close those gaps and then push past them to a AAA presentation
bar, plus a fully-specified plan for Apple's newly-announced foldable, **iPhone Duo**.

## 2. Definition of "usable" (v1) vs. "AAA" (v2+)

| Bar | Meaning here |
|---|---|
| **Usable v1** | A stranger can download the app, play a full legal 8-ball match to a win/loss (local pass-and-play, single device), and quit without confusion or a crash. Placeholder art is acceptable if it reads clearly. |
| **AAA presentation (v2)** | Real PBR art and lighting, a mixed/broadcast-style camera, full audio (not procedural clips), game-feel polish (hit-stop, camera shake, slow-mo on key shots), meta progression, and platform-native touches — including iPhone Duo's expanded display. |

Everything below is organized so v1 is reachable without waiting on art, and v2 layers on
top without re-architecting v1.

## 3. Gap analysis

### 3.1 Blocking gaps for "usable" (must-fix, in priority order)

1. **No match shell exists yet.** `EightBallRules` is a pure evaluator with no consumer.
   Nothing currently: alternates two players, feeds it shot outcomes, shows whose turn it
   is beyond the practice HUD, or presents a win/loss screen. This is the single biggest
   gap between "tech demo" and "game."
2. **No AI opponent.** Even pass-and-play needs *something* for solo players, and an AI
   opponent using the existing physics/rules core is a natural next milestone after local
   2-player, not a v1 blocker but close to one — most casual pool apps live or die on
   single-player-vs-AI being available immediately.
3. **CI has no teeth.** Without `UNITY_LICENSE` configured, EditMode/PlayMode/iOS-build jobs
   silently skip and only shell-script sanity checks run. All the "tests cover X" claims in
   the docs are true of the test files existing, not of them having run in CI. This needs a
   real Unity Personal/Plus license secret wired in before any further feature work, or every
   future PR is trusting local runs only.
4. **No App Store shippable configuration.** No app icon, no `Info.plist` privacy strings
   beyond local-network usage, no bundle identifier/signing story documented, no launch
   screen, no TestFlight pipeline. `docs/unity-project-setup.md` never mentions
   provisioning.
5. **2D legacy stack still ships.** `MainTable.unity` and its whole `Rigidbody2D` chain
   still compile and are still reachable from Build Settings and are still tested. That's
   dead weight in every build and a source of confusion for new contributors (two physics
   stacks, two input stacks). Retire once the match shell supersedes it.
6. **RemoteSensorInputAdapter aim-mapping debt.** The dual-phone docs call out that aim
   mapping needed a fix before "resuming" that work; confirm the shipped fix
   (`RemotePracticeCueMapping`/`RemotePracticeCueController`) actually covers the original
   complaint end-to-end on a real second device, not just in EditMode tests.

### 3.2 Gaps for AAA presentation

1. **Render pipeline.** The project has no URP/HDRP package and every surface uses the
   built-in Standard shader. AAA mobile pool games (Pool Break Pro, 8 Ball Pool's newer
   builds, MetaCue) lean hard on realistic cloth, ball reflections, and rim lighting — none
   of which the Standard shader on Built-in RP does well on mobile. **Migrating to URP is a
   prerequisite**, not a nice-to-have, for every visual item below.
2. **Placeholder art everywhere.** `docs/man-cave-asset-list.md` is a genuinely well-scoped
   shopping list — use it as the art backlog directly. Balls, cue, and cloth also need a
   material pass even though their geometry stays code-built for physics reasons.
3. **Camera work.** `OrbitAimController` is a single fixed-distance orbit rig. AAA pool
   games use a broadcast-style shot camera (follow-the-ball after contact, rail-cam for
   tight shots, slow-motion on pocketing/eight-ball moments) layered on top of the
   player-controlled aim camera, not instead of it.
4. **Audio.** `PracticeFeedbackController` synthesizes six short clips procedurally at
   runtime specifically so no binary audio assets are needed yet. That's a smart placeholder
   strategy but is explicitly not final-quality audio: real ball-click samples (varied by
   velocity/angle), a room ambience bed, cue-tip chalk/leather detail, and mixed pocket
   "thunk" per pocket type are all still needed.
5. **Game feel.** No hit-stop, no camera shake, no haptic ramp tied to shot importance
   (break vs. tap-in), no confetti/highlight moment on clearing the table or winning a match.
   This is cheap to add relative to its perceived-quality payoff and should not wait for art.
6. **Meta layer.** No stats tracking, no unlockables (cues, cloth colors, tables/rooms), no
   settings beyond audio/haptics/reduced-feedback. Even a lightweight local-only
   progression (win streak, shots-per-game, break-speed record) materially changes how
   "finished" the game feels without needing backend infrastructure.
7. **Store presence.** Screenshots, an App Store preview video, a real app icon and launch
   screen, and copy — none of which can start until the art pass above lands enough to look
   presentable.

## 4. iPhone Duo support

### 4.1 What iPhone Duo actually is (don't build against assumptions)

Apple announced **iPhone Duo** on September 9, 2026 (ships October 23, 2026): a book-style
foldable with a **5.4" outer display** and a single continuous **7.6" inner display** when
unfolded — it is *one* folding OLED panel, not two independent screens joined by an app-level
split like Samsung/Surface Duo. So "wider view when opened" is really "the same app window
gets dramatically more logical points to draw into," not "spawn a second scene across a
second display." That distinction changes the whole design:

- There is no second `Display`/`Screen` object to render a second camera into (unlike, say,
  Nintendo 3DS dual-screen or Surface Duo's two-activity model).
- The correct mental model, per Apple's own developer guidance for the device, is: **"how
  much space do I have right now," not "what fold state am I in."** Apps should already be
  fully resizable and adapt to whatever logical viewport they're given, the same way a good
  iPad app adapts to Split View — fold state is just one more way the available size changes,
  and it can also change via Stage Manager / iPhone Mirroring / windowed multitasking on the
  same hardware.
- New iOS 27.1-SDK-only APIs exist for foldable-specific detail (`onHingeChange` in SwiftUI,
  `UIHingeInteraction` in UIKit, and a `reservedRegion` query that reports the hinge/crease
  band and any inner-camera occlusion region) — these require building with the iOS 27.1 SDK
  specifically, are brand new as of this device's launch, and are Swift/UIKit-first. Unity's
  iOS backend does not expose them today.

### 4.2 Design: what "wider view of the table" means for GyroCue

When the player unfolds the phone, the game should visibly use the extra space rather than
just stretch the existing phone layout across it:

1. **Wider table framing.** `OrbitAimController` currently orbits at a fixed
   `distanceMetres`/pitch range regardless of aspect ratio. On the near-square, much larger
   unfolded viewport, pull the camera back and/or widen FOV so more of the table *and* the
   man-cave room is visible in frame — closer to a broadcast wide shot than the tight
   portrait crop the 5.4" screen needs to keep the ball readable.
2. **A secondary information surface, not just bigger buttons.** The extra width is real
   estate for things that don't fit on a phone: a persistent rack/ball-tracker (who's solids,
   who's stripes, what's left), shot history, and — once the match shell exists — both
   players' info simultaneously, laid out beside the table instead of overlaid on it.
3. **Controls stay reachable, don't just get bigger.** The stroke widget and elevation strip
   are viewport-relative already (`PracticeControlLayout`), so keep them a fixed comfortable
   *size* near the bottom/side thumb zone on the big screen rather than letting them scale up
   to something too large to use one-handed.
4. **Respect the hinge.** Nothing interactive (stroke widget, elevation strip, HUD tap
   targets) should sit on the crease band once it's queryable.

### 4.3 Technical plan (phased, because half the platform surface is in beta)

**Phase A — foundation, buildable today, no Duo hardware required:**

- Add an `AdaptiveDisplayController` (new script, `Assets/Scripts/UI/`) that — like the
  existing `SafeAreaFitter` — diffs `Screen.width`/`Screen.height` every frame (Unity apps
  under iOS's resizable-window behavior can be resized live, without a relaunch, so this
  cannot be a one-time `Awake()` check). It classifies the current viewport into a
  `DisplayProfile` (`Compact` / `Expanded`) using a logical-width/aspect threshold, not a
  device-idiom check — matching Apple's own "ask about space, not fold state" guidance, and
  incidentally making this work correctly on any wide/tablet aspect (iPad, Stage Manager),
  not just this one device.
- Extend `OrbitAimController` to take a `DisplayProfile` and expose a second
  distance/FOV/pitch-range tuning set for `Expanded`, blended in smoothly (not a hard cut) so
  a live unfold/refold mid-game doesn't jar the camera.
- Make `PracticeControlLayout`'s widget rects profile-aware (still viewport-relative, just a
  different rect per profile) so widgets keep a comfortable absolute size in `Expanded`.
- Add the ball-tracker/shot-history side panel as a `Expanded`-only UI element in
  `PracticeHud`/`MinimalHudPresenter`, hidden entirely in `Compact`.
- Set the iOS Player Settings to opt into resizable-window behavior (`Requires Full Screen`
  off) so the OS doesn't pin the app to a fixed phone-shaped rect when unfolded — this is the
  baseline "fully resizable app" behavior Apple says gets you "most of the way" to Duo support
  even before touching any Duo-specific API.
- Cover all of the above with **EditMode tests that simulate resolutions/aspects**
  representative of 5.4" outer vs. 7.6" inner logical points (same technique already used by
  `PracticeControlLayoutTests`/`SafeAreaLayoutTests`), so this is verifiable in CI without a
  physical device or the Duo simulator.

**Phase B — device-specific detail, gated on tooling maturity:**

- A small native iOS plugin (Objective-C, invoked via `[DllImport("__Internal")]`, following
  the existing `LocalNetworkPermissionPostprocessor` pattern for build-time integration) that,
  **only when built with the iOS 27.1 SDK**, queries the hinge `reservedRegion` and reports it
  to C# so `AdaptiveDisplayController` can exclude that exact band from interactive layout
  instead of an estimated safe margin. Must no-op cleanly on every other device/SDK
  combination — this cannot become a hard dependency for building the game at all.
- Revisit once Xcode 27.1 leaves beta and GameCI's Unity/Xcode images support it, since CI
  currently builds iOS through `game-ci/unity-builder`, which will need an updated image
  before this plugin can even compile in CI.

**Phase C — validation:**

- Editor: force `DisplayProfile.Expanded` via a debug toggle and play-test framing/HUD in the
  Unity Game View at both logical sizes before any device exists to test on.
- Xcode 27.1's Duo simulator (Device Hub) once it's available, ahead of physical hardware.
- Real-device dogfood after the October 23, 2026 retail launch.

This is intentionally staged so Phase A (the part that actually delivers "wider view of the
table") ships and is testable in CI immediately, without betting the schedule on
still-in-beta Apple tooling.

## 5. Phased roadmap

| Phase | Goal | Key work |
|---|---|---|
| **0 — CI has teeth** | Every future PR's test claims are actually verified | Wire a real `UNITY_LICENSE` secret; confirm EditMode+PlayMode jobs go green, not just skip |
| **1 — Playable match (v1 "usable")** | A stranger can play and finish a real game | Local 2-player pass-and-play match shell consuming `EightBallRules`; win/loss screen; basic single-player-vs-AI opponent (even a simple aim-at-easiest-legal-ball bot); retire `MainTable.unity`/2D stack |
| **2 — Duo Phase A + game feel** | Ship the wide-view feature; make the existing loop feel better cheaply | `AdaptiveDisplayController` + adaptive camera/HUD (Section 4.3 Phase A); hit-stop/camera shake/haptic ramp; store-shippable build config (icon, launch screen, signing docs, privacy strings) |
| **3 — AAA art & audio pass** | Replace every placeholder | URP migration; work through `man-cave-asset-list.md`; real audio set replacing procedural clips; broadcast-style shot camera |
| **4 — Meta + Duo Phase B** | Depth and platform-native polish | Local stats/progression, unlockable cosmetics; hinge-aware layout via native plugin once tooling allows; dual-phone remote-cue real-device hardening |
| **5 — Launch** | Store submission | Screenshots/preview video on final art, TestFlight pass, submission |

Phases 0-1 are the "finish line" the user asked about; 2-5 are the AAA push, with the Duo
feature deliberately split so its most visible part (Phase 2) lands early and its riskiest
part (Phase 4/Phase B) lands once Apple's own tooling has stabilized.

## 6. Open questions for the project owner

- **Android**: docs mention Android permissions for the dual-phone protocol, but is Android a
  real target for v1, or iOS-only until after launch? This changes whether URP mobile
  settings need to support two very different GPU tiers immediately.
- **AI opponent difficulty scope**: is a simple heuristic bot acceptable for v1, or does this
  need to wait for a stronger solver? A heuristic bot is far cheaper and unblocks v1 sooner.
- **License budget**: Unity Personal is free but has runtime/branding constraints at scale;
  confirm which Unity plan this project is licensed under before CI Phase 0 wires in a
  license secret.
- **Art budget/source**: `man-cave-asset-list.md` assumes purchased store assets; confirm
  budget and preferred asset store(s) before Phase 3 starts.

## 7. Issue breakdown

Phase 0-2 is filed as GitHub issues, matching this repo's existing one-issue-per-feature
convention:

1. [#47](https://github.com/rwrife/gyrocue-billiards/issues/47) — Wire `UNITY_LICENSE`/
   `UNITY_EMAIL`/`UNITY_PASSWORD` secrets and confirm EditMode+PlayMode CI jobs run for real.
2. [#48](https://github.com/rwrife/gyrocue-billiards/issues/48) — Local two-player
   pass-and-play match shell consuming `EightBallRules`.
3. [#49](https://github.com/rwrife/gyrocue-billiards/issues/49) — Win/loss screen +
   return-to-title/rematch flow off the match shell.
4. [#50](https://github.com/rwrife/gyrocue-billiards/issues/50) — Minimal
   single-player-vs-AI opponent.
5. [#51](https://github.com/rwrife/gyrocue-billiards/issues/51) — Retire `MainTable.unity`
   and the `Rigidbody2D`/2D input stack.
6. [#52](https://github.com/rwrife/gyrocue-billiards/issues/52) — `AdaptiveDisplayController`
   + `DisplayProfile` classification with EditMode coverage at representative
   Compact/Expanded logical resolutions.
7. [#53](https://github.com/rwrife/gyrocue-billiards/issues/53) — Profile-aware
   `OrbitAimController` framing (camera pulls back/widens on Expanded).
8. [#54](https://github.com/rwrife/gyrocue-billiards/issues/54) — Profile-aware
   `PracticeControlLayout` widget sizing.
9. [#55](https://github.com/rwrife/gyrocue-billiards/issues/55) — Expanded-only
   ball-tracker/shot-history side panel.
10. [#56](https://github.com/rwrife/gyrocue-billiards/issues/56) — iOS resizable-window
    Player Settings + build sanity check in CI.
11. [#57](https://github.com/rwrife/gyrocue-billiards/issues/57) — Hit-stop/camera
    shake/haptic-ramp game-feel pass.
12. [#58](https://github.com/rwrife/gyrocue-billiards/issues/58) — App icon, launch screen,
    signing/provisioning documentation, TestFlight pipeline doc.

Phases 3-5 are intentionally left less granular since their scope depends on the answers in
Section 6 (art budget, platform scope); file them once those are answered.

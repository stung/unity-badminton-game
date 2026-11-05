# Dev Notes

Running log of updates, newest first. Each entry: what changed, why, and anything left open.

## Next up
1. **Birdie physics option (stage 2 of the pause menu work):** `BirdieFlightProfile` assets (Arcade k=0.12, Realistic k=0.21), a `GameOptions` component that saves the choice, and the Options row cycling between them. Changes apply on the next launch.
2. **Bot opponent:** a `BotIntentSource` that uses `LandingPredictor` to move under the birdie and clear it back. That completes the current milestone (player vs bot clear rally).

---

## 2026-11-05 — Pause menu with Options screen (birdie physics placeholder)

**Added**
- Input: `Gameplay/Pause` (DualSense Options / Esc). New `UI` action map: Navigate (stick, d-pad, arrows, WASD), Submit (Cross / Enter / Space), Cancel (Circle / Esc), Point + Click (mouse), Resume (Options). The UI action names match Unity's defaults so the UI input module can use them.
- `Scripts/Match/PauseController`: pure logic. It sets `Time.timeScale = 0`, switches every `PlayerInput` to the UI map (so menu presses can't queue a swing), and raises Paused/Resumed events. A 0.15 s guard stops a single Esc/Options press from both pausing and resuming.
- `Scripts/Player/PauseInput`: reads the Pause/Resume actions and forwards them to `PauseController`.
- `Scripts/UI/PlayerUIInputBinder`: points the EventSystem's `InputSystemUIInputModule` at the player's own UI map.
- `Scripts/UI/PauseMenuView` + `Assets/UI/PauseMenu.uxml/.uss`: UI Toolkit menu (Paused → Resume / Options; Options → Birdie physics, Back). Circle/Esc backs out one level, then resumes.
- `Assets/UI/PanelSettings.asset` (scale with screen size, 1920×1080 reference) + `BadmintonTheme.tss` (imports Unity's default runtime theme). Kept with the other UI files in `Assets/UI/` rather than `Assets/Data/`.
- Scene: `PauseController`, `EventSystem`, and `PauseMenu` objects. `PauseInput` added to the Player.
- Build Settings: removed the stale SampleScene entry; Court is the only scene.

**Fixed**
- The left stick couldn't navigate the menu (only the d-pad could). `UI/Navigate` was a PassThrough action. The DualSense re-sends its full state constantly, so the idle d-pad (bound after the stick) overwrote the stick's direction on every report. Changing it to a Value action makes the input system pick the most-pushed control. Unity's own UI module source flags this ("ps4 controller is spamming this action").
- After that, holding the stick cycled the menu but taps were lost. The analog stick's first move event was weak (just past its small rest deadzone). UI Toolkit ignored it, but the UI module still started its 0.5 s repeat delay. Fix: in the UI map the left stick is now a digital 2D-vector composite (`leftStick/up/down/left/right`), so each direction "presses" at half deflection and sends a full-strength move, like the d-pad. Gameplay movement still uses the analog stick.

**Placeholder**
- The "Birdie physics" row is focusable and shows "Arcade", but does nothing yet. It gets wired up in the next stage.

**Not verified by me**
- The menu's look and gamepad/keyboard navigation. MCP can't press buttons, so I only confirmed it compiles, starts without errors, and is hidden during play.

---

## 2026-11-02 — Prototype: greybox court, movement, birdie sim, clear

Stages 1 and 2 of the first milestone were implemented together in one pass (originally planned as two separate stages with a checkpoint in between).

**Added**
- `Assets/Scenes/Court.unity`: real-scale court (1 unit = 1 m, 13.4 × 6.1 m, net top 1.55 m, origin at net centre, near half is z < 0), net, capsule player, birdie, shadow, landing marker, debug feeder, follow camera.
- `Assets/Input/BadmintonControls.inputactions`: `Gameplay` map with `Move` (left stick / WASD) and `Shot` (Square / Space / J). Keyboard and Gamepad control schemes.
- Player (`Scripts/Player`): `HumanIntentSource` → `CharacterIntent` → `CharacterStateMachine` (Idle / Moving / Swinging / Recovering) → `CharacterMotor`. `CourtSide` converts side-relative intent to world space and clamps the player to their half.
- Birdie (`Scripts/Birdie`): deterministic flight (`BirdieFlightModel`: gravity + quadratic drag, semi-implicit Euler, fixed 1/120 s step, simple net fault). `LandingPredictor` and `LaunchSolver` reuse the same step function, so predictions are exact. `BirdieShadow` and `LandingMarker` provide visuals.
- Shots (`Scripts/Shots`): `ShotData` ScriptableObject (`Assets/Data/Shots/Clear.asset`), `ShotHitter` (reach sphere around the `HitPoint` child).
- `Scripts/Match/DebugFeeder`: R / Triangle feeds the birdie to the player. Temporary until the bot exists.
- `Scripts/Camera/FollowCamera`: per-player viewport rect, ready for split-screen.

**Decisions / deviations from plan**
- Drag k = 0.12 (real shuttle ≈ 0.21) so clears reach the back court at sane speeds and are readable.
- The clear defaults to `AimMode.TargetDepth` (solve speed to land 5.9 m past the net). With fixed power, depth varied by about 4 m with hit position (it went out from mid-court and landed short from the baseline). `FixedPower` is still available on the asset.
- Shot button is Square (`buttonWest`), per request.
- Scene primitives still have their default colliders. They're harmless (nothing uses Unity physics) and can be removed.

**Known gaps / next ideas**
- No swing visual, so a whiff is invisible.
- Arrow keys aren't bound.
- No bot, scoring, serve, or other shots yet (out of scope for this milestone).
- The Court scene is set as build index 0 in Build Settings, but Unity hadn't flushed `EditorBuildSettings.asset` to disk at commit time.

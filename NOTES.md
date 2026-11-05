# Dev Notes

Running log of updates, newest first. Each entry: what changed, why, and anything left open.

## Next up
1. **Shot system stage 3, tuning pass (Realistic only):** depths, speeds, reach and timing windows; hitstop and shake amounts; maybe a more forgiving window for smash returns. Consider a higher minimum contact height for smashes: from 2.0 m contact, a mid/front "smash" often has to take pace off (fallback) and comes out slow.
2. **Bot opponent:** a `BotIntentSource` that uses `LandingPredictor` to get under the birdie and picks a `ShotCategory`, going through the same `ShotTable` as the player.

## TODO / ideas (not scheduled)
- **Shot imprecision:** shots are perfect for now. Later, degrade shot quality from **low stamina**, **bad positioning** (e.g. reaching at the edge of range or off-balance), and **bad timing** (early/late in the active window). Corner shots should get harder to execute from bad positions (e.g. a full cross-court net drop while stretched). Hook point: `ShotData.GetLaunchVelocity`, between `ChooseTarget` and solving the trajectory.
- **Revisit the feel of the birdie:** drag, top falling speed, how hard it "stalls", shot speeds vs reaction time. Do it once more shots and the bot exist, so it's judged in real rallies.
- **Arcade physics mode:** parked as of 2026-11-06. Two physics modes double the shot tuning, so all tuning targets **Realistic** for now. Arcade still exists in the Options menu but its shots aren't tuned. Revisit once Realistic feels right (it may need its own per-profile shot values).
- **Fine-tune ShotTable categories:** e.g. should a low Square in the mid/back court be a Drop instead of a Net Drop? (Left as Net Drop for now, deliberately.)
- **More aim targets for aggressive shots:** left/right **mid-court** spots for smashes and drives (body or half-court smashes), on top of straight and corners.
- **Contact height shapes the trajectory:** the same button at different contact heights should give different flight paths. For example, a lift from near the floor is a high defensive arc, but at waist height it's flatter and faster. Likely more `ShotTable` rows with narrower height bands and their own shot assets (e.g. Low Lift / High Lift, Flat Drop / Floated Drop). It could also be a height-dependent angle on a single shot.
- **Lunging:** a quick boost in the movement direction (greybox: the capsule dashes) to reach wide shots. Likely its own player state with recovery. Ties into stamina later.
- **Jumping:** for jump smashes / intercepting high shots. Raises contact height, which the ShotTable already uses.
- **Visible racket:** a greybox racket (e.g. a thin cylinder plus a flat disc) that swings with the Swinging state, for clearer timing feedback and so a whiff is visible.
- **Input buffering:** accept a shot press slightly before the character can act (e.g. during recovery). Important for the fast, reflex-driven feel.
- Stamina, diving, scoring, serve, dog art (from `CLAUDE.md`'s later list).

---

## 2026-11-06 — Shot system stage 2: attacking shots, hitstop, camera shake (Realistic only)

**Attacking shots** (replace the stage 1 placeholders; tuned for Realistic, k=0.21):

| Shot | Speed | Target depth | Angle range | Hitstop | Shake | Typical result (Python port) |
|---|---|---|---|---|---|---|
| Back Smash | 90 m/s | 4.0 m | -40° to 15° | 0.13 s | 0.85 | Reaches the net in ~0.14–0.23 s, lands ~4.3–5.6 m deep |
| Mid Smash | 75 | 3.0 | -50° to 15° | 0.10 | 0.64 | Net in ~0.08 s, lands ~4.7 m |
| Front Smash | 55 | 2.0 | -70° to 15° | 0.08 | 0.49 | Net in ~0.04 s, lands ~3.2 m |
| Drive | 32 | 5.0 | -10° to 25° | 0.05 | 0.21 | Net in ~0.17 s, lands ~5.5 m |

- New `ShotData.AimMode.FixedSpeed` + `LaunchSolver.SolveAngleAtSpeed`: the speed is fixed and the angle is solved to land on target, then raised just enough to clear the net by `minNetClearance`. So smashes from deep come out flatter and land deeper than their target, like real ones.
- **Take pace off:** if the full-speed shot wouldn't land in court (e.g. a low-contact smash forced flat to clear the net would sail long), the shot falls back to solving the speed for the target (`TargetDepth` logic from `launchAngleDeg`, capped at `maxSpeed`).

**Impact feedback**
- `Scripts/Match/GameTime`: the single owner of `Time.timeScale`. Pause and hitstop both freeze time through it, so they can overlap safely. The hitstop countdown is held while paused. `PauseController` now goes through it.
- `Scripts/Match/HitstopOnImpact`: on every launch (player, feeder, later the bot), freezes the game for the shot's `hitstopSeconds`. A global `hitstopScale` sets it to 0 to disable.
- `Scripts/Camera/CameraShake`: trauma-based shake (strength = trauma², Perlin jitter) on unscaled time, so it shakes during the hitstop freeze. `FollowCamera` applies it after aiming, so it never feeds back into the camera smoothing. A global `intensityScale` controls it.
- Both scale by how hard the shot actually went (`ShotData.ImpactScale` = launch speed / nominal speed for FixedSpeed shots), so a take-pace-off "smash" doesn't freeze and shake like a full one.

**Shake doubled (same day):** per-shot shake intensities × √2 (0.6 / 0.45 / 0.35 / 0.15 → 0.85 / 0.64 / 0.49 / 0.21). Camera offset is intensity² × `maxOffset` (0.25 m), so this exactly doubles the movement (back smash peak about 9 → 18 cm). Doubling the intensities directly would have made it about 4× stronger.

**Fixed (same day)**
- Corner (left/right) smashes had weak or missing hitstop and shake. Clearing the net makes fast shots land deeper than their target, and on a diagonal that extra depth also carried them past the sideline (mid/front smashes by 0.3–0.5 m, back smashes from lower contacts). The game then took pace off, and the slowed shot's scaled feedback was tiny (shake is trauma², so half strength looks like nothing). Fix: FixedSpeed shots re-aim at the same sideline spot at the depth they actually land (1–2 extra solves). Checked in the Python port: corner smashes and drives now go at full speed and land about 2.75–2.8 m from the centre line (sideline at 3.05).

**Other**
- **Realistic is now the default** (first in `GameOptions`; the Birdie's own slot is also Realistic). A saved menu choice still wins, so if you'd picked Arcade before, switch it once in Options.
- Feeder list: Clear, Drop, Net Drop, Lift, **Back Smash**, **Mid Smash**, **Drive**.

---

## 2026-11-05 — Shot system stage 1: three-button, context-sensitive shots

**Controls**
- **Cross / J = Defensive**, **Square / K = Precision**, **Triangle / L = Attack**. The old single `Shot` action (Square / Space / J) is gone.
- Left stick at contact = aim at a **location**, relative to the player. With no sideways input (under 30%, per-shot `sideAimThreshold`) the shot goes straight ahead from where you stand. With left/right input it goes to the **corner** on your left/right: the sideline (0.3 m inside) at that shot's landing depth. So a net drop can cross the whole court, and a back-left drop can land front-right.
  - First version used a per-shot max angle, which couldn't reach the edges: short shots like net drops only moved about 2.3 m sideways. Replaced the same day.
- Debug feeder: **L1 / R = feed**, **R1 / T = cycle feed type**. Triangle is free for Attack now.

**How a shot is chosen**
- The intent now carries a `ShotCategory` instead of a `ShotData`. `ShotHitter` builds a `ShotContext` at contact (court zone from depth, contact height, whether the incoming shot was a smash) and asks `Assets/Data/Shots/ShotTable.asset`. Rules are checked top to bottom and the first match wins:

  | | Cross | Square | Triangle |
  |---|---|---|---|
  | Returning a smash | Lift Return | Block | Drive |
  | Overhead (≥ 2.0 m), back | Clear | Drop | Back Smash |
  | Overhead, mid | Clear | Drop | Mid Smash |
  | Overhead, front | Clear | Net Drop | Front Smash |
  | Below overhead | Lift | Net Drop | Drive |

  Zones: front < 2.2 m from the net, back ≥ 4.5 m (on the ShotTable asset).
- **Real shots:** Clear, Drop, Net Drop, Lift, Block, Lift Return (fixed angle, speed solved to land at the target depth).
- **Placeholders until stage 2:** Back/Mid/Front Smash and Drive, which fly like drops. The smash placeholders already count as smashes, so block and lift return can be tested via the feeder.

**Other changes**
- `LaunchSolver.SolveClearingNet`: raises the launch angle in 5° steps until the arc clears the tape by the shot's `minNetClearance`, so low net drops and blocks don't hit the net.
- **Fixed:** cross-court Square shots from the back court sometimes didn't reach the net in Realistic mode. Below 2.0 m contact, Square in the back court is a Net Drop, which was capped at 30 m/s, too slow for that distance against real drag. Two solver bugs let it through. (1) A shot that landed before the net counted as "clearing" it, because the clearance check only looked at net crossings. (2) The angle search only tried steeper angles, which are shorter. Now falling short counts as failing, the search also tries flatter angles for extra range, and the Net Drop cap is 60 m/s. Checked in a Python port of the solver: cross-court drops, net drops, blocks and clears from both back corners all land on target in both modes.
- Reach is now a vertical cylinder (0.1–3.1 m high, 1 m radius around `HitPoint`) instead of a head-height sphere, so low shots can connect.
- No double hits: `Birdie` remembers `LastShot` and `LastHitter`, and you can't hit your own shot again.
- Swing timing moved from each shot to a shared `Assets/Data/Shots/SwingTiming.asset`, because the shot isn't known until contact.
- `DebugFeeder` now plays real shot assets from the far side (Clear, Drop, Net Drop, Lift, Back Smash placeholder), with a random lateral position and aim.
- Debug readout (top-left): the current feed, and the last shot with its zone, contact height and whether it was a smash return. `PauseMenu` sorts above it.

**Not verified by me:** actual play. MCP can't press buttons, so I only confirmed it compiles and Play mode starts with no errors. (The "PlayerLoop called recursively" errors only appear when the MCP screenshot tool renders during Play mode, not from the game.)

---

## 2026-11-05 — Birdie physics option: Arcade / Realistic

**Added**
- `Scripts/Birdie/BirdieFlightProfile`: ScriptableObject with display name, description, and flight params. Assets: `Assets/Data/Birdie/Arcade.asset` (drag k=0.12, floaty, terminal ≈ 9 m/s) and `Realistic.asset` (k=0.21, real-shuttle drag, terminal ≈ 6.8 m/s).
- `Birdie` now takes a profile (replacing its inline params) and captures the params at launch, so switching mid-flight never alters the current flight or breaks the landing prediction. The switch applies from the next hit or feed.
- `Scripts/Match/GameOptions`: owns the profile list and current choice, applies it to the birdie, and saves it in PlayerPrefs (by asset name, so reordering the list is safe). Raises `Changed` for menus.
- Options menu: the Birdie physics row cycles with left/right, or forward with Cross/click. The description line under it comes from the profile.

**Tuning changes**
- Clear launch angle 50° → **40°** and speed cap 40 → **100 m/s**. With realistic drag a steep clear wastes most of its speed climbing: a 47° clear from the baseline needed about 103 m/s, versus about 71 m/s at 40° (close to real elite clears). Arcade at 40° has a slightly lower arc (apex about 4.6–6.2 m vs about 6.7 m before). Both modes still land at the 5.9 m target depth.
- `DebugFeeder` gets a `maxFeedSpeed` of 150 m/s (realistic feeds to the back court need about 90).

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

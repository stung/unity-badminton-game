# Dev Notes

Running log of updates, newest first. Each entry: what changed, why, and anything left open.

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

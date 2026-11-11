# Dev Notes

Running log of updates, newest first. Each entry: what changed, why, and anything left open.

## Next up
1. **Running speed buildup (high priority):** characters reach top speed instantly from a standstill. Add a short acceleration so mobility is a little weaker, and repositioning has a real cost. The bot uses the same motor, so it gets this too.
2. **Tune drop shots (high priority):** drops arc far too high compared with real badminton, where a drop falls just over the net on a flatter, sharper path.
3. **Shot system stage 3, tuning pass (Realistic only):** depths, speeds, reach and timing windows; hitstop and shake amounts; maybe a more forgiving window for smash returns. Consider a higher minimum contact height for smashes: from 2.0 m contact, a mid/front "smash" often has to take pace off (fallback) and comes out slow.

## TODO / ideas (not scheduled)
- **Split the game assembly per folder (refactor):** all game code is one assembly (`DoggyBadminton`) for now. Splitting by folder (Birdie, Shots, Player, ...) would enforce which systems may use which, but assemblies can't reference each other in a circle, and today the folders use each other both ways. Untangle those dependencies first.
- **Run tests on GitHub (CI):** tests currently run locally (Test Runner, or Claude via Unity MCP). GameCI could run them on every push; it needs a Unity licence stored as GitHub secrets, and it's third-party, so it needs a go-ahead.
- **Different dogs:** selectable dogs with different characteristics, such as speed, size and maybe power. Also an **accuracy / shot quality stat** that sets how much a late aim change costs (a wider free window, or a softer tier drop). For example, a technical dog can disguise shots cheaply and a power dog pays more. Build one all-rounder first (see Telegraph & deception).
- **Supers (idea to consider):** special moves.
- **Visible hitbox (idea to consider):** show the reach area in-game, not just as an Editor gizmo.
- **Serves:** real serves to replace starting each point with a feed.
- **Adjustable bot difficulty:** the first bot only plays easy Defensive (X) shots, for easier testing. Letting it dash would likely help it return smashes.
- **More official Training debug screen:** make the feeder and shot readout look like a proper part of Training mode, and leave it out of Bot mode.
- **Birdies that clip the net (very much a nice-to-have):** a shot just catching the tape could trickle over or drop short. There are ways to fake this without very realistic physics.
- **Tune the racket hitbox:** the dog's reach area is still a tuning placeholder (0.8 m radius, 0.05–2.8 m high, centred 0.6 m ahead).
- **Dive:** when dash and jump are pressed together, do a dive.
- **Dash aim assist (snap to the birdie):** when dashing, bend the dash direction toward the birdie's predicted landing spot, within reason (aim assist: the game nudges your input toward the target). Cap it with a max angle and a max distance so it helps without feeling like autopilot or teleporting. The dash is a fixed 1.5 m, so the assist mostly changes direction, not length. The bot would have to use the same rule.
- **Slow-mo on a pre-hit jump (time dilation):** a slight slow-down of game time, but only while the player is airborne from a jump AND the birdie is about to be hit (in or near reach). No slow-down for jumps with no birdie nearby, or at any other time. Ease in and out so it doesn't pop. `GameTime` already owns `Time.timeScale` for pause and hitstop (a brief freeze on impact), so this should go through it too so the three can overlap safely. It slows everyone (both dogs and the birdie), so it's a global time scale rather than a per-character one; the exact feel gets worked out during implementation. This is also the window where the attacker telegraphs a side and the defender commits (see Telegraph & deception).
- **Turnaround penalty (commitment):** a small cost for running one way and suddenly reversing, so changing direction costs a little (movement commitment: the game makes you live with your choice of direction for a moment). Likely a short drop in speed or a brief slide while the character decelerates and re-accelerates, scaled by how fast they were going and how sharp the turn is (a full 180° at top speed costs the most, a gentle turn costs nothing). Keep it small and tunable in the Inspector, and have the bot follow the same rule, since the motor is shared.
- **Turnaround visual feedback (needs a design pass):** the player needs to see that the penalty happened, not just feel the lag. Ideas to try:
  - **Skid:** the dog slides with its legs braced, using `LegSwingView` to plant the legs instead of swinging them.
  - **Dust puff:** a small particle burst at the feet, a common way to sell a hard stop.
  - **Squash and stretch:** a quick lean back into the turn, then a spring forward (an animation principle: the body briefly deforms to sell weight and speed).
  - **Facing flick:** the dog whips around with a short overshoot, rather than snapping.
  - Scale all of these by turn sharpness so small turns show nothing. Keep them to visuals so game logic doesn't depend on them.
- **Split-step timing bonus:** if the player split steps (the small hop real players do as the opponent hits, so they land ready to push off in any direction) right as the opponent hits the birdie, their next move skips the acceleration penalty from the running speed buildup and starts at full speed. If they then move in the wrong direction, the turnaround/backpedal penalty still applies: a well-timed split step removes the slow start, not the cost of guessing wrong. Open questions: how wide the timing window around contact is (±N frames), and whether "wrong direction" is judged against the birdie's predicted landing spot or its current velocity. Needs the speed buildup and turnaround penalty first, and the bot follows the same rule since the motor is shared.
- **Rally replay with slow-mo final shot (design discussion in progress):** after a point, replay the last ~5 s of the rally and slow down through the final racket contact. Proposed approach, not yet approved:
  - **Recording:** a snapshot ring buffer (fixed-size array that overwrites its oldest entry) storing, every frame, both dogs' and the birdie's position and rotation plus visual state (swing progress for `RacketSwingView` / `LegSwingView`). Snapshots rather than re-simulating from inputs, because character movement uses variable `deltaTime` and the bot uses `Random`, so a re-sim would drift out of sync.
  - **Playback:** interpolate between snapshots so slow-mo stays smooth. Mark the timestamp of the last hit; play at normal speed, then ease down to slow-mo around contact. Use the replay's own clock, not `Time.timeScale`, so it doesn't fight `GameTime` (pause, hitstop).
  - **Game logic off during playback:** intent sources, motor and birdie sim disabled; the replay only drives transforms. Fits as a new match state between point and reset.
  - **Cinematic angles:** a separate full-screen `ReplayCamera` (game logic doesn't read cameras, so this is cheap). Ideas: side-on broadcast view at net height panning with the birdie; low angle behind the hitter for the final shot; slow orbit around the contact point during slow-mo; cut from wide to close-up.
  - Needs a scene change (replay camera) and a plan approval before building.
- **Stamina affects gameplay:** the stamina bar is meant to change what the player can do, not just display.
- **Stamina for smashing:** smashes should cost stamina too, so you can't smash continuously (`ShotData` already has an unused `staminaCost`). The bot has no stamina yet, so it would need it as well to play by the same rules.
- **Stamina between points:** in a real game, replenish stamina slowly between points, so players don't start each point with a full bar.
- **Shot imprecision:** shots are perfect for now. Later, degrade shot quality from **low stamina**, **bad positioning** (e.g. reaching at the edge of range or off-balance), **bad timing** (early/late in the active window), and **late aim changes** (see Telegraph & deception below). Corner shots should get harder to execute from bad positions (e.g. a full cross-court net drop while stretched). Hook point: `ShotData.GetLaunchVelocity`, between `ChooseTarget` and solving the trajectory. Decided 2026-11-11: quality is **3 fixed tiers, Perfect / Normal / Bad**, not a random spread. Fixed penalties are learnable, and both players can see when a shot has gone wrong.
- **Telegraph & deception (design agreed 2026-11-11, not scheduled):** players show where they plan to hit, so the opponent can read it, and can change it at the last moment at a cost. This makes trick shots possible. It mirrors real badminton "holding the shot": the same preparation for every shot, then a late wrist change.
  - **Target zone highlight:** while setting up a shot, the attacker picks **left, right or neutral** with the left stick before choosing the shot. That side of the opponent's court is highlighted for both players. Neutral is a real choice: the defender has to guess, but placement is less sharp. The highlight shows the **displayed aim** (what's been telegraphed), not the true aim, so the attacker is allowed to break the promise.
  - **Late change → shot quality penalty:** changing the side (or shot type) inside a **free window** early in the setup costs nothing. After it, the shot drops a quality tier. Past a **commit point**, no changes are allowed. Stamina was considered and rejected: a late flick costs control, not breath.
  - **Tiers do different things per shot**, defined on each `ShotData` asset (shots are data). Examples: a Bad clear lands mid-court instead of at the back line; a Bad smash comes out slower and steeper; a Bad drop floats high over the net; a Bad corner shot drifts toward the centre. These are all chances for the opponent to counter-push.
  - **Optional second cost: recovery frames** (frames after a shot where you can't act). A late change could add a few, because you hit off-balance. Uses the existing recovering state.
  - **Positioning feeds quality:** being early and well placed starts you at Perfect, so you can afford a late change (dropping to Normal). Scrambling starts you at Normal or Bad, so a late change makes the shot fall apart. Footwork pays for the right to deceive.
  - **Defender commits a side:** during the slowdown the defender can commit left or right. The payoff ties into the **split-step timing bonus**: a correct read gives a fast first step; a wrong read means planting and turning (turnaround penalty); not committing means normal speed. Exact rules TBD.
  - **Goes with slow-mo on a pre-hit jump:** the slowdown gives both players time to pick a side and to read it. Keep it to jump shots in reach, so it stays special.
  - **Bot fairness:** the bot only sees the displayed aim (the highlight and body cues), after a reaction delay, never the true aim. It can be baited by a late flip, and can bluff the same way at the same cost. Difficulty = reaction delay + read accuracy.
  - **Body tells later:** body/racket turning toward the aim during the windup; for dogs, the head and ears pointing at the target, with a "no-look" shot as an advanced trick. Telegraphs need to read from the silhouette in split-screen, where the opponent is small.
  - **One all-rounder dog first.** Per-dog deception costs come later (see Different dogs).
  - Existing hook: shot aim already comes from intent (`aimX`, read by `ShotData` with `sideAimThreshold`). The bot has its own `ChooseAim`. The new part is splitting displayed aim from true aim, plus the commit timing.
  - Suggested build order: (1) shot quality tiers with per-shot effects, (2) displayed vs true aim + highlight + late-change penalty, (3) bot reads the displayed aim, (4) defender commit + split-step payoff, (5) slow-mo on jump shots, (6) body tells.
- **Revisit the feel of the birdie:** drag, top falling speed, how hard it "stalls", shot speeds vs reaction time. Do it once more shots and the bot exist, so it's judged in real rallies.
- **Arcade physics mode:** parked as of 2026-11-06. Two physics modes double the shot tuning, so all tuning targets **Realistic** for now. Arcade still exists in the Options menu but its shots aren't tuned. Revisit once Realistic feels right (it may need its own per-profile shot values).
- **Fine-tune ShotTable categories:** e.g. should a low Square in the mid/back court be a Drop instead of a Net Drop? (Left as Net Drop for now, deliberately.)
- **More aim targets for aggressive shots:** left/right **mid-court** spots for smashes and drives (body or half-court smashes), on top of straight and corners.
- **Contact height shapes the trajectory:** the same button at different contact heights should give different flight paths. For example, a lift from near the floor is a high defensive arc, but at waist height it's flatter and faster. Likely more `ShotTable` rows with narrower height bands and their own shot assets (e.g. Low Lift / High Lift, Flat Drop / Floated Drop). It could also be a height-dependent angle on a single shot.
- **Arm + racket-driven hitbox:** today the racket is purely visual. Contact uses an invisible reach cylinder around `HitPoint`, so a swing at empty air still connects if the birdie is anywhere in range (accepted for the prototype). Later: add an arm (shoulder → elbow → hand) holding the racket, and make contact depend on the **racket head's actual path** during the active window (a swept sphere between frames so fast swings can't skip past the birdie). Reach then comes from body + arm + racket, which lunges extend naturally. Fighting-game-style "active frames" fit the reflex feel, but this raises timing difficulty and the bot must use the same reach model.
- **Per-shot swing animations:** every shot currently uses the same overhead swing. Add underarm (lifts, net drops, blocks), flat (drives) and steeper (smashes) swings. Catch: the shot is only decided at contact, so the swing must start before the shot is known. Options: pick the swing at press time from the birdie's predicted contact height, or blend into the right swing at contact.
- **Input buffering:** accept a shot press slightly before the character can act (e.g. during recovery). Important for the fast, reflex-driven feel.
- Stamina, diving, scoring, serve, dog art (from `CLAUDE.md`'s later list).

---

## 2026-11-09 — Automated tests (Unity Test Framework)
- **How to run:** Window → General → Test Runner → Run All (Edit Mode and Play Mode tabs). Claude also runs them after script changes (see `CLAUDE.md`).
- Game scripts now compile into their own assembly, `Assets/Scripts/DoggyBadminton.asmdef`, so test assemblies can reference them. The Test Framework package (1.8.0, already installed indirectly) is now listed in the manifest by name.
- **Edit Mode tests** (`Assets/Tests/EditMode`, ~0.5 s): landing predictor agrees exactly with the flight model; the sim is deterministic; net faults drop on the hitter's side; `LaunchSolver` lands on target and clears the net; `DogShotTable` picks the intended shot for each button and situation, and splits zones by depth; point rules, serve side and game-over at 5. They load the real tuning assets, so retuning one on purpose may mean updating a test's expectation.
- **Play Mode tests** (`Assets/Tests/PlayMode`, ~2 s): load `DogCourt.unity` and drive the player with `ScriptedIntentSource` (a fake controller). Swing snap hits a birdie behind the dog, misses with the snap off, and still misses when the birdie is out of reach.
- Small code changes for testability: `PointRules.ServesFromRight` / `IsGameOver` (moved out of `MatchController`, same behaviour), `CharacterStateMachine.SetIntentSource`, `CharacterFacing.SnapToBirdieOnSwing`.
- Deleted the leftover template files (`Assets/TutorialInfo`, `Assets/Readme.asset`).

## 2026-11-09 — Swing snap: face the birdie while swinging
- Fixes the whiff where flicking the stick to aim just before a swing turned the dog, moving its reach away from the birdie.
- While **Swinging**, `CharacterFacing` turns the dog to face the birdie on any frame where that would put the birdie in reach (`ShotHitter.TryGetSnapDirection`). The stick then only aims. It runs every swinging frame, not just on the press, so an early press still tracks the birdie in. It also works in the air.
- Stops once the birdie is hit, so the follow-through keeps its facing. The bot gets it too, since it uses the same components.
- Inspector: `Snap To Birdie On Swing` (on/off, for comparing) and `Max Snap Angle` (default 180, so the dog can whip all the way round).
- Being far out of position still misses: the birdie has to be within the reach distance from the body in some direction.

## 2026-11-08 — Bot and Game Mode, stage 2: match rules

**Flow** (`Match/MatchController`): the score shows for 1 s, then "Press R1" (R1 / T). After the rally, everything holds for 0.75 s. First to 5 wins, with no deuce. Then "Game" shows for 1 s, followed by the Retry menu. Retry starts at 0-0 with the human serving. All timings and the winning score are Inspector values.

**Rules**
- `Match/PointRules`: landing in bounds, that side loses; out of bounds, the hitter loses. A net hit drops on the hitter's side, so it needs no rule of its own.
- Singles width in Bot mode: `Assets/Data/Court/DogCourtSingles.asset` (5.18 m). `ShotHitter.SetAimCourt` makes corner shots aim for it; Training stays doubles width.
- The point winner serves next, and the human serves first. Both dogs stand on their own right when the server's score is even and their own left when it's odd, which puts them diagonally opposite.
- R1 launches a Clear from the server to the receiver's side (`ShotData.GetLaunchVelocityToward`), even when the bot is serving. The birdie waits at the server until then.

**Lock:** `CharacterStateMachine.IsLocked` from landing until R1. No walking, swinging, dashing or jumping, but a human can still spin, and a swing or jump in progress finishes.

**UI:** `UI/MatchHudView` + `Assets/UI/MatchHud.uxml/.uss`: scoreboard upper left, big score, start prompt, Game text. Debug readout hidden in Bot mode. The Retry menu is the pause menu held open (`PauseController.HoldOpen`). At game over, Options / Esc / Circle swaps between Retry and the pause menu, so Game Mode stays reachable. Stamina carries between points and refills on Retry and on mode changes.

**Verified:** compiles, no console errors or warnings. Scripted full game to 3-5 with simulated keys: sides, serving, lock, launches and singles aiming (corners stop at ±2.30 m). Out-of-bounds rule cases checked directly, because perfect shots never landed out. Retry, the menu swap, game over to Training, and mid-rally to Training. `Court.unity` still pauses with no errors, though its Game Mode row does nothing there.

**Fixed along the way:** choosing Training from the game-over menu changed mode but left the game paused, because the resume guard blocked the real resume.

**Not verified:** a match played by hand, R1 and Retry on the DualSense, and how the timings feel.

---

## 2026-11-07 — Bot and Game Mode, stage 1: rallying

- **Game Mode:** new first-level pause menu row, Training or Bot. The choice takes effect on leaving the menu, by any exit, and resets everything (`GameModeController`). Picking the mode you're already in does nothing. It's remembered between launches. Training works as before; the bot only exists in Bot mode.
- `Bot/BotIntentSource`: produces the same intent as a human, so same speed, reach, swing timing and `ShotTable`. It waits 0.15 s after each shot (lowered from 0.25 s), walks at the predicted landing spot until its reach covers it, swings when the birdie will be in reach after the windup, and otherwise returns home (3.5 m from the net). X shots only, 50% straight and 25% to each corner. No dash or jump, and no stamina.
- `Bot` object in `DogCourt.unity`: lavender (`DogLavender`), angry brows, far side.
- `Match/RallyStarter`: R1 / T (new `StartPoint` action) launches a Clear from the human's position, with no swing. `Birdie.Clear`, `CharacterStateMachine.ResetToStart` and `CharacterStamina.Refill` support the reset.

**Verified:** compiles, no console errors or warnings. With a simulated T press and scripted shots from the human's side, at a low background frame rate: the launch and all 15 clears, drops, net drops, lifts and drives were returned, aimed straight or at corners. All 6 mid and back smashes beat it, even ones landing about 1 m from it. Switching modes both ways shows and hides the bot and feeder, and resets the birdie, player and stamina.

**Reaction delay at 0.15 s:** 0 of 9 mid, back and front smashes returned, including straight ones landing within about 0.7 m of the bot. The test ran at about 17 fps in the background. A fast smash can cross most of the reach between frames at that rate, and the bot's swing check only looks at one point in time, so the result may not match normal play.

**Not verified:** a real human rally on the DualSense, and picking Game Mode with a real controller (navigation events were sent to the row directly). Smashes may be too reliable a win against this bot, so judge that in play.

Stage 2 (the entry above) replaced R1's stage 1 behaviour.

---

## 2026-11-07 — Puppy look: white body, face and legs

- White body and legs, black face, using new `DogWhite` and `DogBlack` materials. `Player.mat` is shared with the human in `Court.unity`, so it stays blue.
- `Player/Face`: two eyes, a nose and a `:3` mouth (two small arcs of short strokes), replacing the old `Nose` cube. All visual, with no colliders. The dog still faces away from the camera most of the time, so the face mainly shows when it turns or runs toward the camera.
- Four jointless stick legs, each on a hip pivot (`LegFrontLeft` and so on).
- `Player/LegSwingView`: swings diagonal pairs opposite each other, scaled by actual speed, so it's small at a walk, quicker in a dash, and settles when stopped. Legs keep swinging in the air at a set rate (`Air Swing Speed`), including in a jump straight up, and freeze during hitstop. Tune `Max Swing Angle` and `Swings Per Second` in the Inspector.

---

## 2026-11-07 — Dash, jump and stamina bar

**Controls:** R2 / Left Shift = dash, L2 / Space = jump.

**Dash**
- 1.5 m in 0.12 s, toward the stick, or the way the character faces if the stick is centred. Only from idle or moving.
- Spends a fixed distance rather than a fixed time, so it's the same at any frame rate. Exits at running speed, so with no stick it slides about 0.3 m further as it slows.
- New `Dashing` state in `CharacterStateMachine`. Facing snaps to the dash direction and locks.

**Jump**
- 0.8 m high, 0.6 s in the air. Height comes from a parabola on elapsed time, so it's exact at any frame rate. Only from idle or moving.
- No air control: horizontal velocity is fixed at takeoff, and move input resumes on landing. No landing lag for now.
- Airborne lives in `CharacterMotor`, not the state machine, because it has to overlap with swinging. Swings are allowed in the air, and jump + shot on one frame does both. Facing locks in the air so the stick is free to aim.
- `ShotHitter` lifts its reach heights with the jump: at the peak the dog reaches 0.85–3.6 m, so it can meet the birdie earlier but can't reach low shots.
- Dash + jump on one frame is a dash for now (dive TODO).

**Stamina**
- `Player/CharacterStamina`: 100 max, dash and jump cost 15, still +25/s, walking +5/s, running -3/s. Running is 70%+ of top speed, still is under 5%. No change during a dash or jump. Doesn't affect gameplay yet; at 0 you can still dash and jump.
- `UI/StaminaBarView` + `Assets/UI/StaminaBar.uxml/.uss`: top centre, fades green to yellow at half to red at empty. In `DogCourt.unity` only.
- It uses actual speed, so pushing into the court limit counts as standing still and restores stamina quickly.

**Other:** intent gained `DashRequested` and `JumpRequested` for the future bot. The human in `Court.unity` shares this code, so it can dash and jump too, without stamina. The debug readout moved down to sit below the bar. `DogCourt.unity` also carries a camera change made in the Editor: `Base Position` Y from 7 to 4.

**Verified:** compiles, no console errors or warnings. With simulated keys in Play mode: dash direction, cost and neutral-stick dash; jump height, air time and reach lift; +5 m/s kept through a jump with the stick pushed the opposite way; facing lock in the air; still and running stamina rates; bar width and colour at 100/50/25/0.

**Not verified:** R2 and L2 on the DualSense (only keyboard was simulated), a real swing during a jump, and how any of it feels. The walking rate wasn't measured on its own.

---

## 2026-11-07 — Dog version in a new scene (DogCourt)

Rebuilt locally from a cloud session's summary, because that session's files were never pushed. `Court.unity` and its assets are unchanged.

**The dog (`Assets/Scenes/DogCourt.unity`)**
- Sideways capsule: 0.9 m long, 0.35 m wide, back at 0.55 m. `Nose` replaces `FacingNub`. Speed stays 5 m/s.
- Racket held flat in the mouth, gripped mid-handle. `RacketPivot` is rotated so the existing `RacketSwingView` sweeps it horizontally: 0° (ready, sticking out to the dog's right), -30° (cocked), 210° (follow-through), no roll. Its 90° midpoint is straight ahead. Visual only.
- Reach: 0.8 m radius, 0.05–2.8 m high, centred 0.6 m ahead of the dog (`HitPoint`).
- Net 0.70 m via `Assets/Data/Court/DogCourt.asset`, full-size court. `Assets/Data/Shots/DogShotTable.asset` has the same rules with overhead starting at 0.9 m instead of 2.0 m.
- Feeder heights about halved: 1.3, 1.3, 0.5, 0.3, 1.4, 1.35, 0.65 m. Birdie physics, shot assets and camera unchanged.

**New scripts**
- `Player/CharacterFacing`: snaps to face the stick direction past 20% and keeps the last facing on release. Facing moves the reach area but not shot direction. Locked during the swing, so the stick is free to aim; also locked during recovery (`lockDuringRecovery`, on by default). No turning during pause or hitstop.
- `Match/NetVisual`: sizes the net mesh, tape and posts from the court asset, in Edit mode too.
- `CharacterStateMachine.LastIntent`: exposes this frame's intent so facing doesn't read the `IntentSource` twice. `CharacterMotor` still sets its starting rotation, so far-side characters begin facing the net.

**Reach height vs timing window:** "time to press" is the birdie's time in reach plus the 0.25 s active window. At 1.4 m it was 0.47 s (clear) and 0.48 s (lift). The 2.8 m reach gives 0.69 s (+47%) and 0.76 s (+58%). If "50%" should mean time in reach instead, that's about 2.0 m.

**Verified:** compiles, Play mode runs with no console errors or warnings, and the net and dog look right in Edit mode. With simulated keys, the dog turned with the stick, held facing through swing and recovery while `D` was held, the shot aimed right from that same stick, and it turned once recovery ended. Solver: straight and corner smashes, clears and drops from 2.8 m contact land in court at full pace (front smash also checked at 2.0 m).

**Not verified:** how it feels on the DualSense, racket swing look in motion, camera at dog scale. Contact up to 2.8 m with no jump may look odd against a 0.55 m dog. DogCourt isn't in Build Settings (only matters for builds).

---

## 2026-11-07 — CLAUDE.md: communication and commit rules

- New **Communication** section: use game dev terms with a brief explanation, and explain dev-side concepts rather than assuming them.
- New **Workflow** rule: don't commit or push until confirmed; approval for one commit or push doesn't carry over to the next.
- Copied from the cloud branch `claude/confident-einstein-983pcr` (commits `1726bc4`, `a361fdd`). That branch is still on the remote and has no game code.

---

## 2026-11-06 — Greybox racket + overhead swing

- `Assets/Prefabs/Racket.prefab`: greybox racket at real proportions (about 0.68 m): cylinder handle and shaft, oval head as a dark frame disc plus a lighter strings disc. New materials `RacketFrame` and `RacketStrings`.
- Scene: `Player/RacketPivot` (right shoulder, local (0.4, 1.45, 0.05)) holds the racket prefab instance.
- `Scripts/Player/RacketSwingView`: procedural swing on the pivot, driven by `CharacterStateMachine` state + `StateTime` and the shared `SwingTiming`. Ready pose 35° forward → cocked -120° during windup → sweeps to 125° follow-through across the active window (ease-in-out, so it whips through overhead while contact is possible) → eases back to ready over recovery. A slight roll gives the arc some shape.
  - Procedural (not an Animator clip) so it always matches the real hit window when timings are tuned.
  - Runs on game time, so it **freezes mid-swing during hitstop**.
- `CharacterStateMachine` now exposes `StateTime` and `SwingTiming` (read-only) for views.
- Same overhead swing for every shot for now (see TODO).

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

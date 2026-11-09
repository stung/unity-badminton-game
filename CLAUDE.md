# Badminton Game (working title)

## Project
Local PvP badminton game, with split-screen and a bot opponent. Eventually all characters become dogs ("doggy badminton"), but for now use greybox placeholders (capsules, spheres, cubes).
- Engine: Unity 6 LTS, target platform macOS (Apple Silicon)
- Input: Unity Input System package (not the legacy Input Manager). Primary test controller is a PS5 DualSense.
- Language: C#

## Current milestone
Playable prototype: one player and one bot rally clears back and forth on a court.
Later (do NOT build unless asked): other shots (drop, smash, net, drive), diving, stamina bar, scoring, serve, dog art.

## Architecture rules
- Human input and bot AI must both produce the same "intent" data (move direction, shot requested). One shared character controller executes intents, so the bot plays by the same rules as the human.
- The birdie uses a custom, deterministic flight simulation (gravity plus drag), not a standard Rigidbody, so the bot can predict where it will land.
- Shots are data: each shot type is a ScriptableObject (launch angle, power, stamina cost). Adding a shot should mean adding an asset, not rewriting code.
- Use small explicit state machines for player states (idle, moving, swinging, recovering) and match states (serve, rally, point, reset).
- Game logic must not depend on cameras or UI. Each player gets their own camera with a viewport rect for split-screen.
- Keep scripts small, one responsibility per script.

## Conventions
- Scripts go in Assets/Scripts/ with subfolders: Player, Bot, Birdie, Shots, Match, Camera, UI
- ScriptableObject assets go in Assets/Data/
- Tests go in Assets/Tests/EditMode (pure logic, no Play needed) and Assets/Tests/PlayMode (loads DogCourt.unity and runs real frames). Game code compiles into the DoggyBadminton assembly (Assets/Scripts/DoggyBadminton.asmdef).
- Use PascalCase for classes and methods, camelCase for private fields, [SerializeField] instead of public fields
- Expose tuning values (speeds, drag, power) in the Inspector so I can adjust them without code changes
- Comment the "why" for anything non-obvious, especially physics or timing code

## Communication
- Use game dev terminology whenever appropriate, so I learn the vocabulary. If a term isn't immediately obvious, add a brief summary of what it means the first time it comes up (e.g. "coyote time: a short grace window where a jump still works after leaving a ledge").
- I know gaming terms as a player, mostly from action games (e.g. frame data, hitstop, i-frames), but not from the developer's side. Explain dev-side concepts (e.g. interpolation, state machines, fixed timestep, root motion) rather than assuming them.

## Workflow
- Before big changes, propose a plan and wait for my approval
- After changing scripts, check the Unity console for compile errors and fix them, then run the Edit Mode tests
- Before suggesting a commit, also run the Play Mode tests. Report failures rather than committing over them
- When adding a feature or fixing a bug, add or update tests for it where practical
- Do not edit scenes or prefabs unless I ask, or tell me first, since I may have them open in the editor
- Do not add third-party packages or assets without asking. Prefer free CC0 assets (e.g. Kenney.nl) and never generate AI assets.
- Do not commit or push until I confirm. Suggest a commit (with a proposed message) and wait for my go-ahead; approval for one commit/push does not carry over to the next.
- Make small, working steps. Suggest a git commit when something works. Update NOTES.md with things done and what next milestones are.

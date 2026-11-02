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
- Use PascalCase for classes and methods, camelCase for private fields, [SerializeField] instead of public fields
- Expose tuning values (speeds, drag, power) in the Inspector so I can adjust them without code changes
- Comment the "why" for anything non-obvious, especially physics or timing code

## Workflow
- Before big changes, propose a plan and wait for my approval
- After changing scripts, check the Unity console for compile errors and fix them
- Do not edit scenes or prefabs unless I ask, or tell me first, since I may have them open in the editor
- Do not add third-party packages or assets without asking. Prefer free CC0 assets (e.g. Kenney.nl) and never generate AI assets.
- Make small, working steps. Suggest a git commit when something works.

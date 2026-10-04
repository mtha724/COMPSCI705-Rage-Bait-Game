# Running and editing the game

Open `Assets/Levels/Level 1.unity` in Unity 6.6 and press Play. Choose Low, Normal, or Overboard, enter an anonymous participant code, and click Start. Move with A/D or the arrows; jump with Space. Escape opens a pause menu. End run records a voluntary stop and returns to the menu. The optional timer shows active playing time.

The build includes six scenes in order. Every death reloads Level 1. Genuine flags advance; the last genuine flag ends the run. There are no checkpoints, numbered player-facing level labels, or a display of the total level count. `Prototype.unity` preserves the original authored room and is excluded from the build.

## Scenes and starting balance

| Scene | Travel distance | Walking-only target at 5 units/s | Mechanic |
|---|---:|---:|---|
| Level 1 | 25 | 5 s | Opening floor with spikes beneath |
| Level 2 | 35 | 7 s | Five pit platforms, two fake |
| Level 3 | 45 | 9 s | Approach-triggered falling ceiling |
| Level 4 | 55 | 11 s | Fake flag falls; genuine goal is farther away |
| Level 5 | 65 | 13 s | Reverse horizontal controls, then opening floor |
| Level 6 | 75 | 15 s | Fake platforms, falling ceiling, reversed controls |

Travel targets exclude jumps, trap learning, deaths and repeated earlier levels. These values are pilot starting points, not literature-derived requirements. Player speed is 5, jump velocity 9, gravity scale 3, and fall gravity multiplier 1.4. Adjust the Player prefab before balancing gaps. Reversal changes horizontal direction and facing, while Jump remains unchanged.

## Inspector editing

- Player prefab: movement, jump, ground layer, falling gravity, and zero-friction collider material.
- LevelSetup in each scene: internal level number, spawn reference, travel distance and walking-time target.
- CameraFollow: horizontal bounds, vertical position and smoothing.
- OpeningFloor / FakePlatform: disappearance delay; fake platforms activate on a top landing.
- FallingCeiling / FakeFlag: falling delay and initial downward velocity. Their triggers activate once.
- GoalFlag: next scene; blank next scene means completed.
- GameManager prefab: equal restart/transition delays, starting FX condition, optional timer, and optional active-time session limit (0 disables the limit).
- Effects profile assets: particles, lifetime, layers, gain, overlay opacity and death text size.
- FXController on GameManager: replace the five generated audio clips with the team's chosen sound assets.

The included audio cues are original generated placeholders. They are not Roblox recordings. The overboard death uses red particles as a stylised splatter effect. Existing Pixel Adventure art and the existing character animation controller are reused.

The Tools > Rage Game menus generate the player/common prefabs and level layouts. Build Six Levels regenerates the authored Level 1–6 scenes. Normal level editing is through the saved scenes and prefabs. Run Validate Design for asset/scene checks, or Run Gameplay Checks for input, collision, trap, restart, progression and logging checks.

## Data

The persistent GameManager retains session totals across scene changes. Each new run writes a CSV under `Application.persistentDataPath/StudyLogs`, with a unique session identifier. RunLogger exposes the current LogPath while playing. Data is local; the game has no upload endpoint.

Fields: session ID, anonymous participant ID, condition, UTC timestamp, event, detail/cause, elapsed seconds, active seconds, per-level active seconds, internal level, attempt, deaths, furthest level, and player position. Events include session start/end, level start, trap activation, death, retry, goal, pause and resume. Performance events record observed frames per second about once per active second. Completion, voluntary quit, application closure, and configured session limit have distinct reasons. Active time excludes menus, pauses, death delays and goal transitions. The attempt number increments on death/restart; it does not represent a voluntary choice to continue by itself. Exclude logs marked with participant code `automated-validation` from study data.

Record the play session separately with your screen recorder, including game audio. Capture the participant code, condition and session start so video can be matched with CSV timestamps. Keep recordings and participant data outside the Git repository.

## Validation

Batch checks can be run with Unity's existing editor executable, `-batchmode -projectPath <project> -executeMethod PlatformerValidation.Run -logFile <log>`. These Play-mode checks include UI and particle rendering, so retain a graphics device. Do not supply `-quit`, because the validation runner waits for Play mode and exits when finished. Results are written to the ignored `Logs/validation.json` file. A successful run prints `PLATFORMER_VALIDATION_OK`.

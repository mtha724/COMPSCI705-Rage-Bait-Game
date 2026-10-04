# Running and editing the game

Open `Assets/Levels/Level 1.unity` in Unity 6.6 and press Play. Choose Low, Normal, or Overboard, enter an anonymous participant code, and click Start. Move with A/D or the arrows; jump with Space. Escape opens a pause menu. End run records a voluntary stop and returns to the menu. The optional timer shows active playing time.

The build includes six scenes in order. The default death destination is Level 1, selected by the scene name in GameManager.Restart; local testing edits can change it. Reloading recreates the saved player and trap state. Genuine flags advance; the last genuine flag ends the run. There are no checkpoints, numbered player-facing level labels, or a display of the total level count. `Prototype.unity` preserves the original authored room and is excluded from the build.

## Scenes and starting balance

| Scene | Travel distance | Walking-only target at 5 units/s | Mechanic |
|---|---:|---:|---|
| Level 1 | 25 | 5 s | Opening floor with an image-free lethal hole |
| Level 2 | 40.85 | 8.17 s | Seven authored platforms, three fake, then an opening hole before the flag |
| Level 3 | 45 | 9 s | Eight falling bricks, seven popup spike groups, then an opening hole |
| Level 4 | 55 | 11 s | Three floor-opening escaping flags, copied parkour, then a fourth escaping flag |
| Level 5 | 65 | 13 s | Reversed controls, chasing saw, visible spikes, copied parkour, visible hole, then normal controls |
| Level 6 | 75 | 15 s | Fake platforms, falling ceiling, reversed controls |

Travel targets exclude jumps, trap learning, deaths and repeated earlier levels. These values are pilot starting points, not literature-derived requirements. Player speed is 5, jump velocity 9, gravity scale 3, and fall gravity multiplier 1.4. Adjust the Player prefab before balancing gaps. Reversal changes horizontal direction and facing, while Jump remains unchanged. The second zone in Level 5 restores normal input only after the final visible hole. Irregular popup timings use per-trap seeds so the same layout and timing rules apply to all FX conditions.

## Inspector editing

- Player prefab: movement, jump, ground layer, falling gravity, and zero-friction collider material.
- LevelSetup in each scene: internal level number, spawn reference, travel distance and walking-time target.
- CameraFollow: horizontal bounds, vertical position and smoothing.
- OpeningFloor / FakePlatform: disappearance delay; fake platforms activate on a top landing.
- FallingCeiling: falling delay and initial downward velocity. Each approach trigger activates once.
- PopupSpikes: exposure/hidden durations, rise time, activation distance and random seed. The first four groups use regular cycles; the last three use seeded irregular intervals.
- EscapingFlagTrap: linked floor and flag, escape distance and speed. It has no Goal component.
- ChasingSaw: speed, vertical tracking limits and artwork. Its speed starts below the player run speed.
- ReverseZone: Restore Normal Controls is enabled only on the narrow trigger after Level 5's final hole.
- HoleHazard: image-free trigger collider with death cause hole. Disabling it prevents the hole from killing the player.
- GoalFlag: next scene; blank next scene means completed.
- GameManager prefab: equal restart/transition delays, starting FX condition, optional timer, and optional active-time session limit (0 disables the limit).
- Effects profile assets: particles, lifetime, layers, gain, overlay opacity and death text size.
- FXController on GameManager: replace the five generated audio clips with the team's chosen sound assets.

The included audio cues are original generated placeholders. They are not Roblox recordings. The overboard death uses red particles as a stylised splatter effect. Existing Pixel Adventure art and the existing character animation controller are reused.

The Tools > Rage Game menus generate the player/common prefabs and level layouts. Build Six Levels regenerates the authored Level 1–6 scenes. Normal level editing is through the saved scenes and prefabs. The original phase builders recreate the earlier baseline layouts; they do not preserve these later manual revisions. LevelRevisionBuilder.Apply is a separate authoring operation for this revision and rewrites Levels 1–5. Level 6 is intentionally unchanged and scheduled for a future rework. New hazard/platform artwork uses Simple sprite renderers and repeated images. Run Validate Design for asset/scene checks, or Run Gameplay Checks for input, collision, trap, restart, progression and logging checks.

## Data

The persistent GameManager retains session totals across scene changes. Each new run writes a CSV under `Application.persistentDataPath/StudyLogs`, with a unique session identifier. RunLogger exposes the current LogPath while playing. Data is local; the game has no upload endpoint.

Fields: session ID, anonymous participant ID, condition, UTC timestamp, event, detail/cause, elapsed seconds, active seconds, per-level active seconds, internal level, attempt, deaths, furthest level, and player position. Events include session start/end, level start, trap activation, death, retry, goal, pause and resume. Performance events record observed frames per second about once per active second. Completion, voluntary quit, application closure, and configured session limit have distinct reasons. Active time excludes menus, pauses, death delays and goal transitions. The attempt number increments on death/restart; it does not represent a voluntary choice to continue by itself. Exclude logs marked with participant code `automated-validation` from study data.

Record the play session separately with your screen recorder, including game audio. Capture the participant code, condition and session start so video can be matched with CSV timestamps. Keep recordings and participant data outside the Git repository.

## Validation

Batch checks can be run with Unity's existing editor executable, `-batchmode -projectPath <project> -executeMethod PlatformerValidation.Run -logFile <log>`. These Play-mode checks include UI and particle rendering, so retain a graphics device. Do not supply `-quit`, because the validation runner waits for Play mode and exits when finished. Results are written to the ignored `Logs/validation.json` file. A successful run prints `PLATFORMER_VALIDATION_OK`.

# Running and editing the game

Open `Assets/Levels/Level 1.unity` in Unity 6.6 and press Play. Choose Low, Normal, or Overboard, enter an anonymous participant code, and click Start. Move with A/D or the arrows; jump with Space. Escape opens a pause menu. End run records a voluntary stop and returns to the menu. The optional timer shows active playing time.

The build includes six scenes in order. The default death destination is Level 1, selected by the scene name in GameManager.Restart; local testing edits can change it. Reloading recreates the saved player and trap state. Genuine flags advance; the last genuine flag ends the run. There are no checkpoints, numbered player-facing level labels, or a display of the total level count. `Prototype.unity` preserves the original authored room and is excluded from the build.

## Scenes and starting balance

| Scene | Travel distance | Walking-only target at 5 units/s | Mechanic |
|---|---:|---:|---|
| Level 1 | 25 | 5 s | Opening floor with an image-free lethal hole |
| Level 2 | 40.85 | 8.17 s | Seven authored platforms, three fake, then an opening hole before the flag |
| Level 3 | 45 | 9 s | Eight falling bricks, seven popup spike groups, then an opening hole |
| Level 4 | 55 | 11 s | One flag escapes three opening floors, crosses the copied parkour, then escapes a fourth floor to become the finish |
| Level 5 | 65 | 13 s | Reversed controls, chasing saw, visible spikes, copied parkour, visible hole, then normal controls |
| Level 6 | About 50 along the folded route | About 10 s | Advancing saw column, climb, spike/fake-platform roof, right drop, reversed middle return, skill fall, final chase and escaping flag over an opening hole |

Travel targets exclude jumps, trap learning, deaths and repeated earlier levels. These values are pilot starting points, not literature-derived requirements. Player speed is 5, jump velocity 9, gravity scale 3, and fall gravity multiplier 1.4. Adjust the Player prefab before balancing gaps. Reversal changes horizontal direction and facing, while Jump remains unchanged. The second zone in Level 5 restores normal input only after the final visible hole. Irregular popup timings use per-trap seeds so the same layout and timing rules apply to all FX conditions.

## Inspector editing

- Player prefab: movement, jump, ground layer, falling gravity, and zero-friction collider material.
- LevelSetup in each scene: internal level number, spawn reference, travel distance and walking-time target.
- CameraFollow: horizontal bounds, vertical position and smoothing. Follow Vertical is enabled in Level 6, where Look Ahead is 0 so the camera works in both travel directions.
- OpeningFloor / FakePlatform: disappearance delay; fake platforms activate on a top landing.
- FallingCeiling: falling delay and initial downward velocity. Each approach trigger activates once. Level 3's eight bricks and Level 6's two bricks enable Disappear On Landing and disappear 0.15 seconds after touching the ground, removing their artwork and collider together.
- PopupSpikes: exposure/hidden durations, rise time, activation distance and random seed. The first four groups use regular cycles; the last three use seeded irregular intervals.
- EscapingFlagTrap: linked floor, shared RunningFlag and zero-based stage index. The four triggers open their own floors and advance the same flag in order.
- RunningFlag on Level 4's GoalFlag: four Next Stops and escape speed. The flag moves from x=8 to 17, 26, 47 and finally 55; its goal collider activates only after the final movement finishes. Scene reload resets the whole sequence.
- ChasingSaw: speed, vertical tracking limits and artwork. Its speed starts below the player run speed.
- Level 6 PressureSaw_1–3: each uses its own Inspector speed and fixed height; the folded-level builder defaults to speed 0.32. Their End X reaches the right camera boundary so they continue across the level, including after the later speed-up trigger. PressureSawSpeedUpTrigger multiplies all three current speeds by 2.5 once per attempt. Re-entering has no further effect, and scene reload restores the authored speeds. The separate FinalChasingSaw retains its own speed.
- RevealHazard: initially hidden, harmless spikes that become visible and lethal on a trigger. The skill-fall spike stays exposed after appearing; steer right once below the return ledge.
- ShortcutTrap: barrier, collapsing ledge and spike reveal. Jumping toward the apparent opening reveals the wall and removes the ledge underneath the player.
- ReverseZone: Restore Normal Controls is enabled on the trigger after Level 5's final hole and at the end of Level 6's middle return.
- HoleHazard: image-free trigger collider with death cause hole. Ground-level pit colliders are centred at y=-8, with the fallback KillZone at y=-10, so the whole character falls out of view before death. Ground and wall colliders use PlayerNoFriction, including both vertical edges of each pit. The elevated roof pit kills five units below its lip, before the middle ledge catches the fall. RoofPitCameraTrigger holds vertical framing during a downward fall; a safe landing or scene reload restores tracking.

Apply Pitfall and Saw Adjustments updates these settings in all six saved scenes and the reusable pit/platform prefabs while preserving the authored layout. Run Pitfall and Saw Checks verifies off-screen deaths, friction-free pit-wall contact, the 2.5x speed change, trigger re-entry, pause and reload.
- GoalFlag: next scene; blank next scene means completed.
- GameManager prefab: equal restart/transition delays, starting FX condition, optional timer, and optional active-time session limit (0 disables the limit).
- Effects profile assets: particles, lifetime, layers, gain, overlay opacity and death text size.
- FXController on GameManager supplies the five baseline movement/death/goal clips. `Assets/Effects/Overboard.asset` controls its red player tint, bold/thick red `YOU DIED` text and the four additional cues. Clip sources and exact collection cuts are documented in `Assets/Audio/Overboard/Sources.md`. Starting a new run plays Minecraft Cave; completing a death restart plays Eye of Rah; each falling block's first ground landing plays Pokémon Wall. The selected death cue can finish across the fixed 0.85-second restart delay.

The included audio cues are original generated placeholders. They are not Roblox recordings. The overboard death uses red particles as a stylised splatter effect. Existing Pixel Adventure art and the existing character animation controller are reused.

The Tools > Rage Game menus generate the player/common prefabs and level layouts. Build Six Levels regenerates the authored Level 1–6 scenes. Normal level editing is through the saved scenes and prefabs. The original phase builders recreate the earlier baseline layouts; they do not preserve these later manual revisions. LevelRevisionBuilder.Apply is a separate authoring operation for this revision and rewrites Levels 1–5. Rebuild Folded Level 6 replaces only Level 6 with the sketch-based route. New hazard/platform/background artwork uses Simple sprite renderers and repeated images. Run Validate Design for asset/scene checks, Run Gameplay Checks for the whole game, or Run Level 6 Checks for the negative trap cases and a complete route traversal with real keyboard input.

Level 6 follows: climb left → cross the roof → descend the right shaft → travel left along the middle ledge → fall and steer right beneath that ledge → jump the bottom spikes and opening floor to the escaping flag. Its compact footprint is roughly 34 units wide, with about 50 units of horizontal route travel, slightly above Level 3's 45. Vertical jumps, controlled falls and trap timing add to its actual completion time. It deliberately does not keep the earlier 75-unit straight-line layout.

## Data

The persistent GameManager retains session totals across scene changes. Each new run writes a CSV under `Application.persistentDataPath/StudyLogs`, with a unique session identifier. RunLogger exposes the current LogPath while playing. Data is local; the game has no upload endpoint.

Fields: session ID, anonymous participant ID, condition, UTC timestamp, event, detail/cause, elapsed seconds, active seconds, per-level active seconds, internal level, attempt, deaths, furthest level, and player position. Events include session start/end, level start, trap activation, death, retry, goal, pause and resume. Performance events record observed frames per second about once per active second. Completion, voluntary quit, application closure, and configured session limit have distinct reasons. Active time excludes menus, pauses, death delays and goal transitions. The attempt number increments on death/restart; it does not represent a voluntary choice to continue by itself. Exclude logs marked with participant code `automated-validation` from study data.

Record the play session separately with your screen recorder, including game audio. Capture the participant code, condition and session start so video can be matched with CSV timestamps. Keep recordings and participant data outside the Git repository.

## Validation

Batch checks can be run with Unity's existing editor executable, `-batchmode -projectPath <project> -executeMethod PlatformerValidation.Run -logFile <log>`. These Play-mode checks include UI and particle rendering, so retain a graphics device. Do not supply `-quit`, because the validation runner waits for Play mode and exits when finished. Results are written to the ignored `Logs/validation.json` file. A successful run prints `PLATFORMER_VALIDATION_OK`.

Run Overboard FX Checks, or batch method `PlatformerValidation.RunOverboardFeedback`, checks the saved audio references and selected clip lengths, new-run versus revival events, real falling-block impacts, red sprite tint, bold death text, contrast and clearing feedback after revival. It also checks that Low and Normal retain their original presentation. Save the current scene before using this tool; it opens Level 1 without regenerating any layouts.

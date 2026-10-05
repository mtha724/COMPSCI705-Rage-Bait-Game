# Feedback study: decisions to pilot

## Conditions

All three conditions use the same scenes, traps, positions, timings, movement physics, restart delay and transition delay. The comparison varies combined audiovisual feedback; it cannot establish separate causal effects for sound versus visuals.

| Parameter | Low | Normal | Overboard |
|---|---:|---:|---:|
| Footstep particles | 0 | 2 | 12 |
| Jump particles | 0 | 8 | 32 |
| Landing particles | 0 | 6 | 28 |
| Death / goal particles | 0 | 24 | 112 |
| Audio layers | 0 | 1 | 2 |
| Base audio gain | 0 | 0.14 | 0.24 |
| Red/green overlay opacity | 0 | 0 | 0.5 |
| Death text size | 44 | 44 | 112 |
| Death text | You died | You died | YOU DIED, bold, thick red glyphs with dark outline/backing |
| Player tint on death | Original | Original | Red |
| Additional event cues | None | None | Selected death, revival, new-run and falling-block sounds; gain 0.7 |
| Particle lifetime | 0.4 s | 0.4 s | 0.75 s |

Restart delay is 0.85 seconds and goal delay is 0.65 seconds in every condition. Keep device/headphone volume fixed and check the final audio mix for clipping. Keep screen resolution and rendering performance comparable. Low and Normal retain their original feedback; Overboard uses the selected clips documented in `Assets/Audio/Overboard/Sources.md` alongside the existing movement/goal cues. Each selected cue plays once per corresponding event, and its tail can continue across the unchanged restart delay. The cave sound plays when starting a new run, the Eye of Rah sound after a death restart, and the Pokémon wall sound on each falling block's first ground landing.

## Evidence and practitioner accounts

- Dominic Kao (2020), [The effects of juiciness in an action RPG](https://experts.illinois.edu/en/publications/the-effects-of-juiciness-in-an-action-rpg/), compared four feedback intensities with 3,018 participants. None and Extreme produced less play time than Medium and High in that action RPG. This motivates an intensity comparison; it does not predict this platformer's outcome or prescribe these exact parameter values.
- Hicks, Rogers, Gerling and Nacke (2024), [Juicy Audio: Audio Designers' Conceptualization of the Term in Video Games](https://eprints.staffs.ac.uk/8527/1/3677084.pdf), interviewed twelve professional audio designers. This provides practitioner accounts and concepts for emphasis and audiovisual coherence.
- Martin Jonasson and Petri Purho (2012), [Juice It or Lose It](https://gdcvault.com/play/1016487/Juice-It-or-Lose), is a practitioner demonstration of building feedback into a simple game. Use it for design examples rather than causal evidence about persistence.

Choose effects through these sources and design intent, document parameter values, then pilot whether players perceive Low / Normal / Overboard as distinct. Literature supports the dimensions to investigate; the team must validate its implementation and values.

## Persistence and baseline

Use voluntary stopping and subsequent attempts alongside active play time, deaths and furthest progress. More deaths may indicate a harder game. A completed run or researcher-imposed time limit is different from quitting. Record prior platformer and Crossy Road familiarity.

Crossy Road is an external comparison of persistence, not a matched control for the platformer's FX. Use the same participants and a consistent procedure, and record active play time, elapsed time, deaths, subsequent retries, score, and stopping reason. Scores are game-specific and should not be combined directly with platformer levels. Use `CrossyRoadSessionTemplate.csv` for manual video coding.

If participants play all three FX conditions, counterbalance their order, because knowledge of traps carries over. Also balance whether Crossy Road comes before or after the platformer. A between-participant FX comparison avoids repeated trap learning, with a different sample-size tradeoff. Agree the study design and stopping rule with the team before recruitment.

Pilot goals: confirm all levels are beatable with learned traps, later levels have longer uninterrupted traversal, all deaths reset to Level 1, effects do not change mechanics/timing, and all conditions maintain comparable frame rate. Recording participant sessions and running Crossy Road trials require the actual study sessions; the implementation supplies logging and a coding template.

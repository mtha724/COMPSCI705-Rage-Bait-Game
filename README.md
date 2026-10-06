# COMPSCI705-Rage-Bait-Game

## Running the Game

1. Go to the [latest release](https://github.com/mtha724/COMPSCI705-Rage-Bait-Game/releases/tag/v0.1.0) or click **Releases** on the right-hand side of this repository's main page.
2. Under **Assets**, download the zip file for your device:
   - **Windows**: `OneMoreTry-v0.1.0-Windows.zip`
   - **macOS**: `OneMoreTry-v0.1.0-macOS-Universal.zip`
   - **Linux**: `OneMoreTry-v0.1.0-Linux-x86_64.tar.gz`
   - **Web**: `OneMoreTry-v0.1.0-Web.zip`
3. Extract the downloaded file.
4. Open the extracted folder and run the **Ragebaitgame** application.

> **Important:** Do not move, rename or delete any other files in the extracted folder.

## Game Functionality

### Main Menu

1. **Choose a feedback mode** by clicking **Low**, **Normal**, or **Overboard**. This sets the intensity of the visual and audio effects (FX) during gameplay. The current selection is shown above the buttons.
2. **Toggle the timer (optional)** by clicking **Toggle timer** to show or hide the on-screen timer. Its current state is shown next to the feedback mode.
3. **Click Start** to begin playing.

<img width="845" height="473" alt="image" src="https://github.com/user-attachments/assets/e781d519-14cf-4017-9a78-af7ee1d676c8" />

### How to Play

Reach the flag at the end of the level.

| Action | Controls |
|---|---|
| Move | `A` / `D` or `←` / `→` |
| Jump | `Space`, `W`, or `↑` |
| Return to menu | `Esc` |

## Differences between Project Plan and Implementation

- The project originally planned for mouse-only controls. During implementation, these evolved into keyboard input, as described in the [Game Functionality](#game-functionality) section.

## Setting Up the Project

### Installing Unity
Search "Unity" in the browser
Click Pricing, find the Personal (free) option and click Download now
From there pick the download for your OS type (linux, mac, windows) and click download and follow the instructions there, you will install Unity Hub and the Editor.

Note that if you encounter 403 error switch to a different browser and try the same steps again. From experience Chrome gave 403 error but Edge worked.

### Setup
Clone the project

```
git clone https://github.com/mtha724/COMPSCI705-Rage-Bait-Game.git
```

In Unity Hub click `add` select `add from disk` and open.

### Playable prototype
Open Assets/Levels/Level 1.unity in Unity 6.6, press Play, select a feedback condition and click Start. See [development instructions](Docs/Development.md) and [study design notes](Docs/StudyDesign.md).

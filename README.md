# COMPSCI705-Rage-Bait-Game

# Set Up

## Installing Unity
Search "Unity" in the browser
Click Pricing, find the Personal (free) option and click Download now
From there pick the download for your OS type (linux, mac, windows) and click download and follow the instructions there, you will install Unity Hub and the Editor.

Note that if you encounter 403 error switch to a different browser and try the same steps again. From experience Chrome gave 403 error but Edge worked.

## Setup
Clone the project

```
git clone https://github.com/mtha724/COMPSCI705-Rage-Bait-Game.git
```

In Unity Hub click `add` select `add from disk` and open.
## Browser game (GitHub Pages)

The `web/` folder contains the ready-to-play Unity Web export from
`Builds/v0.1.0/Packages/OneMoreTry-v0.1.0-Web.zip`. No Unity installation or
build step is needed to deploy this version.

### First deployment

1. Push the `feature/browser-game` branch to GitHub.
2. Open repository **Settings > Pages** and set **Source** to **GitHub Actions**.
3. In **Settings > Environments > github-pages**, if deployment branches are
   restricted, allow `feature/browser-game` (and `main` after merging).
4. In **Actions**, open **Deploy browser game** and re-run the latest workflow
   if it ran before Pages was enabled. After merging the workflow into the
   default branch, it can also be started with **Run workflow**.
5. Open https://mtha724.github.io/COMPSCI705-Rage-Bait-Game/ once deployment succeeds.

The workflow publishes only `web/`. Changes to that folder on `feature/browser-game`
or `main` deploy automatically. Both branches target the same site; after merging,
delete the feature branch or remove it from the workflow's branch list to keep
`main` as the only publishing branch.

### Updating the game

Replace the contents of `web/` with a fresh Unity Web export, retaining
`index.html`, `Build/`, `TemplateData/`, and `.nojekyll`. Include `StreamingAssets/`
if the new export generates it. Enable **Decompression Fallback** in Unity when
using compressed builds; the included `.unityweb` build already has it enabled.
Keep the generated relative asset URLs so the game works under the repository path.
Commit and push the updated `web/` folder. Editing Unity source files alone does
not update this prebuilt game.

For a local preview with Python installed, run `python -m http.server 8000 --directory web`
from the repository root, then open http://localhost:8000. Use an HTTP server;
opening `index.html` directly as a file will not load the Unity game correctly.

# Greyhaven v0.8.0-preview — The Island, Brought to Life

**A playable Windows x64 preview of Greyhaven**, an original top-down fantasy island RPG. Start a local single-player campaign, explore the island, fight and cast spells, take on quests, manage your equipment, and experiment with a map editor.

## What is new in this release

- A much richer visual presentation: creatures, roofs, buildings, clothing, item icons, effects, and painted interface panels.
- **39 synthesized sound cues** covering combat, UI interactions, four ambient loops, and three regional music tracks.
- **F8 audio controls** with persistent volume settings for Master, Effects, Music, and Ambience.
- The existing island, town, dungeon, main quest and three side quests remain playable, with compatibility support for Phase 7 saves.

## Download and play

**Download `Greyhaven-v0.8.0-preview-Windows-x64.zip`.** Extract it completely to a writable folder and double-click `Run-Greyhaven.cmd`. The launcher starts the local game server and client for you; no .NET SDK or proprietary Ultima Online installation is required. `Map-Editor.cmd` launches the included editor. `Start-Media-Preview.html` opens the optional offline art and sound showcase.

**Do not download GitHub's automatic “Source code” ZIP if you only want to play.** That is the buildable source repository, not the ready-to-run game.

## Status and requirements

- Platform: **Windows x64** only for the supplied runtime.
- Stage: **pre-release**, single-player/local host. No co-op in this milestone.
- Install: **portable extraction**; does not require an installer.
- Saves: stored locally under `UserData`; back up that folder before replacing a previous version.
- Testing: the executables were cross-published for Windows, with native simulation/graphics checks on Linux. Clean-machine Windows gameplay has **not yet been independently certified**.

## Source and license information

The matching GitHub source repository includes corresponding server sources and upstream dependency trees. Original foundation code is MIT; ServUO-derived/server code is GPL-covered, and other dependencies have separate licenses. Original content/media reuse terms have not yet been finalized. Read `LICENSE-BOUNDARIES.md` and `docs/CREDITS_AND_LICENSES.md` before redistribution. Complete a license review before wide public release.

For controls, screenshots, builds and known limitations, see the repository **README** and **docs**. This is the **v0.8.0-preview** milestone; later co-op and release certification are not included.

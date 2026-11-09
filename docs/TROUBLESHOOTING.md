# Troubleshooting Greyhaven

**Nothing happens when I click Run-Greyhaven.cmd**  
Extract the *entire* Windows ZIP first (not individual EXE files). Keep the `Runtime`, `content`, and `art` folders beside the launcher scripts. Use a writable folder such as `Games\Greyhaven`. Check whether Windows SmartScreen or antivirus has blocked a newly downloaded, unsigned preview build; only proceed when you trust the source. The included runtime is self-contained; do not install a random SDK to work around a missing file.

**There is no picture or audio**  
Check the display and audio device, graphics driver and Windows output device. F8 adjusts Greyhaven's separate Master/Music/Ambience/Effects levels. The application uses FNA and native SDL/audio components shipped in `Runtime/`. This preview has not undergone clean-machine Windows certification.

**My saves disappeared after updating**  
Use the same `UserData` from the old extracted folder. Do not overwrite it with a fresh empty directory. Back up the entire UserData folder before replacing the game. Campaign slots and editor exports are not part of release downloads.

**The map editor does not start**  
Use `Map-Editor.cmd` next to `Run-Greyhaven.cmd`, not the editor EXE by itself. Use `Play-Edited-Map.cmd` after you export a valid map in the workshop.

**A game bug persists**  
Report your Windows version, zip name, steps to reproduce, expected and actual result, and whether the issue occurs on a new campaign. Do not publicly attach private saves or full logs.

**Need to build or diagnose code?**  
Use the matching source repository for this release tag and see `docs/BUILDING_FROM_SOURCE.md`. The Windows player archive intentionally omits source trees, debug symbols, logs and build tools.

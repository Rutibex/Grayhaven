# Phase 3 — Single-player campaign foundation

Run `Run-Greyhaven.cmd` after extracting the complete archive on Windows x64. Choose one of three campaign slots, then **New campaign**. Starting a new campaign refuses to overwrite an occupied slot. **Continue latest** selects the newest readable manual, quick, automatic or exit save. Each category can also be loaded explicitly from the campaign screen. Keyboard arrows and Enter, or mouse clicks, select an action. Esc closes the campaign screen.

In the world, **Esc** cancels spell targeting first; otherwise it pauses or resumes the authoritative host. Losing window focus pauses the campaign and requires explicit resume. Tile movement and item transfers stop while paused. The host's elapsed play time also stops. Rendering can finish an already accepted tile interpolation; authority remains on integer tiles.

| Control | Action |
| --- | --- |
| F5 | Quick save |
| F9 | Quick load, remaining paused |
| Esc | Pause/resume |
| 1 / 2 / 3 / 4 while paused | Select manual / quick / autosave / exit save |
| S while paused | Save selected category |
| L while paused | Load selected category |
| Q while paused | Save and return to the campaign screen |

The editor keeps its existing F5 reload and Esc exit behavior. Ground, backpack and paperdoll transfers remain server-authoritative. This phase adds campaign lifecycle; combat, quests, nested containers and the full island remain later milestones.

## Persistence

Each campaign has its own `UserData/Campaigns/slot1` (or slot2/slot3) directory. Saves contain the map snapshot, content fingerprint, map hash, player tile/facing, elapsed play time, revision and all item identities, definitions, positions and equipment. A new campaign writes its initial manual save before accepting a connection. The host autosaves every 60 seconds while playing, including while no commands are being sent. Manual and quick saves are available on demand. Quit and disconnect write an exit save.

Writes flush a temporary file before atomic replacement; the previous verified version becomes `.bak`. Checksum and schema checks detect damaged files. A damaged primary falls back to a readable backup and reports recovery for an in-session load. The campaign screen identifies wholly unreadable slots. Both damaged files are retained; the game never silently starts over. Starting a new game in another empty slot remains possible. Copy the whole campaign folder to back it up. Future content compatibility migrations are not implemented: incompatible package/map saves are rejected explicitly.

A campaign owns its saved map. Continuing rehydrates that map for both client and server, so later map editor exports do not alter an existing campaign. `Play-Edited-Map.cmd` uses the export for a **new** campaign; choose an empty slot. To start over in all three occupied slots, first back up and then move the appropriate campaign folder out of `UserData/Campaigns` while the game is closed. There is deliberately no destructive delete button in this milestone.

One host may hold a given campaign at a time. The launcher starts a loopback-only host, waits for readiness, and then opens the native client. Save-and-quit acknowledges the completed write before stopping the host. Closing the client window requests the same shutdown. Unexpected disconnect writes an exit save and retires the host after a short grace period. An unused campaign host times out after 30 seconds. The launcher allows 10 seconds for shutdown before its last-resort process cleanup. A sudden machine/process failure can lose progress since the latest completed save; autosaves and backups provide recovery points, not a transaction log for every step.

## Development and verification

The existing `Build-Original-Content.cmd` rebuilds the Windows runtime. The MIT `Greyhaven.Content` assembly owns save DTOs and storage; the GPL-covered host owns world mutation, pause, clock, persistence timing and shutdown. The native FNA presentation owns campaign and pause interfaces. The original ServUO map implementation still validates the loaded map, and tile-based collision/interpolation remains unchanged.

```sh
dotnet build tests/Greyhaven.Content.Tests -c Release
dotnet tests/Greyhaven.Content.Tests/bin/Release/net10.0/Greyhaven.Content.Tests.dll .
python tests/verify_campaign.py --runtime /path/to/linux-runtime --dotnet /path/to/dotnet --render
python tests/verify_original_boot.py --runtime /path/to/linux-runtime --dotnet /path/to/dotnet --render
```

`verify_campaign.py` runs entirely in a temporary game root, leaving personal campaigns alone. It covers pause/clock authority, save/load, duplicate host refusal, graceful quit, disconnect, killed-host autosave recovery, damaged backup recovery, incompatible content rejection, occupied slot protection, map snapshots, and native launcher/menu rendering. The shorter autosave interval is a test-only command-line option; shipping default is 60 seconds. Native rendering is tested with Linux offscreen FNA/OpenGL. Windows x64 is cross-published here; Windows execution is still a clean-machine acceptance task.

Evidence and native screen captures are in `docs/phase3`. Earlier phase documents and evidence remain historical records of those milestones.

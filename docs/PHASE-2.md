# Phase 2 — Boot entirely on original content

## What runs

Extract the complete release and double-click `Run-Greyhaven.cmd`. A small launcher starts an owned local ServUO-derived server on a free loopback port, waits for its ready handshake, and opens the original-content client. When the client exits, the launcher stops only that server. No installed .NET runtime, UO installation, registry entry, original client files or asset conversion is needed for the supplied Windows x64 build.

`Map-Editor.cmd` opens the original-content editor. `Play-Edited-Map.cmd` starts the client and server with the map exported by the editor. `Run-Art-Proof.cmd` retains the Phase 1 rig inspector; the offline HTML art inspector also remains available.

This is a **32×32 boot fixture**, Wayfarer's Reach, with terrain, elevation, a court, ruined arch, props and a tidehound visual. The actual town, dungeon and full island remain Phase 6. Campaign saves, pause, main menu and recovery remain Phase 3. Eight spell icons expose an original targeting cursor/effect sample; spell rules remain Phase 5. Co-op is not enabled: the server accepts one local session and binds only loopback.

## Controls

| Client | Action |
|---|---|
| WASD / arrows | Eight-direction integer tile steps with 200 ms interpolation |
| Hold right mouse over ground | Walk toward the selected tile |
| Drag item | Ground ↔ backpack ↔ paperdoll; server validates transfers |
| I or B / P / M / J | Backpack / paperdoll / spellbook / field journal |
| Window title / x | Move / close gump |
| O / Tab / F1 | Outfit / tile grid / help |
| Spell icon then ground | Original targeting-effect preview; Esc cancels |
| Esc | Exit when no targeting cursor is active |

| Editor | Action |
|---|---|
| WASD / arrows | Pan the map |
| 1–4 / terrain buttons | Select grass, dirt, stone or water |
| Left mouse | Paint terrain, or place the selected prop |
| O / F3 | Cycle original props / toggle placement mode |
| Right mouse | Erase props at a tile |
| + / − | Raise/lower the tile under the pointer by two units |
| Ctrl+Z / Ctrl+Y | Undo/redo |
| Ctrl+S / F5 | Export / reload exported map |
| R / Tab / Esc | Restore current loaded fixture / grid / exit |

Default editor output: `UserData/Maps/reach-edit.json`. The packaged original map is never overwritten by the UI. A blocked/invalid spawn prevents export. To play it, close the editor and use `Play-Edited-Map.cmd`. Map changes are loaded at boot; live server map synchronization is not part of this phase.

## Provider and boot boundaries

| Application | Original-content path |
|---|---|
| Client | `Greyhaven.Presentation.OriginalGame`, shared original package, FNA texture/audio loading, named text, original gumps and server state |
| Server | New .NET 10 host compiles the supplied ServUO core and Ultima source; installs the original provider into **real ServUO TileMatrix and TileData**; uses original definitions and ServUO static collision |
| Editor | Shared original renderer and map tools; original overload of **CentrED TileDataProvider** fills its native land/static structures from the same definitions |

The source forks also expose explicit original boot modes: `TazUO --original-content` and `CentrED --original-content`. The release uses smaller launchers linked to the same original presentation/provider code, avoiding legacy client/editor embedded UI resources. It does **not** claim that every legacy UO gump, loader, skill, housing or profession system has been converted. Those paths are outside this original mode. Normal legacy fork entry points remain available for development.

ServUO's original net48 build is preserved and still compiles. `src/Greyhaven.ServUO.Core` is a separate net10 target of the complete supplied Server and Ultima source. Legacy script compilation, shard configuration, accounts and World.Load are deliberately not run by the original host. No legacy scripts or lore enter the fixture. Persistent simulation and richer entities are later milestones.

`OriginalContentBridge` installs typed land/static tables and chunk callbacks before TileData initialization. TileMatrix bypasses legacy file readers and map patches. Core.FindDataFile explicitly throws once original mode is installed. Server terrain/elevation and placed-object blocks are checked against the shared map at boot. Movement also uses the shared corner/height policy, with ServUO collision checks; two-unit corner transitions follow the original cell-height policy rather than forcing legacy averaged-Z slope semantics.

The native original renderer uses baked 2D sprites and original terrain surfaces. Raised tiles have simple filled skirts; this is not a finished slope/autotile system. No runtime 3D or EA imagery is used.

## Package format

The application root contains `content/original/package.json` and `art/compiled`. Package-relative paths are root-contained, hash checked, and cannot traverse folders or follow package symlinks. Missing files, invalid definitions, disabled legacy features or corrupt digests stop boot with a useful error; no registry search or legacy fallback occurs.

- `definitions.json`: original terrain, static, item, equipment and spell identities; collision, height, wet/surface flags; explicit disabled out-of-scope systems.
- `map.json`: named terrain cells and heights, named placed statics, spawn and 8×8 chunk convention.
- `text.en.json`: original names, journal text and interface labels; no cliloc dependency.
- `audio.json` and six WAVs: original deterministic synthesized step, pickup, drop, opening, targeting and cancellation cues.
- `assets.json`: SHA-256 integrity manifest for original map, definitions, text, audio and runtime sprite assets.
- `art/compiled/manifest.json`: Phase 1 sprite atlas, frame, facing, pivot, font and interface contract.

`tools/content/build_package.py` reproduces the fixture and synthesized cues. It never reads legacy data. Rebuilding overwrites the development fixture; it does not modify exported UserData maps. Rebuilding art requires rebuilding this content integrity manifest afterwards.

## Local protocol

The server and client exchange versioned JSON lines over loopback TCP. Both must supply the same package fingerprint and canonical map hash. A mismatch fails the handshake. The server owns tile positions, movement timing, item identities, pickup range and equipment replacement. The client interpolates accepted positions; it never changes the authoritative tile ahead of acknowledgement. A disconnected client stops issuing simulation commands and shows a connection-lost message; it does not switch to a second local simulation.

Commands in this milestone are hello, step, transfer and ping. Transfers reject unknown items, nonwearable equipment, blocked/out-of-range ground targets and remote pickups. Invalid transfers leave the item unchanged. Networking is an internal single-player host boundary, not a production multiplayer protocol. The launcher is a convenience boot path; campaign/session menus and robust recovery are still Phase 3.

## Developer commands

Use .NET SDK 10.0.402 and the existing pinned dependency sources. Windows runtimes are self-contained .NET 10.0.13.

```console
python tools/project.py content-build
python tools/project.py original-build
python tools/project.py content-check
python tools/project.py runtime-publish
```

For manual hosting, use `Runtime/Greyhaven.Server.exe --content-root ROOT --port 2599`, then `Runtime/Greyhaven.Client.exe --content-root ROOT --connect 127.0.0.1:2599`. Use `--map FILE.json` on both endpoints for a selected editor export. The editor accepts `--export FILE.json` for a different output file.

All three applications accept `--check` and `--audit FILE.json` for headless validation. Client/editor accept `--capture FILE.png` for a native render then exit. `tests/verify_original_boot.py` drives isolated boots, protocol checks, edited-map reloads and failure cases. The 20-test C# suite additionally checks the real ServUO and CentrED adapters.

Logs and provider file audits go under `UserData/Logs`. Audits record original content files opened by the provider; they are not an OS-wide file-access trace. Windows graphics/input behavior still requires a Windows host test. Native render and live connection tests were run on Linux using SDL offscreen/OpenGL.

Portable builds use `packages.lock.json`. Explicit Windows/Linux publishes use separate `packages.win-x64.lock.json` and `packages.linux-x64.lock.json` files, selected through Directory.Build.props. Publishing cannot overwrite the portable restore graph. All available flavors are mirrored in manifests/nuget-resolved.json.

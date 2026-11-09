# Accepted foundation decisions

1. Preserve UO feel: eight-direction tile-authoritative movement with interpolation. Continuous-position movement is explicitly out of scope.
2. Preserve ground/container/gump drag-and-drop, targeting cursors and movable paperdoll/spellbook windows.
3. Original generated 2D cutout parts and shared rigs; bake frames into atlases. No expectation of user-authored animation frames and no runtime 3D.
4. Windows x64 is the first supported demo target. Keep the FNA baselines initially; do not convert to MonoGame during bootstrap.
5. Local authoritative server process; automatic launching, save slots and pause arrive in Phase 3. Optional two-player co-op shares that simulation.
6. Preserve existing framework targets until individual baselines build. Modernizing ServUO net48 is a separate measured change.
7. Original content uses stable named IDs, localization keys and domain-scoped numeric adapters. The skeleton contracts are not wired into legacy engines in Phase 0.
8. Forks stay separated under vendor; original package data and generated art remain outside vendor. No blanket relicensing.

## Intended boundaries

| Module | Responsibility | Current state |
|---|---|---|
| Client fork | Presentation, picking, input and gumps | Legacy implementation preserved; original mode described below |
| Simulation fork | Validate commands, own entities and persist campaign | Legacy implementation preserved; original mode described below |
| Editor fork | Terrain/static authoring; later gameplay tools | Legacy implementation preserved; original mode described below |
| Contracts | Named content, tile positions, visual domains | Initial standalone definitions |
| Content packages | Original art, metadata, text and sound | Descriptor only |
| Tools | Verify baselines, bootstrap dependencies, restore/build, audit | Implemented |

## Migration seams

Client: UOFileManager and individual loaders become replaceable providers; preserve renderer and input paths. MobileView and LayerOrder provide the initial equipment seam. Editor: TileDataProvider and MapManager load shared original definitions. Server: TileData and TileMatrix consume the same collision and elevation data. World and timer lifecycle changes are Phase 3.

Keep protocol compatibility internally while proving original visuals. Do not freeze all UO numeric limits into the permanent content schema. No launcher should silently search for or download EA data.

## Phase 2 implementation

The independent mode now uses `Greyhaven.Content` for named map/definition/text/visual/audio data and integrity validation. The native client/editor original renderer lives in `Greyhaven.Presentation`; the full forks expose an explicit early `--original-content` branch. Smaller release entry points use that same code without legacy embedded UI assets.

The server's independent net10 host compiles the complete supplied ServUO core and installs an `OriginalContentBridge` before TileData initialization. Real TileMatrix chunk and tile-data structures receive the original catalog. The original net48 target still builds. The editor's original TileDataProvider overload fills its native CentrED structures from the shared catalog.

A loopback-only single-session JSON protocol owns tile movement, item state and transfers on the server. Matching package fingerprints and map hashes are mandatory. A small launcher starts/stops an owned host; Phase 3 still supplies campaign menus, saves, pause, recovery and more complete session lifecycle. See PHASE-2.md for exact scope and retained legacy boundaries.

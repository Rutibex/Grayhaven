# Demo milestones

0. Foundation: isolated source forks, provenance, dependency bootstrap, build entry points, original content contracts and dependency audit.
1. Art proof: one eight-facing humanoid, cutout rig, outfits, hats, weapon, shield, monster and interface samples.
2. Independent-content boot: client/server/editor providers; no UO installation or proprietary data.
3. Campaign lifecycle: local host, save slots, pause, reload, autosave.
4. Inventory/paperdoll: physical item drag/drop, nested containers, stacking, equipment, trade, journal and quickbar.
5. Combat/magic: melee/ranged, five enemies, eight spells, spellbook, effects, death/recovery.
6. Island/editor: 256x256 target island, town, interiors and two-floor dungeon; gameplay placement tools.
7. Adventure: named NPCs, limited schedules, main quest and three side quests.
8. Art/audio completion: original tiles, architecture, props, outfits, creatures, icons, gumps, effects and sounds.
9. Co-op: two-player host/join, authoritative transfers, shared quest rules and reconnect.
10. Release: clean-machine install, complete playthrough, persistence and multiplayer verification.

Phase 0 is not accepted as a reproducible engine baseline until all three upstream forks compile against recorded dependency revisions and NuGet locks. Tooling validation alone does not satisfy that build gate.

Phase 7 implementation: see PHASE_7.md, PHASE_7_SERVUO.md and PHASE_7_VALIDATION.md.

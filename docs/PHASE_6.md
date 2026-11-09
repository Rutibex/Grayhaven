# Phase 6 — Island and map workshop

Extract the complete release and run **Run-Greyhaven.cmd** on Windows x64. Start a **new campaign in an empty slot** to explore Greyhaven Island. Continuing a Phase 3–5 campaign preserves its original saved map, possessions and combat state; it does not replace that map with the island.

## The island

The shared 256×256 tile atlas contains Greyhaven's coast, Morrowwood, Saltfen, Saint Orin Cemetery, the Broken Beacon, Gullhook Docks, and two compact dungeon floors in a separate eastern sector of the atlas. Stairs join those sectors; there is no loading screen or continuous-position movement. All actors still move by authoritative integer tile steps with visual interpolation.

Greyhaven has eight roofed buildings: the Lantern and Gull inn, Nessa's Provisions, Anchor Forge, House of Quiet Tides, Tidal Grimoire, and three homes. Fifteen named residents have short original conversations. Town and docks are sanctuaries. Nessa trades at the town square; Elin heals visitors. Residents remain at authored positions in this milestone; schedules and full quests belong to Phase 7.

The town interiors use nine new original generated furniture sprites: bed, table, stool, anvil, bookcase, forge, workbench, gravestone and barrel. Roofs use grouped, tinted original stone tiles and disappear when the player enters; front walls cut away for readability. They are a simple roof treatment, with dedicated architecture art still planned for Phase 8. Existing cutout rigs and animations are retained. Two original synthesized ambient loops distinguish outdoors and cellars.

## Explore and interact

| Control | Action |
| --- | --- |
| G | Open/close the island atlas |
| E | Use the nearest interaction within two tiles |
| Double-click a resident, sign, door, lever or stair | Interact within two tiles |
| Double-click a chest | Open its movable inventory gump |
| Drag the labelled brass key to your backpack | Carry the physical vault key |
| Tab / Space / F / H / M | Existing combat, crystal throw, tonic and magic controls |
| F5 / F9 | Quick save / quick load |

The minimap marks residents and stairs; the atlas lists the major areas. Signs and discoveries add journal entries. The map is fully revealed in this milestone. Clicking the large atlas closes it; there is no player fast travel.

Take the north road to the Broken Beacon and double-click the stair marker to descend. The Drowned Cellars contain **16 rooms across two floors**, paired return stairs, supply chests, a brass key in the upper vaults, a repeating salt-rune trap, a tide lever, a portcullis, a key-locked final entrance and the Lantern Keeper encounter. Open the lower gate with its lever, carry the upper-floor key, then open the vault door to reach the final chamber. The key cannot be sold or merged into a different stack. Containers and their contents are ordinary physical inventory entities and can be dragged as before.

Thirteen persistent encounters use the five Phase 5 enemy archetypes. The Lantern Keeper uses the brineguard rules and original armored rig; this milestone does not introduce a new boss animation or separate boss combat rules. Defeated enemies remain defeated. Doors, gate state, trap recharge, discoveries, loot, key location and enemy state save with the campaign. The trap applies damage when stepped on and recharges after three seconds.

## Map workshop

Open **Map-Editor.cmd**. The native original-content workshop and the full CentrED fork use the same content definitions and editing code. Export with **Ctrl+S** to `UserData/Maps/reach-edit.json`. The packaged source map remains unchanged. **F5** reloads that export. **Play-Edited-Map.cmd** starts a campaign from it; continuing an existing campaign restores that campaign's saved map.

| Control | Tool |
| --- | --- |
| WASD / arrows | Pan the editor camera |
| 1–4 | Choose terrain |
| F2 / category buttons | Terrain, static, prefab or gameplay-object palettes |
| / / search field | Search asset names and stable IDs; Enter applies |
| + beside a palette row | Toggle favorite; favorites persist in the local editor profile |
| Favorites row | Filter the palette |
| Mouse wheel over palette / PageUp / PageDown | Scroll assets |
| [ / ] | Brush radius, from 1×1 through 7×7 |
| Space | Toggle terrain rectangle mode; drag its opposite corner |
| F | Flood fill the pointed terrain region |
| O | Cycle original static props |
| Left mouse | Paint, place a prop, stamp a building or place a gameplay object |
| Right mouse | Erase statics and gameplay objects at a tile |
| + / − | Raise/lower the current brush in two-unit increments |
| Ctrl+Z / Ctrl+Y | Undo/redo |
| V | Select the pointed tile and inspect a gameplay object or roof group |
| F4 | Edit selected interaction or roof properties |
| Tab inside properties | Choose the property field; typing replaces its value |
| Enter / Esc inside properties | Apply that field / cancel |
| Delete | Remove selected gameplay object or roof group |
| K | Set spawn at a clear, validated tile |
| F8 | Toggle roof display/cutaway |
| G | Open the atlas; click an atlas location to move the editor camera |
| F7 | Validate references, spawn, placement and navigation |
| F6 / Playtest button | Play the selected tile in an isolated campaign |

Cottage and inn prefabs stamp floors, collision walls, a working door and a named roof group. Gameplay palettes place residents/merchants/healers, all five encounter types, signs, doors, gates, containers, keys, traps, levers, stairs, discovery/quest trigger markers, lights and ambience markers. The property form edits names, text, coordinates, appearance, actor/encounter types, roles, target IDs, required key IDs, values and loot. `loot` uses a JSON list of `{ "definition": "greyhaven.item.healing-tonic", "amount": 2 }` entries. Roof groups expose name, bounds and visual. Trigger markers currently create discovery journal entries; they are an authoring seam for Phase 7 quests, not a full quest scripting system.

A lever's `targetId` must name a gate. A stair's `targetId` must name another stair/portal object. A locked door's `requiredItemId` uses the physical key ID `world.item.<key-object-id>`. Newly placed levers/stairs initially choose an existing compatible target; inspect and change that target before export. World IDs remain stable in exports. The default residents are static; an NPC's role selects merchant/healer behavior or conversation only.

The pointer readout previews terrain collision and elevation. F7 checks named references, duplicate identities, actor overlap, safe-area encounters, loot, required keys and reachable placements. Navigation validation treats doors and gates as author-openable and follows stairs. Separate gameplay tests verify the shipped dungeon's key/lever progression. Invalid edits remain editable, but export and playtesting require validation. Changes that break references must be repaired or undone.

F6 exports an exact map snapshot beneath `UserData/Playtests/<session>/map.json`, launches an owned host/client at the selected walkable tile, and saves that test session beneath the same playtest directory. It does not use any of your three normal campaign slots. The selection must be clear; tile V selects a new position. Normal playtesting uses the full inventory, combat, pause and save interfaces.

## Source and verification

`tools/content/build_island.py` contains explicit routes, buildings, rooms, encounters and interaction placements; seeded vegetation dressing is deterministic. `tools/art/bake_furniture.py` extracts the reviewed original prop atlas and is included in the main art bake. The original source atlas, generation prompt, extraction bounds and provenance are included. `tools/content/build_package.py` builds the map, definitions, text, audio and hashes. The former 32×32 mechanics map remains at `content/original/maps/reach-fixture.json`.

`Greyhaven.World.Tests` verifies island reachability, real ServUO tile agreement, live object behavior, key/lever progression, authored NPCs/enemies, saves and editor operations. `verify_island.py` exercises TCP interactions, isolated playtest saves, fresh-host reloads, native game views, and native workshop routes including the selected-tile playtest button. These native checks run on Linux with offscreen SDL/OpenGL; cross-published Windows binaries have not been executed on Windows here.

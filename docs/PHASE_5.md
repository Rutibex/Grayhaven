# Phase 5 — Combat, magic and enemy behavior

Run `Run-Greyhaven.cmd` after extracting the complete release on Windows x64. The existing 32x32 Wayfarer's Reach fixture now contains five persistent enemy encounters. The starting camp, within three tiles of its spawn, is a sanctuary: enemies cannot enter, and you cannot start hostile attacks from it. Walk east past the camp boundary to meet the first tidehound. The full island and dungeon remain Phase 6.

## Controls

| Control | Action |
| --- | --- |
| Tab | Toggle war/peace mode; editor Tab still toggles its grid |
| Click an enemy | Select it and display its health/behavior |
| Double-click an enemy | Enter war mode and attack; close to melee range to auto-swing |
| Space | Attack selected enemy |
| F | Throw a carried tide crystal at the selected enemy, or choose a target |
| H | Drink a carried healing tonic |
| M | Open spellbook; click a spell then its target |
| 1–9 / 0 | Activate quickbar bindings |
| C | Cancel a cast; paid mana is not refunded |
| R after falling | Recover at camp |
| R while alive and near your fallen pack | Reclaim its contents and equipment |
| Esc | Cancel targeting/dragging or pause/resume |
| F5 / F9 | Quick save / quick load |

The existing tile movement, ground↔gump dragging, paperdoll, containers, trade and journal remain. Spell icons can still be dragged to quickbar slots. An entirely blank quickbar is seeded with Dawn, Ember, Bind, Ward, tonic, crystal, Renew, Storm, Venom and Gate. Existing nonblank shortcut arrangements remain intact. A crystal shortcut consumes the selected stack's identity; F chooses an available carried stack.

## Combat and resources

The host owns health (100), mana (60), stamina (100), attack/cast cooldowns, hit resolution, enemy state and loot. Actions never trust client damage or resource values. Melee auto-swings against the chosen target while in war mode, in reach and visible. There is no automatic player movement toward the target. Unarmed damage is 5; sword damage is 12; greatblade damage is 18 with slower swings. A shield reduces incoming damage by 2; Ward halves the remaining damage. Enemy armor reduces incoming damage. These are deterministic demo rules, without accuracy dice or attribute progression yet.

Melee consumes stamina and shares a server-enforced swing timer. Throwing consumes one tide crystal and 6 stamina, has range 6 and a 1.4-second cooldown, and creates a projectile whose impact is resolved by the host. Crystals are physical inventory items and can be replenished through Nessa's trade gump. A tonic consumes one unit, restores up to 30 health and has a five-second recovery. It cannot be wasted at full health. Exhausting an item stack clears its shortcut as in Phase 4.

Mana and stamina regenerate during active play, approximately 2 and 3 units per second. Camp also restores health. Pause and offline time do not advance combat, regen, projectiles or status timers. The client requests read-only snapshots to show enemy activity even when no keys are pressed. Large accepted displacements such as Gate/recovery snap visually; ordinary movement keeps tile interpolation.

## Eight spells

| Spell | Mana | Windup | Range | Effect |
| --- | ---: | ---: | ---: | --- |
| Dawn | 6 | 0.45s | Self | Heal 18 and cleanse poison |
| Storm | 14 | 1.0s | 6 | 16 damage in radius 2, with a three-second slow |
| Ember | 8 | 0.65s | 6 | Projectile impact for 18, then a short burn |
| Renew | 10 | 0.8s | Self | Health pulses over five seconds |
| Ward | 10 | 0.5s | Self | Halve incoming damage for eight seconds |
| Venom | 9 | 0.6s | 5 | Projectile impact for 6, then poison pulses |
| Bind | 8 | 0.5s | 5 | Root one enemy for four seconds; it can still attack in range |
| Gate | 16 | 0.9s | 5 | Blink to a visible, walkable, unoccupied tile |

Click a spell in the book or its shortcut, then a world target. Self spells affect the player. Ember, Venom and Bind require a living enemy. Storm accepts a tile or enemy. Gate accepts a clear destination. Spellbook tooltips show costs and effects. A cast bar shows progress.

The host checks target type, mana, range and line of sight before taking mana. Movement, damage or manual cancellation interrupts a cast; paid mana is not refunded. Ranged targets are checked again at resolution, so a moved, defeated or newly obstructed target can fizzle. Gate checks occupancy again before teleporting. Line of sight rejects obstructing terrain, statics, excessive elevation differences and diagonal corner shortcuts. Hostile magic is forbidden inside camp. Bind stops movement rather than disabling all actions; a rooted caster can still cast or use Gate.

## Five enemy types

| Enemy | Health | Behavior |
| --- | ---: | --- |
| Tidehound | 32 | Fast melee pursuit and frequent bites |
| Shore Raider | 50 | Armed melee pursuit; flees briefly at low health |
| Reef Slinger | 36 | Ranged projectiles, retreats at close range |
| Salt Hexer | 42 | Ranged magic, periodic roots and poisonous bolts |
| Brineguard | 80 | Slow, armored, heavy melee attacks |

Enemies acquire visible targets beyond camp, navigate integer tiles around map obstacles and occupied actor cells, and attack on individual timers. Routing is bounded. They do not overlap living actors or cut map corners. Pursuit is leashed to the home area; enemies return home when the player escapes, recovers or enters camp, and recover health there. Slows and roots affect movement. Enemy health, position, statuses, deadlines and deaths persist. This milestone does not respawn defeated encounters; start a new campaign or load an earlier save to replay them.

The five types reuse original cutout rigs and existing outfit/equipment combinations, with walk, attack and casting clips. No proprietary assets, runtime 3D or newly drawn animation-frame sets are required. Brineguard has an armored appearance; the player's O outfit toggle remains cosmetic rather than granting free defense. Original synthesized swing, impact, casting, healing and death cues extend the existing audio set.

## Death and recovery

On defeat, the player's carried roots—including worn equipment—move into an anchored fallen-pack container at the death tile. Nested bags retain their contents and IDs. The pack cannot be picked up or sold, but its contents can be dragged out after recovery. Falling blocks movement, item handling and combat until R recovers at camp. Recovery restores partial health/mana, clears statuses and grants ten seconds of protection; starting hostile combat ends that protection.

Return within two tiles of the fallen pack and press R to reclaim its contents and restore available equipment slots. This is atomic: if the resulting capacity/weight would be invalid, manually retrieve selected items instead. Older fallen packs remain lootable if the player falls again. At the absolute 512-item pool limit, where another container cannot be allocated, carried roots drop directly on the death tile; possessions are still conserved. The deepest legal bag hierarchy has a reserved extra parent link for a fallen pack. Enemy defeats create a single silver loot stack when the item pool has room. Defeated enemies and loot flags do not reset on load.

## Persistence and compatibility

Combat saves retain resources, active casts, in-flight effects, cooldowns, statuses, all five encounters, defeat count and fallen-pack references. A saved projectile applies its damage once after resuming. Loading remains paused until explicitly resumed. The host uses monotonic elapsed time and takes detached state snapshots under the world lock before network serialization.

The published Phase 3 and Phase 4 fingerprints are recognized. Copy the complete UserData directory into the newly extracted release while the game is closed. Existing possessions, stock, notes, positions and nonblank shortcut arrangements are retained; missing combat state initializes once. Older executables cannot read new combat fields, so keep a UserData backup with the previous release if you want to return to it. Other incompatible packages are rejected.

`Build-Original-Content.cmd` now runs content, inventory and combat assertion suites before publishing the self-contained Windows runtime. Developer integration commands are:

```sh
python tests/verify_combat.py --runtime /path/to/linux-runtime --dotnet /path/to/dotnet --render
python tests/verify_inventory.py --runtime /path/to/linux-runtime --dotnet /path/to/dotnet --render
```

Evidence and native captures are under `docs/phase5`. Native UI routes are exercised in-process against a live host, not through physical OS mouse automation. Windows binaries are cross-published; native testing here uses Linux offscreen FNA/OpenGL. A Windows run remains required for final platform acceptance.

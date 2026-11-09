# Phase 4 — Inventory and paperdoll

Extract the complete release and run `Run-Greyhaven.cmd` on Windows x64. A new campaign contains the original equipment, eight healing tonics, 125 silver marks, three tide crystals, two sealed scrolls, a field satchel and a ground sea chest. Nessa the outfitter stands beside the starting camp. Continue restores the campaign's inventory graph, merchant stock, journal, outfit and shortcuts.

## Controls

| Control | Action |
| --- | --- |
| Left drag | Move an item between ground, backpack, open containers and paperdoll |
| Drop on a closed container icon | Put the item inside it |
| Drop on a compatible stack | Combine the stacks |
| Shift-drag | Choose the quantity to move when released; Enter confirms and Esc cancels |
| Single click / hover | Inspect name, quantity, total weight, equipment requirements and capacity |
| Double click | Open a container, equip/unequip equipment, or inspect another item |
| I / B | Open/close backpack |
| P | Open/close paperdoll |
| M | Open/close spellbook |
| J | Open/close journal |
| T | Open/close Nessa's merchant gump; actual trading requires being nearby |
| N | Write a journal note; Enter saves, Esc cancels |
| O | Change the saved travel outfit |
| 1–9 / 0 | Activate the corresponding quickbar shortcut |
| Drag an item or spell to the quickbar | Bind a shortcut; this does not move or consume it |
| Right click a quickbar slot | Clear its shortcut |
| Mouse wheel over merchant / journal | Page merchandise or scroll journal history |
| Esc | Cancel a text/quantity prompt or targeting; otherwise pause/resume |
| F5 / F9 | Quick save / quick load, as in Phase 3 |

Gumps retain movable title bars and close buttons. Local window positions and visibility persist in `UserData/Interface/gumps.json`; nested container windows are reopened explicitly. The default backpack is beside the paperdoll so it does not cover character information. Opening the backpack from the paperdoll's button also works.

## Inventory rules

The server commits each inventory transaction against a copied candidate state. It validates the complete result before replacing live state; rejected drops, purchases and splits change neither items nor money. The client keeps items at their accepted positions until the reply arrives, so cancellation and rejection need no recovery spawn or duplicated temporary item.

Each object has a stable unique ID and quantity. Partial moves retain the source ID and create a new ID for the moved portion. Combining whole stacks removes the source ID and redirects its quickbar shortcuts to the surviving stack. Stack limits are 9,999 units. Tonics, marks, crystals and scrolls stack; equipment and containers do not. A full stack can be moved without a quantity prompt; Shift-drag provides exact splitting. Merges that would overflow are rejected in full.

Containers retain their contents when moved or dropped. A field satchel holds 12 direct objects and 20 units of contents weight; a sea chest holds 24 objects and 60 units. Nested contents contribute to ancestor weight. A container counts as one direct object in its parent. The backpack holds 125 direct objects; total carried weight is limited to 100 units. There may be at most 512 world item objects and eight container parent links. Containers cannot be placed inside themselves or descendants, and orphan parents are invalid. Contents of a dropped container cannot be accessed once its ground root is more than two tiles away. The client closes out-of-reach container gumps. Picking up a filled container includes all of its contents in carrying weight.

The paperdoll shows the demo's supported hat, weapon and off-hand slots, character preview, base attributes and carried weight. Dropping on an explicit slot checks that slot; dropping on the character auto-selects the equipment's slot. Equipping replacement gear puts the previous piece in the backpack. The Mariner Greatblade uses both hands and stows a shield; equipping a shield stows a two-handed weapon. If there is no room or weight allowance for the resulting inventory, the entire operation fails. Current requirements use the demo's base STR/INT 10. Attribute progression, armor/combat statistics and additional equipment families arrive with later game systems.

All equipment and containers reuse existing original Greyhaven sprites and rig combinations. The greatblade shares the existing blade art, and the new satchel/chest use the existing original bag/chest artwork. No new hand-drawn animation frames or proprietary UO assets are required.

## Merchant, journal and quickbar

Click Nessa or press T to open the merchant. Choose Buy/Sell, select a row, then Review quantity/confirm. The prompt shows quantity, unit price and total. Nessa's reach is two tiles; prices, stock, quantities and payment are server-owned. Confirmation includes the inventory revision to prevent a stale or replayed offer charging twice. Silver can be taken from carried nested containers. Purchases require stock, sufficient marks, carrying weight and backpack room. Sales require a carried, unequipped item; filled containers must be emptied first. Equipment is traded one piece at a time. Merchant stock persists per campaign. This is single-player merchant trade; two-player secure trade belongs to the later co-op phase.

The journal records successful transfers, equipment changes, outfit changes and trades alongside personal notes. It retains the newest 200 entries, with up to 240 printable characters per entry. The interface accepts up to 200 characters per note so entries fit cleanly. Mouse wheel scrolls history. Notes and activity persist with the campaign.

Quickbar slots store item IDs or spell IDs, not copies of the objects. Containers open; equipment toggles wear; ordinary items show inspection information. Item shortcuts remain visible but cannot act when their object is no longer carried. Selling the last unit clears its shortcut; merging remaps the shortcut. Spell shortcuts open the original targeting preview. Actual healing, item consumption and spell execution are Phase 5 systems and are not simulated here.

## Save compatibility

Copy your entire `UserData` directory into the new extracted project while the game is closed. The exact published Phase 3 content fingerprint is recognized. Its player position, elapsed time, equipment and eight item identities/quantities remain intact. New container specimens and merchant stock are initialized once when upgrading that saved state. Loading an old save does not overwrite that save file: subsequent normal save actions write the new state using the existing Phase 3 backup rules. Old Phase 3 executables cannot read the new fields; keep the previous release and a backup of UserData if you want to return to it. Other incompatible content packages are rejected.

Single-player pause, autosave, save slots, graceful quit and embedded campaign maps remain unchanged. Tile-authoritative movement and interpolation remain the movement model.

## Build and verification

`Build-Original-Content.cmd` builds the runtime, runs both content and inventory assertion suites and publishes self-contained Windows x64 binaries. Developers can run the live inventory test against a Linux runtime:

```sh
python tests/verify_inventory.py --runtime /path/to/linux-runtime --dotnet /path/to/dotnet --render
```

The test uses a temporary root and exercises real TCP authority, Phase 3 save upgrade and the native UI's item hit-testing, container drops, quantity confirmation, shortcut binding, equipment drop, merchant confirmation and note-input handler. It drives these native UI routes in-process; it does not claim physical OS mouse/keyboard automation. Native captures and evidence are in `docs/phase4`. Linux offscreen FNA rendering is exercised; the supplied Windows binaries are cross-published and need Windows execution for final platform acceptance.

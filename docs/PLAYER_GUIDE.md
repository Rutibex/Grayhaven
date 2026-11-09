# Playing Greyhaven

## Get started

Download the **Windows x64** release archive, extract it completely into its own folder, and double-click `Run-Greyhaven.cmd`. Create a new campaign in one of the three slots. The launcher manages a local server and opens the player client; there is no network lobby or public server in this milestone.

## Where to go

Greyhaven Island includes a town, shoreline, woods, marsh, cemetery, and the Broken Beacon. Start in town, speak to residents, and explore the island; the dungeon has two floors, locked passages and an encounter route. To begin the main quest, speak with **Sera** about **The Light Below**. **Orrin**, **Lena**, and **Mara** introduce side quests. Quest acceptance and reward collection require explicit nearby interactions. **Nessa** trades in the town square.

## Movement, combat, and items

| Action | Control |
|---|---|
| Move | WASD, arrow keys, or hold right mouse on the ground |
| Interact | E, or double-click a nearby target |
| War mode | Tab |
| Attack | Space |
| Throw crystal / tonic | F / H |
| Bandage / meditate | Y / F10 |
| Recovery/reclaim | R |
| Inventory, paperdoll, magic | I, P, M |
| Journal, quests, skills | J, Q, K |
| Atlas | G |
| Merchant trade | T |
| Journal note | N |
| Save, load, pause | F5, F9, Esc |
| Audio | F8; use arrows or sliders to adjust levels |

Drag items between containers, the ground, the paperdoll, and the quickbar. **Shift-drag** splits a stack; **double-click** opens containers. Select a spell then a valid tile or creature to cast; Esc cancels targeting. Interface window title bars can be dragged to reposition them.

## Workshop and map editing

Run `Map-Editor.cmd` from the extracted folder. **F2** opens the object palette, **V** selects world items, **F4** edits object properties, **F7** checks map references and reachability, and **F6** starts a separate playtest. **Ctrl+S** exports the edited map to `UserData/Maps/reach-edit.json`; `Play-Edited-Map.cmd` starts a campaign against that export. **Ctrl+Z/Y** undo/redo.

## Audio sampler

Open `Start-Media-Preview.html` from the extracted Windows release to browse example graphics and play packaged sound cues. It uses local files and is entirely optional; the actual game is launched from `Run-Greyhaven.cmd`.

## Saved games

Game data, map exports and interface preferences are saved locally under `UserData` inside the extracted game folder. **Back up `UserData` before upgrading or moving the installation**. A Phase 7 save-upgrade path is implemented and was exercised in project validation, but backing up saves is still recommended. Never upload your private saved campaign directory when reporting bugs.

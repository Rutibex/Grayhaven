# Original-data migration inventory

`python tools/project.py audit` records exact source locations in `manifests/uo-data-references.json`. This targeted static scan is not an exhaustive runtime trace. Required/optional behavior varies with client version and enabled features.

| Family | Existing consumers | Replacement and proof |
|---|---|---|
| Terrain/maps: map*.mul / LegacyMUL.uop, patches, facet data | Server TileMatrix; client MapLoader; editor landscape | Original chunk terrain/elevation provider; all three agree on coordinates and heights |
| Scenery: statics*.mul / staidx*.mul | Server TileMatrix; client MapLoader; editor | Original placed scenery records and collision metadata |
| tiledata.mul | Server TileData; client TileDataLoader; editor TileDataProvider | Shared original tile definitions; consistent surfaces, height, collision and equipment references |
| art.mul / artidx.mul / artLegacyMUL.uop | Client ArtLoader; editor resource loading | Original atlases, pivots and sprite metadata |
| texmaps.mul / texidx.mul | Client TexmapsLoader; editor | Original terrain textures and transitions |
| anim*.mul / animation UOP / animdata.mul and definition tables | Client animation and asset loaders | Cutout rigs baked into original directional clips; no inherited body IDs required for authoring |
| gumpart / gumpidx / gump UOP | Client GumpsLoader and numeric UI references | Original gump skin and named UI assets; audit hardcoded UI numbers separately |
| fonts.mul / unifont*.mul and bundled fonts | Client FontsLoader and embedded resources | Approved font with explicit license and original style rules |
| Cliloc.* | Client ClilocLoader; server localization numbers | Original text catalog; named localization keys; replace server text references |
| sound*.mul / sound*.uop, music config/files | Client sound/music loaders; numeric server sound calls | Original audio event catalog and licensed original sounds |
| hues.mul / light.mul | Client palettes/lights; editor | Original color masks, palettes and light sprites |
| multi.mul / multi.idx / MultiCollection.uop | Client MultiLoader and house/boat definitions | Original building prefabs; defer unused boats/housing systems |
| speech.mul / skills.mul / professions and .def tables | Client startup and skills UI | Original ability/skill catalog and NPC text; no inherited lore |
| Embedded PNG/TTF/ICO/native libraries | csproj resources and runtime copy rules | Resource review queue; explicit provenance before public release |
| Scripted content and configuration | Server Scripts, regions, decoration, quests and spawn definitions | Original island scripts, names, recipes, quests, dialogue and map placements |

## Safe migration order

1. Trace startup on all three applications; distinguish mandatory loaders from optional features.
2. Inject providers without removing working rendering/input paths.
3. Supply tiny original fixtures for every mandatory provider.
4. Replace all numeric UI and text dependencies reachable in the demo.
5. Disable out-of-demo content through explicit configuration.
6. Test with no UO installation, no registry discovery and an empty legacy-data search path.
7. Complete a clean-machine playthrough and audit loaded files. Only then declare EA-data independence.

Phase 0 preserves legacy source and embedded resources, but does not bundle extracted EA client-data archives. Do not confuse source preservation with approval to redistribute every existing embedded resource.

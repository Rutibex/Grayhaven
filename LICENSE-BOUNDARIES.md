# License boundaries

Original Greyhaven foundation code and documentation are provided under MIT (see LICENSE).
No license here overrides an upstream or third-party license.

| Subtree | Supplied top-level license | Handling |
|---|---|---|
| vendor/ServUO | GPL version 2 text | Preserve notices; distribute covered modifications and corresponding source under applicable GPL terms |
| vendor/TazUO | BSD 2-Clause | Preserve copyright, conditions and disclaimer |
| vendor/CentrED | MIT | Preserve copyright and permission notice |

Treat ServUO-derived modifications as GPL-covered. Do not copy ServUO implementation into MIT contracts. Keep the local simulation process independent and communicate through versioned commands; process separation is not a legal exemption by itself. Determine combined-work obligations before release.

Third-party submodules, NuGet packages, native binaries, fonts, images and shader resources require their own provenance/license review. Top-level licenses do not establish permission for every embedded resource. `manifests/resources-review.json` is a review queue, not clearance. No EA assets may enter `content/` or `art/`.

New story, maps, dialogue, sprites and audio will have explicit per-package licensing. Do not apply the user's book license automatically to this project. No final game-content license has yet been selected.

## Phase 2 runtime

`src/Greyhaven.ServUO.Core` compiles the complete supplied ServUO Server and Ultima code. That assembly and `src/Greyhaven.Server` are GPL-covered; corresponding source is included in the full project ZIP. The existing net48 target remains available. Original package, network contracts, presentation, launcher and editor glue are separate original MIT code and do not override upstream terms.

The small Runtime folder contains framework/native libraries and these application assemblies. Its `Notices` directory preserves applicable available framework, FNA, font, project and ServUO notices. Original source/vendor resources remain a development review queue; original-mode boot does not load them as game art. Final original game-content licensing remains undecided.

## Phase 7 server adaptations

`src/Greyhaven.Server/AdventureSimulation.cs`, `ProgressionSimulation.cs` and `TownSimulation.cs` are GPL-2.0 server adaptations informed by the bundled ServUO sources. Their comments identify this boundary. `manifests/phase7-servuo-sources.json` records the consulted source hashes; `docs/PHASE_7_SERVUO.md` describes adopted behavior and original demo differences. The quest catalog, names and dialogue are original Greyhaven content. No legacy quest text or cliloc data is loaded into the demo.

## Phase 8 media

The four new source sheets have their generation prompts and SHA-256 records in art/source/provenance.json. Audio is synthesized from the original recipes in tools/content/build_audio.py, without sampled recordings or soundfonts. These content files retain the original game-content licensing decision above; MIT code does not assign a separate final content license.

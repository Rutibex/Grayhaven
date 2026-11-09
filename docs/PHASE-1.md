# Phase 1 — Prove the art and animation pipeline

## Scope and result

The production problem is authoring consistent animation frames. This proof replaces that workload with a small set of generated directional cutouts, reusable 2D rigs, and deterministic frame baking. It does not ask the player or author to draw individual frames. The seven recovered image sheets are retained in their original form. A truncated interface attachment was replaced by its intact saved copy; none of the recovered art was regenerated.

| Asset | Authored inputs | Baked output |
|---|---|---|
| Humanoid | Five facings, six parts, two outfits | 24 equipment combinations × 8 facings × 28 frames = 5,376 |
| Teal material specimen | Recolor mask applied to burgundy torso fabric | 8 × 28 = 224 |
| Tidehound | Five facings, six parts | 8 × 20 = 160 |
| Environment | Four terrain surfaces, eight props | Terrain diamonds and transparent prop sprites |
| Interface/items | Three panels, four item icons, eight spell icons, four gear icons | Runtime images and offline/native interface proof |

Total: **5,760 baked frames**, generated from **137 extracted parts**. Frames are 128×128, pivot `(64,112)`, 16 columns per atlas. Human clips: idle 4 frames at 4 fps, walk 8 at 10 fps, attack 8 at 12 fps, cast 8 at 10 fps. Creature clips omit cast.

## Rebuild workflow

1. Keep original sheets in `art/source/generated`. `art/source/provenance.json` records their hashes and recovery origin. Original lost-workspace prompts were not recovered and are not invented here.
2. Edit reviewed grid boundaries and rig math in `tools/art/build_art.py`. `art/rigs/*.json` describes the production contract; the procedural pose implementation currently lives in Python, rather than a general animation editor.
3. Run `Build-Art.cmd`, or install the pinned requirements and run `python tools/project.py art-build`.
4. Inspect `art/compiled/contact-sheet.png`, `walk-proof.gif`, and the offline/native proof in all facings and equipment combinations.
5. Run validation and tests before accepting changes. Builds are staged. A failure leaves the previously accepted compiled directory intact.

The atlas manifest records clip ranges, frame rates, pivots, dimensions and filenames. `attachments.json` describes shared head/hand placement. Cutouts are exported under `parts`; extraction rectangles are recorded in `slices.json`. `checksums.json` covers every compiled PNG. PNGs are encoded into memory, verified, written with a temporary name, read back, and atomically installed; validation fully decodes every PNG.

Two complete bakes were compared: all 196 PNGs were byte-identical in this environment. The self-contained preview embeds those images. Regenerating the images makes a new offline preview as part of the same staged build.

## Movement and interaction proof

Movement authority is an integer tile coordinate. A step commits the next tile and interpolates presentation for 0.2 seconds. A second step cannot start until the previous visual transition completes. The 44×44 terrain projection uses half-tile vectors `(22,22)` and `(-22,22)`. Inclusive edge pixels overlap slightly at joins to avoid hairline gaps.

The backpack, paperdoll and spellbook are movable gumps. Item drag/drop supports ground ↔ bag and gear ↔ paperdoll. Invalid targets cancel the transfer; replacement gear returns to the bag. Each proof item retains one identity through transfers. This is temporary local model state, not the future server's authoritative inventory transaction system.

The browser and native preview implement separate lightweight interaction models and share the complete visual asset contract. Node tests exercise the browser model; browser tests exercise pointer/keyboard events. Native rendering and integrated client startup were smoke-tested; native pointer interactions still need a Windows manual pass.

The native Game class is linked by the standalone launcher and compiled directly into TazUO. The client checks `--art-proof` immediately after native library setup and before language, settings, login or UO content initialization:

```console
TazUO.exe --art-proof --art-assets PATH_TO_ART_COMPILED
```

Optional `--art-capture PATH` writes a proof render and exits. The standalone launcher uses `--assets PATH` and `--capture PATH` instead. Normal client behavior is unchanged outside this explicit proof mode.

## Production limits to carry forward

- Three facings mirror five authored facings. Physical handedness and directional lighting mirror too. Asymmetric heraldry and strict hand consistency require authored left/right attachment variants before broad content production.
- This is a rigid cutout proof with modest rotation and stride. It demonstrates a practical pipeline, but needs further tuning for foot planting, shoulder seams and weapon reach before final combat animation. It is not a full skeletal/mesh animation editor.
- The teal atlas is a fixed-equipment material specimen. Its mask selects burgundy torso cloth and leaves steel and skin alone; it is not a complete dye system. Regular outfit/gear combinations are the equipment acceptance set.
- Source sheets contain a few colored edge pixels and differing part proportions. Extraction uses alpha trimming; there is no global chroma-key pass that might remove original colored details.
- The isometric kit is a visual sample, not a completed map, collision set, autotile system, or editor import.
- The eight named spell emblems prove the magic interface. Rules, targeting, effects, costs and cooldowns belong to Phase 5.
- No saved campaign, multiplayer state or audio content is introduced in this phase.

## Baseline preservation

The existing `Main.cs` modification is explicitly recorded in `manifests/fork-overrides.json`, including the original and reviewed current hash. Foundation verification accepts only that recorded change and continues to verify other source and dependency files.

Phase 0 accidentally packaged generated `obj_core` intermediate files inside dependency source. Phase 1 excludes `obj_*` and `bin_*` from packaging and dependency manifests. Source and pinned revisions are retained; build intermediates are regenerated by the SDK. NuGet lock contents remain recorded for reproducible restores.

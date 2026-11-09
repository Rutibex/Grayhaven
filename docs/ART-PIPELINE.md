# Original 2D asset production contract

Phase 1 must prove the art in motion before bulk generation. Codex generates and prepares the assets; the user is not expected to draw animation frames.

- Fixed isometric camera; preserve UO projection during initial integration.
- Eight logical facings. Reuse mirrored art only when asymmetry and lighting remain correct.
- Generated directional cutout parts, shared humanoid and creature rigs, and baked sprite atlases.
- Hats and hair are directional attachments following the head; weapons and shields follow hands.
- Shared animation timing, ground pivots, attachment points and direction-dependent occlusion.
- Inventory icons, ground visuals, equipment appearance and paperdoll appearance reference the same item definition but may use different artwork.
- Palette masks support material variants without independent animation production.
- Preview every facing and supported equipment combination; inspect feet sliding, seams and clipping.
- No runtime 3D and no dependence on EA sprites as production source.

Folders: `art/source` holds generated originals and rig inputs; `art/compiled` holds extracted transparent parts, atlases and metadata. Track generation history, edits, source references and license status per asset before it enters a distributable content package.

First proof: one humanoid, two outfits, two hats, sword, shield, one monster, a small environment kit, and sample backpack/paperdoll/spellbook. Art dimensions and final rig format remain a Phase 1 decision based on the in-client test.

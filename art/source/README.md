# Original source art

Seven recovered original image-generation sheets and the new Phase 6 furniture atlas live in `generated`. Provenance and SHA-256 hashes are in `provenance.json`. Keep originals unchanged; edit extraction/rig inputs and rebuild instead. The fully recovered interface file is used, not the truncated attachment copy.

The included DejaVu Sans Mono font has its own notice in `FONT-LICENSE.txt`. Runtime font rendering uses the baked bitmap atlas. The game-content license remains undecided; no EA artwork was used as production input.

The furniture generation prompt is in `generated/furniture-prompt.txt`. Its reviewed extraction is reproducible with `tools/art/bake_furniture.py` and the main art bake.

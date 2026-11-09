# Phase 8 — Original art and audio production

This release finishes the demo's original-content media set. Run **Run-Greyhaven.cmd** for the game or open **Start-Media-Preview.html** from the extracted folder to inspect the cast, art sheets and soundtrack offline. No UO installation, external soundfont, audio recording or proprietary game data is used.

## What changed

- Four new generated source sheets: architecture, directional enemies, relics/effects and blank gump frames. Exact prompts and source SHA-256 records are included.
- Distinct Shore Raider, Reef Slinger, Salt Hexer and Brineguard art. Each uses five authored facings, three mirrored facings and shared cutout motion for idle/walk/attack/cast. The original Tidehound rig remains.
- Linen and moss resident clothing treatments, alongside the original player outfits, hats, sword and shield. The Mariner Greatblade now has its own held-blade cutout across both outfits and all three hats, using the existing attachment rig and two-hand equipment rules.
- Timber town walls, dark cellar masonry, red tile roofs, wooden interiors, cellar flagstones and shore sand. The roof still hides inside its building. Open doors and raised gates show their passable state.
- Dedicated bandage, Lantern Heart, brass key, letters, satchel and greatblade item visuals. These remain the same inventory objects and quest identities; their pictures no longer borrow unrelated icons.
- Painted quest, skills, journal and trade frames, rendered with nine slices over a dark backing so text stays readable. Existing backpack, paperdoll and spellbook art remains.
- Eight original effect cutouts, animated through motion, scale, rotation and fade. Existing combat timing, damage, range and targeting remain authoritative.
- 39 original PCM audio cues: 32 effects, four ambient loops and three 48-second music pieces. Material footsteps, doors, gates, cloth, coins, medicine, creature attacks and different spell sounds are routed in the client.

The terrain and prop definitions retain the original collision contracts. Two additional props, a jetty piling and a ruined column, are available in the editor palette. The existing island map bytes are unchanged, and existing Phase 7 campaign maps remain usable.

## Audio controls

**F8** opens sound settings. Adjust Master, Effects, Music and Ambience with the bars or Up/Down and Left/Right. **M** toggles mute while that panel is open; **Enter** previews a cue. F8 or Esc closes it. Levels save locally and survive restarting. M continues to open the spellbook outside the sound panel.

Town, coast and cellar select their own score and ambience, with fades between regions. Music and ambience pause with the campaign and resume from their positions. The master bus also controls effects. Sound settings are interface preferences, separate from campaign progress.

## Production pipeline

Install the pinned Python dependencies in tools/art/requirements.txt and tools/content/requirements.txt. The full art build is staged, so a failed build retains the accepted compiled art:

```sh
python tools/art/build_art.py
python tools/content/build_package.py
python tools/project.py content-check
python -m unittest discover -s tests -p 'test_*.py'
```

Generated source images are under art/source/phase8; their prompts and provenance are supplied. tools/art/bake_production.py extracts cells and builds the new shared rigs. No generated individual animation frames are requested. Mirroring keeps the original handedness tradeoff of the cutout pipeline; enemy motion is intentionally a stylized puppet animation rather than hand-drawn frame animation.

The music and sound design are authored in tools/content/build_audio.py using modal/additive instruments, filtered noise and deterministic envelopes. The soundtrack uses original D Dorian/aeolian motifs at 80 BPM. No sampled recordings or soundfonts are bundled. art/source/phase8/audio-production.json records every cue's duration, category, sample format, peak, RMS, seam measurement and hash.

The audio preview page uses real WAV files; native audio-route tests use a dummy device and do not establish how a particular loudspeaker sounds. Source PCM is checked for silence, clipping and loop discontinuity.

## Compatibility

The explicit Phase 7 content fingerprint is supported. Item IDs, stack quantities, equipment requirements, skills, quest state, NPC positions, reputation, doors and the embedded campaign map retain their meanings. New art is selected by existing object identity or content definition. Custom NPC actor assignments remain respected. This phase adds no multiplayer protocol or campaign schema change; co-op remains Phase 9.

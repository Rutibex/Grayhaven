# Phase 0 delivery boundaries

Implemented: source isolation, source/archive fingerprints, upstream comparison evidence, pinned dependency bootstrap and bundled dependency verification, unified build tools and Windows menu, immutable-source build overrides, original content contracts, empty original package descriptors, CI configuration, data migration inventory and license/provenance records.

No gameplay systems or artwork have been replaced in this phase. The client, server and editor remain legacy implementations at runtime. Their original UO data requirements are explicitly inventoried for Phase 2. Campaign launcher, pause and save slots are Phase 3; Foundation.cmd is a development menu, not a game launcher.

Optional co-op, tile-authoritative movement and UO-style item dragging are accepted architectural constraints. Cutout animation is the accepted production direction. There is no continuous-position movement conversion.

The included CI workflow has not been run on hosted Windows runners in this session. Build verification here is Linux cross-compilation/source compilation with the pinned SDK, not a Windows graphics/audio runtime test. See VALIDATION.md for measured results.

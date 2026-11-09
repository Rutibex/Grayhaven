# Building Greyhaven from source

This is the **developer repository**, not the Windows player download. The project archives pinned upstream dependency sources and their licensing/verification metadata. All source required for the included original-content programs remains in this tree.

## Requirements

- Windows x64 is the primary supported distribution target; use the .NET SDK **10.0.402** specified by `global.json`.
- Python **3.10+**, Git for fetching dependencies if your source checkout lacks them.
- For original legacy fork builds, the **.NET Framework 4.8 Developer Pack** may be needed.
- Pillow and other Python requirements apply only when rebaking original art/content (`tools/art/requirements.txt`, `tools/content/requirements.txt`).

## Commands

```console
python tools/project.py verify
python tools/project.py original-build
python tools/project.py content-check
python tools/project.py runtime-publish
```

The `runtime-publish` command creates the shared self-contained `Runtime/` directory. Once it exists, run `Run-Greyhaven.cmd` from the repository root with `content/original/` and `art/compiled/` present. The provided `Build-Original-Content.cmd` wraps the original build/content check/publish flow on Windows.

To regenerate art/audio and the package integrity manifest, use the commands documented in `docs/PHASE_8.md` and `tools/content/build_package.py`. Do not discard source cutouts or upstream notices from the corresponding source distribution; they are not needed in the player ZIP but are preserved here for reproducibility and licensing.

**Reproducibility caveat:** the original verification manifest and upstream snapshot are kept as-is; changes to tracked vendor snapshots require deliberate updates. The original `tools/verify_release.py` validates the original full phase archive, not this split source/player release; use `scripts/verify_player_archive.py` for the curated Windows ZIP.

Build and test commands are supplied based on the original project. The repackaging process did not run a Windows binary or a fresh .NET build in this environment.

## Preparing this source ZIP as a Git repository

`git add .` alone omits files inside upstream-ignored dependency trees. After `git init -b main`, run **`python scripts/stage_github_repo.py`** to stage every path required by the byte-exact source manifests, then commit. This script does not stage the generated `Runtime` directory or local saves.

Vendored `.gitattributes` rules have a source-packaging-only `-text` override recorded in `manifests/fork-overrides.json` and `manifests/bundled-dependencies.json` so GitHub checkout preserves the original upstream bytes.

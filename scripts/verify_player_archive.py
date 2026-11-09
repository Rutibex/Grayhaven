#!/usr/bin/env python3
"""Basic validation for Greyhaven's curated Windows player ZIP; Python 3.10+."""
import hashlib, json, sys, zipfile
from pathlib import PurePosixPath

if len(sys.argv) != 2:
    raise SystemExit("Usage: python scripts/verify_player_archive.py path/to/Greyhaven-v0.8.0-preview-Windows-x64.zip")
path = sys.argv[1]
with zipfile.ZipFile(path) as archive:
    names = archive.namelist()
    files = set(names)
    assert len(names) == len(files), "Duplicate archive names"
    assert archive.testzip() is None, "ZIP CRC mismatch"
    assert all(n.startswith("Greyhaven/") and ".." not in PurePosixPath(n).parts for n in names), "Unsafe paths"
    assert not any("/UserData/" in n or "/vendor/" in n or n.startswith("Greyhaven/src/") or n.endswith(".pdb") for n in names), "Developer/user file in player archive"
    required = ["Run-Greyhaven.cmd", "Map-Editor.cmd", "Play-Edited-Map.cmd", "Start-Media-Preview.html", "Runtime/Greyhaven.Client.exe", "Runtime/Greyhaven.Server.exe", "Runtime/Greyhaven.Editor.exe", "Runtime/Greyhaven.Launcher.exe", "content/original/package.json", "art/compiled/manifest.json", "Runtime/Notices/ServUO-GPL-2.txt"]
    for r in required:
        assert "Greyhaven/"+r in files, f"Missing {r}"
    hashes = json.loads(archive.read("Greyhaven/content/original/assets.json"))["files"]
    for rel, digest in hashes.items():
        assert "Greyhaven/"+rel in files, f"Missing content asset {rel}"
        assert hashlib.sha256(archive.read("Greyhaven/"+rel)).hexdigest() == digest, f"Wrong hash: {rel}"
    assert sum(n.startswith("Greyhaven/content/original/audio/") and n.endswith(".wav") for n in names) == 39, "Incorrect sound cue count"
    for role in ("Client", "Server", "Editor", "Launcher"):
        assert archive.read(f"Greyhaven/Runtime/Greyhaven.{role}.exe")[:2] == b"MZ", f"Bad Windows executable {role}"
    print(f"PASS: {len(files)} safe player files, {len(hashes)} integrity-checked original assets, 39 sound cues, four Windows executables")

#!/usr/bin/env python3
"""Non-build consistency checks for published Greyhaven repository."""
import json, hashlib, pathlib
root=pathlib.Path(__file__).resolve().parents[1]
assert (root/'src/Greyhaven.Server/Program.cs').is_file()
assert (root/'vendor/ServUO/Server').is_dir()
assert (root/'vendor/TazUO/external/FNA').is_dir()
assert (root/'vendor/CentrED/Shared').is_dir()
assert (root/'README.md').is_file()
assert (root/'LICENSE-BOUNDARIES.md').is_file()
assert not (root/'Runtime').exists(), 'Player binaries must not be in repository'
for n in ['town','inventory','bestiary','audio']:
    assert (root/f'docs/screenshots/{n}.png').is_file()
assets=json.loads((root/'content/original/assets.json').read_text())['files']
for p,h in assets.items():
    assert hashlib.sha256((root/p).read_bytes()).hexdigest()==h, p
print(f'PASS: repository and {len(assets)} original content asset hashes')

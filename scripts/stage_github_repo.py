#!/usr/bin/env python3
"""Stage a complete Greyhaven Git repo, including dependency files ignored by upstream."""
import json
import subprocess
from pathlib import Path

root=Path(__file__).resolve().parents[1]
if not (root/'.git').exists():
    raise SystemExit('Run git init -b main in the extracted Greyhaven directory first.')

def git(*args, data=None):
    subprocess.run(['git','-C',str(root),*args],input=data,check=True)

# Source checkout includes pinned external packages as plain source files,
# not recursive Git submodules. Those upstream projects ignore their own folders.
git('add','.')
snapshot=json.loads((root/'manifests/source-snapshot.json').read_text())
deps=json.loads((root/'manifests/bundled-dependencies.json').read_text())
paths=sorted({r['path'] for r in snapshot} | {f['path'] for d in deps for f in d['files']})
for path in paths:
    if not (root/path).is_file():
        raise SystemExit('Missing required source: '+path)
# Force add *only* the pinned vendor file list, not arbitrary build outputs.
git('add','-f','--pathspec-from-file=-','--pathspec-file-nul',data=('\0'.join(paths)+'\0').encode())
print(f'Staged project and {len(paths)} byte-verified pinned source files.')

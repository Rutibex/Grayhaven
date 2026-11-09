"""Package source without machine-specific outputs or dependency Git databases."""
import json, zipfile, hashlib
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
BASELINE={row['path'] for row in json.loads((ROOT/'manifests/source-snapshot.json').read_text())}
SKIP={'.git','bin','obj','__pycache__','.vs','.idea','artifacts','output','publish','UserData','ArtProof'}
def included(f):
    rel=f.relative_to(ROOT)
    if rel.as_posix() in BASELINE:return True
    if any(x in SKIP or x.startswith(('obj_','bin_','.art-bake-','.compiled-')) for x in rel.parts): return False
    if f.suffix.lower() in {'.nupkg','.snupkg','.writing'}:return False
    if rel.parent.as_posix()=='vendor/ServUO' and f.suffix.lower() in {'.dll','.exe','.pdb'}:return False
    return True
# Freeze fetched dependency source bytes so bundled dependencies can be verified without Git.
rows=[]
for dep in json.loads((ROOT/'manifests/dependencies.lock.json').read_text()):
 files=[]
 for f in sorted((ROOT/dep['path']).rglob('*')):
  if f.is_file() and included(f):files.append({'path':f.relative_to(ROOT).as_posix(),'sha256':hashlib.sha256(f.read_bytes()).hexdigest()})
 rows.append({'path':dep['path'],'commit':dep['commit'],'files':files})
(ROOT/'manifests/bundled-dependencies.json').write_text(json.dumps(rows,indent=2)+'\n')
out=ROOT.parent/'Greyhaven_Phase_8_Art_and_Audio.zip'
with zipfile.ZipFile(out,'w',zipfile.ZIP_DEFLATED,compresslevel=6) as z:
 for f in sorted(ROOT.rglob('*')):
  if f.is_file() and included(f):z.write(f,Path('Greyhaven')/f.relative_to(ROOT))
print(out, out.stat().st_size)

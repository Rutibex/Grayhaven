#!/usr/bin/env python3
"""Create an isolated original-content root for earlier 32x32 mechanics integration suites."""
import argparse,hashlib,json,shutil
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
ap=argparse.ArgumentParser();ap.add_argument('destination',type=Path);a=ap.parse_args();out=a.destination.resolve()
if out.exists() and any(out.iterdir()):raise SystemExit('Choose an empty destination.')
for name in ['art','content']:shutil.copytree(ROOT/name,out/name,dirs_exist_ok=True)
shutil.copyfile(ROOT/'content/original/maps/reach-fixture.json',out/'content/original/map.json')
p=out/'content/original/assets.json';v=json.loads(p.read_text());v['files']['content/original/map.json']=hashlib.sha256((out/'content/original/map.json').read_bytes()).hexdigest();p.write_text(json.dumps(v,indent=2)+'\n')
for phase in ['phase2','phase3','phase4','phase5']:(out/'docs'/phase).mkdir(parents=True,exist_ok=True)
print(out)

#!/usr/bin/env python3
"""Deterministic, non-generative extraction of the original Phase 6 prop atlas."""
from pathlib import Path
import hashlib,json
from PIL import Image
ROOT=Path(__file__).resolve().parents[2]
# Cell extraction bounds are normalized against the reviewed 1280px preview.
SLICES={'bed':([0,0,455,405],[88,90]),'table':([455,0,870,405],[88,70]),'stool':([870,0,1280,405],[32,38]),'anvil':([0,405,450,840],[48,60]),'bookcase':([450,405,860,840],[70,98]),'forge':([860,405,1280,840],[80,98]),'workbench':([0,840,485,1280],[88,74]),'gravestone':([485,840,855,1280],[42,68]),'barrel':([855,840,1280,1280],[42,60])}
def append_props(out,manifest):
 source=ROOT/'art/source/generated/furniture.png';im=Image.open(source).convert('RGBA');slices={}
 for name,(box,size) in SLICES.items():
  box=tuple(round(v*(im.width if i%2==0 else im.height)/1280) for i,v in enumerate(box));part=im.crop(box);bbox=part.getchannel('A').getbbox();assert bbox,name;part=part.crop(bbox);part.thumbnail(size,Image.Resampling.LANCZOS);dest=out/f'images/{name}.png';dest.parent.mkdir(exist_ok=True,parents=True);part.save(dest,optimize=True);manifest['images'][name]={'image':f'images/{name}.png','size':list(part.size)};slices[name]={'sourceBox':box,'trim':bbox,'size':list(part.size)}
 (out/'furniture-slices.json').write_text(json.dumps(slices,indent=2)+'\n')
if __name__=='__main__':
 out=ROOT/'art/compiled';m=json.loads((out/'manifest.json').read_text());append_props(out,m);(out/'manifest.json').write_text(json.dumps(m,indent=2)+'\n');checks={p.relative_to(out).as_posix():hashlib.sha256(p.read_bytes()).hexdigest() for p in sorted(out.rglob('*.png'))};(out/'checksums.json').write_text(json.dumps(checks,indent=2)+'\n');print('Baked nine original furniture sprites.')

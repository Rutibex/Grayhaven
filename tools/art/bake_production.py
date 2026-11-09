#!/usr/bin/env python3
"""Phase 8 deterministic layout extraction and shared cutout rigs. No frame-by-frame generated animation."""
from pathlib import Path
import json,math,hashlib
from PIL import Image,ImageDraw,ImageOps
import numpy as np
from scipy.ndimage import label
ROOT=Path(__file__).resolve().parents[2];SOURCE=ROOT/'art/source/phase8';RES=Image.Resampling.LANCZOS
ARCH=['timber-left','timber-right','cellar-left','cellar-right','roof-terracotta','floor-wood','floor-cellar','sand','jetty-piling','gate-closed','stairs-down','beacon-lit','door-closed','door-open','ruin-column','signpost']
RELICS=['bandage','lantern-heart','vault-key','greatblade','letter-packet','satchel','gate-rune','corpse-marker','fx-ember','fx-venom','fx-storm','fx-ward','fx-heal','fx-gate','fx-bind','fx-quest']
SIZES=[(44,76)]*4+[(44,44)]*4+[(42,64),(48,70),(44,44),(44,76),(44,64),(44,64),(44,76),(42,64)]
def cells(filename,cols,rows):
 im=Image.open(SOURCE/filename).convert('RGBA');out=[]
 for y in range(rows):
  for x in range(cols):
   box=(round(x*im.width/cols),round(y*im.height/rows),round((x+1)*im.width/cols),round((y+1)*im.height/rows))
   if filename=='relics.png' and y==0 and x==3:box=(round(im.width*.71),0,im.width,round(im.height*.31))
   part=im.crop(box)
   if filename=='creatures.png' or filename=='relics.png' and y<2:
    # Ignore isolated flecks or adjacent-cell spill while preserving nearby detached gear/glow.
    alpha=np.array(part.getchannel('A'));labels,count=label(alpha>28);sizes=np.bincount(labels.ravel());sizes[0]=0;main=int(sizes.argmax());yy,xx=np.where(labels==main);keep=np.zeros(count+1,dtype=bool)
    for component in range(1,count+1):
     if sizes[component]<max(12,sizes[main]*.001):continue
     cy,cx=np.where(labels==component)
     if xx.min()-15<=cx.mean()<=xx.max()+15 and yy.min()-15<=cy.mean()<=yy.max()+15:keep[component]=True
    alpha[~keep[labels]]=0;part.putalpha(Image.fromarray(alpha))
   bounds=part.getchannel('A').point(lambda v:255 if v>28 else 0).getbbox();assert bounds,(filename,x,y);out.append((part.crop(bounds),box,bounds))
 return out

def append_production(out,m,parts=None):
 from build_art import atlas,human,save_png
 rows=[]
 def image(name,part,size):
  part=part.copy();part.thumbnail(size,RES);save_png(part,out/f'images/{name}.png');m['images'][name]={'image':f'images/{name}.png','size':list(part.size)}
 for idx,(name,(im,box,trim),size) in enumerate(zip(ARCH,cells('architecture.png',4,4),SIZES)):
  if 4<=idx<=7:
   w,h=im.size;im=im.crop((int(w*.34),int(h*.34),int(w*.66),int(h*.60))).resize((44,44),RES);mask=Image.new('L',(44,44));d=ImageDraw.Draw(mask)
   for y in range(44):width=2*(y+1) if y<22 else 2*(44-y);x=(44-width)//2;d.line((x,y,x+width-1,y),fill=255)
   im.putalpha(mask)
  image(name,im,size);rows.append({'name':name,'source':'architecture.png','cell':box,'trim':trim,'size':size})
 for name,(im,box,trim) in zip(RELICS,cells('relics.png',4,4)):
  image(name,im,(96,96) if name.startswith('fx-') else (48,48));rows.append({'name':name,'source':'relics.png','cell':box,'trim':trim})
 # Five authored facings per enemy. Cut into a torso and two leg regions, with an overlap at waist.
 sources=cells('creatures.png',5,4);frameproof=[]
 for row,name in enumerate(['shore-raider','reef-slinger','salt-hexer','brineguard']):
  poses=[]
  for face in range(5):
   im,box,trim=sources[row*5+face];im=im.copy();im.thumbnail((90,104),RES);pose=Image.new('RGBA',(128,128));pose.alpha_composite(im,(64-im.width//2,112-im.height));poses.append(pose);save_png(pose,out/f'production/poses/{name}-{face}.png');rows.append({'name':name+'-'+str(face),'source':'creatures.png','cell':box,'trim':trim})
  def render(face,clip,frame):
   from build_art import CLIPS
   im=poses[face];n=CLIPS[clip][0];phase=frame*math.tau/n;motion=math.sin(phase);action=math.sin(math.pi*frame/(n-1)) if clip in ['attack','cast'] else 0
   canvas=Image.new('RGBA',(128,128));waist=86
   # Adjacent alpha cutouts retain the artist's silhouette; no AI frame inference or inverse kinematics.
   for left in [True,False]:
    x0,x1=(0,64) if left else (64,128);leg=im.crop((x0,waist,x1,128));sign=1 if left else -1;dx=round(sign*motion*2) if clip=='walk' else 0;dy=round(-max(0,sign*motion)*2) if clip=='walk' else 0;canvas.alpha_composite(leg,(x0+dx,waist+dy))
   body=Image.new('RGBA',(128,128));body.alpha_composite(im.crop((0,0,128,waist+3)),(0,0));angle=(-7 if face>=2 else 5)*action if clip=='attack' else 0
   if angle:body=body.rotate(angle,resample=Image.Resampling.BICUBIC,center=(64,waist))
   bob=round(-abs(motion)) if clip=='walk' else round(math.sin(phase)*.6) if clip=='idle' else round(-action*2) if clip=='cast' else 0;canvas.alpha_composite(body,(round(action*3) if face>=2 else 0,bob));return canvas
  desc,frames=atlas(out,name,render,['idle','walk','attack','cast']);m['actors'][name]=desc
  frameproof.append((name,frames))
 if parts is None:
  parts={group:[[Image.open(out/f'parts/{group}/{r}-{c}.png').convert('RGBA') for c in range(6 if group=='armor' else 4)] for r in range(5)] for group in ['armor','equipment']}
 for name,color in [('resident-linen',(205,180,128)),('resident-moss',(91,132,97))]:
  cloth=[[im.copy() for im in row] for row in parts['armor']]
  for row in cloth:
   im=row[1];p=im.load()
   for y in range(im.height):
    for x in range(im.width):
     r,g,b,a=p[x,y]
     if r>g*1.2 and r>b*1.1 and r>28:
      lum=min(1,(r+g+b)/300);p[x,y]=(*(round(c*lum) for c in color),a)
  desc,frames=atlas(out,name,lambda r,c,f:human(cloth[r],parts['equipment'][r],r,c,f,0,0,0),['idle','walk','attack','cast']);m['actors'][name]=desc;frameproof.append((name,frames))
 # The greatblade shares the hero rig but uses its own source cutout and longer blade transform.
 blade=cells('relics.png',4,4)[3][0].rotate(-135,resample=Image.Resampling.BICUBIC,expand=True);bb=blade.getchannel('A').point(lambda v:255 if v>28 else 0).getbbox();blade=blade.crop(bb)
 if 'humanoid' not in parts:parts['humanoid']=[[Image.open(out/f'parts/humanoid/{r}-{c}.png').convert('RGBA') for c in range(6)] for r in range(5)]
 gear=[[im.copy() for im in row] for row in parts['equipment']]
 for row in gear:row[2]=blade
 for outfit in ['humanoid','armor']:
  for hat in range(3):
   name=f'{outfit}-h{hat}-s1-b0-greatblade';desc,_=atlas(out,name,lambda r,c,f:human(parts[outfit][r],gear[r],r,c,f,hat,1,0,False,True),['idle','walk','attack','cast']);m['actors'][name]=desc
 # Artist-made blank frame sprites, fitted with nine-slice rendering by the game.
 if (SOURCE/'panels.png').exists():
  for name,(im,box,trim) in zip(['gump-quests','gump-skills','gump-journal','gump-trade'],cells('panels.png',2,2)):
   image(name,im,(420,315));rows.append({'name':name,'source':'panels.png','cell':box,'trim':trim})
 (out/'production-slices.json').write_text(json.dumps(rows,indent=2)+'\n')
 # Review sheet shows actual baked frames and every new static/icon at runtime scale.
 sheet=Image.new('RGBA',(1000,700),(22,35,39,255));d=ImageDraw.Draw(sheet)
 for idx,(name,frames) in enumerate(frameproof):
  for j in range(5):sheet.alpha_composite(frames[j*28+4+2],(j*135,idx*108))
  d.text((690,idx*108+38),name,fill=(230,208,160))
 save_png(sheet,out/'production-actors.png')
 return rows

def main():
 out=ROOT/'art/compiled';m=json.loads((out/'manifest.json').read_text());append_production(out,m);(out/'manifest.json').write_text(json.dumps(m,indent=2)+'\n');(out/'checksums.json').write_text(json.dumps({p.relative_to(out).as_posix():hashlib.sha256(p.read_bytes()).hexdigest() for p in sorted(out.rglob('*.png'))},indent=2)+'\n');print('Production sprites and rigs baked.')
if __name__=='__main__':main()

#!/usr/bin/env python3
"""Deterministic cutout bake. Source sheets -> alpha parts -> shared rigs -> runtime atlases.
No generative calls, EA data, or runtime 3D. All output PNGs are fully encoded and checked.
"""
from pathlib import Path
import argparse,hashlib,io,json,math,os,shutil,tempfile
from PIL import Image,ImageDraw,ImageFont,ImageOps
ROOT=Path(__file__).resolve().parents[2]; SRC=ROOT/'art/source/generated'; OUT=ROOT/'art/compiled'
DIRS=['N','NE','E','SE','S','SW','W','NW']; MAP=[0,1,2,3,4,3,2,1]
CLIPS={'idle':(4,4),'walk':(8,10),'attack':(8,12),'cast':(8,10)}
RES=Image.Resampling.LANCZOS

def sha(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest()
def write_json(p,v):p.parent.mkdir(parents=True,exist_ok=True);p.write_text(json.dumps(v,indent=2)+'\n')
def save_png(im,p):
 p=Path(p);p.parent.mkdir(parents=True,exist_ok=True);b=io.BytesIO();im.save(b,format='PNG',compress_level=7);raw=b.getvalue()
 assert raw[-8:]==b'IEND\xaeB`\x82'
 with Image.open(io.BytesIO(raw)) as check:check.load()
 tmp=p.with_suffix('.writing')
 with tmp.open('wb') as f:f.write(raw);f.flush();os.fsync(f.fileno())
 assert tmp.read_bytes()==raw;os.replace(tmp,p)

def grid(name,xs,ys,out):
 im=Image.open(SRC/(name+'.png')).convert('RGBA');im.load();result=[];boxes=[]
 for r in range(len(ys)-1):
  row=[]
  for c in range(len(xs)-1):
   rect=(xs[c],ys[r],xs[c+1],ys[r+1]);cut=im.crop(rect);box=cut.getchannel('A').point(lambda a:255 if a>24 else 0).getbbox();assert box,(name,r,c)
   cut=cut.crop(box);row.append(cut);save_png(cut,out/f'parts/{name}/{r}-{c}.png');boxes.append({'row':r,'column':c,'box':[rect[0]+box[0],rect[1]+box[1],rect[0]+box[2],rect[1]+box[3]]})
  result.append(row)
 return result,boxes

def fit(im,w,h):return im.resize((max(1,round(w)),max(1,round(h))),RES)
def stamp(dst,im,x,y,w,h,angle=0,anchor=(.5,0)):
 im=fit(im,w,h);pad=Image.new('RGBA',(160,160));ax,ay=80,80;pad.alpha_composite(im,(round(ax-im.width*anchor[0]),round(ay-im.height*anchor[1])))
 if angle:pad=pad.rotate(angle,resample=Image.Resampling.BICUBIC,center=(ax,ay))
 dst.alpha_composite(pad,(round(x-ax),round(y-ay)))

def human(parts,gear,row,clip,frame,hat=0,sword=0,shield=0,tint=False,heavy=False):
 im=Image.new('RGBA',(128,128));n=CLIPS[clip][0];phase=2*math.pi*frame/n;walk=clip=='walk';act=math.sin(math.pi*frame/(n-1)) if clip in ('attack','cast') else 0
 bob=(-abs(math.sin(phase))*1.3 if walk else math.sin(phase)*.7 if clip=='idle' else -act)
 # Parallel projection of stride. Keep feet close to the shared ground pivot.
 fx,fy=[(0,-.55),(.75,-.4),(1,0),(.75,.4),(0,.55)][row]
 spread=[8,7,4,7,8][row];tw=[29,28,23,28,29][row]
 def leg(idx,sign):
  swing=math.sin(phase+ (0 if sign==1 else math.pi))*4 if walk else 0
  stamp(im,parts[idx],64+sign*spread*.5+fx*swing,80+fy*swing+min(0,-swing)*.25,13,32,(-swing*1.7 if row>0 else swing),(.5,0))
 leg(4,-1);leg(5,1)
 def arm(idx,sign,near):
  swing=math.sin(phase+(math.pi if sign<0 else 0))*12 if walk else 0
  angle=swing+(-act*75 if sign==1 and clip=='attack' else sign*act*50 if clip=='cast' else 0)
  x=64+sign*(tw/2-2);y=56+bob
  stamp(im,parts[idx],x,y,10,35,angle)
  # Rotated hand anchor and held objects share exactly the same transform.
  a=math.radians(angle);hand=(x+math.sin(a)*30,y+math.cos(a)*30)
  if sword and sign==1:stamp(im,gear[2],hand[0],hand[1],10 if heavy else 8,47 if heavy else 43,angle, (.5,.88))
  if shield and sign==-1:stamp(im,gear[3],hand[0],hand[1]-10,21 if row!=2 else 9,26,angle*.2)
  return hand
 far=-1 if row<3 else 1
 arm(2 if far==-1 else 3,far,False)
 torso=parts[1]
 if tint:
  # Explicit material-mask recolor: saturated burgundy fabric only; steel/skin unchanged.
  torso=torso.copy(); px=torso.load()
  for yy in range(torso.height):
   for xx in range(torso.width):
    r,g,b,a=px[xx,yy]
    if r>g*1.2 and r>b*1.1 and r>28:px[xx,yy]=(round(r*.48),min(255,round(g*1.4)),min(255,round(b*1.65)),a)
 stamp(im,torso,64,54+bob,tw,33)
 stamp(im,parts[0],64+(3 if row==2 else 1 if row in (1,3) else 0),55+bob,23,29,0,(.5,1))
 if hat:stamp(im,gear[hat-1],64+(2 if row==2 else 0),32+bob,34 if hat==1 else 36,18 if hat==1 else 29,0,(.5,.72))
 arm(2 if far==1 else 3,-far,True)
 return im

def monster(parts,row,clip,frame):
 im=Image.new('RGBA',(128,128));n=CLIPS[clip][0];p=2*math.pi*frame/n;gait=clip=='walk';act=math.sin(math.pi*frame/(n-1)) if clip=='attack' else 0;bob=-abs(math.sin(p))*1.5 if gait else math.sin(p)*.6
 side=row==2;diagonal=row in (1,3)
 bw=57 if side else 45 if diagonal else 31;bh=35 if side else 42
 anchors=[(-19,79),(19,79),(-20,83),(20,83)] if side else [(-10,80),(10,80),(-13,84),(13,84)]
 for i in range(4):
  x,y=anchors[i];s=math.sin(p+(math.pi if i in (1,2) else 0))*4 if gait else 0
  stamp(im,parts[i+2],64+x+s,y,11,29, s*2)
 stamp(im,parts[1],64,78+bob,bw,bh,0,(.5,.5))
 hx=89 if side else 76 if diagonal else 64;hy=75 if row<2 else 81 if side else 84
 stamp(im,parts[0],hx+act*4,hy+bob,32,35,act*10,(.5,.5))
 return im

def atlas(out,name,renderer,clips):
 frames=[];directions={};i=0
 for di,d in enumerate(DIRS):
  directions[d]={}
  for clip in clips:
   n,fps=CLIPS[clip];directions[d][clip]={'start':i,'count':n,'fps':fps}
   for f in range(n):
    im=renderer(MAP[di],clip,f)
    if di>4:im=ImageOps.mirror(im);shift=Image.new('RGBA',im.size);shift.alpha_composite(im,(1,0));im=shift
    frames.append(im);i+=1
 sheet=Image.new('RGBA',(2048,math.ceil(i/16)*128))
 for j,im in enumerate(frames):sheet.alpha_composite(im,((j%16)*128,(j//16)*128))
 path='actors/'+name+'.png';save_png(sheet,out/path)
 return {'image':path,'cell':[128,128],'columns':16,'pivot':[64,112],'frames':i,'directions':directions},frames

def build(out):
 out.mkdir(parents=True,exist_ok=True);parts={};slices={}
 specs={'humanoid':([0,230,450,630,830,1060,1254],[0,250,500,750,1000,1254]),'armor':([0,230,450,630,830,1060,1254],[0,250,500,750,1000,1254]),'equipment':([0,300,600,830,1122],[0,280,560,840,1120,1402]),'monster':([0,250,650,830,1000,1180,1374],[0,229,458,687,916,1145]),'environment':([0,362,724,1086,1448],[0,362,724,1086]),'icons':([0,362,724,1086,1448],[0,362,724,1086]),'interface':([0,625,1125,1810],[0,869])}
 for name,(xs,ys) in specs.items():parts[name],slices[name]=grid(name,xs,ys,out)
 manifest={'schema':1,'projection':{'tileSize':[44,44],'halfTile':[22,22],'movementAuthority':'tile','presentation':'interpolated'},'actors':{},'images':{},'variants':[]}
 proof_frames=None
 for outfit in ('humanoid','armor'):
  for hat in range(3):
   for sword in range(2):
    for shield in range(2):
     name=f'{outfit}-h{hat}-s{sword}-b{shield}';desc,frames=atlas(out,name,lambda r,c,f:human(parts[outfit][r],parts['equipment'][r],r,c,f,hat,sword,shield),CLIPS)
     manifest['actors'][name]=desc;manifest['variants'].append({'id':name,'outfit':outfit,'hat':hat,'sword':sword,'shield':shield})
     if name=='humanoid-h1-s1-b1':proof_frames=frames
 desc,frames=atlas(out,'tidehound',lambda r,c,f:monster(parts['monster'][r],r,c,f),['idle','walk','attack']);manifest['actors']['tidehound']=desc
 # Recolor variant proves a cloth mask without another source sheet or bespoke animation.
 desc,_=atlas(out,'armor-teal',lambda r,c,f:human(parts['armor'][r],parts['equipment'][r],r,c,f,1,1,1,True),CLIPS);manifest['actors']['armor-teal']=desc
 def item(name,im,size):
  im.thumbnail(size,RES);save_png(im,out/f'images/{name}.png');manifest['images'][name]={'image':f'images/{name}.png','size':list(im.size)}
 # Seamless 44-pixel tile mask, rows 2..44,44..2. Crop central material, excluding raised rims.
 for c,name in enumerate(['grass','dirt','stone','water']):
  raw=parts['environment'][0][c];cx,cy=raw.width//2,int(raw.height*.44);patch=raw.crop((cx-45,cy-30,cx+45,cy+30)).resize((44,44),RES).convert('RGBA');mask=Image.new('L',(44,44));md=ImageDraw.Draw(mask)
  for y in range(44):w=2*(y+1) if y<22 else 2*(44-y);x=(44-w)//2;md.line((x,y,x+w-1,y),fill=255)
  patch.putalpha(mask);item(name,patch,(44,44))
 names=['wall-left','wall-right','door','tree','chest','chest-open','lantern','shrub']
 for i,name in enumerate(names):item(name,parts['environment'][1+i//4][i%4].copy(),(112,140))
 for c,name in enumerate(['potion','coins','crystal','scroll']):item(name,parts['icons'][0][c].copy(),(36,36))
 for i,name in enumerate(['dawn','storm','ember','renew','ward','venom','bind','gate']):item('spell-'+name,parts['icons'][1+i//4][i%4].copy(),(42,42))
 for c,name in enumerate(['hat-leather','hat-wizard','sword','shield']):item(name,parts['equipment'][4][c].copy(),(40,40))
 for c,name in enumerate(['backpack','paperdoll','spellbook']):item(name,parts['interface'][0][c].copy(),[(270,360),(240,400),(420,310)][c])
 # Bitmap font avoids runtime font packages and produces identical native/browser labels.
 font=ImageFont.truetype(str(ROOT/'art/source/DejaVuSansMono.ttf'),14);fontim=Image.new('RGBA',(16*9,6*18));draw=ImageDraw.Draw(fontim)
 for i in range(96):draw.text(((i%16)*9,(i//16)*18),chr(i+32),font=font,fill='white',stroke_width=0)
 save_png(fontim,out/'images/font.png');manifest['font']={'image':'images/font.png','cell':[9,18],'columns':16,'first':32}
 from bake_furniture import append_props
 append_props(out,manifest)
 from bake_production import append_production
 append_production(out,manifest,parts)
 write_json(out/'manifest.json',manifest);write_json(out/'slices.json',slices)
 write_json(out/'attachments.json',{'pivot':[64,112],'head':[64,32],'hands':'Per-frame positions from shared shoulder rotation; identical transform used for arms and equipment.','mirroring':'Five authored facings. SW/W/NW mirror SE/E/NE around X=64. Asymmetric heraldry and physical handedness require future left-handed variants.'})
 contact=Image.new('RGBA',(8*160,4*150),(24,30,31,255));dr=ImageDraw.Draw(contact)
 for y,clip in enumerate(CLIPS):
  for d in range(8):
   im=proof_frames[d*28+{'idle':0,'walk':4,'attack':12,'cast':20}[clip]+2];contact.alpha_composite(im,(d*160+16,y*150));dr.text((d*160+20,y*150+124),DIRS[d]+' '+clip,font=font,fill='white')
 save_png(contact,out/'contact-sheet.png')
 gif=[]
 for f in range(8):
  canvas=Image.new('RGBA',(8*160,190),(24,30,31,255));draw=ImageDraw.Draw(canvas)
  for d in range(8):canvas.alpha_composite(proof_frames[d*28+4+f].resize((160,160),RES),(d*160,0));draw.text((d*160+60,165),DIRS[d],font=font,fill='white')
  gif.append(canvas.convert('RGB'))
 gif[0].save(out/'walk-proof.gif',save_all=True,append_images=gif[1:],duration=100,loop=0)
 write_json(out/'checksums.json',{p.relative_to(out).as_posix():sha(p) for p in sorted(out.rglob('*.png'))})
 validate(out)
 return manifest

def validate(out=OUT):
 m=json.loads((out/'manifest.json').read_text());checks=json.loads((out/'checksums.json').read_text())
 for name,digest in checks.items():
  p=out/name;assert sha(p)==digest,name
  with Image.open(p) as im:im.load()
 for actor in m['actors'].values():
  with Image.open(out/actor['image']) as im:assert im.width==2048 and im.height>=math.ceil(actor['frames']/16)*128
  for direction in DIRS:
   for clip,desc in actor['directions'][direction].items():assert desc['start']+desc['count']<=actor['frames']
 for name in ('grass','dirt','stone','water'):
  im=Image.open(out/m['images'][name]['image']);assert sum(a>0 for a in im.getchannel('A').getdata())==1012
 print(f"Validated {len(checks)} PNGs, {len(m['actors'])} atlases, {sum(x['frames'] for x in m['actors'].values())} frames.")

def main():
 ap=argparse.ArgumentParser();ap.add_argument('--validate',action='store_true');a=ap.parse_args()
 if a.validate:validate();return
 stage=Path(tempfile.mkdtemp(prefix='.art-bake-',dir=ROOT/'art'))
 try:
  build(stage)
  from make_preview import make
  make(stage)
  backup=ROOT/'art/.compiled-previous'
  if backup.exists():shutil.rmtree(backup)
  if OUT.exists():OUT.rename(backup)
  try:stage.rename(OUT)
  except BaseException:
   if backup.exists():backup.rename(OUT)
   raise
  if backup.exists():shutil.rmtree(backup)
 finally:
  if stage.exists():shutil.rmtree(stage)
if __name__=='__main__':main()

#!/usr/bin/env python3
"""Original offline PCM production: seeded foley, physical modal instruments and composed looping score.
No samples, soundfonts, voices, licensed recordings, network or generative audio service.
"""
from pathlib import Path
import hashlib,json,wave
import numpy as np
from scipy.signal import butter,sosfilt
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'content/original/audio';RATE=44100
CATALOG=[]
def filtered(n,seed,lo=80,hi=7000):
 x=np.random.default_rng(seed).normal(0,1,n);return sosfilt(butter(3,[lo,hi],btype='bandpass',fs=RATE,output='sos'),x)
def env(t,attack=.005,decay=.15):return (1-np.exp(-t/attack))*np.exp(-t/decay)
def save(name,x,loop=False,category='effects'):
 x=np.asarray(x);x=np.nan_to_num(x);peak=float(np.max(np.abs(x)));x=x*(min(.85/peak,1) if peak else 1)
 # Explicit periodic seam, with a quiet equal-power wrap crossfade; one-shots fade at both ends.
 if loop:
  k=min(2048,len(x)//20);a=(1-np.cos(np.linspace(0,np.pi,k)))*.5;shape=(k,1) if x.ndim==2 else (k,);a=a.reshape(shape);head=x[:k].copy()[::-1];x[-k:]=x[-k:]*(1-a)+head*a;x[-1]=x[0]
  x[0]=x[-1]=(x[0]+x[-1])*.5
 else:
  k=min(220,len(x)//4);fade=np.linspace(0,1,k);x[:k]*=fade.reshape((-1,1)) if x.ndim==2 else fade;x[-k:]*=fade[::-1].reshape((-1,1)) if x.ndim==2 else fade[::-1]
 pcm=np.round(np.clip(x,-.95,.95)*32767).astype('<i2');p=OUT/(name+'.wav');OUT.mkdir(parents=True,exist_ok=True)
 with wave.open(str(p),'wb') as w:w.setnchannels(1 if x.ndim==1 else 2);w.setsampwidth(2);w.setframerate(RATE);w.writeframes(pcm.tobytes())
 CATALOG.append({'id':'greyhaven.audio.'+name,'path':p.relative_to(ROOT).as_posix(),'provenance':'Original authored '+category+'; reproducible PCM synthesis in tools/content/build_audio.py. No sampled recordings.'})
 return {'name':name,'category':category,'loop':loop,'duration':len(x)/RATE,'channels':1 if x.ndim==1 else 2,'peak':float(np.max(np.abs(x))),'rms':float(np.sqrt(np.mean(x*x))),'seam':float(np.max(np.abs(x[0]-x[-1]))),'sha256':hashlib.sha256(p.read_bytes()).hexdigest()}
def foley(name,seed,duration=.5,kind='wood'):
 t=np.arange(int(RATE*duration))/RATE;n=filtered(len(t),seed,80,8000);low=filtered(len(t),seed+400,45,800)
 if kind=='step':x=(low*.45+n*.09)*env(t,.002,.035)+np.sin(2*np.pi*(82*t-20*t*t))*env(t,.002,.028)*.2
 elif kind=='metal':x=sum(np.sin(2*np.pi*f*t)*np.exp(-t/(.08+.025*i)) for i,f in enumerate([733,1207,1981,3143]))*.07+n*env(t,.001,.02)*.15
 elif kind=='cloth':x=n*env(t,.02,.10)*.20+low*env(t,.01,.06)*.08
 elif kind=='liquid':x=sum(np.sin(2*np.pi*(f*t-180*t*t))*env(np.maximum(0,t-d),.003,.045)*(t>=d) for f,d in [(720,0),(940,.06),(1180,.13)])*.10+n*env(t,.01,.05)*.025
 elif kind=='swing':x=n*np.sin(np.pi*np.minimum(t/duration,1))**3*.32+low*env(t,.02,.08)*.1
 elif kind=='impact':x=low*env(t,.001,.06)*.5+n*env(t,.001,.02)*.2+np.sin(2*np.pi*74*t)*env(t,.001,.08)*.22
 elif kind=='death':x=low*env(t,.002,.25)*.35+sum(np.sin(2*np.pi*f*t)*np.exp(-t/.3) for f in [61,83,109])*.06
 elif kind=='wood':x=low*env(t,.002,.06)*.25+sum(np.sin(2*np.pi*f*t)*np.exp(-t/.07) for f in [186,391,631])*.08+n*env(t,.002,.025)*.1
 else:x=sum(np.sin(2*np.pi*f*t)*env(t,.008,.17) for f in [440,660,880])*.08+n*env(t,.03,.12)*.03
 return x

def music(zone):
 # Original D Dorian/aeolian motifs, sixteenth-bar structure, 80 bpm, 16 bars = 48s.
 dur=48.;x=np.zeros((int(RATE*dur),2));beat=.75
 motifs={'town':[62,65,69,72,69,67,65,64,62,67,69,74,72,69,65,64], 'coast':[62,69,67,65,64,62,57,60,62,65,67,69,67,64,62,57], 'cellar':[50,57,53,60,57,52,53,50,48,55,52,58,55,50,52,48]}
 def note(midi,start,length,amp,pan,voice):
  f=440*2**((midi-69)/12);t=np.arange(int(RATE*length))/RATE
  if voice=='plucked':v=sum(np.sin(2*np.pi*f*h*t)*np.exp(-t*(1.8+h*.7)) / h**1.5 for h in range(1,8))*env(t,.004,1.2)
  elif voice=='flute':v=(np.sin(2*np.pi*f*t+.012*np.sin(2*np.pi*4*t))+np.sin(2*np.pi*f*2*t)*.13)*np.sin(np.pi*np.minimum(t/length,1))**.7
  else:v=sum(np.sin(2*np.pi*f*h*t)/h**2 for h in [1,2,3])*np.sin(np.pi*t/length)**2
  v=v*amp;idx=(int(start*RATE)+np.arange(len(v)))%len(x);x[idx,0]+=v*np.sqrt(1-pan);x[idx,1]+=v*np.sqrt(pan)
 for bar in range(16):
  root=[50,46,53,48][bar%4] if zone!='cellar' else [38,41,36,43][bar%4]
  for n in [0,7,12]:note(root+n,bar*3,3.8,.025,.45,'pad')
  for b in range(4):note(root+12+[0,7,12,7][b],bar*3+b*beat,1.5,.045,.25 if b%2==0 else .75,'plucked')
  note(motifs[zone][bar],bar*3+.75,1.7,.055 if zone!='cellar' else .035,.6,'flute')
  if bar%2:note(motifs[zone][(bar+1)%16],bar*3+2.25,.7,.025,.55,'plucked')
 # Circular early reflections keep the soundtrack dry enough under interaction sounds.
 return x+np.roll(x,int(.17*RATE),axis=0)*.12+np.roll(x,int(.31*RATE),axis=0)*.07

def build():
 global CATALOG;CATALOG=[];rows=[]
 kinds={'step':'step','step-stone':'step','step-wood':'wood','step-sand':'cloth','pickup':'cloth','drop':'wood','open':'wood','target':'metal','cancel':'cloth','swing':'swing','impact':'impact','cast':'magic','death':'death','heal':'magic','door':'wood','gate':'metal','coins':'metal','bandage':'cloth','drink':'liquid','quest':'magic','ember':'swing','venom':'liquid','storm':'impact','ward':'metal','bind':'cloth','portal':'magic','hound':'death','raider':'impact','slinger':'swing','hexer':'magic','brineguard':'metal','chest':'wood'}
 for i,(name,kind) in enumerate(kinds.items()):
  x=foley(name,8100+i,.65 if kind in ['magic','death'] else .4,kind)
  if name=='step-stone':x=x*.7+foley(name,901,.4,'metal')*.12
  if name=='step-sand':x=x*.65
  if name=='storm':x=x+np.roll(x,3900)*.45+np.roll(x,9100)*.3
  rows.append(save(name,x))
 for i,name in enumerate(['ambient-wind','ambient-cellar','ambient-town','ambient-coast']):
  count=RATE*16;t=np.arange(count)/RATE;noise=filtered(count,1900+i,80 if i!=1 else 45,1800 if i!=1 else 700)
  x=noise*(.05+.035*np.sin(2*np.pi*t/8)**2);stereo=np.column_stack([x,np.roll(x,1300)])
  if i==3:stereo*=1+np.sin(2*np.pi*t/4)[:,None]*.55
  if i==1:
   for offset in [1.2,4.8,8.7,13.1]:
    drop=foley('drip',int(offset*100),.4,'liquid')*.15;start=int(offset*RATE);stereo[start:start+len(drop),0]+=drop;stereo[start:start+len(drop),1]+=drop*.7
  rows.append(save(name,stereo,True,'ambience'))
 for name in ['town','coast','cellar']:rows.append(save('music-'+name,music(name),True,'music'))
 # Remove replaced/orphaned cues so a rebuild has a closed production inventory.
 keep={r['name']+'.wav' for r in rows}
 for p in OUT.glob('*.wav'):
  if p.name not in keep:p.unlink()
 (ROOT/'content/original/audio.json').write_text(json.dumps(CATALOG,indent=2)+'\n')
 (ROOT/'art/source/phase8/audio-production.json').write_text(json.dumps({'schema':1,'sampleRate':RATE,'format':'PCM signed 16 bit','composer':'Original Greyhaven procedural score and sound design','music':'Three original 48-second scores at 80 BPM, D Dorian/aeolian. Modal plucked strings, breath-like lead and sustained low strings.','sources':'Seeded filtered noise, modal resonators and additive instruments only. No external samples or soundfonts.','cues':rows},indent=2)+'\n')
 print(len(rows),'production PCM cues generated')
 return CATALOG
if __name__=='__main__':build()

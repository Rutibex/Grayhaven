#!/usr/bin/env python3
"""Live TCP quest/inventory/training/save integration and native gump routes."""
import argparse,json,os,shutil,socket,subprocess,tempfile,time,collections
from pathlib import Path
ap=argparse.ArgumentParser();ap.add_argument('--root',type=Path,default=Path(__file__).resolve().parents[1]);ap.add_argument('--runtime',type=Path,required=True);ap.add_argument('--dotnet',required=True);ap.add_argument('--render',action='store_true');a=ap.parse_args();root=a.root.resolve();runtime=a.runtime.resolve();out=root/'docs/phase7';out.mkdir(exist_ok=True);checks=[];processes=[]
env={**os.environ,'DOTNET_ROOT':str(Path(a.dotnet).parent),'SDL_VIDEODRIVER':'offscreen','SDL_AUDIODRIVER':'dummy','FNA3D_FORCE_DRIVER':'OpenGL','LIBGL_ALWAYS_SOFTWARE':'1'}
def check(name,ok):assert ok,name;checks.append(name);print('PASS',name,flush=True)
with tempfile.TemporaryDirectory(prefix='greyhaven-adventure-') as temp:
 home=Path(temp)/'game';home.mkdir()
 for name in ['art','content']:shutil.copytree(root/name,home/name,copy_function=os.link)
 shutil.copytree(runtime,home/'Runtime',copy_function=os.link)
 saves=Path(temp)/'saves'
 def host(load=None):
  ready=Path(temp)/('ready-'+str(time.time_ns())+'.json');log=ready.with_suffix('.log');stream=log.open('w');cmd=[a.dotnet,str(runtime/'Greyhaven.Server.dll'),'--content-root',str(home),'--save-root',str(saves),'--campaign','slot1','--port','0','--ready-file',str(ready)]+(['--load',load] if load else ['--start-at','109,170'])
  proc=subprocess.Popen(cmd,env=env,stdout=stream,stderr=subprocess.STDOUT);processes.append((proc,stream))
  for _ in range(1200):
   if ready.exists():break
   if proc.poll()!=None:raise AssertionError(log.read_text())
   time.sleep(.02)
  info=json.loads(ready.read_text());sock=socket.create_connection(('127.0.0.1',info['port']),timeout=15);wire=sock.makefile('rwb',buffering=0)
  def call(**cmd):wire.write((json.dumps(cmd)+'\n').encode());return json.loads(wire.readline())
  r=call(type='hello',fingerprint=info['fingerprint'],mapHash=info['mapHash'],protocol=1);assert r['ok'];return proc,sock,wire,call,r['state']
 def q(s,id):return next(q for q in s['adventure']['quests'] if q['id']==id)
 try:
  proc,sock,wire,call,state=host();check('live host exposes four original quests and eleven skills',len(state['adventure']['quests'])==4 and len(state['adventure']['skills'])==11)
  check('live host rejects distant quest acceptance',not call(type='quest-accept',targetId='quest.beacon')['ok'])
  r=call(type='quest-accept',targetId='quest.forge');check('explicit nearby acceptance activates forge quest',r['ok'] and q(r['state'],'quest.forge')['status']=='active')
  crystal=next(i for i in r['state']['items'] if i['definition'].endswith('tide-crystal') and i['where']=='bag');total=crystal['amount']
  check('unmarked carried crystals cannot complete supply quest',not call(type='quest-claim',targetId='quest.forge')['ok'])
  r=call(type='quest-mark',targetId='quest.forge',itemId=crystal['id']);check('live quest mark makes required supplies ready',r['ok'] and q(r['state'],'quest.forge')['status']=='ready')
  check('marked supplies cannot become thrown ammunition',not call(type='throw',itemId=crystal['id'],targetId='encounter.woods.hound1')['ok'])
  r=call(type='transfer',itemId=crystal['id'],destination='bag',x=50,y=280,amount=1);check('live split conserves quest identity',r['ok'] and all(i['questId']=='quest.forge' for i in r['state']['items'] if i['definition']==crystal['definition'] and i['where']=='bag'))
  r=call(type='quest-claim',targetId='quest.forge');check('live turn-in consumes exactly three supplies',r['ok'] and q(r['state'],'quest.forge')['status']=='claimed' and sum(i['amount'] for i in r['state']['items'] if i['definition']==crystal['definition'] and i['where']=='bag')==total-3)
  check('live host refuses replayed reward',not call(type='quest-claim',targetId='quest.forge')['ok'])
  r=call(type='train',targetId='npc.orrin',itemId='swords',amount=10);check('nearby teacher grants a paid fractional skill increase',r['ok'] and next(k for k in r['state']['adventure']['skills'] if k['id']=='swords')['value']==310)
  check('gain lock command is authoritative',call(type='skill-lock',itemId='swords',destination='locked')['ok'] and not call(type='train',targetId='npc.orrin',itemId='swords',amount=10)['ok'])
  # Walk real authoritative tiles from the forge to the inn; no host warp command exists.
  m=json.load(open(home/'content/original/map.json'));defs=json.load(open(home/'content/original/definitions.json'));td={d['id']:d for d in defs['terrain']};sd={d['id']:d for d in defs['statics']};blocked=set()
  for o in m['statics']:
   d=sd[o['definition']];z=m['cells'][o['y']*m['width']+o['x']]['z']
   if d['impassable'] and o['z']+d['height']>z and o['z']<z+16:blocked.add((o['x'],o['y']))
  def valid(t):x,y=t;return 0<=x<m['width'] and 0<=y<m['height'] and t not in blocked and not td[m['cells'][y*m['width']+x]['terrain']]['impassable']
  def walk(target):
   s=call(type='ping')['state'];start=(s['x'],s['y']);queue=collections.deque([start]);prev={start:None};end=None
   while queue:
    at=queue.popleft()
    if max(abs(at[0]-target[0]),abs(at[1]-target[1]))<=1:end=at;break
    for dx,dy in [(1,0),(-1,0),(0,1),(0,-1)]:
     t=(at[0]+dx,at[1]+dy)
     if valid(t) and t not in prev and abs(m['cells'][t[1]*m['width']+t[0]]['z']-m['cells'][at[1]*m['width']+at[0]]['z'])<=2:prev[t]=at;queue.append(t)
   assert end;route=[]
   while prev[end]!=None:route.append(end);end=prev[end]
   for t in reversed(route):
    for o in m['objects']:
     if o['kind']=='door' and (o['x'],o['y'])==t:
      st=call(type='ping')['state']
      if not next(v for v in st['world']['objects'] if v['id']==o['id'])['active']:assert call(type='interact',targetId=o['id'])['ok']
    time.sleep(.205);r=call(type='step',dx=t[0]-s['x'],dy=t[1]-s['y']);assert r['ok'],r['message'];s=r['state']
   return s
  state=walk((70,170));check('tile-authoritative town route opens ordinary forge and inn doors',max(abs(state['x']-70),abs(state['y']-170))<=1)
  r=call(type='quest-accept',targetId='quest.harbour');check('delivery acceptance issues a unique physical letter',r['ok'] and len([i for i in r['state']['items'] if i['id']=='quest.item.harbour-letter'])==1)
  state=walk((88,225));r=call(type='interact',targetId='npc.cora');check('walking physical letters to Cora completes delivery objectives',r['ok'] and q(r['state'],'quest.harbour')['status']=='ready')
  r=call(type='quest-claim',targetId='quest.harbour');check('recipient consumes letter and awards original delivery reward',r['ok'] and not any(i['id']=='quest.item.harbour-letter' for i in r['state']['items']))
  assert call(type='save',destination='quick')['ok'];expected=call(type='pause')['state'];assert call(type='quit')['ok'];wire.close();sock.close();proc.wait(timeout=10)
  proc,sock,wire,call,state=host('quick');check('fresh host restores claimed quests, skill locks and NPC records',state['paused'] and q(state,'quest.harbour')['status']=='claimed' and q(state,'quest.forge')['status']=='claimed' and next(k for k in state['adventure']['skills'] if k['id']=='swords')['lock']=='locked' and len(state['adventure']['npcs'])==15)
  assert call(type='quit')['ok'];wire.close();sock.close();proc.wait(timeout=10)
  if a.render:
   for scene in ['quests','skills']:
    capture=out/('native-'+scene+'.png');r=subprocess.run([a.dotnet,str(runtime/'Greyhaven.Launcher.dll'),'--content-root',str(home),'--save-root',str(Path(temp)/scene),'--campaign','slot1','--load','new','--start-at','109,170','--adventure-scene',scene,'--capture',str(capture)],env=env,capture_output=True,text=True,timeout=100);assert r.returncode==0,r.stdout+r.stderr;check('native '+scene+' gump renders against owned live host',capture.stat().st_size>10000)
   capture=out/'native-adventure-routes.png';r=subprocess.run([a.dotnet,str(runtime/'Greyhaven.Launcher.dll'),'--content-root',str(home),'--save-root',str(Path(temp)/'smoke'),'--campaign','slot1','--load','new','--start-at','109,170','--adventure-smoke','--capture',str(capture)],env=env,capture_output=True,text=True,timeout=100)
   logs='\n'.join(p.read_text() for p in (home/'UserData/Logs').glob('*.log'));(out/'native-adventure.log').write_text(logs);assert r.returncode==0,r.stdout+r.stderr+logs;check('native accept-mark-claim and skill lock/training click handlers pass','Native adventure routes:' in logs)
 finally:
  for proc,stream in processes:
   if proc.poll()==None:proc.kill();proc.wait()
   stream.close()
(out/'adventure-integration.json').write_text(json.dumps({'checks':len(checks),'passed':checks,'nativeRender':a.render,'platform':'Linux FNA offscreen; native handlers invoked directly, not OS mouse automation'},indent=2)+'\n')

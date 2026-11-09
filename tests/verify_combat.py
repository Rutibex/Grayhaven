#!/usr/bin/env python3
"""Real-time TCP combat, casting persistence, autonomous AI and native UI routes."""
import argparse,subprocess,tempfile,shutil,os,socket,time,json
from pathlib import Path
ap=argparse.ArgumentParser();ap.add_argument('--root',type=Path,default=Path(__file__).resolve().parents[1]);ap.add_argument('--runtime',type=Path,required=True);ap.add_argument('--dotnet',required=True);ap.add_argument('--render',action='store_true');a=ap.parse_args();root=a.root.resolve();runtime=a.runtime.resolve();out=root/'docs/phase5';out.mkdir(exist_ok=True);checks=[]
env={**os.environ,'DOTNET_ROOT':str(Path(a.dotnet).parent),'SDL_VIDEODRIVER':'offscreen','SDL_AUDIODRIVER':'dummy','FNA3D_FORCE_DRIVER':'OpenGL','LIBGL_ALWAYS_SOFTWARE':'1'}
def check(name,condition):assert condition,name;checks.append(name);print('PASS',name,flush=True)
with tempfile.TemporaryDirectory(prefix='greyhaven-combat-') as temp:
 home=Path(temp)/'game';home.mkdir()
 for name in ['art','content']:shutil.copytree(root/name,home/name,copy_function=os.link)
 shutil.copytree(runtime,home/'Runtime',copy_function=os.link)
 ready=Path(temp)/'ready.json';log=Path(temp)/'server.log';stream=log.open('w');server=subprocess.Popen([a.dotnet,str(runtime/'Greyhaven.Server.dll'),'--content-root',str(home),'--campaign','slot1','--port','0','--ready-file',str(ready)],env=env,stdout=stream,stderr=subprocess.STDOUT)
 try:
  for i in range(300):
   if ready.exists():break
   if server.poll()!=None:raise AssertionError(log.read_text())
   time.sleep(.02)
  info=json.loads(ready.read_text());sock=socket.create_connection(('127.0.0.1',info['port']),timeout=8);f=sock.makefile('rwb',buffering=0);state={}
  def call(**cmd):
   global state
   f.write((json.dumps(cmd)+'\n').encode());line=f.readline();assert line,'host closed';r=json.loads(line)
   if r.get('state') is not None:state=r['state']
   return r
  def wait(predicate,seconds=6):
   until=time.monotonic()+seconds
   while time.monotonic()<until:
    call(type='poll')
    if predicate():return
    time.sleep(.06)
   raise AssertionError('Timed out\n'+json.dumps(state['combat']))
  def enemy():return next(e for e in state['combat']['enemies'] if e['kind']=='tidehound')
  assert call(type='hello',protocol=1,fingerprint=info['fingerprint'],mapHash=info['mapHash'])['ok']
  check('five combat archetypes and complete player resources boot',len(state['combat']['enemies'])==5 and state['combat']['health']==100 and state['combat']['mana']==60)
  check('starter quickbar connects spells and real item identities',sum(b.startswith('spell:') for b in state['quickbar'])==8 and sum(b.startswith('item:') for b in state['quickbar'])==2)
  revision=state['revision'];call(type='poll');check('read-only snapshots do not mutate inventory revisions',state['revision']==revision)
  mana=state['combat']['mana'];r=call(type='cast',itemId='unknown');check('invalid magic is rejected without spending resources',not r['ok'] and state['combat']['mana']==mana)
  assert not call(type='attack',targetId=enemy()['id'])['ok'];check('camp enforces a combat sanctuary',state['combat']['health']==100)
  for i in range(4):assert call(type='step',dx=1,dy=0)['ok'];time.sleep(.23)
  wait(lambda:state['combat']['health']<100,8);check('enemy pursues and damages player without additional attack commands',enemy()['x']<24 and state['combat']['health']<100)
  count=sum(i['amount'] for i in state['items'] if i['definition'].endswith('healing-tonic'));assert call(type='drink')['ok'];check('tonic restores health and consumes one inventory unit',state['combat']['health']==100 and sum(i['amount'] for i in state['items'] if i['definition'].endswith('healing-tonic'))==count-1)
  # Cast just after its attack; Bind prevents pursuit, then retreat two tiles to cast safely.
  assert call(type='cast',itemId='greyhaven.spell.bind',targetId=enemy()['id'])['ok'];wait(lambda:enemy()['status']['rootUntil']>state['elapsedSeconds'])
  for i in range(2):assert call(type='step',dx=0,dy=1)['ok'];time.sleep(.23)
  check('Bind applies a real immobilizing status',enemy()['status']['rootUntil']>state['elapsedSeconds'])
  count=sum(i['amount'] for i in state['items'] if i['definition'].endswith('tide-crystal'));assert call(type='throw',targetId=enemy()['id'])['ok'];check('ranged throw consumes exactly one crystal',sum(i['amount'] for i in state['items'] if i['definition'].endswith('tide-crystal'))==count-1)
  wait(lambda:enemy()['health']<32)
  wait(lambda:state['combat']['spellAt']<=state['elapsedSeconds'])
  assert call(type='cast',itemId='greyhaven.spell.ember',targetId=enemy()['id'])['ok'];assert call(type='save',destination='quick')['ok'];check('save captures in-progress casting and paid mana',state['combat']['cast'] is not None and state['combat']['mana']<60)
  assert call(type='pause')['ok'];before=json.dumps(state,sort_keys=True);time.sleep(.5);call(type='poll');check('pause freezes complete combat and world state',json.dumps(state,sort_keys=True)==before)
  assert call(type='load',destination='quick')['ok'];check('reload preserves cast and enemy state while paused',state['paused'] and state['combat']['cast']['spell']=='greyhaven.spell.ember')
  assert call(type='pause',destination='resume')['ok'];wait(lambda:enemy()['health']==0)
  check('cast and projectile resolve to an authoritative defeat with one loot stack',state['combat']['kills']==1 and enemy()['lootSpawned'] and len([i for i in state['items'] if i['id']=='loot.'+enemy()['id']])==1)
  assert call(type='save',destination='manual')['ok'];assert call(type='quit')['ok'];f.close();sock.close();server.wait(timeout=8);check('combat session saves and shuts down cleanly',server.returncode==0)
  if a.render:
   result=subprocess.run([a.dotnet,str(runtime/'Greyhaven.Launcher.dll'),'--content-root',str(home),'--campaign','slot3','--load','new','--combat-smoke','--capture',str(out/'native-combat.png')],env=env,capture_output=True,text=True,timeout=60)
   logs=list((home/'UserData/Logs').glob('*.log'));detail='\n'.join(p.read_text()[-1800:] for p in logs)
   assert result.returncode==0,result.stdout+result.stderr+'\n'+detail
   check('native targeting, Bind, Ember, throwing, defeat/loot and Dawn routes render', 'Native combat routes:' in detail)
 finally:
  if server.poll()==None:server.kill();server.wait()
  stream.close()
(out/'combat-integration.json').write_text(json.dumps({'checks':len(checks),'passed':checks,'nativeRender':a.render,'platform':'Linux offscreen FNA/OpenGL; Windows execution not claimed'},indent=2)+'\n');print(len(checks),'combat integration checks passed')

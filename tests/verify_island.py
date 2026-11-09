#!/usr/bin/env python3
"""Real host island interactions, separate playtest saves and native workshop/game renders."""
import argparse,json,os,shutil,socket,subprocess,tempfile,time
from pathlib import Path
ap=argparse.ArgumentParser();ap.add_argument('--root',type=Path,default=Path(__file__).resolve().parents[1]);ap.add_argument('--runtime',type=Path,required=True);ap.add_argument('--dotnet',required=True);ap.add_argument('--render',action='store_true');a=ap.parse_args();root=a.root.resolve();runtime=a.runtime.resolve();out=root/'docs/phase6';out.mkdir(exist_ok=True);checks=[];processes=[]
env={**os.environ,'DOTNET_ROOT':str(Path(a.dotnet).parent),'SDL_VIDEODRIVER':'offscreen','SDL_AUDIODRIVER':'dummy','FNA3D_FORCE_DRIVER':'OpenGL','LIBGL_ALWAYS_SOFTWARE':'1'}
def check(name,ok):assert ok,name;checks.append(name);print('PASS',name,flush=True)
with tempfile.TemporaryDirectory(prefix='greyhaven-island-') as temp:
 home=Path(temp)/'game';home.mkdir()
 for name in ['art','content']:shutil.copytree(root/name,home/name,copy_function=os.link)
 shutil.copytree(runtime,home/'Runtime',copy_function=os.link)
 savehome=Path(temp)/'playtest'
 def host(load=None,start='71,176'):
  ready=Path(temp)/('ready-'+str(time.time_ns())+'.json');log=Path(temp)/('server-'+str(time.time_ns())+'.log');stream=log.open('w');cmd=[a.dotnet,str(runtime/'Greyhaven.Server.dll'),'--content-root',str(home),'--save-root',str(savehome),'--campaign','slot1','--port','0','--ready-file',str(ready)]
  cmd+=['--load',load] if load else ['--start-at',start]
  proc=subprocess.Popen(cmd,env=env,stdout=stream,stderr=subprocess.STDOUT);processes.append((proc,stream))
  for n in range(1000):
   if ready.exists():break
   if proc.poll()!=None:raise AssertionError(log.read_text())
   time.sleep(.02)
  info=json.loads(ready.read_text());sock=socket.create_connection(('127.0.0.1',info['port']),timeout=15);wire=sock.makefile('rwb',buffering=0)
  def call(**cmd):wire.write((json.dumps(cmd)+'\n').encode());reply=json.loads(wire.readline());assert reply is not None;return reply
  reply=call(type='hello',fingerprint=info['fingerprint'],mapHash=info['mapHash'],protocol=1);assert reply['ok'];return proc,sock,wire,call,reply['state']
 try:
  proc,sock,wire,call,state=host()
  check('island host initializes 13 authored enemies and world object state',len(state['combat']['enemies'])==13 and state['world']['version']==1 and len(state['world']['objects'])==84)
  check('selected playtest tile is authoritative and main saves stay untouched',(state['x'],state['y'])==(71,176) and (savehome/'UserData/Campaigns/slot1/manual.json').exists() and not (home/'UserData/Campaigns').exists())
  r=call(type='step',dx=0,dy=-1);check('closed town door blocks a live tile step',not r['ok'] and r['state']['y']==176)
  r=call(type='interact',targetId='town.inn.door');check('live interaction opens the placed door',r['ok'] and next(o for o in r['state']['world']['objects'] if o['id']=='town.inn.door')['active'])
  r=call(type='step',dx=0,dy=-1);check('opened doorway accepts a real tile step',r['ok'] and r['state']['y']==175)
  check('occupied door refuses to close',not call(type='interact',targetId='town.inn.door')['ok'])
  r=call(type='interact',targetId='cellar.lever');check('out of reach lever cannot remotely open a dungeon gate',not r['ok'] and not next(o for o in r['state']['world']['objects'] if o['id']=='cellar.gate')['active'])
  assert call(type='save',destination='quick')['ok'];assert call(type='quit')['ok'];wire.close();sock.close();proc.wait(timeout=10)
  proc,sock,wire,call,state=host(load='quick');check('fresh host restores door state and selected tile',state['paused'] and state['y']==175 and next(o for o in state['world']['objects'] if o['id']=='town.inn.door')['active'])
  check('loaded world containers do not duplicate their identities',len({i['id'] for i in state['items']})==len(state['items']) and len([i for i in state['items'] if i['id']=='world.item.cellar.key'])==1)
  assert call(type='quit')['ok'];wire.close();sock.close();proc.wait(timeout=10)
  # A second isolated campaign starts at the authored stairs, never rewriting island spawn.
  savehome=Path(temp)/'stairs-test';proc,sock,wire,call,state=host(start='89,46')
  r=call(type='interact',targetId='stairs.beacon');check('live stair interaction reaches upper dungeon floor',r['ok'] and (r['state']['x'],r['state']['y'])==(200,28))
  r=call(type='interact',targetId='stairs.upper');check('paired stair returns to the authored beacon',r['ok'] and (r['state']['x'],r['state']['y'])==(89,46))
  assert call(type='quit')['ok'];wire.close();sock.close();proc.wait(timeout=10)
  if a.render:
   def launch(name,*args):
    capture=out/(name+'.png');result=subprocess.run([a.dotnet,str(runtime/'Greyhaven.Launcher.dll'),'--content-root',str(home),'--save-root',str(Path(temp)/name),'--campaign','slot1','--load','new','--world-scene','clean','--capture',str(capture),*args],env=env,capture_output=True,text=True,timeout=90)
    logs='\n'.join(p.read_text()[-2000:] for p in (home/'UserData/Logs').glob('*.log'));assert result.returncode==0,result.stdout+result.stderr+logs;check('native '+name+' renders through owned live host',capture.stat().st_size>10000)
   launch('island-town');launch('island-interior','--start-at','70,170');launch('dungeon-upper','--start-at','200,28');launch('dungeon-lower','--start-at','200,108')
   capture=out/'island-atlas.png';result=subprocess.run([a.dotnet,str(runtime/'Greyhaven.Launcher.dll'),'--content-root',str(home),'--save-root',str(Path(temp)/'atlas'),'--campaign','slot1','--load','new','--world-scene','atlas','--capture',str(capture)],env=env,capture_output=True,text=True,timeout=90);assert result.returncode==0,result.stdout+result.stderr;check('native island atlas renders authored routes and dungeon floors',capture.exists())
   result=subprocess.run([a.dotnet,str(runtime/'Greyhaven.Editor.dll'),'--content-root',str(home),'--workshop-smoke','--export',str(Path(temp)/'workshop.json'),'--capture',str(out/'native-workshop.png')],env=env,capture_output=True,text=True,timeout=90);(out/'native-workshop.log').write_text(result.stdout+result.stderr);assert result.returncode==0,result.stdout+result.stderr;check('native workshop search favorite prefab properties validate export reload routes', 'Native workshop routes:' in result.stdout)
   proof=Path(temp)/'workshop.json.playtest.png'
   for n in range(600):
    if proof.exists():break
    time.sleep(.1)
   check('native selected-tile playtest button launches an isolated campaign',proof.exists())
   shutil.copyfile(proof,out/'native-selected-playtest.png')
 finally:
  for proc,stream in processes:
   if proc.poll()==None:proc.kill();proc.wait()
   stream.close()
(out/'island-integration.json').write_text(json.dumps({'checks':len(checks),'passed':checks,'nativeRender':a.render,'platform':'Linux native offscreen FNA/OpenGL; Windows execution not claimed'},indent=2)+'\n');print(len(checks),'live island checks passed')

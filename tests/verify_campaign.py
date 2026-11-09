#!/usr/bin/env python3
"""Phase 3 black-box host lifecycle; all campaigns live in a temporary root."""
import argparse,json,os,shutil,socket,subprocess,tempfile,time,hashlib
from pathlib import Path
ap=argparse.ArgumentParser();ap.add_argument('--root',type=Path,default=Path(__file__).resolve().parents[1]);ap.add_argument('--runtime',type=Path,required=True);ap.add_argument('--dotnet',required=True);ap.add_argument('--render',action='store_true');a=ap.parse_args();root=a.root.resolve();runtime=a.runtime.resolve();results=[];processes=[]
env={**os.environ,'DOTNET_ROOT':str(Path(a.dotnet).parent),'SDL_VIDEODRIVER':'offscreen','SDL_AUDIODRIVER':'dummy','FNA3D_FORCE_DRIVER':'OpenGL','LIBGL_ALWAYS_SOFTWARE':'1'}
def check(name,body):body();results.append(name);print('PASS',name,flush=True)
def require(value):assert value
with tempfile.TemporaryDirectory(prefix='greyhaven-campaign-test-') as temp:
 d=Path(temp);home=d/'game';home.mkdir()
 for name in ['content','art']:shutil.copytree(root/name,home/name,copy_function=os.link)
 shutil.copytree(runtime,home/'Runtime',copy_function=os.link)
 saves=home/'UserData/Campaigns/slot1';capture=root/'docs/phase3';capture.mkdir(exist_ok=True)
 def boot(load=None,slot='slot1',extra=(),fail=False):
  ready=d/('ready-'+str(time.time_ns())+'.json');log=d/('host-'+str(time.time_ns())+'.log');stream=log.open('w');cmd=[a.dotnet,str(runtime/'Greyhaven.Server.dll'),'--content-root',str(home),'--campaign',slot,'--port','0','--autosave-seconds','1','--ready-file',str(ready),*extra]
  if load:cmd+=['--load',load]
  p=subprocess.Popen(cmd,stdout=stream,stderr=subprocess.STDOUT,env=env);processes.append((p,stream))
  for _ in range(240):
   if ready.exists():
    try:return p,json.loads(ready.read_text())
    except json.JSONDecodeError:pass
   if p.poll() is not None:
    stream.flush()
    if fail:assert p.returncode!=0;return p,log.read_text()
    raise AssertionError(log.read_text())
   time.sleep(.025)
  p.kill();raise AssertionError('Host readiness timeout')
 def connect(info):
  s=socket.create_connection(('127.0.0.1',info['port']),timeout=8);f=s.makefile('rwb',buffering=0)
  r=call(f,type='hello',protocol=1,fingerprint=info['fingerprint'],mapHash=info['mapHash']);assert r['ok'];return s,f,r['state']
 def call(f,**command):f.write((json.dumps(command)+'\n').encode());line=f.readline();assert line,'Host closed unexpectedly';return json.loads(line)
 def close(s,f):f.close();s.close()
 def read(kind='manual'):return json.loads(json.loads((saves/(kind+'.json')).read_text())['payload'])
 def rewrite(path,change):
  envelope=json.loads(path.read_text());payload=json.loads(envelope['payload']);change(payload);envelope['payload']=json.dumps(payload,separators=(',',':'));envelope['sha256']=hashlib.sha256(envelope['payload'].encode()).hexdigest().upper();path.write_text(json.dumps(envelope))
 try:
  p,info=boot();s,f,state=connect(info)
  check('new campaign creates a durable initial manual save',lambda:require(read()['state']['x']==16))
  potion=next(i['id'] for i in state['items'] if i['definition'].endswith('healing-tonic'));wizard=next(i['id'] for i in state['items'] if i['definition'].endswith('wizard-hat'))
  assert call(f,type='step',dx=1,dy=0)['ok'];assert call(f,type='transfer',itemId=potion,destination='ground',x=17,y=20)['ok'];assert call(f,type='transfer',itemId=wizard,destination='equipped')['ok']
  assert call(f,type='save',destination='quick')['ok'];expected=read('quick')['state']
  check('quick save preserves tile, equipment and ground item identity',lambda:require(expected['x']==17 and next(i for i in expected['items'] if i['id']==potion)['where']=='ground' and next(i for i in expected['items'] if i['id']==wizard)['where']=='equipped'))
  time.sleep(1.2);paused=call(f,type='pause')['state'];time.sleep(1.2);again=call(f,type='ping')['state']
  check('host pause freezes authoritative elapsed time',lambda:require(paused['paused'] and again['elapsedSeconds']==paused['elapsedSeconds']))
  check('host pause blocks movement and item transfers',lambda:require(not call(f,type='step',dx=1)['ok'] and not call(f,type='transfer',itemId=potion,destination='bag')['ok']))
  check('running campaign autosaves without input commands',lambda:require(read('autosave')['state']['elapsedSeconds']>0))
  loaded=call(f,type='load',destination='quick');check('in-session reload restores saved world and stays paused',lambda:require(loaded['ok'] and loaded['type']=='loaded' and loaded['state']['items']==expected['items'] and loaded['state']['elapsedSeconds']==expected['elapsedSeconds'] and loaded['state']['paused']))
  check('invalid save name is rejected without changing world',lambda:require(not call(f,type='save',destination='../bad')['ok'] and call(f,type='ping')['state']['x']==17))
  second,log=boot(load='latest',fail=True);check('second host is refused while campaign is open',lambda:require('already open' in log))
  quit=call(f,type='quit');close(s,f);p.wait(timeout=8)
  check('save-and-quit acknowledges persistence and stops host cleanly',lambda:require(quit['ok'] and p.returncode==0 and read('exit')['state']['x']==17))
  p,info=boot('latest');s,f,state=connect(info)
  check('fresh host continues campaign with original item state',lambda:require(state['x']==17 and state['items']==expected['items'] and state['paused']))
  assert call(f,type='pause',destination='resume')['ok'];time.sleep(.25);assert call(f,type='step',dx=1,dy=0)['ok'];close(s,f);p.wait(timeout=8)
  check('client disconnect saves progress and retires owned local host',lambda:require(p.returncode==0 and read('exit')['state']['x']==18))
  p,info=boot('latest');s,f,state=connect(info);assert call(f,type='pause',destination='resume')['ok'];time.sleep(.25);assert call(f,type='step',dx=1,dy=0)['ok'];time.sleep(1.3);p.kill();p.wait();close(s,f)
  p,info=boot('autosave');s,f,state=connect(info)
  check('autosave recovers progress after abrupt host termination',lambda:require(state['x']==19))
  assert call(f,type='save',destination='manual')['ok'];assert call(f,type='save',destination='manual')['ok'];assert call(f,type='quit')['ok'];close(s,f);p.wait(timeout=8)
  (saves/'manual.json').write_text('partial write')
  p,info=boot('manual');s,f,state=connect(info)
  check('damaged primary recovers verified backup on fresh boot',lambda:require(state['x']==19));assert call(f,type='quit')['ok'];close(s,f);p.wait(timeout=8)
  rewrite(saves/'quick.json',lambda x:x.update(fingerprint='wrong'));rewrite(saves/'quick.json.bak',lambda x:x.update(fingerprint='wrong')) if (saves/'quick.json.bak').exists() else None
  failed,log=boot('quick',fail=True);check('incompatible content save fails explicitly',lambda:require('different content package' in log))
  (saves/'manual.json.bak').write_text('also damaged');failed,log=boot('manual',fail=True)
  check('unrecoverable save is retained and never silently reset',lambda:require('retained' in log and (saves/'manual.json').read_text()=='partial write'))
  failed,log=boot(fail=True);check('new campaign refuses to overwrite an occupied slot',lambda:require('already exists' in log))
  # Snapshot survives external map editing: rehydrate the exact saved map before host creation.
  modified=json.loads((home/'content/original/maps/reach.json').read_text()) if (home/'content/original/maps/reach.json').exists() else read('exit')['map']
  modified['cells'][19*32+15]['z']=2;mapfile=d/'modified.json';mapfile.write_text(json.dumps(modified))
  p,info=boot(slot='slot2',extra=['--map',str(mapfile)]);s,f,state=connect(info);assert call(f,type='quit')['ok'];close(s,f);p.wait(timeout=8)
  slot2=home/'UserData/Campaigns/slot2/exit.json';snapshot=json.loads(json.loads(slot2.read_text())['payload'])['map'];mapfile.write_text(json.dumps(snapshot));p,info=boot('exit',slot='slot2',extra=['--map',str(mapfile)]);s,f,state=connect(info)
  check('campaign embeds and reloads edited map elevation',lambda:require(snapshot['cells'][19*32+15]['z']==2));assert call(f,type='quit')['ok'];close(s,f);p.wait(timeout=8)
  if a.render:
   def run(role,*args):
    r=subprocess.run([a.dotnet,str(home/'Runtime'/f'Greyhaven.{role}.dll'),'--content-root',str(home),*map(str,args)],capture_output=True,text=True,env=env,timeout=45);assert r.returncode==0,r.stdout+r.stderr
   check('native campaign menu renders without starting a host',lambda:run('Client','--campaign-menu',d/'selection.json','--capture',capture/'campaign-menu.png'))
   check('launcher starts new campaign and shuts down owned host',lambda:run('Launcher','--campaign','slot3','--load','new','--capture',capture/'campaign-world.png'))
   check('launcher continues saved campaign and renders pause menu',lambda:run('Launcher','--campaign','slot3','--load','latest','--capture',capture/'campaign-pause.png'))
   check('launcher restores campaign snapshot for edited map',lambda:run('Launcher','--campaign','slot2','--load','latest','--capture',d/'edited-campaign.png'))
 finally:
  for p,stream in processes:
   if p.poll() is None:p.kill();p.wait()
   stream.close()
report={'checks':len(results),'passed':results,'nativeRender':a.render,'platform':os.uname().sysname,'scope':'Linux host and native FNA offscreen; Windows binaries cross-published, not executed here.'};(root/'docs/phase3/campaign-checks.json').write_text(json.dumps(report,indent=2)+'\n');print(len(results),'campaign lifecycle checks passed')

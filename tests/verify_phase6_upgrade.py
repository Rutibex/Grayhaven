#!/usr/bin/env python3
"""Create a save with the actual published Phase 6 host, then load it with Phase 7."""
import argparse,hashlib,json,os,socket,subprocess,tempfile,time,zipfile
from pathlib import Path
ap=argparse.ArgumentParser();ap.add_argument('--phase6',type=Path,required=True);ap.add_argument('--runtime',type=Path,required=True);ap.add_argument('--dotnet',required=True);a=ap.parse_args();root=Path(__file__).resolve().parents[1];checks=[]
def check(name,ok):assert ok,name;checks.append(name);print('PASS',name,flush=True)
with tempfile.TemporaryDirectory(prefix='greyhaven-p6-upgrade-') as tmp:
 d=Path(tmp)
 with zipfile.ZipFile(a.phase6) as z:
  for n in z.namelist():
   if n.startswith(('Greyhaven/art/','Greyhaven/content/','Greyhaven/Runtime/')):z.extract(n,d)
 old=d/'Greyhaven';savehome=d/'saves';env={**os.environ,'DOTNET_ROOT':str(Path(a.dotnet).parent)};config=d/'linux.runtimeconfig.json';config.write_text('{"runtimeOptions":{"tfm":"net10.0","framework":{"name":"Microsoft.NETCore.App","version":"10.0.13"}}}')
 def host(legacy=False):
  ready=d/('old.json' if legacy else 'new.json');log=ready.with_suffix('.log');stream=log.open('w');cmd=([a.dotnet,'exec','--runtimeconfig',str(config),str(old/'Runtime/Greyhaven.Server.dll')] if legacy else [a.dotnet,str(a.runtime.resolve()/'Greyhaven.Server.dll')])+['--content-root',str(old if legacy else root),'--save-root',str(savehome),'--campaign','slot1','--port','0','--ready-file',str(ready)]+(['--start-at','109,173'] if legacy else ['--load','quick'])
  proc=subprocess.Popen(cmd,env=env,stdout=stream,stderr=subprocess.STDOUT)
  for _ in range(1000):
   if ready.exists():break
   if proc.poll()!=None:raise AssertionError(log.read_text())
   time.sleep(.02)
  info=json.loads(ready.read_text());s=socket.create_connection(('127.0.0.1',info['port']),timeout=20);f=s.makefile('rwb',buffering=0)
  def call(**cmd):f.write((json.dumps(cmd)+'\n').encode());return json.loads(f.readline())
  r=call(type='hello',protocol=1,fingerprint=info['fingerprint'],mapHash=info['mapHash']);assert r['ok'];return proc,stream,s,f,call,r['state'],info
 p,log,s,f,call,state,info=host(True)
 try:
  check('actual Phase 6 fingerprint matches explicitly supported predecessor',info['fingerprint']=='4e9056c6a8bcb7c5b8766ca0cc0f516e03895173e635006d2607dec2b4b83079')
  assert call(type='interact',targetId='town.forge.door')['ok'];assert call(type='save',destination='quick')['ok'];expected=call(type='ping')['state'];assert call(type='quit')['ok']
 finally:f.close();s.close();p.wait(timeout=10);log.close()
 path=savehome/'UserData/Campaigns/slot1/quick.json';digest=hashlib.sha256(path.read_bytes()).hexdigest()
 p,log,s,f,call,state,info=host()
 try:
  check('old primary save bytes remain unchanged during upgrade',hashlib.sha256(path.read_bytes()).hexdigest()==digest)
  check('Phase 6 position, inventory identities and opened door survive',(state['x'],state['y'])==(expected['x'],expected['y']) and all(any(j['id']==i['id'] and j['amount']==i['amount'] and j['where']==i['where'] for j in state['items']) for i in expected['items']) and next(v for v in state['world']['objects'] if v['id']=='town.forge.door')['active'])
  check('town progression initializes exactly once and loads paused',state['paused'] and state['adventure']['version']==1 and len(state['adventure']['quests'])==4 and len(state['adventure']['skills'])==11 and len([i for i in state['items'] if i['id']=='adventure.bandages'])==1)
  assert call(type='save',destination='manual')['ok'];r=call(type='load',destination='manual');check('upgraded save reload does not duplicate starter bandages or residents',r['ok'] and len([i for i in r['state']['items'] if i['id']=='adventure.bandages'])==1 and len(r['state']['adventure']['npcs'])==15)
  assert call(type='quit')['ok']
 finally:f.close();s.close();p.wait(timeout=10);log.close()
(root/'docs/phase7/phase6-upgrade.json').write_text(json.dumps({'checks':len(checks),'passed':checks,'predecessor':'Actual Phase 6 archive host; original save generated during this test'},indent=2)+'\n')

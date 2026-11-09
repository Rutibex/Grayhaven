#!/usr/bin/env python3
"""Live inventory authority, Phase 3 upgrade and native interface routes in an isolated root."""
import argparse,os,shutil,tempfile,subprocess,socket,json,time,hashlib
from pathlib import Path
ap=argparse.ArgumentParser();ap.add_argument('--root',type=Path,default=Path(__file__).resolve().parents[1]);ap.add_argument('--runtime',type=Path,required=True);ap.add_argument('--dotnet',required=True);ap.add_argument('--render',action='store_true');a=ap.parse_args();root=a.root.resolve();runtime=a.runtime.resolve();checks=[];processes=[];out=root/'docs/phase4';out.mkdir(exist_ok=True)
env={**os.environ,'DOTNET_ROOT':str(Path(a.dotnet).parent),'SDL_VIDEODRIVER':'offscreen','SDL_AUDIODRIVER':'dummy','FNA3D_FORCE_DRIVER':'OpenGL','LIBGL_ALWAYS_SOFTWARE':'1'}
def check(name,ok):assert ok,name;checks.append(name);print('PASS',name,flush=True)
with tempfile.TemporaryDirectory(prefix='greyhaven-inventory-') as temp:
 home=Path(temp)/'game';home.mkdir()
 for name in ['content','art']:shutil.copytree(root/name,home/name,copy_function=os.link)
 shutil.copytree(runtime,home/'Runtime',copy_function=os.link)
 def host(slot='slot1',load=None):
  ready=Path(temp)/('ready-'+str(time.time_ns())+'.json');log=Path(temp)/('host-'+str(time.time_ns())+'.log');f=log.open('w');cmd=[a.dotnet,str(runtime/'Greyhaven.Server.dll'),'--content-root',str(home),'--campaign',slot,'--port','0','--ready-file',str(ready)]
  if load:cmd+=['--load',load]
  p=subprocess.Popen(cmd,env=env,stdout=f,stderr=subprocess.STDOUT);processes.append((p,f))
  for i in range(300):
   if ready.exists():break
   if p.poll()!=None:raise AssertionError(log.read_text())
   time.sleep(.02)
  info=json.loads(ready.read_text());sock=socket.create_connection(('127.0.0.1',info['port']),timeout=8);wire=sock.makefile('rwb',buffering=0)
  def call(**cmd):wire.write((json.dumps(cmd)+'\n').encode());return json.loads(wire.readline())
  welcome=call(type='hello',protocol=1,fingerprint=info['fingerprint'],mapHash=info['mapHash']);assert welcome['ok'];return p,sock,wire,call,welcome['state'],info
 try:
  p,s,f,call,state,info=host()
  def current():return call(type='ping')['state']
  def item(suffix):return next(i for i in current()['items'] if i['definition'].endswith(suffix))
  tonic=item('healing-tonic')['id'];satchel=item('field-satchel')['id'];marks=item('silver-marks')['id']
  check('live boot includes quantities and original container definitions',len(state['items'])==10 and next(i for i in state['items'] if i['id']==tonic)['amount']==8)
  r=call(type='transfer',itemId=tonic,destination='container',targetId=satchel,x=70,y=150);check('TCP drop into nested container',r['ok'] and next(i for i in r['state']['items'] if i['id']==tonic)['parentId']==satchel)
  r=call(type='transfer',itemId=tonic,destination='bag',amount=3,x=80,y=150);check('TCP split conserves all units',r['ok'] and sum(i['amount'] for i in r['state']['items'] if i['definition'].endswith('healing-tonic'))==8 and len(r['state']['items'])==11)
  split=next(i for i in r['state']['items'] if i['definition'].endswith('healing-tonic') and i['id']!=tonic)['id']
  assert call(type='bind',slot=2,itemId=split)['ok'];r=call(type='transfer',itemId=split,destination='stack',targetId=tonic);check('TCP merge rewrites binding without duplication',r['ok'] and r['state']['quickbar'][2]=='item:'+tonic and len(r['state']['items'])==10)
  def inventory(s):return {k:s[k] for k in ['items','quickbar','outfit','merchantStock','journal','revision']}
  before=current();r=call(type='transfer',itemId=satchel,destination='container',targetId=satchel);check('invalid container cycle leaves inventory state unchanged',not r['ok'] and inventory(r['state'])==inventory(before))
  revision=r['state']['revision'];r=call(type='trade',destination='buy',targetId='greyhaven.item.healing-tonic',amount=2,expectedRevision=revision);check('TCP merchant charges authoritative price',r['ok'] and sum(i['amount'] for i in r['state']['items'] if i['definition'].endswith('silver-marks'))==109)
  duplicate=call(type='trade',destination='buy',targetId='greyhaven.item.healing-tonic',amount=2,expectedRevision=revision);check('replayed trade cannot charge a second time',not duplicate['ok'] and inventory(duplicate['state'])==inventory(r['state']))
  assert call(type='note',text='Remember the nested medicine.')['ok'];assert call(type='bind',slot=3,targetId='greyhaven.spell.dawn')['ok']
  assert call(type='outfit')['ok'];assert call(type='save',destination='quick')['ok'];expected=call(type='ping')['state'];assert call(type='quit')['ok'];f.close();s.close();p.wait(timeout=8)
  p,s,f,call,state,info=host(load='quick');check('fresh host restores nested stack, shortcuts, outfit, journal and stock',state['items']==expected['items'] and state['quickbar']==expected['quickbar'] and state['journal']==expected['journal'] and state['outfit']==1 and state['merchantStock']==expected['merchantStock'])
  assert call(type='quit')['ok'];f.close();s.close();p.wait(timeout=8)
  # Authentic Phase 3 shape: absent quantities/container/quickbar/journal fields get defaults.
  original=json.loads((root/'content/art-proof/package.json').read_text());oldItems=[]
  for n,d in enumerate(original['items']):oldItems.append({'id':'reach.item.'+str(n),'definition':d['id'],'where':'equipped' if d['equipment'] and d['equipment']['variantValue']==1 else 'bag','x':55+n%3*55,'y':125+n//3*55,'z':0})
  oldState={'x':16,'y':19,'z':0,'facing':3,'revision':12,'items':oldItems,'elapsedSeconds':123.5,'paused':False}
  payload={'schema':1,'campaign':'slot2','fingerprint':'01468f2e1e814f2fe6623c5866e616b7cfebb1b55475895a7f53369001be7bba','mapHash':info['mapHash'],'savedUtc':'2026-10-16T10:00:00Z','map':json.loads((home/'content/original/map.json').read_text()),'state':oldState}
  savedir=home/'UserData/Campaigns/slot2';savedir.mkdir(parents=True);raw=json.dumps(payload,separators=(',',':'));(savedir/'manual.json').write_text(json.dumps({'payload':raw,'sha256':hashlib.sha256(raw.encode()).hexdigest().upper()}));originalBytes=(savedir/'manual.json').read_bytes()
  p,s,f,call,state,info=host(slot='slot2',load='manual');check('published Phase 3 save upgrades existing possessions without multiplying them',state['inventoryVersion']==1 and len(state['items'])==10 and all(next(i for i in state['items'] if i['id']==old['id'])['amount']==1 for old in oldItems) and state['elapsedSeconds']==123.5)
  check('Phase 3 primary save remains untouched on load',(savedir/'manual.json').read_bytes()==originalBytes)
  assert call(type='quit')['ok'];f.close();s.close();p.wait(timeout=8)
  p,s,f,call,state,info=host(slot='slot2',load='latest');check('upgrade is idempotent across subsequent saves',len(state['items'])==10 and len([j for j in state['journal'] if 'upgraded' in j['text']])==1)
  assert call(type='quit')['ok'];f.close();s.close();p.wait(timeout=8)
  # Phase 4 shape retains inventory, custom shortcut layout and stock, adding combat once.
  legacy=json.loads(json.dumps(state));legacy.pop('combat',None);custom=['']*10;custom[0]='spell:greyhaven.spell.renew';custom[1]='item:'+next(i['id'] for i in legacy['items'] if i['definition'].endswith('healing-tonic'));legacy['quickbar']=custom
  payload['fingerprint']='44431d14bbee9637e356634c70724aaf9349eb31e8624aa1d0e33413c7fddb55';payload['state']=legacy
  raw=json.dumps(payload,separators=(',',':'));(savedir/'quick.json').write_text(json.dumps({'payload':raw,'sha256':hashlib.sha256(raw.encode()).hexdigest().upper()}))
  p,s,f,call,state,info=host(slot='slot2',load='quick');check('published Phase 4 save preserves custom inventory shortcuts and stock',state['items']==legacy['items'] and state['quickbar']==custom and state['merchantStock']==legacy['merchantStock'])
  check('Phase 4 upgrade initializes exactly five combat encounters',state['combat']['version']==1 and len(state['combat']['enemies'])==5)
  assert call(type='quit')['ok'];f.close();s.close();p.wait(timeout=8)
  # Authentic Phase 5 fields and legacy map hash: metadata and world progress did not exist.
  phase5=json.loads(json.dumps(state));phase5.pop('world',None)
  payload['fingerprint']='e62670b9ac91f8afbed38734a61c9f89ae29863ab66054b55b1efdf0958e92eb';payload['mapHash']='54d5dfb36ba60630997d5207966a81552ae87862e82b2a70b2ae48f526b6e8ff';payload['state']=phase5
  raw=json.dumps(payload,separators=(',',':'));saved=(savedir/'manual.json');saved.write_text(json.dumps({'payload':raw,'sha256':hashlib.sha256(raw.encode()).hexdigest().upper()}));before=saved.read_bytes()
  p,s,f,call,state,info=host(slot='slot2',load='manual');check('published Phase 5 save restores combat inventory and shortcuts with its legacy map hash',state['combat']==phase5['combat'] and state['items']==phase5['items'] and state['quickbar']==phase5['quickbar'] and state['world']['version']==1 and state['world']['objects']==[])
  check('Phase 5 load preserves its original primary save bytes',saved.read_bytes()==before)
  assert call(type='quit')['ok'];f.close();s.close();p.wait(timeout=8)
  if a.render:
   def launch(*args):
    r=subprocess.run([a.dotnet,str(runtime/'Greyhaven.Launcher.dll'),'--content-root',str(home),*map(str,args)],env=env,capture_output=True,text=True,timeout=50)
    if r.returncode:raise AssertionError(r.stdout+r.stderr+'\n'+ '\n'.join(x.read_text()[-3000:] for x in (home/'UserData/Logs').glob('*.log')))
   launch('--campaign','slot3','--load','new','--inventory-smoke','--capture',out/'native-inventory.png');check('native drag/drop, split prompt, quickbar, paperdoll, trade and text-input routes',True)
   logs=list((home/'UserData/Logs').glob('Greyhaven.Client*.log'));assert any('Native inventory routes:' in log.read_text() for log in logs)
   launch('--campaign','slot3','--load','latest','--inventory-scene','trade','--capture',out/'native-trade.png');check('native merchant gump renders saved stock',True)
   launch('--campaign','slot3','--load','latest','--inventory-scene','journal','--capture',out/'native-journal.png');check('native journal gump renders persisted activity and notes',True)
   layout=json.loads((home/'UserData/Interface/gumps.json').read_text());check('gump layout persists separately from campaign state',any(p['id']=='journal' for p in layout))
 finally:
  for p,f in processes:
   if p.poll()==None:p.kill();p.wait()
   f.close()
(out/'inventory-integration.json').write_text(json.dumps({'checks':len(checks),'passed':checks,'nativeRender':a.render,'platform':'Linux offscreen FNA; Windows cross-published separately'},indent=2)+'\n');print(len(checks),'live inventory checks passed')

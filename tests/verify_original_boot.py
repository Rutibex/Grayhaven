#!/usr/bin/env python3
"""Black-box original-content boots, TCP authority, exports and failure modes.
Requires built Linux runtime (or pass a Windows runtime and --dotnet dotnet).
"""
import argparse,json,os,shutil,socket,subprocess,tempfile,time
from pathlib import Path
ap=argparse.ArgumentParser();ap.add_argument('--root',type=Path,default=Path(__file__).resolve().parents[1]);ap.add_argument('--runtime',type=Path,required=True);ap.add_argument('--dotnet',default='dotnet');ap.add_argument('--render',action='store_true');a=ap.parse_args();root=a.root.resolve();runtime=a.runtime.resolve();results=[]
def run(role,*args,ok=True,env=None):
 r=subprocess.run([a.dotnet,str(runtime/f'Greyhaven.{role}.dll'),'--content-root',str(root),*map(str,args)],capture_output=True,text=True,env=env,timeout=30)
 if (r.returncode==0)!=ok:raise AssertionError(role+' unexpected exit '+str(r.returncode)+'\n'+r.stdout+'\n'+r.stderr)
 return r

def check(name,fn):fn();results.append(name);print('PASS',name,flush=True)
for role in ['Server','Client','Editor']:check(role+' validates an original-only package',lambda role=role:run(role,'--check','--audit',root/'docs/phase2'/f'{role.lower()}-check-files.json'))
with tempfile.TemporaryDirectory(prefix='greyhaven-boot-') as temp:
 d=Path(temp);ready=d/'ready.json';serverlog=(d/'server.log').open('w');server=subprocess.Popen([a.dotnet,str(runtime/'Greyhaven.Server.dll'),'--content-root',str(root),'--port','0','--ready-file',str(ready),'--audit',str(root/'docs/phase2/server-live-files.json')],stdout=serverlog,stderr=subprocess.STDOUT)
 try:
  for _ in range(200):
   if ready.exists():break
   if server.poll()!=None:raise AssertionError('Server exited: '+(d/'server.log').read_text())
   time.sleep(.025)
  info=json.loads(ready.read_text());port=info['port']
  def connect():
   sock=socket.create_connection(('127.0.0.1',port),timeout=5);return sock,sock.makefile('rwb',buffering=0)
  def exchange(f,msg):f.write((json.dumps(msg,separators=(',',':'))+'\n').encode());return json.loads(f.readline())
  def hello(f,**kw):return exchange(f,{'type':'hello','protocol':1,'fingerprint':info['fingerprint'],'mapHash':info['mapHash'],**kw})
  def mismatch():
   s,f=connect()
   try:assert not hello(f,fingerprint='wrong')['ok']
   finally:f.close();s.close()
   time.sleep(.1)
  check('TCP handshake rejects mismatched original content',mismatch)
  s,f=connect();welcome=hello(f);assert welcome['ok'];state=welcome['state'];count=len(state['items'])
  check('TCP boot returns original player and all item identities',lambda:(_ for _ in ()).throw(AssertionError()) if count!=10 else None)
  potion=next(x['id'] for x in state['items'] if x['definition'].endswith('healing-tonic'));wizard=next(x['id'] for x in state['items'] if x['definition'].endswith('wizard-hat'))
  def transfers():
   r=exchange(f,{'type':'transfer','itemId':potion,'destination':'ground','x':16,'y':20});assert r['ok'];assert next(x for x in r['state']['items'] if x['id']==potion)['where']=='ground'
   r=exchange(f,{'type':'transfer','itemId':potion,'destination':'bag','x':80,'y':140});assert r['ok'];assert len(r['state']['items'])==10
  check('TCP ground/backpack transfers retain one identity',transfers)
  def equip():
   r=exchange(f,{'type':'transfer','itemId':wizard,'destination':'equipped'});assert r['ok'];assert next(x for x in r['state']['items'] if x['definition'].endswith('trail-hat'))['where']=='bag';assert not exchange(f,{'type':'transfer','itemId':potion,'destination':'equipped'})['ok']
  check('TCP paperdoll replacement and invalid equipment validation',equip)
  def move():
   r=exchange(f,{'type':'step','dx':1,'dy':0});assert r['ok'];assert (r['state']['x'],r['state']['y'])==(17,19);r=exchange(f,{'type':'step','dx':1,'dy':0});assert not r['ok'];assert (r['state']['x'],r['state']['y'])==(17,19)
  check('TCP movement commits integer tiles and enforces step timing',move)
  def busy():
   s2,f2=connect()
   try:assert json.loads(f2.readline())['type']=='busy'
   finally:f2.close();s2.close()
  check('Second simultaneous session is rejected explicitly',busy)
  f.close();s.close();time.sleep(.1)
  if a.render:
   env={**os.environ,'SDL_VIDEODRIVER':'offscreen','SDL_AUDIODRIVER':'dummy','FNA3D_FORCE_DRIVER':'OpenGL','LIBGL_ALWAYS_SOFTWARE':'1'}
   check('Native client renders original content through live server',lambda:run('Client','--connect',f'127.0.0.1:{port}','--capture',root/'docs/phase2/client-native.png','--audit',root/'docs/phase2/client-files.json',env=env))
   check('Native editor renders original terrain/statics',lambda:run('Editor','--capture',root/'docs/phase2/editor-native.png','--audit',root/'docs/phase2/editor-files.json',env=env))
 finally:
  server.terminate();server.wait(timeout=5);serverlog.close()
 # Full editor export -> client & real ServUO provider reload, with shared terrain/elevation/statics agreement.
 export=d/'edited-map.json'
 check('Editor exports a changed original map',lambda:run('Editor','--edit-smoke','--export',export,'--audit',d/'edit-files.json'))
 hashes=[]
 for role in ['Server','Client','Editor']:
  def reload(role=role):
   r=run(role,'--map',export,'--check','--audit',d/(role+'-edited-files.json'));report=json.loads(r.stdout.strip().splitlines()[-1]);hashes.append(report['mapHash'])
  check(role+' reloads editor output without legacy data',reload)
 assert len(set(hashes))==1 and hashes[0]!=info['mapHash']
 # A disposable content-only root, with no vendor directory and no original engine installation.
 isolated=d/'isolated';shutil.copytree(root/'content/original',isolated/'content/original');shutil.copytree(root/'art/compiled',isolated/'art/compiled',ignore=shutil.ignore_patterns('preview.html','parts','contact-sheet.png','walk-proof.gif'))
 def isolated_run(ok=True):
  r=subprocess.run([a.dotnet,str(runtime/'Greyhaven.Client.dll'),'--content-root',str(isolated),'--check','--audit',str(d/'isolated-files.json')],capture_output=True,text=True,timeout=30);assert (r.returncode==0)==ok,r.stdout+r.stderr;return r
 check('Content-only root boots without vendor or UO directories',lambda:isolated_run())
 image=isolated/'art/compiled/images/grass.png';original=image.read_bytes();image.unlink()
 check('Missing original art fails explicitly without fallback',lambda:isolated_run(False));image.write_bytes(original+b'corrupt')
 check('Corrupt original art fails integrity validation',lambda:isolated_run(False));image.write_bytes(original)
 descriptor=isolated/'content/original/package.json';original_descriptor=descriptor.read_text();data=json.loads(original_descriptor);data['visuals']='../tiledata.mul';descriptor.write_text(json.dumps(data))
 check('Package path traversal/legacy data reference is rejected',lambda:isolated_run(False));descriptor.write_text(original_descriptor)
 invalid=json.loads(export.read_text());invalid['cells'][0]['terrain']='legacy.unknown';bad=d/'invalid-map.json';bad.write_text(json.dumps(invalid))
 check('Invalid edited-map references fail before rendering',lambda:run('Client','--map',bad,'--check','--audit',d/'bad-files.json',ok=False))
 # All provider file reads remain in the original package; explicitly selected map JSON is the only optional outside input.
 for path in (root/'docs/phase2').glob('*files.json'):
  data=json.loads(path.read_text());assert data['legacyFallback'] is False
  assert not any(Path(row['path']).suffix.lower() in {'.mul','.uop','.idx','.rle','.def','.ttf','.ico'} for row in data['loadedFiles'])
 results.append('All provider audits contain original-only content reads')
report={'passed':len(results),'checks':results,'nativeRender':a.render,'auditScope':'Provider file reads, isolated content-only boots, and guarded ServUO legacy lookup; not an OS syscall trace.'};(root/'docs/phase2/boot-integration.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report,indent=2))

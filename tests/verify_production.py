from pathlib import Path
import os,subprocess,tempfile,shutil,json
import argparse
ap=argparse.ArgumentParser();ap.add_argument('--runtime',type=Path,required=True);ap.add_argument('--dotnet',default='dotnet');a=ap.parse_args();r=Path(__file__).resolve().parents[1];out=r/'docs/phase8';out.mkdir(parents=True,exist_ok=True);rt=a.runtime.resolve();d=a.dotnet;env={**os.environ,'SDL_VIDEODRIVER':'offscreen','SDL_AUDIODRIVER':'dummy','FNA3D_FORCE_DRIVER':'OpenGL','LIBGL_ALWAYS_SOFTWARE':'1'}
cases=[('town','86,181',[]),('interior','109,172',[]),('cellar','200,28',[]),('bestiary','86,181',['--production-scene','bestiary']),('quests','109,172',['--adventure-scene','forge']),('audio-town','86,181',['--production-smoke']),('audio-cellar','200,28',['--production-smoke']),('audio-coast','89,232',['--production-smoke'])];passed=[]
with tempfile.TemporaryDirectory(prefix='greyhaven-native-') as t:
 home=Path(t)
 for n in ['content','art']:shutil.copytree(r/n,home/n,copy_function=os.link)
 (home/'Runtime').symlink_to(rt,target_is_directory=True)
 for n,pos,flags in cases:
  cmd=[d,str(rt/'Greyhaven.Launcher.dll'),'--content-root',str(home),'--campaign','slot1','--load','new','--start-at',pos,*flags,'--capture',str(out/f'native-{n}.png')]
  p=subprocess.run(cmd,capture_output=True,text=True,env=env,timeout=120)
  logs='\n'.join(x.read_text(errors='replace') for x in (home/'UserData/Logs').glob('*.log'))
  (out/f'native-{n}.log').write_text(p.stdout+p.stderr+logs)
  assert p.returncode==0,(n,(p.stdout+p.stderr+logs)[-3000:])
  if 'audio-' in n:assert 'Native production routes:' in logs,n
  assert (out/f'native-{n}.png').exists();passed.append(n);print('PASS '+n,flush=True)
  shutil.rmtree(home/'UserData')
(out/'native-production.json').write_text(json.dumps({'checks':len(passed),'passed':passed,'platform':'Linux offscreen SDL/OpenGL, dummy audio'},indent=2)+'\n')
with tempfile.TemporaryDirectory(prefix='greyhaven-inventory-') as t:
 home=Path(t)/'fixture';subprocess.run(['python3',str(r/'tools/content/prepare_fixture.py'),str(home)],check=True,capture_output=True)
 (home/'Runtime').symlink_to(rt,target_is_directory=True)
 p=subprocess.run([d,str(rt/'Greyhaven.Launcher.dll'),'--content-root',str(home),'--campaign','slot3','--load','new','--inventory-smoke','--capture',str(out/'native-inventory.png')],capture_output=True,text=True,env=env,timeout=150)
 logs='\n'.join(x.read_text(errors='replace') for x in (home/'UserData/Logs').glob('*.log'));(out/'native-inventory.log').write_text(p.stdout+p.stderr+logs)
 assert p.returncode==0,(p.stdout+p.stderr+logs)[-3000:]
 assert 'Native inventory routes:' in logs
 print('PASS native inventory',flush=True)

#!/usr/bin/env python3
"""Publish shared-runtime Windows launchers without bundling legacy client resources."""
import argparse,hashlib,json,shutil,subprocess,os
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
ap=argparse.ArgumentParser();ap.add_argument('--dotnet',default='dotnet');a=ap.parse_args();out=ROOT/'Runtime'
if out.exists():shutil.rmtree(out)
for name in ['Server','Client','Editor','Launcher']:
 log=ROOT/'artifacts/logs'/('phase8-'+name.lower()+'-publish.log');log.parent.mkdir(parents=True,exist_ok=True)
 with log.open('w') as f:
  result=subprocess.run([a.dotnet,'publish',str(ROOT/f'src/Greyhaven.{name}/Greyhaven.{name}.csproj'),'-c','Release','-r','win-x64','--self-contained','true','-o',str(out),'-p:UseSharedCompilation=false','-m:1','-nr:false'],cwd=ROOT,stdout=f,stderr=subprocess.STDOUT,env={**os.environ,'DOTNET_PROCESSOR_COUNT':'2','DOTNET_CLI_USE_MSBUILD_SERVER':'0'})
 if result.returncode:raise RuntimeError(log.read_text()[-6000:])
 print('Published '+name,flush=True)
# All four programs share the exact same .NET runtime and the original content files.
(out/'Notices').mkdir(exist_ok=True)
for notice in (ROOT/'vendor/TazUO/external/FNA').rglob('*'):
 if notice.is_file() and ('LICENSE' in notice.name.upper() or 'COPYING' in notice.name.upper()) and not any(x in notice.parts for x in ('obj','bin','obj_core')):
  dest=out/'Notices/FNA'/notice.relative_to(ROOT/'vendor/TazUO/external/FNA');dest.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(notice,dest)
for source,name in [('LICENSE','Greyhaven-MIT.txt'),('vendor/ServUO/LICENSE','ServUO-GPL-2.txt'),('vendor/TazUO/LICENSE.md','TazUO-BSD.txt'),('art/source/FONT-LICENSE.txt','DejaVu-font.txt')]:shutil.copyfile(ROOT/source,out/'Notices'/name)
for package in ['microsoft.netcore.app.runtime.win-x64','microsoft.netcore.app.host.win-x64']:
 base=Path.home()/'.nuget/packages'/package/'10.0.13'
 for notice in base.glob('*.TXT'):
  dest=out/'Notices'/package/notice.name;dest.parent.mkdir(exist_ok=True);shutil.copyfile(notice,dest)
for package in ['system.drawing.common','system.codedom','system.diagnostics.eventlog','system.diagnostics.performancecounter','microsoft.win32.systemevents']:
 base=Path.home()/'.nuget/packages'/package
 if base.exists():
  versions=list(base.glob('*'))
  for version in versions:
   if version.name!='10.0.0':continue
   for p in version.rglob('*'):
    if p.is_file() and any(k in p.name.upper() for k in ['LICENSE','NOTICE']):dest=out/'Notices'/package/p.name;dest.parent.mkdir(exist_ok=True);shutil.copyfile(p,dest)
# Do not package stale legacy boot logs or development symbol files in the small runtime.
for p in out.glob('*.pdb'):p.unlink()
rows=[{'path':p.relative_to(ROOT).as_posix(),'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in sorted(out.rglob('*')) if p.is_file()]
(ROOT/'manifests/phase8-runtime-files.json').write_text(json.dumps(rows,indent=2)+'\n')
print('Runtime file manifest:',len(rows),'files')

#!/usr/bin/env python3
"""Greyhaven foundation tools. Standard library only; no network unless bootstrap/build."""
import argparse, hashlib, json, os, re, shutil, subprocess, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TARGETS = {'server':'vendor/ServUO/Scripts/Scripts.csproj',
           'client':'vendor/TazUO/src/ClassicUO.Client/ClassicUO.Client.csproj',
           'editor':'vendor/CentrED/CentrED/CentrED.csproj',
           'contracts':'src/Greyhaven.Contracts/Greyhaven.Contracts.csproj'}
def read(name): return json.loads((ROOT/'manifests'/name).read_text())
def run(args,cwd=ROOT,log=None):
    print('> '+' '.join(map(str,args)),flush=True)
    if log:
        log.parent.mkdir(parents=True,exist_ok=True)
        with log.open('w') as f: result=subprocess.run(args,cwd=cwd,stdout=f,stderr=subprocess.STDOUT,env={**os.environ, 'DOTNET_PROCESSOR_COUNT':os.environ.get('DOTNET_PROCESSOR_COUNT','2'), 'UseSharedCompilation':'false', 'MSBUILDDISABLENODEREUSE':'1', 'DOTNET_CLI_USE_MSBUILD_SERVER':'0', 'RestorePackagesWithLockFile':'true', 'RestoreLockedMode':('true' if len(args)>1 and args[1]=='build' else 'false')})
        if result.returncode: print(log.read_text(errors='replace')[-6500:])
    else: result=subprocess.run(args,cwd=cwd,env={**os.environ, 'DOTNET_PROCESSOR_COUNT':os.environ.get('DOTNET_PROCESSOR_COUNT','2'), 'UseSharedCompilation':'false', 'MSBUILDDISABLENODEREUSE':'1', 'DOTNET_CLI_USE_MSBUILD_SERVER':'0', 'RestorePackagesWithLockFile':'true', 'RestoreLockedMode':('true' if len(args)>1 and args[1]=='build' else 'false')})
    if result.returncode: raise RuntimeError(f'Command failed ({result.returncode})'+(f'; log: {log}' if log else ''))
def git(path,*args): return subprocess.check_output(['git','-C',str(path),*args],text=True).strip()
def bundled_ok(row):
    manifest=ROOT/'manifests/bundled-dependencies.json'
    if not manifest.exists(): return False
    for entry in json.loads(manifest.read_text()):
        if entry['path']==row['path'] and entry['commit']==row['commit']:
            return bool(entry['files']) and all((ROOT/f['path']).is_file() and hashlib.sha256((ROOT/f['path']).read_bytes()).hexdigest()==f['sha256'] for f in entry['files'])
    return False

def verify(snapshots=True):
    errors=[]
    overrides={x['path']:x for x in read('fork-overrides.json')} if (ROOT/'manifests/fork-overrides.json').exists() else {}
    if snapshots:
        for row in read('source-snapshot.json'):
            if row['path'] in overrides and overrides[row['path']]['originalSha256']!=row['sha256']:
                errors.append('Invalid original hash in override: '+row['path'])
            f=ROOT/row['path']
            if not f.is_file() or hashlib.sha256(f.read_bytes()).hexdigest()!=overrides.get(row['path'],row)['sha256']:
                errors.append('Source changed/missing: '+row['path'])
    for row in read('dependencies.lock.json'):
        f=ROOT/row['path']
        if not (f/'.git').exists():
            if not bundled_ok(row): errors.append('Dependency missing or bundled files changed: '+row['path'])
            continue
        try:
            if git(f,'rev-parse','HEAD')!=row['commit']:errors.append('Wrong revision: '+row['path'])
            if git(f,'status','--porcelain','--untracked-files=no'):errors.append('Modified dependency: '+row['path'])
            # check_output.strip removes leading status on first line, so inspect raw output instead.
            raw=subprocess.check_output(['git','-C',str(f),'submodule','status','--recursive'],text=True)
            if any(line and line[0] in '-+U' for line in raw.splitlines()):errors.append('Uninitialized/mismatched nested submodule: '+row['path'])
        except subprocess.CalledProcessError:errors.append('Invalid git dependency: '+row['path'])
    for e in errors: print('FAIL '+e)
    print(f'Verification: {len(errors)} issue(s).')
    return not errors

def bootstrap():
    if not shutil.which('git'): raise RuntimeError('Install Git first.')
    for row in read('dependencies.lock.json'):
        dest=ROOT/row['path'];dest.parent.mkdir(parents=True,exist_ok=True)
        if not (dest/'.git').exists():
            if bundled_ok(row):
                print('Verified bundled dependency: '+row['path']);continue
            if dest.exists() and any(dest.iterdir()):raise RuntimeError(f'Refusing to overwrite nonempty dependency: {dest}')
            dest.mkdir(exist_ok=True)
            run(['git','init',str(dest)])
            run(['git','remote','add','origin',row['url']],cwd=dest)
        else:
            if git(dest,'remote','get-url','origin')!=row['url']:raise RuntimeError(f'Unexpected remote: {dest}')
            if git(dest,'status','--porcelain','--untracked-files=no'):raise RuntimeError(f'Refusing to reset modified dependency: {dest}')
        run(['git','fetch','--depth','1','origin',row['commit']],cwd=dest)
        run(['git','checkout','--detach',row['commit']],cwd=dest)
        run(['git','-c','url.https://github.com/.insteadOf=git://github.com/','submodule','update','--init','--recursive','--depth','1'],cwd=dest)
    if not verify(): raise RuntimeError('Dependency verification failed.')

def doctor():
    ok=True
    for tool in ['git','dotnet']:
        found=shutil.which(tool);print(f'{tool}: {found or "MISSING"}');ok=ok and bool(found)
    if shutil.which('dotnet'):
        run(['dotnet','--version'])
    for row in read('dependencies.lock.json'):
        exists=(ROOT/row['path']/'.git').exists() or bundled_ok(row);print(('OK ' if exists else 'MISSING ')+row['path']);ok=ok and exists
    if sys.platform!='win32':print('NOTE: first supported baseline is Windows x64. ServUO needs .NET Framework 4.8 reference assemblies; other hosts are exploratory.')
    else:
        refs=Path(os.environ.get('ProgramFiles(x86)',r'C:\Program Files (x86)'))/'Reference Assemblies/Microsoft/Framework/.NETFramework/v4.8'
        print('net48 developer reference assemblies: '+('OK' if refs.exists() else 'MISSING'));ok=ok and refs.exists()
    return ok

def materialize_locks():
    manifest=ROOT/'manifests/nuget-resolved.json'
    if not manifest.exists():return
    for row in json.loads(manifest.read_text()):
        path=(ROOT/row['path']).resolve()
        if not path.is_relative_to(ROOT.resolve()) or not (path.name=='packages.lock.json' or path.name.startswith('packages.') and path.name.endswith('.lock.json')):raise RuntimeError('Invalid lock manifest path')
        if not path.parent.exists():continue
        if path.exists():
            if json.loads(path.read_text())!=row['lock']:raise RuntimeError('NuGet lock differs from recorded baseline: '+row['path'])
        else:path.write_text(json.dumps(row['lock'],indent=2)+'\n')

def build(target):
    if not shutil.which('dotnet'):raise RuntimeError('Install .NET 10 SDK first.')
    materialize_locks()
    names=list(TARGETS) if target=='all' else [target]
    if any(n in ('client','editor') for n in names) and not verify(snapshots=False):raise RuntimeError('Run bootstrap to install the pinned dependencies.')
    for name in names:
        path=TARGETS[name];folder=ROOT/path
        project_root=folder.parent if name=='contracts' else ROOT/'vendor'/ {'server':'ServUO','client':'TazUO','editor':'CentrED'}[name]
        locks=list(project_root.rglob('packages.lock.json'))
        # Locked restore after the initial restore; never silently update an existing lock.
        restore=['dotnet','restore',path,'-p:RestorePackagesWithLockFile=true','--disable-parallel','-m:1','-nr:false']
        if locks:restore.append('--locked-mode')
        if name=='server':restore.append('-p:Platform=x64')
        if name=='client':restore.append('-p:SelfContained=false')
        run(restore,log=ROOT/f'artifacts/logs/{name}-restore.log')
        args=['dotnet','build',path,'-c','Release','--no-restore','-m:1','-nr:false','-p:UseSharedCompilation=false']
        if name=='client':args.append('-p:SelfContained=false')
        if name=='server':
            args.extend(['-p:Platform=x64','-p:OutputPath='+str(ROOT/'artifacts/build/server')+os.sep])
        run(args,log=ROOT/f'artifacts/logs/{name}-build.log')

def audit():
    rows=[]
    pattern=re.compile(r'\.(?:mul|uop|def|rle|idx)\b|[Cc]liloc\.|FindDataFile|GetUOFilePath')
    for component,folders in [('ServUO',['Server','Ultima','Scripts/Misc']),('TazUO',['src/ClassicUO.Assets','src/ClassicUO.IO']),('CentrED',['Shared','CentrED','Server'])]:
        for folder in folders:
            for f in sorted((ROOT/'vendor'/component/folder).rglob('*.cs')):
                for number,line in enumerate(f.read_text(errors='replace').splitlines(),1):
                    if pattern.search(line):rows.append({'component':component,'path':f.relative_to(ROOT).as_posix(),'line':number,'evidence':line.strip()})
    out=ROOT/'manifests/uo-data-references.json';out.write_text(json.dumps(rows,indent=2)+'\n')
    print(f'Recorded {len(rows)} candidate data references. Static scan is evidence, not a complete runtime dependency proof.')

def validate_content():
    package=json.loads((ROOT/'content/core/package.json').read_text())
    assert package['movement']=={'authority':'tile','directions':8,'presentation':'interpolated'}
    ids=set()
    for group in ('items','tiles','visuals'):
        for row in package[group]:
            assert re.fullmatch(r'[a-z][a-z0-9_.-]+',row['id']),row['id']
            assert row['id'] not in ids,row['id'];ids.add(row['id'])
    mappings=json.loads((ROOT/'content/core/legacy-ids.json').read_text())['mappings'];keys=set();names=set()
    domains={'Terrain','Static','Body','Equipment','Gump','Icon','Sound'}
    for row in mappings:
        key=(row['domain'],row['legacyId']);name=(row['domain'],row['contentId'])
        assert row['domain'] in domains and type(row['legacyId']) is int and 0<=row['legacyId']<=65535
        assert key not in keys and name not in names,'Duplicate bridge mapping'
        keys.add(key);names.add(name)
    print('Content descriptor and legacy ID bridge validated.')

def menu():
    choices={'1':doctor,'2':bootstrap,'3':verify,'4':lambda:build('all'),'5':audit,'7':lambda:__import__('webbrowser').open((ROOT/'Start-Art-Preview.html').as_uri()),'8':lambda:run([sys.executable,str(ROOT/'tools/art/build_art.py')]),'9':lambda:run(['dotnet','publish',str(ROOT/'src/Greyhaven.ArtProof/Greyhaven.ArtProof.csproj'),'-c','Release','-r','win-x64','--self-contained','true','-o',str(ROOT/'ArtProof')]),'6':lambda:os.startfile(ROOT/'README.md') if sys.platform=='win32' else print((ROOT/'README.md').read_text())}
    while True:
        print('\nGreyhaven Foundation & Art Proof\n1 Prerequisites\n2 Fetch pinned dependencies\n3 Verify sources\n4 Build all\n5 Audit UO data references\n6 Read guide\n7 Open art proof\n8 Bake art\n9 Publish native proof\n0 Exit')
        choice=input('Choose: ').strip()
        if choice=='0':return
        if choice in choices:
            try:choices[choice]()
            except Exception as e:print('ERROR:',e)

def main():
    parser=argparse.ArgumentParser();parser.add_argument('command',choices=['doctor','bootstrap','verify','build','audit','validate-content','init-repository','menu','art-build','art-validate','art-publish','content-build','content-check','runtime-publish','original-build']);parser.add_argument('target',nargs='?',choices=['all',*TARGETS],default='all');a=parser.parse_args()
    try:
        if a.command=='content-build':
            run([sys.executable,str(ROOT/'tools/content/build_package.py')]);return 0
        if a.command=='runtime-publish':
            run([sys.executable,str(ROOT/'tools/content/publish_runtime.py')]);return 0
        if a.command=='original-build':
            for name in ('Server','Client','Editor','Launcher'):
                run(['dotnet','build',str(ROOT/f'src/Greyhaven.{name}/Greyhaven.{name}.csproj'),'-c','Release','-p:UseSharedCompilation=false','-m:1','-nr:false'],log=ROOT/f'artifacts/logs/phase8-{name.lower()}-build.log')
            return 0
        if a.command=='content-check':
            for suite in ('Content','Inventory','Combat','World','Adventure'):
                run(['dotnet','build',str(ROOT/f'tests/Greyhaven.{suite}.Tests'),'-c','Release','-p:UseSharedCompilation=false','-m:1','-nr:false'])
                run(['dotnet',str(ROOT/f'tests/Greyhaven.{suite}.Tests/bin/Release/net10.0/Greyhaven.{suite}.Tests.dll'),str(ROOT)])
            return 0
        if a.command in ('art-build','art-validate'):
            run([sys.executable,str(ROOT/'tools/art/build_art.py')]+(['--validate'] if a.command=='art-validate' else []));return 0
        if a.command=='art-publish':
            run(['dotnet','publish',str(ROOT/'src/Greyhaven.ArtProof/Greyhaven.ArtProof.csproj'),'-c','Release','-r','win-x64','--self-contained','true','-o',str(ROOT/'ArtProof'),'-p:UseSharedCompilation=false','-m:1','-nr:false'],log=ROOT/'artifacts/logs/artproof-publish.log');return 0
        if a.command=='doctor':return 0 if doctor() else 1
        if a.command=='verify':return 0 if verify() else 1
        if a.command=='bootstrap':bootstrap()
        elif a.command=='build':build(a.target)
        elif a.command=='audit':audit()
        elif a.command=='validate-content':validate_content()
        elif a.command=='init-repository':
            if (ROOT/'.git').exists():raise RuntimeError('Repository already exists; refusing to reinitialize.')
            run(['git','init','-b','main'])
            paths='\0'.join(row['path'] for row in read('source-snapshot.json'))+'\0'
            subprocess.run(['git','add','-f','--pathspec-from-file=-','--pathspec-file-nul'],cwd=ROOT,input=paths.encode(),check=True)
            run(['git','add','.'])
            print('Repository initialized and staged. Commit using your own Git identity.')
        elif a.command=='menu':menu()
    except (RuntimeError,OSError,AssertionError,subprocess.CalledProcessError) as e: print('ERROR:',e,file=sys.stderr);return 1
    return 0
if __name__=='__main__':sys.exit(main())

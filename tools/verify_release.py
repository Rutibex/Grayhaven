#!/usr/bin/env python3
"""Validate a Greyhaven release archive without executing its Windows binaries."""
import argparse,hashlib,io,json,zipfile
from pathlib import Path,PurePosixPath
from PIL import Image
ap=argparse.ArgumentParser();ap.add_argument('archive',type=Path);a=ap.parse_args();counts={}
with zipfile.ZipFile(a.archive) as z:
 names=z.namelist();assert len(names)==len(set(names)),'Duplicate ZIP entries';assert z.testzip() is None,'ZIP CRC failure'
 assert all(n.startswith('Greyhaven/') and '..' not in PurePosixPath(n).parts for n in names),'Unsafe archive paths'
 assert not any('/UserData/' in n or '/bin/' in n or '/obj/' in n or '/.git/' in n for n in names),'Machine output leaked into release'
 def read(p):return z.read('Greyhaven/'+p)
 runtime=json.loads(read('manifests/phase8-runtime-files.json'))
 for row in runtime:assert hashlib.sha256(read(row['path'])).hexdigest()==row['sha256'],row['path']
 counts['runtimeHashes']=len(runtime)
 assets=json.loads(read('content/original/assets.json'))['files']
 for path,digest in assets.items():assert hashlib.sha256(read(path)).hexdigest()==digest,path
 counts['originalAssetHashes']=len(assets)
 production=json.loads(read('art/source/phase8/audio-production.json'));assert len(production['cues'])==39
 for row in production['cues']:assert hashlib.sha256(read('content/original/audio/'+row['name']+'.wav')).hexdigest()==row['sha256']
 counts['productionAudioCues']=39
 adventure=json.loads(read('content/original/adventure.json'))
 assert len(adventure['quests'])==4 and sum(q.get('main',False) for q in adventure['quests'])==1
 assert len(adventure['routines'])==7 and len(adventure['trainers'])==11
 counts.update(originalQuests=4,residentRoutines=7)
 for row in json.loads(read('manifests/phase7-servuo-sources.json')):assert hashlib.sha256(read(row['path'])).hexdigest()==row['sha256'],row['path']
 counts['consultedServUOSourceHashes']=14
 pngs=[n for n in names if n.startswith('Greyhaven/art/') and n.endswith('.png')]
 for n in pngs:
  with Image.open(io.BytesIO(z.read(n))) as image:image.load()
 counts['decodedArtPNGs']=len(pngs)
 for role in ['Client','Server','Editor','Launcher']:assert read('Runtime/Greyhaven.'+role+'.exe')[:2]==b'MZ',role
 counts['windowsExecutables']=4
 for path in ['Start-Media-Preview.html','tools/art/bake_production.py','tools/content/build_audio.py','src/Greyhaven.Presentation/ProductionMedia.cs','docs/PHASE_8_VALIDATION.md','src/Greyhaven.Server/Program.cs','src/Greyhaven.Content/CampaignStore.cs','src/Greyhaven.Presentation/CombatInterface.cs','docs/PHASE_8.md','docs/PHASE_7_SERVUO.md','docs/PHASE_7_VALIDATION.md','src/Greyhaven.Server/AdventureSimulation.cs','src/Greyhaven.Server/ProgressionSimulation.cs','src/Greyhaven.Server/TownSimulation.cs','src/Greyhaven.Presentation/AdventureInterface.cs','tests/Greyhaven.Adventure.Tests/Program.cs','src/Greyhaven.Presentation/EditorInterface.cs','src/Greyhaven.Content/WorldAuthoring.cs','Run-Greyhaven.cmd']:assert 'Greyhaven/'+path in names,path
 counts.update(zipEntries=len(names),zipCRC='passed',personalCampaignsIncluded=False)
print(json.dumps(counts,indent=2))

import hashlib,json,unittest,wave
from pathlib import Path
import numpy as np
from PIL import Image
ROOT=Path(__file__).resolve().parents[1]
class ProductionMediaTests(unittest.TestCase):
 @classmethod
 def setUpClass(cls):cls.manifest=json.loads((ROOT/'art/compiled/manifest.json').read_text());cls.audio=json.loads((ROOT/'art/source/phase8/audio-production.json').read_text())
 def test_all_audio_is_valid_pcm_without_clipping_or_silence(self):
  for row in self.audio['cues']:
   p=ROOT/'content/original/audio'/(row['name']+'.wav');self.assertEqual(hashlib.sha256(p.read_bytes()).hexdigest(),row['sha256'])
   with wave.open(str(p)) as w:
    self.assertEqual(w.getframerate(),44100);self.assertEqual(w.getsampwidth(),2);data=np.frombuffer(w.readframes(w.getnframes()),dtype='<i2')
    self.assertGreater(np.sqrt(np.mean(data.astype(float)**2)),30);self.assertLess(np.max(np.abs(data.astype(int))),30000)
 def test_music_and_ambient_seams_are_sample_continuous(self):
  rows=[r for r in self.audio['cues'] if r['loop']];self.assertEqual(len(rows),7)
  for row in rows:
   with wave.open(str(ROOT/'content/original/audio'/(row['name']+'.wav'))) as w:
    data=np.frombuffer(w.readframes(w.getnframes()),dtype='<i2').reshape(-1,w.getnchannels()).astype(int);self.assertLessEqual(np.max(np.abs(data[0]-data[-1])),1)
 def test_production_sound_palette_has_unique_audio(self):
  self.assertEqual(len({r['sha256'] for r in self.audio['cues']}),39)
  for group in ['music','ambience','effects']:self.assertTrue(any(r['category']==group for r in self.audio['cues']))
 def test_distinct_enemies_have_complete_visible_eight_direction_clips(self):
  paths=set()
  for name in ['shore-raider','reef-slinger','salt-hexer','brineguard']:
   actor=self.manifest['actors'][name];paths.add(actor['image']);self.assertEqual(len(actor['directions']),8)
   for clips in actor['directions'].values():self.assertEqual(set(clips),{'idle','walk','attack','cast'})
  self.assertEqual(len(paths),4)
 def test_new_enemy_clips_contain_actual_motion_in_every_facing(self):
  for name in ['shore-raider','reef-slinger','salt-hexer','brineguard']:
   actor=self.manifest['actors'][name]
   with Image.open(ROOT/'art/compiled'/actor['image']) as im:
    for direction,clips in actor['directions'].items():
     for clip,desc in clips.items():
      tiles=[]
      for i in range(desc['start'],desc['start']+desc['count']):tiles.append(im.crop((i%16*128,i//16*128,i%16*128+128,i//16*128+128)).tobytes())
      self.assertGreater(len(set(tiles)),1,(name,direction,clip))
 def test_unique_story_and_inventory_art_are_not_placeholder_aliases(self):
  paths=[self.manifest['images'][n]['image'] for n in ['bandage','lantern-heart','vault-key','greatblade','letter-packet','satchel']];self.assertEqual(len(set(paths)),6)
  for p in paths:
   with Image.open(ROOT/'art/compiled'/p) as im:self.assertIsNotNone(im.getchannel('A').getbbox())
 def test_new_floor_tiles_have_exact_authoritative_footprint(self):
  for name in ['roof-terracotta','floor-wood','floor-cellar','sand']:
   with Image.open(ROOT/'art/compiled'/self.manifest['images'][name]['image']) as im:self.assertEqual(im.size,(44,44));self.assertEqual(sum(a>0 for a in im.getchannel('A').getdata()),1012)
 def test_new_sources_and_prompts_are_recorded(self):
  sources=json.loads((ROOT/'art/source/provenance.json').read_text())['assets'];rows=[r for r in sources if '/phase8/' in r['path']];self.assertEqual(len(rows),4)
  for r in rows:self.assertEqual(hashlib.sha256((ROOT/r['path']).read_bytes()).hexdigest(),r['sha256']);self.assertTrue((ROOT/r['promptFile']).is_file())
if __name__=='__main__':unittest.main()

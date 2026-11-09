import importlib.util,json,hashlib,io,sys,tempfile,unittest
from pathlib import Path
from PIL import Image,ImageChops
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'art/compiled'
spec=importlib.util.spec_from_file_location('art',ROOT/'tools/art/build_art.py');art=importlib.util.module_from_spec(spec);spec.loader.exec_module(art)
class ArtPipelineTests(unittest.TestCase):
 @classmethod
 def setUpClass(cls):cls.m=json.loads((OUT/'manifest.json').read_text())
 def test_all_sources_fully_decode_and_match_provenance(self):
  for a in json.loads((ROOT/'art/source/provenance.json').read_text())['assets']:
   p=ROOT/a['path'];self.assertEqual(art.sha(p),a['sha256'])
   with Image.open(p) as im:im.load()
 def test_frame_budget_and_equipment_combinations(self):
  self.assertEqual(len(self.m['variants']),24);self.assertEqual(len({(v['outfit'],v['hat'],v['sword'],v['shield']) for v in self.m['variants']}),24)
  self.assertEqual(sum(a['frames'] for name,a in self.m['actors'].items() if (name.startswith(('humanoid-','armor-')) or name=='tidehound') and not name.endswith('-greatblade')),5760)
 def test_137_extracted_parts(self):self.assertEqual(len(list((OUT/'parts').rglob('*.png'))),137)
 def test_every_frame_is_visible_and_inside_cell(self):
  for name,a in self.m['actors'].items():
   im=Image.open(OUT/a['image'])
   for index in range(a['frames']):
    tile=im.crop((index%16*128,index//16*128,index%16*128+128,index//16*128+128));box=tile.getchannel('A').getbbox();self.assertIsNotNone(box,(name,index));self.assertGreater(box[0],0);self.assertLess(box[2],128)
 def test_animation_ranges_and_ground_pivots(self):
  for a in self.m['actors'].values():
   self.assertEqual(a['pivot'],[64,112]);seen=[]
   for d in art.DIRS:
    for c,desc in a['directions'][d].items():seen.extend(range(desc['start'],desc['start']+desc['count']))
   self.assertEqual(sorted(seen),list(range(a['frames'])))
 def test_equipment_changes_baked_pixels(self):
  base=Image.open(OUT/self.m['actors']['humanoid-h0-s0-b0']['image'])
  for name in ['humanoid-h1-s0-b0','humanoid-h2-s0-b0','humanoid-h0-s1-b0','humanoid-h0-s0-b1','armor-h0-s0-b0']:
   other=Image.open(OUT/self.m['actors'][name]['image']);self.assertIsNotNone(ImageChops.difference(base,other).convert('RGB').getbbox())
 def test_three_mirrored_directions_preserve_pivot(self):
  a=self.m['actors']['humanoid-h1-s1-b1'];im=Image.open(OUT/a['image']);from PIL import ImageOps
  def cell(i):return im.crop((i%16*128,i//16*128,i%16*128+128,i//16*128+128))
  for target,source in [('SW','SE'),('W','E'),('NW','NE')]:
   src=cell(a['directions'][source]['walk']['start']+2);expected=Image.new('RGBA',(128,128));expected.alpha_composite(ImageOps.mirror(src),(1,0));actual=cell(a['directions'][target]['walk']['start']+2);self.assertEqual(expected.tobytes(),actual.tobytes())
 def test_tile_footprint_and_row_coverage(self):
  # Inclusive edge pixels intentionally overlap at joins to prevent hairline seams.
  im=Image.open(OUT/'images/grass.png');a=im.getchannel('A');self.assertEqual(sum(v>0 for v in a.getdata()),1012)
  for y in range(44):self.assertEqual(sum(a.getpixel((x,y))>0 for x in range(44)),2*(y+1) if y<22 else 2*(44-y))
 def test_large_png_is_complete_and_reproducible(self):
  im=Image.frombytes('RGBA',(679,629),bytes((i*73+i//7)%256 for i in range(679*629*4)))
  with tempfile.TemporaryDirectory() as d:
   p=Path(d)/'a.png';q=Path(d)/'b.png';art.save_png(im,p);art.save_png(im,q);self.assertEqual(p.read_bytes(),q.read_bytes());self.assertEqual(Image.open(p).tobytes(),im.tobytes())
 def test_compiled_integrity_and_decode(self):art.validate(OUT)
 def test_native_boot_precedes_uo_settings_and_data(self):
  text=(ROOT/'vendor/TazUO/src/ClassicUO.Client/Main.cs').read_text(encoding='utf-8-sig');self.assertLess(text.index('"--art-proof"'),text.index('TazLang.Load()'));code=(ROOT/'vendor/TazUO/src/ClassicUO.Client/Greyhaven/ArtProofGame.cs').read_text();self.assertNotIn('ClassicUO.Assets',code)
 def test_failed_staged_build_preserves_previous_art(self):
  from unittest.mock import patch
  with tempfile.TemporaryDirectory() as d:
   root=Path(d);(root/'art/compiled').mkdir(parents=True);old=root/'art/compiled/accepted.txt';old.write_text('accepted')
   with patch.object(art,'ROOT',root),patch.object(art,'OUT',root/'art/compiled'),patch.object(art,'build',side_effect=RuntimeError('forced bake failure')),patch.object(sys,'argv',['build_art.py']):
    with self.assertRaises(RuntimeError):art.main()
   self.assertEqual(old.read_text(),'accepted');self.assertEqual(list((root/'art').glob('.art-bake-*')),[])
 def test_self_contained_offline_browser(self):
  html=(OUT/'preview.html').read_text();self.assertNotIn('/*MANIFEST*/',html);self.assertNotIn('/*MODEL*/',html);self.assertNotIn('fetch(',html);self.assertIn('data:image/png;base64,',html)
if __name__=='__main__':unittest.main()

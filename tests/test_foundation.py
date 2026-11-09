import contextlib, importlib.util, io, json, tempfile, unittest
from pathlib import Path
from unittest.mock import patch
spec=importlib.util.spec_from_file_location('project',Path(__file__).resolve().parents[1]/'tools/project.py')
p=importlib.util.module_from_spec(spec);spec.loader.exec_module(p)
class FoundationTests(unittest.TestCase):
    def test_snapshot_detects_corruption(self):
        import hashlib
        with tempfile.TemporaryDirectory() as d:
            r=Path(d);(r/'manifests').mkdir();(r/'a').write_text('original')
            (r/'manifests/source-snapshot.json').write_text(json.dumps([{'path':'a','sha256':hashlib.sha256(b'original').hexdigest()}]))
            (r/'manifests/dependencies.lock.json').write_text('[]')
            with patch.object(p,'ROOT',r),contextlib.redirect_stdout(io.StringIO()):
                self.assertTrue(p.verify());(r/'a').write_text('changed');self.assertFalse(p.verify())
    def test_duplicate_bridge_rejected(self):
        with tempfile.TemporaryDirectory() as d:
            r=Path(d);(r/'content/core').mkdir(parents=True)
            (r/'content/core/package.json').write_text(json.dumps({'movement':{'authority':'tile','directions':8,'presentation':'interpolated'},'items':[],'tiles':[],'visuals':[]}))
            mapping={'domain':'Icon','legacyId':1,'contentId':'item.a'}
            (r/'content/core/legacy-ids.json').write_text(json.dumps({'mappings':[mapping,mapping]}))
            with patch.object(p,'ROOT',r),self.assertRaises(AssertionError):p.validate_content()
    def test_bundled_dependency_detects_tampering(self):
        import hashlib
        with tempfile.TemporaryDirectory() as d:
            r=Path(d);(r/'manifests').mkdir();(r/'vendor/dep').mkdir(parents=True)
            f=r/'vendor/dep/file.cs';f.write_text('source')
            row={'path':'vendor/dep','commit':'abc'}
            entry={**row,'files':[{'path':'vendor/dep/file.cs','sha256':hashlib.sha256(b'source').hexdigest()}]}
            (r/'manifests/bundled-dependencies.json').write_text(json.dumps([entry]))
            with patch.object(p,'ROOT',r):
                self.assertTrue(p.bundled_ok(row));f.write_text('modified');self.assertFalse(p.bundled_ok(row))
    def test_missing_lock_restored_and_different_lock_rejected(self):
        with tempfile.TemporaryDirectory() as d:
            r=Path(d);(r/'manifests').mkdir();(r/'project').mkdir()
            lock={'version':1,'dependencies':{}}
            (r/'manifests/nuget-resolved.json').write_text(json.dumps([{'path':'project/packages.lock.json','lock':lock}]))
            with patch.object(p,'ROOT',r):
                p.materialize_locks();self.assertEqual(json.loads((r/'project/packages.lock.json').read_text()),lock)
                (r/'project/packages.lock.json').write_text('{"changed":true}')
                with self.assertRaises(RuntimeError):p.materialize_locks()
    def test_current_content_policy(self):p.validate_content()
    def test_subprocess_failure_not_reported_as_success(self):
        import sys
        with self.assertRaises(RuntimeError):p.run([sys.executable,'-c','raise SystemExit(7)'])
if __name__=='__main__':unittest.main()

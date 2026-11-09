from pathlib import Path
import base64,json
ROOT=Path(__file__).resolve().parents[2]
def make(out):
 m=json.loads((out/'manifest.json').read_text());paths={x['image'] for x in m['actors'].values()}|{x['image'] for x in m['images'].values()}|{m['font']['image']}
 data={p:'data:image/png;base64,'+base64.b64encode((out/p).read_bytes()).decode() for p in sorted(paths)}
 html=(ROOT/'tools/art/preview-template.html').read_text().replace('/*MANIFEST*/',json.dumps(m)).replace('/*IMAGES*/',json.dumps(data)).replace('/*MODEL*/',(ROOT/'tools/art/proof-model.js').read_text())
 (out/'preview.html').write_text(html)
if __name__=='__main__':make(ROOT/'art/compiled')

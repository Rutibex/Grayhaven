#!/usr/bin/env python3
"""Build the original island package and deterministic synthetic audio.
The compact mechanics fixture remains available under maps/. Original definitions and art only; never reads UO data.
"""
import json,hashlib,math,random,struct,wave
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'content/original'
def dump(name,obj):(OUT/name).write_text(json.dumps(obj,indent=2)+'\n')
def main():
 OUT.mkdir(parents=True,exist_ok=True);(OUT/'audio').mkdir(exist_ok=True)
 text={'map.island':'Greyhaven Island','game.title':'Greyhaven','map.name':'Wayfarer\'s Reach','hero.name':'The Wayfarer','ui.inventory':'BACKPACK','ui.paperdoll':'PAPERDOLL','ui.spellbook':'THE TIDAL GRIMOIRE','ui.journal':'FIELD JOURNAL','ui.connected':'Original-content server connected','ui.offline':'Local original-content world','ui.editor':'GREYHAVEN MAP WORKSHOP','ui.loading':'Opening Wayfarer\'s Reach…','ui.empty':'No objects here.','ui.inspect':'Inspect','journal.welcome':'The tide has left an unfamiliar shore. A ruined arch stands inland; a small camp marks the path ahead.','journal.scope':'This is the original-content boot fixture. The town, dungeon and adventure arrive in later milestones.'}
 tiles=[]
 for i,(name,blocked,wet) in enumerate([('grass',False,False),('dirt',False,False),('stone',False,False),('water',True,True)]):
  key='greyhaven.terrain.'+name;text['terrain.'+name]=name.title();tiles.append({'id':key,'numericId':4096+i,'nameKey':'terrain.'+name,'visual':name,'impassable':blocked,'wet':wet,'surface':not blocked,'height':0})
 statics=[]
 for i,(name,height,blocked) in enumerate([('wall-left',20,True),('wall-right',20,True),('door',20,True),('tree',25,True),('chest',12,True),('chest-open',12,True),('lantern',8,False),('shrub',5,True),('bed',8,True),('table',10,True),('stool',6,True),('anvil',10,True),('bookcase',24,True),('forge',22,True),('workbench',12,True),('gravestone',12,True),('barrel',12,True),('jetty-piling',12,True),('ruin-column',20,True)]):
  text['static.'+name]=name.replace('-',' ').title();statics.append({'id':'greyhaven.static.'+name,'numericId':8192+i,'nameKey':'static.'+name,'visual':name,'impassable':blocked,'surface':False,'wet':False,'height':height})
 art=json.loads((ROOT/'content/art-proof/package.json').read_text());items=[]
 for a in art['items']:
  key='item.'+a['id'].split('.')[-1];text[key]=a['displayName'];items.append({'id':a['id'],'nameKey':key,'groundVisual':a['groundVisual'],'inventoryVisual':a['inventoryVisual'],'equipment':a['equipment'],'weight':0.01 if a['inventoryVisual']=='coins' else 1,'stackable':a['inventoryVisual'] in ['coins','potion','crystal','scroll'],'price':{'coins':1,'potion':8,'crystal':20,'scroll':12}.get(a['inventoryVisual'],30)})
 for a in items:
  if a['equipment'] is not None:
   a['equipment']['requiredStrength']=8 if a['equipment']['slot'] in ['sword','shield'] else 0
   a['equipment']['requiredIntelligence']=8 if a['inventoryVisual']=='hat-wizard' else 0
 for name,visual,slots,weight,capacity,price in [('field-satchel','satchel',12,1,20,18),('sea-chest','chest',24,8,60,40)]:
  key='item.'+name;text[key]=name.replace('-',' ').title();items.append({'id':'greyhaven.item.'+name,'nameKey':key,'groundVisual':visual,'inventoryVisual':visual,'equipment':None,'weight':weight,'stackable':False,'containerSlots':slots,'containerWeight':capacity,'price':price})
 text['item.mariner-greatblade']='Mariner Greatblade';items.append({'id':'greyhaven.item.mariner-greatblade','nameKey':'item.mariner-greatblade','groundVisual':'greatblade','inventoryVisual':'greatblade','equipment':{'slot':'sword','variantValue':1,'twoHanded':True,'requiredStrength':10},'weight':5,'stackable':False,'price':75})
 text['item.bandage']='Clean Bandage';items.append({'id':'greyhaven.item.bandage','nameKey':'item.bandage','groundVisual':'bandage','inventoryVisual':'bandage','equipment':None,'weight':0.1,'stackable':True,'price':3})
 text['item.fallen-pack']='Fallen Pack';items.append({'id':'greyhaven.item.fallen-pack','nameKey':'item.fallen-pack','groundVisual':'backpack','inventoryVisual':'backpack','anchored':True,'equipment':None,'weight':0.1,'stackable':False,'containerSlots':128,'containerWeight':100,'price':1})
 spells=[]
 for a in art['spells']:
  key='spell.'+a['displayName'].lower();text[key]=a['displayName'];spells.append({'id':a['id'],'nameKey':key,'visual':a['icon'],'rulesEnabled':True})
 # A compact chunked island fixture, with a shore, paved court, wall/arch, props and elevation sample.
 cells=[]
 for y in range(32):
  for x in range(32):
   land=((x-16)/12)**2+((y-16)/10)**2<1
   name='grass' if land else 'water';z=0
   if land and (x==16 or y==18):name='dirt'
   if land and 13<=x<=21 and 13<=y<=17:name='stone'
   if land and y<=10:z=2
   cells.append({'terrain':'greyhaven.terrain.'+name,'z':z})
 objects=[]
 for i,(name,x,y) in enumerate([('wall-left',18,13),('wall-left',19,13),('door',20,13),('wall-right',21,13),('tree',10,16),('tree',24,17),('chest',14,17),('lantern',16,16),('shrub',22,20)]):objects.append({'id':'reach.prop.'+str(i),'definition':'greyhaven.static.'+name,'x':x,'y':y,'z':0})
 dump('map.json',{'schema':1,'id':'greyhaven.map.reach','nameKey':'map.name','width':32,'height':32,'chunkSize':8,'spawn':{'x':16,'y':19,'z':0},'cells':cells,'statics':objects})
 from build_island import build
 build()
 from build_adventure import build as build_adventure
 build_adventure()
 text['journal.welcome']='You arrived in Greyhaven. The north road climbs to the Broken Beacon; the west trail enters Morrowwood.'
 text['journal.scope']='Explore the island, speak with its townspeople, and descend through the two floors of the Drowned Cellars.'
 dump('text.en.json',text);dump('definitions.json',{'terrain':tiles,'statics':statics,'items':items,'spells':spells,'abilities':[],'prefabs':[],'palette':{'uiGold':'#d9bb80','uiInk':'#18262b'},'features':{'boats':False,'housing':False,'legacySkills':False,'legacyProfessions':False,'legacyMusic':False}})
 from build_audio import build as build_audio
 audio=build_audio()
 dump('package.json',{'schema':1,'id':'greyhaven.original','version':'0.8.0','compatibleFingerprints':['b8b3c1badd104e5e4e7783d056b49ec3de803760583061325436209e2dfa2b20','4e9056c6a8bcb7c5b8766ca0cc0f516e03895173e635006d2607dec2b4b83079','e62670b9ac91f8afbed38734a61c9f89ae29863ab66054b55b1efdf0958e92eb','01468f2e1e814f2fe6623c5866e616b7cfebb1b55475895a7f53369001be7bba','44431d14bbee9637e356634c70724aaf9349eb31e8624aa1d0e33413c7fddb55'],'adventure':'content/original/adventure.json','titleKey':'game.title','definitions':'content/original/definitions.json','map':'content/original/map.json','text':'content/original/text.en.json','audio':'content/original/audio.json','visuals':'art/compiled/manifest.json','integrity':'content/original/assets.json','movement':{'authority':'tile','directions':8,'interpolationMilliseconds':200,'maxStepHeight':2},'legacyFallback':False})
 paths=[OUT/'maps/reach-fixture.json']+[OUT/n for n in ['definitions.json','map.json','text.en.json','audio.json','adventure.json']]+list((OUT/'audio').glob('*.wav'))+[ROOT/'art/compiled/manifest.json']
 m=json.loads((ROOT/'art/compiled/manifest.json').read_text());paths.extend(ROOT/'art/compiled'/p for p in {x['image'] for x in m['actors'].values()}|{x['image'] for x in m['images'].values()}|{m['font']['image']})
 dump('assets.json',{'algorithm':'sha256','files':{p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest() for p in sorted(set(paths))}})
 print('Original package: 256×256 island and retained 32×32 mechanics fixture, 4 terrain definitions, 19 static definitions, 13 items, 8 active spells, 39 original production WAV cues.')
if __name__=='__main__':main()

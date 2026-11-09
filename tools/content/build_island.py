#!/usr/bin/env python3
"""Authored Greyhaven island layout. Deterministic dressing, explicit rooms/routes/objects."""
import json,math,random,hashlib
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'content/original';T='greyhaven.terrain.';S='greyhaven.static.';I='greyhaven.item.'
def build():
 w=h=256;cells=[{'terrain':T+'water','z':0} for _ in range(w*h)];statics=[];objects=[];regions=[];roofs=[];rng=random.Random(68021)
 def cell(x,y,name='grass',z=0):cells[y*w+x]={'terrain':T+name,'z':z}
 def rect(x,y,ww,hh,name='stone',z=0):
  for yy in range(y,y+hh):
   for xx in range(x,x+ww):cell(xx,yy,name,z)
 def prop(name,x,y):
  statics.append({'id':'island.prop.'+str(len(statics)),'definition':S+name,'x':x,'y':y,'z':cells[y*w+x]['z']})
 def obj(id,kind,name,x,y,**kw):
  o={'id':id,'kind':kind,'name':name,'x':x,'y':y,'visual':'lantern','text':'',**kw};objects.append(o);return o
 def region(id,name,x,y,ww,hh,kind='wilderness',safe=False):regions.append({'id':id,'name':name,'kind':kind,'x':x,'y':y,'width':ww,'height':hh,'safe':safe})
 for y in range(12,244):
  for x in range(8,188):
   a=math.atan2((y-128)/108,(x-94)/79);r=((x-94)/79)**2+((y-128)/108)**2;edge=1+.045*math.sin(a*7)+.025*math.cos(a*11)
   if r<edge:
    z=0
    if y<75 and 57<x<131:z=2*min(4,(75-y)//8)
    cell(x,y,'dirt' if r>edge-.075 else 'grass',z)
 region('region.island','Greyhaven Island',0,0,192,256)
 region('region.woods','Morrowwood',25,70,48,79)
 region('region.marsh','Saltfen',130,109,45,58)
 region('region.beacon','The Broken Beacon',75,28,40,40)
 region('region.graves','Saint Orin Cemetery',123,69,35,34)
 region('region.town','Greyhaven Town',60,153,62,58,'town',True)
 region('region.docks','Gullhook Docks',77,211,30,29,'town',True)
 # Continuous flat paths connect all authored landmarks. Northern steps rise two units at a time.
 def path(points,width=3):
  for (x1,y1),(x2,y2) in zip(points,points[1:]):
   n=max(abs(x2-x1),abs(y2-y1))
   for j in range(n+1):
    x=round(x1+(x2-x1)*j/max(1,n));y=round(y1+(y2-y1)*j/max(1,n));z=2*min(4,max(0,(75-y)//8)) if 57<x<131 and y<75 else 0
    for dy in range(-(width//2),width//2+1):
     for dx in range(-(width//2),width//2+1):cell(x+dx,y+dy,'dirt',z)
 path([(88,228),(88,145),(88,45)])
 path([(38,124),(88,124),(151,124)])
 path([(88,145),(148,145),(148,86)])
 path([(88,108),(116,108),(116,94)])
 # Town square, streets and docks.
 rect(61,154,60,56,'grass');rect(78,178,24,8);rect(80,156,4,50,'dirt');rect(98,156,3,50,'dirt');rect(62,181,58,3,'dirt');rect(62,199,58,3,'dirt')
 rect(85,211,6,25,'stone');rect(81,229,22,4,'stone')
 buildings=[]
 def building(id,name,x,y,ww,hh):
  rect(x,y,ww,hh);doorx=x+ww//2;doory=y+hh-1
  for xx in range(x,x+ww):prop('wall-left',xx,y)
  for yy in range(y+1,y+hh):prop('wall-right',x,yy)
  for xx in range(x+1,x+ww):
   if xx!=doorx:prop('wall-left',xx,y+hh-1)
  for yy in range(y+1,y+hh-1):prop('wall-right',x+ww-1,yy)
  obj(id+'.door','door',name+' door',doorx,doory,visual='door',text='A stout Greyhaven door.')
  roofs.append({'id':id+'.roof','name':name,'kind':'roof','x':x,'y':y,'width':ww,'height':hh,'visual':'stone'})
  prop('chest',x+2,y+2);prop('lantern',x+ww-2,y+2)
  buildings.append({'id':id,'name':name,'x':x,'y':y,'width':ww,'height':hh,'door':[doorx,doory]})
 for args in [('town.inn','The Lantern and Gull',64,165,14,11),('town.provisions','Nessa\'s Provisions',85,165,12,10),('town.forge','The Anchor Forge',103,165,13,10),('town.healer','House of Quiet Tides',64,187,14,10),('town.magic','The Tidal Grimoire',85,187,12,10),('town.home1','The Netmaker\'s House',104,187,12,10),('town.home2','Alder Cottage',64,202,12,7),('town.home3','Fisher\'s Rest',103,202,13,7)]:building(*args)
 people=[('nessa','Nessa','merchant',86,180,'The east road reaches Saltfen. The north road climbs to the Broken Beacon. I trade supplies here in the square.'),('mara','Mara Gull','innkeeper',70,170,'Welcome to the Lantern and Gull. The island paths are marked on your atlas.'),('orrin','Orrin Vale','smith',109,170,'A shield and a patient step will carry you farther than a hurried blade.'),('elin','Elin Reed','healer',70,191,'The town is a sanctuary. Return here to recover your strength.'),('sera','Sera Venn','mage',90,191,'The old beacon cellars descend twice. A lever opens the lower ward; the brass key opens the final vault.'),('tomas','Tomas Netter','resident',109,191,'I mend the nets while the tidehounds prowl the western shore.'),('ada','Ada Moss','resident',69,205,'I have lived here all my life. The cemetery road is quiet only by daylight.'),('brin','Brin Pike','resident',109,205,'I saw a salt hexer beyond Saint Orin\'s wall.'),('jory','Jory Bell','guard',81,158,'Beyond these streets the sanctuary ends. Keep a healing tonic ready.'),('lena','Lena Wren','guard',116,181,'There is a side path west into Morrowwood.'),('pella','Pella Ash','herbalist',66,181,'Saltfen water is bitter. Stay on the causeway.'),('ivo','Ivo Finch','scribe',97,181,'Signs and discovery marks are copied into your field journal.'),('cora','Cora Salt','fisher',88,225,'The dock is sound, but there are no boats sailing in this demo.'),('fen','Fen Low','dockhand',97,230,'The supply chests can be opened like your own backpack.'),('maeve','Maeve Thorn','resident',118,201,'The Lantern Keeper waits below the beacon. Take the upper cellar key first.')]
 for n,(id,name,role,x,y,text) in enumerate(people):obj('npc.'+id,'npc',name,x,y,role=role,actor=['humanoid-h2-s0-b0','humanoid-h1-s0-b0','armor-h1-s1-b1'][n%3],text=text)
 for id,name,x,y,txt in [('sign.square','Greyhaven Waystone',83,181,'NORTH: Broken Beacon and cellars. WEST: Morrowwood. EAST: Saltfen and Saint Orin. SOUTH: Gullhook docks.'),('sign.woods','Morrowwood Trail',50,124,'Morrowwood: wolves of the tide shelter beneath the trees.'),('sign.fen','Saltfen Causeway',139,145,'Keep to the raised path. Slingers watch the fen.'),('sign.beacon','Broken Beacon',89,55,'Beneath the beacon: the Drowned Cellars. Double-click the stair marker to descend.'),('sign.grave','Saint Orin Cemetery',146,100,'Rest for the lost; watch for the living.')]:obj(id,'sign',name,x,y,text=txt)
 # Beacon ruin and cemetery are hand laid landmarks.
 rect(81,39,18,16,'stone',8)
 for x in range(81,99):
  if x not in (88,89):prop('wall-left',x,39)
 for y in range(40,53):prop('wall-right',81,y)
 for x,y in [(97,40),(98,43),(97,47),(82,51)]:prop('wall-left',x,y)
 # Two-unit ramp joins the raised beacon court to the marked southern road.
 rect(86,55,5,1,'dirt',6)
 rect(86,56,5,1,'dirt',4)
 rect(132,78,23,20,'dirt')
 for y in [81,86,91]:
  for x in [135,140,145,150]:prop('gravestone',x,y)
 # Furnished interiors use the generated original Phase 6 prop atlas.
 for name,x,y in [('bed',66,167),('bed',66,171),('table',72,168),('stool',73,169),('stool',72,171),('workbench',87,167),('barrel',90,167),('barrel',94,168),('forge',111,167),('anvil',107,169),('workbench',105,172),('barrel',113,168),('bed',66,189),('bed',66,193),('table',74,189),('bookcase',87,189),('bookcase',95,189),('table',92,189),('bed',106,189),('table',112,189),('workbench',106,194),('bed',66,204),('table',72,204),('stool',72,205),('bed',105,204),('table',112,204),('stool',113,205),('barrel',93,230),('barrel',99,230)]:prop(name,x,y)
 # Authored forest clusters avoid streets, buildings and interaction cells.
 occupied={(o['x'],o['y']) for o in objects}|{(s['x'],s['y']) for s in statics}
 for y in range(30,230):
  for x in range(18,180):
   c=cells[y*w+x]
   if c['terrain']!=T+'grass' or (59<=x<=123 and y>=151) or any(abs(x-xx)<2 and abs(y-yy)<2 for xx,yy in occupied):continue
   chance=.12 if 25<x<72 and 70<y<151 else .015
   if rng.random()<chance:prop('tree' if rng.random()<.82 else 'shrub',x,y)
 # Two isolated dungeon floors occupy the eastern atlas, reached by paired stairs.
 for floor,base in [(1,24),(2,104)]:
  region('region.cellar'+str(floor),'Drowned Cellars - '+('Upper Vaults' if floor==1 else 'Lower Ward'),194,base-2,56,30,'dungeon')
  for row in range(2):
   for col in range(4):rect(196+col*13,base+row*13,10,10)
  rect(200,base+5,42,2);rect(200,base+18,42,2)
  for col in range(4):rect(200+col*13,base+5,2,15)
  # Outer floor edge walls; collision is water beyond floor.
  ground={(x,y) for y in range(base,base+24) for x in range(194,250) if cells[y*w+x]['terrain']==T+'stone'}
  for x,y in sorted(ground):
   if (x,y-1) not in ground:prop('wall-left',x,y-1);cell(x,y-1,'stone')
   if (x-1,y) not in ground:prop('wall-right',x-1,y);cell(x-1,y,'stone')
  for col in range(4):obj(f'cellar{floor}.light{col}','light','Vault lantern',198+col*13,base+2,visual='lantern')
  for row in range(2):
   for col in range(4):obj(f'cellar{floor}.room{row*4+col+1}','trigger',f'Vault {floor}.{row*4+col+1}',200+col*13,base+3+row*13,text=f'You entered vault {floor}.{row*4+col+1} of the Drowned Cellars.')
 obj('stairs.beacon','portal','Descend to the Drowned Cellars',89,46,visual='stone',targetId='stairs.upper',text='The stair spirals into cool darkness.')
 obj('stairs.upper','portal','Return to Broken Beacon',200,28,visual='stone',targetId='stairs.beacon',text='Salt air welcomes you back to the beacon.')
 obj('stairs.down','portal','Descend to the Lower Ward',239,44,visual='stone',targetId='stairs.lower',text='The lower ward waits beyond the arch.')
 obj('stairs.lower','portal','Ascend to Upper Vaults',200,108,visual='stone',targetId='stairs.down',text='You climb to the upper vaults.')
 obj('cellar.key','key','Brass Vault Key',215,28,visual='scroll',text='A brass key wrapped in a brittle vellum label.')
 obj('cellar.lock','door','Locked vault door',234,122,visual='door',requiredItemId='world.item.cellar.key',text='The brass vault key fits the ancient lock.')
 # Gate spans a one-cell passage carved in an otherwise solid wall of collision statics.
 for yy in range(104,128):
  if yy!=122 and cells[yy*w+232]['terrain']==T+'stone':prop('wall-right',232,yy)
 prop('wall-right',234,123)
 obj('cellar.gate','gate','Lower Ward portcullis',232,122,visual='door',text='The portcullis is controlled by the nearby tide lever.')
 obj('cellar.lever','lever','Tide Lever',226,120,visual='lantern',targetId='cellar.gate',text='Chains rattle. The Lower Ward portcullis rises.')
 obj('cellar.trap','trap','Cracked salt rune',213,122,visual='spell-venom',value=12,text='The salt rune bites. Its magic takes three seconds to recharge.')
 for id,name,x,y,loot in [('town.supplies','Dock Supplies',94,230,[('healing-tonic',3)]),('woods.cache','Woodland Cache',42,119,[('silver-marks',30),('tide-crystal',4)]),('cellar.chest1','Upper Vault Chest',240,28,[('healing-tonic',4),('silver-marks',40)]),('cellar.treasure','Warden\'s Strongbox',240,122,[('silver-marks',120),('tide-crystal',8)])]:
  cell(x,y,'stone' if x>190 or id.startswith('town') else 'grass');obj(id,'container',name,x,y,visual='chest',loot=[{'definition':I+k,'amount':a} for k,a in loot],text='Double-click the chest to open its inventory.')
 for id,name,x,y,text in [('discover.woods','Morrowwood',42,124,'The Morrowwood trail opens under a canopy of salt-bent trees.'),('discover.fen','Saltfen',148,145,'You reached Saltfen: reed beds and old stones flank the causeway.'),('discover.beacon','Broken Beacon',88,52,'The Broken Beacon overlooks the island. Its cellars are open below.'),('discover.grave','Saint Orin Cemetery',148,96,'Saint Orin\'s old graves have weathered more storms than the town.')]:obj(id,'trigger',name,x,y,text=text)
 encounters=[('woods.hound1','Morrowwood Tidehound','tidehound',44,121),('woods.hound2','Trail Tidehound','tidehound',66,125),('woods.raider','Woodland Raider','raider',52,108),('fen.slinger','Fen Slinger','slinger',148,139),('fen.raider','Causeway Raider','raider',155,124),('grave.hexer','Grave Hexer','hexer',148,96),('beacon.guard','Beacon Brineguard','brineguard',92,49),('upper.hound','Vault Tidehound','tidehound',204,42),('upper.slinger','Vault Slinger','slinger',227,28),('upper.hexer','Upper Salt Hexer','hexer',239,41),('lower.raider','Ward Raider','raider',215,108),('lower.hexer','Ward Hexer','hexer',225,122),('lower.boss','The Lantern Keeper','brineguard',239,120)]
 # Remove foliage at gameplay objects/encounters, leaving no authored object unreachable.
 for id,name,kind,x,y in encounters:
  cell(x,y,'stone' if x>190 else 'grass',cells[y*w+x]['z']);obj('encounter.'+id,'encounter',name,x,y,archetype=kind)
 protected={(o['x'],o['y']) for o in objects}
 statics[:]=[s for s in statics if (s['x'],s['y']) not in protected]
 # Ambient markers define visual/audio locations without external content dependencies.
 obj('amb.docks','ambient','Harbour wind',89,232,text='Wind brushes the harbour ropes.')
 obj('amb.woods','ambient','Woodland hush',45,124,text='Leaves rustle under the sea wind.')
 m={'schema':1,'id':'greyhaven.map.island','nameKey':'map.island','width':w,'height':h,'chunkSize':8,'spawn':{'x':86,'y':181,'z':0},'cells':cells,'statics':statics,'regions':regions,'roofs':roofs,'objects':objects}
 (OUT/'map.json').write_text(json.dumps(m,separators=(',',':'))+'\n')
 (ROOT/'docs/phase6/island-layout.json').write_text(json.dumps({'size':[w,h],'buildings':buildings,'townspeople':len(people),'dungeonFloors':2,'dungeonRooms':16,'encounters':len(encounters),'statics':len(statics),'objects':len(objects)},indent=2)+'\n')
 return m
if __name__=='__main__':
 m=build();print(f"Island: {m['width']}x{m['height']}, {len(m['statics'])} statics, {len(m['objects'])} runtime objects, {len(m['roofs'])} roof groups")

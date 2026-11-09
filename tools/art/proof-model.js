class ProofModel {
 constructor(){this.tile={x:0,y:0};this.from={x:0,y:0};this.progress=1;this.direction=3;this.clip='idle';this.outfit='humanoid';this.hat=1;this.sword=1;this.shield=1;this.paused=false;this.time=0;this.monster=false;this.teal=false;
 this.panels=[{id:'book',x:610,y:100,w:360,h:310},{id:'doll',x:1010,y:80,w:240,h:400},{id:'bag',x:960,y:420,w:270,h:360}];
 this.items=[{id:'leather',icon:'hat-leather',slot:'hat',value:1,where:'equipped'},{id:'wizard',icon:'hat-wizard',slot:'hat',value:2,where:'bag',x:68,y:128},{id:'blade',icon:'sword',slot:'sword',value:1,where:'equipped'},{id:'buckler',icon:'shield',slot:'shield',value:1,where:'equipped'},{id:'tonic',icon:'potion',where:'bag',x:127,y:136},{id:'gold',icon:'coins',where:'bag',x:173,y:207},{id:'gem',icon:'crystal',where:'ground',x:2,y:1},{id:'lore',icon:'scroll',where:'bag',x:80,y:236}];this.message='Drag items between the ground, backpack, and paperdoll.';this.drag=null;this.walking=false;}
 project(x,y){return {x:420+(x-y)*22,y:350+(x+y)*22};}
 unproject(x,y){return {x:Math.round(((x-420)+(y-350))/44),y:Math.round(((y-350)-(x-420))/44)};}
 step(dx,dy){if(this.paused||this.progress<1||(!dx&&!dy))return false;let x=this.tile.x+dx,y=this.tile.y+dy;if(Math.abs(x)>6||Math.abs(y)>6)return false;this.from={...this.tile};this.tile={x,y};this.progress=0;this.walking=true;const dirs={'-1,-1':0,'0,-1':1,'1,-1':2,'1,0':3,'1,1':4,'0,1':5,'-1,1':6,'-1,0':7};this.direction=dirs[`${dx},${dy}`];return true;}
 update(dt){if(this.paused)return;this.time+=dt;this.progress=Math.min(1,this.progress+dt/.2);if(this.progress===1)this.walking=false;}
 position(){const p=this.progress;return {x:this.from.x+(this.tile.x-this.from.x)*p,y:this.from.y+(this.tile.y-this.from.y)*p};}
 actor(){if(this.monster)return 'tidehound';if(this.teal)return 'armor-teal';return `${this.outfit}-h${this.hat}-s${this.sword}-b${this.shield}`;}
 panelAt(x,y){return [...this.panels].reverse().find(p=>x>=p.x&&x<p.x+p.w&&y>=p.y&&y<p.y+p.h);}
 itemPosition(it){if(it.where==='ground')return this.project(it.x,it.y);const p=this.panels.find(p=>p.id===(it.where==='bag'?'bag':'doll'));if(it.where==='bag')return {x:p.x+it.x,y:p.y+it.y};return {x:p.x+23,y:p.y+({hat:100,sword:165,shield:230}[it.slot])};}
 begin(x,y){const panel=this.panelAt(x,y);const it=[...this.items].reverse().find(it=>{const p=this.itemPosition(it);return Math.abs(x-p.x)<23&&Math.abs(y-p.y)<23&&(!panel?it.where==='ground':(panel.id==='bag'?it.where==='bag':panel.id==='doll'&&it.where==='equipped'));});if(it){this.drag={kind:'item',item:it,x,y};return;}
 if(panel&&y<panel.y+30){this.panels=this.panels.filter(p=>p!==panel).concat(panel);this.drag={kind:'panel',panel,dx:x-panel.x,dy:y-panel.y};}}
 move(x,y){if(!this.drag)return;if(this.drag.kind==='panel'){let p=this.drag.panel;p.x=Math.max(0,Math.min(1280-p.w,x-this.drag.dx));p.y=Math.max(58,Math.min(800-p.h,y-this.drag.dy));}else Object.assign(this.drag,{x,y});}
 syncGear(){for(const slot of ['hat','sword','shield'])this[slot]=this.items.find(i=>i.where==='equipped'&&i.slot===slot)?.value||0;}
 drop(x,y){if(!this.drag)return false;const drag=this.drag;this.drag=null;if(drag.kind==='panel')return true;const it=drag.item,p=this.panelAt(x,y);let ok=false;
 if(p?.id==='bag'){Object.assign(it,{where:'bag',x:Math.max(42,Math.min(p.w-42,x-p.x)),y:Math.max(100,Math.min(p.h-50,y-p.y))});ok=true;}
 else if(p?.id==='doll'&&it.slot){const old=this.items.find(i=>i!==it&&i.where==='equipped'&&i.slot===it.slot);if(old)Object.assign(old,{where:'bag',x:60,y:170});it.where='equipped';ok=true;}
 else if(!p&&y>70){const t=this.unproject(x,y);if(Math.abs(t.x)<=6&&Math.abs(t.y)<=6){Object.assign(it,{where:'ground',...t});ok=true;}}
 this.syncGear();this.message=ok?`${it.id}: ${it.where}`:'Drop cancelled; item returned to its previous location.';return ok;}
}
if(typeof module!=='undefined')module.exports=ProofModel;

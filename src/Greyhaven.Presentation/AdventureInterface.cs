// SPDX-License-Identifier: MIT
using Greyhaven.Content;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
namespace Greyhaven.Presentation;
public sealed partial class OriginalGame {
 readonly Panel questGump=new("quests",220,105,840,642,false),skillsGump=new("skills",200,115,880,620,false);int questRow;string talkingNpc="",trackedQuest="";bool beaconChoice;readonly Dictionary<string,EnemyVisual> npcViews=[];
 Point WorldPosition(MapObject o){var n=!editor?AdventureRules.Npc(state,o.Id):null;return n==null?new(o.X,o.Y):new(n.X,n.Y);}
 void AdventureSnapshot(){foreach(var n in state.Adventure.Npcs){if(!npcViews.TryGetValue(n.Id,out var v)){v=new(){From=new(n.X,n.Y),To=new(n.X,n.Y),At=clock,Duration=.8};npcViews[n.Id]=v;}if(v.To!=new Vector2(n.X,n.Y)){v.From=v.Position(clock);v.To=new(n.X,n.Y);v.At=clock;}}}
WorldItem? questContext;Vector2 contextAt;
 Panel? AdventureAt(Vector2 p)=>skillsGump.Contains(p)?skillsGump:questGump.Contains(p)?questGump:null;
 List<QuestDefinition> QuestRows()=>state.Adventure.Quests.Select(q=>package.AdventureCatalog.Quests.First(d=>d.Id==q.Id)).ToList();
 QuestProgress? Progress(string id)=>state.Adventure.Quests.FirstOrDefault(q=>q.Id==id);
 void AdventureInput(KeyboardState k,MouseState mouse){if(editor||state.Paused||TextCapture)return;if(Press(k,Keys.Q)){questGump.Visible=!questGump.Visible;skillsGump.Visible=false;conversation="";}if(Press(k,Keys.K)){skillsGump.Visible=!skillsGump.Visible;questGump.Visible=false;conversation="";}if(Press(k,Keys.F10))SendInventory(new(){Type="meditate"});if(Press(k,Keys.Y))SendInventory(new(){Type="bandage"});if(mouse.RightButton==ButtonState.Pressed&&oldMouse.RightButton==ButtonState.Released&&At(pointer)?.Id is "bag" or "doll"||mouse.RightButton==ButtonState.Pressed&&oldMouse.RightButton==ButtonState.Released&&At(pointer)?.Id.StartsWith("container:")==true){var i=HitItem(pointer);if(i!=null&&InventoryRules.Carried(state,i)){questContext=i;contextAt=new(Math.Clamp(pointer.X,0,1000),Math.Clamp(pointer.Y,110,650));}}
 }
 void TalkAdventure(MapObject o){if(o.Kind!="npc")return;talkingNpc=o.Id;var rows=QuestRows();int index=rows.FindIndex(q=>(q.Giver==o.Id||q.Recipient==o.Id)&&Progress(q.Id)?.Status!="claimed");if(index>=0){questRow=index;questGump.Visible=true;skillsGump.Visible=false;conversation="";}}
 void QuestAction(string type,QuestDefinition d,string item=""){SendInventory(new(){Type=type,TargetId=d.Id,ItemId=item,ExpectedRevision=state.Revision});}
 bool AdventureBegin(Vector2 p){
  if(beaconChoice&&conversation!=""&&p.X>=34&&p.X<620&&p.Y>=610&&p.Y<640){if(p.X<310)SendInventory(new(){Type="restore-beacon"});else SendInventory(new(){Type="interact",TargetId="stairs.beacon"});beaconChoice=false;conversation="";return true;}
  if(questContext!=null){var item=InventoryRules.Find(state,questContext.Id);questContext=null;if(item!=null&&p.X>=contextAt.X&&p.X<contextAt.X+270&&p.Y>=contextAt.Y&&p.Y<contextAt.Y+65){var q=item.QuestId!=""?Progress(item.QuestId):state.Adventure.Quests.FirstOrDefault(q=>q.Status is "active" or "ready"&&package.AdventureCatalog.Quests.First(d=>d.Id==q.Id).Objectives.Any(o=>o.Kind=="obtain"&&o.Target==item.Definition));if(q!=null)QuestAction("quest-mark",package.AdventureCatalog.Quests.First(d=>d.Id==q.Id),item.Id);else message="No accepted supply quest needs this item.";return true;}}
  var g=AdventureAt(p);if(g==null){if(conversation!=""&&talkingNpc!=""&&p.Y>=610&&p.Y<640&&p.X>=30&&p.X<350){if(p.X<190){questGump.Visible=true;skillsGump.Visible=false;}else{skillsGump.Visible=true;questGump.Visible=false;}conversation="";return true;}return false;}
  if(p.X>g.X+g.W-28&&p.Y<g.Y+30){g.Visible=false;return true;}if(p.Y<g.Y+30){moving=g;dragOffset=p-new Vector2(g.X,g.Y);return true;}
  if(g==questGump){var rows=QuestRows();if(rows.Count==0)return true;if(p.X<g.X+236&&p.Y>=g.Y+70&&p.Y<g.Y+70+rows.Count*85)questRow=Math.Clamp(((int)p.Y-g.Y-70)/85,0,rows.Count-1);var d=rows[Math.Clamp(questRow,0,rows.Count-1)];var q=Progress(d.Id)!;
   if(p.Y>=g.Y+550&&p.Y<g.Y+580&&p.X>=g.X+258){if(p.X<g.X+490)QuestAction("quest-mark",d);else{trackedQuest=d.Id;message="Tracking: "+d.Name;}}
   if(p.Y>=g.Y+590&&p.Y<g.Y+622){if(p.X>=g.X+258&&p.X<g.X+438){if(q.Status=="available")QuestAction("quest-accept",d);else if(q.Status=="ready")QuestAction("quest-claim",d);else message="Complete the objectives, then return to the quest recipient.";}else if(p.X<g.X+628&&p.X>=g.X+448){if(q.Status is "active" or "ready")QuestAction("quest-abandon",d);}else if(p.X>=g.X+640){skillsGump.Visible=true;questGump.Visible=false;}}
  }else{
   int row=((int)p.Y-g.Y-100)/33;if(p.Y>=g.Y+100&&row>=0&&row<state.Adventure.Skills.Count){var k=state.Adventure.Skills[row];if(p.X>=g.X+570&&p.X<g.X+665){string next=k.Lock=="up"?"down":k.Lock=="down"?"locked":"up";SendInventory(new(){Type="skill-lock",ItemId=k.Id,Destination=next});}else if(p.X>=g.X+680)SendInventory(new(){Type="train",TargetId=talkingNpc,ItemId=k.Id,Amount=10,ExpectedRevision=state.Revision});}
   if(p.Y>=g.Y+493&&p.Y<g.Y+525){int col=Math.Clamp(((int)p.X-g.X-20)/270,0,2);string id=new[]{"str","dex","int"}[col],old=col==0?state.Adventure.StrengthLock:col==1?state.Adventure.DexterityLock:state.Adventure.IntelligenceLock;SendInventory(new(){Type="stat-lock",ItemId=id,Destination=old=="up"?"down":old=="down"?"locked":"up"});}
   if(p.Y>=g.Y+560&&p.Y<g.Y+600){if(p.X<g.X+300)SendInventory(new(){Type="meditate"});else if(p.X<g.X+570)SendInventory(new(){Type="bandage"});else{questGump.Visible=true;skillsGump.Visible=false;}}
  }return true;
 }
 void AdventureFrame(Panel p,string name){Rect(p.X+7,p.Y+8,p.W,p.H,ColorOf(0,0,0,140));PanelFrame(p==questGump?"gump-quests":"gump-skills",p.X,p.Y,p.W,p.H);Rect(p.X+64,p.Y+4,p.W-100,28,ColorOf(25,39,43,190));Text(name,p.X+76,p.Y+7,Color.Gold);Text("x",p.X+p.W-22,p.Y+6);}
 void Button(int x,int y,int w,string label){Rect(x,y,w,29,ColorOf(96,78,45));Text(label,x+8,y+5,ColorOf(239,218,167));}
 void DrawAdventure(){if(editor)return;var a=state.Adventure;if(a.Enabled){Text(AdventureRules.Clock(state)+" | "+AdventureRules.Title(state),750,98,ColorOf(220,196,145));if(a.Meditating)Text("MEDITATING",750,119,Color.Cyan);if(a.BandageAt>0)Text($"BANDAGING {Math.Max(0,a.BandageAt-state.ElapsedSeconds):0.0}s",750,139,Color.LightGreen);if(a.BeaconRestored)Text("BEACON RESTORED",20,305,Color.Gold);}
  if(beaconChoice&&conversation!=""){Button(34,610,260,"Install Lantern Heart");Button(324,610,290,"Descend to cellars");}
  if(conversation!=""&&talkingNpc!=""){Button(34,610,146,"Quests [Q]");Button(196,610,146,"Skills [K]");}
  if(questGump.Visible){var p=questGump;AdventureFrame(p,"QUEST JOURNAL");var rows=QuestRows();if(rows.Count==0){Wrap("This saved map has no Greyhaven adventure cast. Start a new island campaign to play the town quests.",p.X+25,p.Y+70,80);return;}questRow=Math.Clamp(questRow,0,rows.Count-1);for(int i=0;i<rows.Count;i++){var q=Progress(rows[i].Id)!;if(i==questRow)Rect(p.X+24,p.Y+64+i*85,212,80,ColorOf(70,68,45));Wrap(rows[i].Name,p.X+32,p.Y+71+i*85,21,Color.Gold);Text(q.Status.ToUpperInvariant(),p.X+32,p.Y+117+i*85,ColorOf(155,193,181));}
   var d=rows[questRow];var progress=Progress(d.Id)!;Text(d.Name,p.X+258,p.Y+43,Color.Gold);string npc=package.Map.Objects.First(o=>o.Id==(progress.Status=="ready"?d.Recipient:d.Giver)).Name;Text((progress.Status=="ready"?"Reward from ":"Quest giver: ")+npc,p.X+258,p.Y+68);Wrap(progress.Status=="claimed"?d.Completion:d.Description,p.X+258,p.Y+98,61);
   for(int i=0;i<d.Objectives.Count;i++){var o=d.Objectives[i];bool done=progress.Counts[i]>=o.Amount;Text((done?"[+] ":"[ ] ")+o.Text,p.X+258,p.Y+286+i*27,done?ColorOf(150,199,153):ColorOf(224,216,190));if(o.Amount>1)Text($"{progress.Counts[i]}/{o.Amount}",p.X+p.W-48,p.Y+286+i*27,Color.Gold);}
   string rewards=string.Join(", ",d.Rewards.Select(r=>r.Amount+" "+package.Localize(itemDefs[r.Definition].NameKey)));Wrap("Rewards: "+rewards+" | Fame "+d.Fame+", karma "+d.Karma,p.X+258,p.Y+505,61,ColorOf(188,204,185));Button(p.X+258,p.Y+550,230,"Mark required supplies");Button(p.X+505,p.Y+550,300,"Track next objective");Button(p.X+258,p.Y+590,180,progress.Status=="available"?"Accept quest":progress.Status=="ready"?"Collect reward":progress.Status=="claimed"?"Completed":"In progress");Button(p.X+448,p.Y+590,180,"Abandon");Button(p.X+640,p.Y+590,170,"Skills / training");
  }
  if(skillsGump.Visible){var p=skillsGump;AdventureFrame(p,"SKILLS AND CHARACTER");if(!a.Enabled){Text("Progression belongs to the Greyhaven island adventure.",p.X+25,p.Y+70);return;}Text($"Skill total {a.Skills.Sum(k=>k.Value)/10.0:0.0}/700.0 | Individual cap 100.0",p.X+20,p.Y+45,Color.Gold);Text("Skill                         Value / cap     Gain lock      Training",p.X+20,p.Y+76);
   for(int i=0;i<a.Skills.Count;i++){var k=a.Skills[i];var d=AdventureRules.Skills.First(d=>d.Id==k.Id);int y=p.Y+100+i*33;Text(d.Name,p.X+20,y+4);Text($"{k.Value/10.0:0.0} / 100.0",p.X+340,y+4);Button(p.X+570,y,95,k.Lock=="up"?"GAIN":k.Lock=="down"?"LOWER":"LOCK");bool teach=package.AdventureCatalog.Trainers.Any(t=>t.NpcId==talkingNpc&&t.Skill==k.Id);if(teach)Button(p.X+680,y,175,"Train +1.0 (10m)");}
   Text($"Stats {a.Strength+a.Dexterity+a.Intelligence}/225 | Individual cap 125",p.X+20,p.Y+470,Color.Gold);Button(p.X+20,p.Y+493,250,$"STR {a.Strength} [{a.StrengthLock}]");Button(p.X+290,p.Y+493,250,$"DEX {a.Dexterity} [{a.DexterityLock}]");Button(p.X+560,p.Y+493,285,$"INT {a.Intelligence} [{a.IntelligenceLock}]");Text($"Fame {a.Fame} | Karma {a.Karma} | {AdventureRules.Title(state)}",p.X+20,p.Y+533,ColorOf(188,204,185));Button(p.X+20,p.Y+560,250,"Meditate [F10]");Button(p.X+290,p.Y+560,250,"Bandage self [Y]");Button(p.X+560,p.Y+560,285,"Open quest journal");
  }
  if(questContext!=null){Rect((int)contextAt.X,(int)contextAt.Y,270,65,ColorOf(18,33,38,250));Text(questContext.QuestId==""?"Mark quest item":"Unmark quest item",(int)contextAt.X+10,(int)contextAt.Y+12,Color.Gold);Text("Click to apply; Esc cancels",(int)contextAt.X+10,(int)contextAt.Y+38);}
  if(trackedQuest!=""&&!questGump.Visible&&!skillsGump.Visible){var q=Progress(trackedQuest);var d=package.AdventureCatalog.Quests.FirstOrDefault(d=>d.Id==trackedQuest);if(q!=null&&d!=null){int n=d.Objectives.FindIndex(o=>q.Counts[d.Objectives.IndexOf(o)]<o.Amount);string text=q.Status=="ready"?"Return to "+package.Map.Objects.First(o=>o.Id==d.Recipient).Name:q.Status=="claimed"?"Quest completed":n>=0?d.Objectives[n].Text:d.Name;Rect(20,332,320,84,ColorOf(16,29,33,225));Text("TRACKED QUEST [Q]",30,340,Color.Gold);Wrap(text,30,366,33);}}
 }
 int adventureSmokeStep;bool adventureSmokeDone;double adventureSmokeSeconds;
 void PrepareAdventureScene(){if(editor||options.AdventureScene==null)return;foreach(var p in panels)p.Visible=false;help=false;talkingNpc="npc.orrin";questRow=Math.Max(0,QuestRows().FindIndex(q=>q.Id==(options.AdventureScene=="main"?"quest.beacon":"quest.forge")));questGump.Visible=options.AdventureScene!="skills";skillsGump.Visible=!questGump.Visible;}
 void RunAdventureSmoke(float dt){if(!options.AdventureSmoke||adventureSmokeDone)return;adventureSmokeSeconds+=dt;if(adventureSmokeSeconds>20)throw new ContentException("Native adventure smoke timed out: "+adventureSmokeStep);if(session?.Busy!=false)return;if(state.Paused&&adventureSmokeStep!=10){SendInventory(new(){Type="pause",Destination="resume"});return;}
  void Require(bool ok,string why){if(!ok)throw new ContentException("Native adventure route: "+why);}
  switch(adventureSmokeStep){
   case 0:foreach(var p in panels)p.Visible=false;help=false;TalkAdventure(package.Map.Objects.First(o=>o.Id=="npc.orrin"));questRow=QuestRows().FindIndex(q=>q.Id=="quest.forge");AdventureBegin(new(questGump.X+280,questGump.Y+600));break;
   case 1:Require(Progress("quest.forge")?.Status=="active","accept");AdventureBegin(new(questGump.X+280,questGump.Y+555));break;
   case 2:Require(Progress("quest.forge")?.Status=="ready","mark supplies");AdventureBegin(new(questGump.X+280,questGump.Y+600));break;
   case 3:Require(Progress("quest.forge")?.Status=="claimed","claim once");AdventureBegin(new(questGump.X+660,questGump.Y+600));break;
   case 4:Require(skillsGump.Visible&&!questGump.Visible,"switch gumps");AdventureBegin(new(skillsGump.X+590,skillsGump.Y+110));break;
   case 5:Require(AdventureRules.Skill(state,"swords").Lock=="down","gain lock");AdventureBegin(new(skillsGump.X+590,skillsGump.Y+110));break;
   case 6:Require(AdventureRules.Skill(state,"swords").Lock=="locked","locked skill");AdventureBegin(new(skillsGump.X+590,skillsGump.Y+110));break;
   case 7:Require(AdventureRules.Skill(state,"swords").Lock=="up","gain skill");AdventureBegin(new(skillsGump.X+700,skillsGump.Y+110));break;
   case 8:Require(AdventureRules.Skill(state,"swords").Value==310,"paid training");SendInventory(new(){Type="save",Destination="quick"});break;
   case 9:foreach(var visual in npcViews.Values){visual.From=Vector2.Zero;visual.To=Vector2.Zero;}SendInventory(new(){Type="load",Destination="quick"});break;
   case 10:Require(state.Paused&&state.Adventure.Npcs.All(n=>npcViews.TryGetValue(n.Id,out var v)&&v.From==new Vector2(n.X,n.Y)&&v.To==v.From),"loaded resident poses snap while paused");SendInventory(new(){Type="pause",Destination="resume"});break;
   case 11:adventureSmokeDone=true;Console.WriteLine("Native adventure routes: quest accept, supply mark, reward claim, movable gump switch, gain/decrease/lock cycle, paid trainer and paused reload pose reset passed.");break;
  }adventureSmokeStep++;
 }

}

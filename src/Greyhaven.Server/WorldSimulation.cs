// GPL-2.0: original gameplay objects hosted by the ServUO-linked world.
using Greyhaven.Content;
using System.Text.Json;
namespace Greyhaven.Server;
public static class WorldSimulation {
 public static void Initialize(ContentPackage p,SessionState s){if(s.World.Version==1)return;s.World=new(){Version=1};foreach(var o in p.Map.Objects){s.World.Objects.Add(new(){Id=o.Id});if(o.Kind is "container" or "key"){
  string id=WorldAuthoring.ItemId(o);if(s.Items.Any(i=>i.Id==id))continue;
  s.Items.Add(new(){Id=id,Definition=o.Kind=="key"?"greyhaven.item.sealed-scroll":"greyhaven.item.sea-chest",Where="ground",X=o.X,Y=o.Y,Z=p.Map.Get(o.X,o.Y).Z});
  int n=0;foreach(var l in o.Loot)s.Items.Add(new(){Id=id+".loot."+n,Definition=l.Definition,Amount=l.Amount,Where="container",ParentId=id,X=60+n++%3*55,Y=130+n/3*45});
 }}WorldAuthoring.ValidateProgress(p,s);InventoryRules.Validate(p,s);}
 public static (SessionState state,string message) Interact(ContentPackage p,SessionState original,Command cmd){
  if(original.Combat.Dead&&p.Map.Objects.Any(o=>o.Id==cmd.TargetId&&o.Kind=="npc"&&o.Role=="healer"))return AdventureSimulation.Apply(p,original,new(){Type="resurrect",TargetId=cmd.TargetId});
  if(original.Paused||original.Combat.Dead)throw new ContentException("Resume and recover before interacting.");
  var o=p.Map.Objects.FirstOrDefault(o=>o.Id==cmd.TargetId)??throw new ContentException("Unknown world interaction.");
  var at=o.Kind=="npc"?AdventureRules.Position(original,o):(o.X,o.Y);if(CombatRules.Distance(original.X,original.Y,at.Item1,at.Item2)>2)throw new ContentException("Move within two tiles to interact.");
  var s=JsonSerializer.Deserialize<SessionState>(Wire.Encode(original),ContentPackage.Json)!;var progress=WorldAuthoring.Progress(s,o.Id)??throw new ContentException("Object progress is missing.");string message=o.Text!=""?o.Text:o.Name;
  if(o.Kind is "door" or "gate"){
   if(o.Kind=="gate")throw new ContentException("Find the lever that controls this gate.");
   if(o.RequiredItemId!=""&&!s.Items.Any(i=>i.Id==o.RequiredItemId&&InventoryRules.Carried(s,i)))throw new ContentException("This door needs the brass vault key.");
   if(progress.Active&&(s.X==o.X&&s.Y==o.Y||s.Combat.Enemies.Any(e=>e.Health>0&&e.X==o.X&&e.Y==o.Y)))throw new ContentException("The doorway is occupied.");
   progress.Active=!progress.Active;message=o.Name+(progress.Active?" opened.":" closed.");
  }else if(o.Kind=="lever"){var target=WorldAuthoring.Progress(s,o.TargetId)??throw new ContentException("Broken lever target.");target.Active=true;progress.Active=true;}
  else if(o.Kind=="portal"){
   var dest=p.Map.Objects.First(d=>d.Id==o.TargetId);if(!WorldAuthoring.CanStand(p,s,dest.X,dest.Y)||s.Combat.Enemies.Any(e=>e.Health>0&&e.X==dest.X&&e.Y==dest.Y))throw new ContentException("The destination stair is occupied.");
   CombatSimulation.Interrupt(s);s.X=dest.X;s.Y=dest.Y;s.Z=p.Map.Get(dest.X,dest.Y).Z;s.Combat.TargetId="";s.Combat.WarMode=false;
  }else if(o.Kind=="npc"&&o.Role=="healer"){s.Combat.Health=100;s.Combat.Status=new();message=o.Name+" tends your wounds.";}
  else if(o.Kind is "trap" or "encounter")throw new ContentException("That is not a conversation or usable object.");
  InventoryTransactions.Log(s,message);s.Revision++;WorldAuthoring.ValidateProgress(p,s);CombatRules.Validate(p,s);InventoryRules.Validate(p,s);return(s,message);
 }
 public static void Enter(ContentPackage p,SessionState s){
  foreach(var o in p.Map.Objects.Where(o=>o.Kind=="trigger"&&CombatRules.Distance(s.X,s.Y,o.X,o.Y)<=1))if(!s.World.Visited.Contains(o.Id)){s.World.Visited.Add(o.Id);InventoryTransactions.Log(s,o.Text);}
  foreach(var o in p.Map.Objects.Where(o=>o.Kind=="trap"&&s.X==o.X&&s.Y==o.Y)){var progress=WorldAuthoring.Progress(s,o.Id);if(progress!=null&&progress.ReadyAt<=s.ElapsedSeconds){progress.ReadyAt=s.ElapsedSeconds+3;CombatSimulation.EnvironmentalDamage(p,s,o.Value);InventoryTransactions.Log(s,o.Text);}}
 }
}

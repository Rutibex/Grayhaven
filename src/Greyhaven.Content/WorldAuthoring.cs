// SPDX-License-Identifier: MIT
namespace Greyhaven.Content;
public sealed class MapArea {
 public string Id {get;set;}="";public string Name {get;set;}="";public string Kind {get;set;}="wilderness";public int X {get;set;}public int Y {get;set;}public int Width {get;set;}public int Height {get;set;}public bool Safe {get;set;}public string Visual {get;set;}="stone";
 public bool Contains(int x,int y)=>x>=X&&y>=Y&&x<X+Width&&y<Y+Height;
}
public sealed class MapLoot {public string Definition {get;set;}="";public int Amount {get;set;}=1;}
public sealed class MapObject {
 public string Id {get;set;}="";public string Kind {get;set;}="sign";public string Name {get;set;}="";public string Text {get;set;}="";public int X {get;set;}public int Y {get;set;}
 public string Visual {get;set;}="lantern";public string Actor {get;set;}="humanoid-h2-s0-b0";public string Archetype {get;set;}="tidehound";public string Role {get;set;}="resident";public string TargetId {get;set;}="";public string RequiredItemId {get;set;}="";public int Value {get;set;}=8;public List<MapLoot> Loot {get;set;}=[];
}
public sealed class ObjectProgress {public string Id {get;set;}="";public bool Active {get;set;}public double ReadyAt {get;set;}}
public sealed class WorldProgress {public int Version {get;set;}public List<ObjectProgress> Objects {get;set;}=[];public List<string> Visited {get;set;}=[];}
public static class WorldAuthoring {
 public static readonly string[] Kinds=["npc","encounter","sign","door","gate","container","key","trap","lever","portal","trigger","light","ambient"];
 public static string ItemId(MapObject o)=>"world.item."+o.Id;
 public static bool IsKey(ContentPackage p,string id)=>p.Map.Objects.Any(o=>o.Kind=="key"&&ItemId(o)==id);
 public static string ItemName(ContentPackage p,WorldItem item)=>item.Label!=""?item.Label:p.Map.Objects.FirstOrDefault(o=>ItemId(o)==item.Id)?.Name??p.Localize(p.Definitions.Items.First(d=>d.Id==item.Definition).NameKey);
 public static string EnemyName(ContentPackage p,EnemyState enemy)=>p.Map.Objects.FirstOrDefault(o=>o.Id==enemy.Id&&o.Kind=="encounter")?.Name??CombatRules.Enemy(enemy.Kind).Name;
 public static string AreaName(WorldMap map,int x,int y)=>map.Regions.LastOrDefault(r=>r.Contains(x,y))?.Name??"Open coast";
 public static bool Safe(WorldMap map,int x,int y)=>map.Regions.Any(r=>r.Safe&&r.Contains(x,y));
 public static ObjectProgress? Progress(SessionState s,string id)=>s.World.Objects.FirstOrDefault(o=>o.Id==id);
 public static bool Blocks(ContentPackage p,SessionState s,int x,int y)=>p.Map.Objects.Any(o=>o.X==x&&o.Y==y&&(o.Kind is "door" or "gate")&&Progress(s,o.Id)?.Active!=true);
 public static bool CanStand(ContentPackage p,SessionState s,int x,int y)=>p.CanStand(p.Map,x,y)&&!Blocks(p,s,x,y);
 public static void Validate(ContentPackage p,WorldMap map) {
  if(map.Regions is null||map.Roofs is null||map.Objects is null||map.Objects.Count>512||map.Regions.Count>256||map.Roofs.Count>256)throw new ContentException("Invalid world metadata collections.");
  var ids=new HashSet<string>(StringComparer.Ordinal);
  void Id(string id){if(string.IsNullOrWhiteSpace(id)||id.Length>80||!id.All(c=>char.IsAsciiLetterOrDigit(c)||c is '.' or '-' or '_')||!ids.Add(id))throw new ContentException("Invalid or duplicate world identity: "+id);}
  foreach(var a in map.Regions.Concat(map.Roofs)) {if(a==null)throw new ContentException("Null map area.");Id(a.Id);if(string.IsNullOrWhiteSpace(a.Name)||a.Name.Length>80||a.Width<1||a.Height<1||!map.Contains(a.X,a.Y)||!map.Contains(a.X+a.Width-1,a.Y+a.Height-1))throw new ContentException("Invalid area bounds: "+a.Id);p.ImagePath(a.Visual);}
  var objects=new Dictionary<string,MapObject>();var actors=new HashSet<(int,int)>();
  foreach(var o in map.Objects){if(o==null)throw new ContentException("Null world object.");Id(o.Id);objects.Add(o.Id,o);if(!Kinds.Contains(o.Kind)||!map.Contains(o.X,o.Y)||!p.CanStand(map,o.X,o.Y)||string.IsNullOrWhiteSpace(o.Name)||o.Name.Length>80||o.Text is null||o.Text.Length>240||o.TargetId is null||o.RequiredItemId is null||o.Actor is null||o.Visual is null||o.Role is null||o.Archetype is null||o.Loot is null||o.Loot.Count>20||o.Value<0||o.Value>100)throw new ContentException("Invalid world object: "+o.Id);p.ImagePath(o.Visual);if(o.Kind=="npc")p.ActorPath(o.Actor);if(o.Kind=="encounter"){CombatRules.Enemy(o.Archetype);if(Safe(map,o.X,o.Y)||CombatRules.Distance(o.X,o.Y,map.Spawn.X,map.Spawn.Y)<=3)throw new ContentException("Encounter placed in sanctuary: "+o.Id);}if(o.Kind is "npc" or "encounter"&&!actors.Add((o.X,o.Y)))throw new ContentException("Overlapping actor placements.");foreach(var l in o.Loot){if(l is null)throw new ContentException("Null loot entry.");var def=p.Definitions.Items.FirstOrDefault(i=>i.Id==l.Definition)??throw new ContentException("Unknown loot definition.");if(l.Amount<1||l.Amount>9999||!def.Stackable&&l.Amount!=1)throw new ContentException("Invalid world loot quantity.");}}
  foreach(var o in map.Objects){if(o.TargetId!=""&&(!objects.TryGetValue(o.TargetId,out var target)||o.Kind=="portal"&&target.Kind!="portal"||o.Kind=="lever"&&target.Kind!="gate"))throw new ContentException("Broken world target: "+o.Id);if(o.Kind is "portal" or "lever"&&o.TargetId=="")throw new ContentException("Missing interaction target: "+o.Id);if(o.RequiredItemId!=""&&!map.Objects.Any(k=>k.Kind=="key"&&ItemId(k)==o.RequiredItemId))throw new ContentException("Unknown key reference: "+o.Id);if(o.Kind=="portal"&&o.TargetId==o.Id)throw new ContentException("Portal cannot target itself.");}
  if(map.Objects.Where(o=>o.Kind=="portal").GroupBy(o=>(o.X,o.Y)).Any(g=>g.Count()>1))throw new ContentException("Overlapping stair markers.");
  if(map.Objects.Count(o=>o.Kind=="encounter")>64)throw new ContentException("At most 64 encounters are supported.");
  if(map.Objects.Any(o=>o.X==map.Spawn.X&&o.Y==map.Spawn.Y&&o.Kind is "door" or "gate" or "encounter" or "trap"))throw new ContentException("Spawn overlaps a blocking gameplay object.");
 }
 public static void ValidateProgress(ContentPackage p,SessionState s){var w=s.World;if(w is null||w.Version is <0 or >1||w.Objects is null||w.Visited is null||w.Objects.Count>512||w.Visited.Count>256||w.Objects.Any(o=>o is null||o.Id is null)||w.Objects.Select(o=>o.Id).Distinct().Count()!=w.Objects.Count||w.Visited.Distinct().Count()!=w.Visited.Count)throw new ContentException("Invalid world progress.");foreach(var o in w.Objects)if(!p.Map.Objects.Any(d=>d.Id==o.Id)||!double.IsFinite(o.ReadyAt)||o.ReadyAt<0||o.ReadyAt>s.ElapsedSeconds+3600)throw new ContentException("Unknown world progress or timer.");if(w.Visited.Any(id=>!p.Map.Objects.Any(o=>o.Id==id&&o.Kind=="trigger")))throw new ContentException("Unknown visited trigger.");}
 // Reachability treats doors/gates as author-openable, and follows portals. Runtime locks are checked separately.
 public static HashSet<(int x,int y)> Reachable(ContentPackage p,WorldMap map){var seen=new HashSet<(int,int)>{(map.Spawn.X,map.Spawn.Y)};var q=new Queue<(int x,int y)>();q.Enqueue((map.Spawn.X,map.Spawn.Y));var links=map.Objects.Where(o=>o.Kind=="portal").ToDictionary(o=>(o.X,o.Y),o=>map.Objects.First(t=>t.Id==o.TargetId));while(q.TryDequeue(out var t)){foreach(var v in new[]{(1,0),(-1,0),(0,1),(0,-1)})if(p.CanStep(map,t.x,t.y,v.Item1,v.Item2)&&seen.Add((t.x+v.Item1,t.y+v.Item2)))q.Enqueue((t.x+v.Item1,t.y+v.Item2));if(links.TryGetValue(t,out var d)&&seen.Add((d.X,d.Y)))q.Enqueue((d.X,d.Y));}return seen;}
 public static List<string> NavigationIssues(ContentPackage p,WorldMap map){var reached=Reachable(p,map);return map.Objects.Where(o=>o.Kind is not ("light" or "ambient")&&!reached.Contains((o.X,o.Y))).Select(o=>"Unreachable "+o.Kind+": "+o.Name+" ("+o.Id+")").ToList();}
}

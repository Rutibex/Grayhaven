// SPDX-License-Identifier: MIT
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Greyhaven.Content;

public sealed class ContentException(string message) : Exception(message);
public sealed class Definition {
 public string Id {get;set;}="";public int NumericId {get;set;}public string NameKey {get;set;}="";public string Visual {get;set;}="";
 public bool Impassable {get;set;}public bool Surface {get;set;}public bool Wet {get;set;}public int Height {get;set;}
}
public sealed class Equipment {public bool TwoHanded {get;set;}public int RequiredStrength {get;set;}public int RequiredIntelligence {get;set;}public string Slot {get;set;}="";public int VariantValue {get;set;}}
public sealed class ItemDefinition {public bool Anchored {get;set;}public int ContainerSlots {get;set;}public decimal ContainerWeight {get;set;}public int Price {get;set;}=1;
 public string Id {get;set;}="";public string NameKey {get;set;}="";public string GroundVisual {get;set;}="";public string InventoryVisual {get;set;}="";public Equipment? Equipment {get;set;}public decimal Weight {get;set;}public bool Stackable {get;set;}
}
public sealed class SpellDefinition {public string Id {get;set;}="";public string NameKey {get;set;}="";public string Visual {get;set;}="";public bool RulesEnabled {get;set;}}
public sealed class Definitions {
 public List<Definition> Terrain {get;set;}=[];public List<Definition> Statics {get;set;}=[];public List<ItemDefinition> Items {get;set;}=[];public List<SpellDefinition> Spells {get;set;}=[];
 public List<string> Abilities {get;set;}=[];public List<string> Prefabs {get;set;}=[];public Dictionary<string,string> Palette {get;set;}=[];public Dictionary<string,bool> Features {get;set;}=[];
}
public sealed class Cell {public string Terrain {get;set;}="";public int Z {get;set;}}
public sealed class Position {public int X {get;set;}public int Y {get;set;}public int Z {get;set;}}
public sealed class Placement {public string Id {get;set;}="";public string Definition {get;set;}="";public int X {get;set;}public int Y {get;set;}public int Z {get;set;}}
public sealed class WorldMap {
 public int Schema {get;set;}=1;public string Id {get;set;}="";public string NameKey {get;set;}="";public int Width {get;set;}public int Height {get;set;}public int ChunkSize {get;set;}=8;
 public List<MapArea> Regions {get;set;}=[];public List<MapArea> Roofs {get;set;}=[];public List<MapObject> Objects {get;set;}=[];
 public Position Spawn {get;set;}=new();public List<Cell> Cells {get;set;}=[];public List<Placement> Statics {get;set;}=[];
 public bool Contains(int x,int y)=>x>=0&&y>=0&&x<Width&&y<Height;
 public Cell Get(int x,int y)=>Contains(x,y)?Cells[y*Width+x]:throw new ContentException($"Map coordinate out of bounds: {x}, {y}");
 [JsonIgnore] Dictionary<(int,int),List<Placement>>? spatial;[JsonIgnore] int indexedCount=-1;
 public void InvalidateSpatial(){spatial=null;indexedCount=-1;}
 public IReadOnlyList<Placement> ObjectsAt(int x,int y){if(spatial==null||indexedCount!=Statics.Count){spatial=Statics.GroupBy(o=>(o.X,o.Y)).ToDictionary(g=>g.Key,g=>g.ToList());indexedCount=Statics.Count;}return spatial.TryGetValue((x,y),out var list)?list:Array.Empty<Placement>();}
 public WorldMap Clone()=>JsonSerializer.Deserialize<WorldMap>(JsonSerializer.Serialize(this,ContentPackage.Json),ContentPackage.Json)!;
}
public sealed class AudioCue {public string Id {get;set;}="";public string Path {get;set;}="";public string Provenance {get;set;}="";}
public sealed class FileAccessRecord {public string Path {get;set;}="";public string Kind {get;set;}="";public long Bytes {get;set;}public string Sha256 {get;set;}="";}
public sealed class Movement {public string Authority {get;set;}="";public int Directions {get;set;}public int InterpolationMilliseconds {get;set;}public int MaxStepHeight {get;set;}}
public sealed class PackageDescriptor {public string Adventure {get;set;}="";
 public List<string> CompatibleFingerprints {get;set;}=[];public int Schema {get;set;}public string Id {get;set;}="";public string Version {get;set;}="";public string TitleKey {get;set;}="";public string Definitions {get;set;}="";public string Map {get;set;}="";public string Text {get;set;}="";public string Audio {get;set;}="";public string Visuals {get;set;}="";public string Integrity {get;set;}="";public bool LegacyFallback {get;set;}public Movement Movement {get;set;}=new();
}
public sealed class ContentPackage : IDisposable {
 public static readonly JsonSerializerOptions Json=new(){PropertyNamingPolicy=JsonNamingPolicy.CamelCase,PropertyNameCaseInsensitive=false,WriteIndented=true,UnmappedMemberHandling=JsonUnmappedMemberHandling.Disallow};
 // Visual manifest is extensible across bake versions; it is hash-checked as an opaque visual contract.
 public AdventureCatalog AdventureCatalog {get;}public string Root {get;}public PackageDescriptor Descriptor {get;}public Definitions Definitions {get;}public WorldMap Map {get;private set;}public Dictionary<string,string> Text {get;}public List<AudioCue> Audio {get;}public JsonDocument Visuals {get;}
 public IReadOnlyDictionary<string,Definition> Terrain=>terrain;public IReadOnlyDictionary<string,Definition> Statics=>statics;public string MapHash=>Hash(JsonSerializer.SerializeToUtf8Bytes(Map,Json));public string Fingerprint {get;}
 readonly Dictionary<string,Definition> terrain,statics;readonly Dictionary<string,string> hashes;readonly List<FileAccessRecord> reads=[];readonly object gate=new();
 public IReadOnlyList<FileAccessRecord> Accesses {get {lock(gate)return reads.ToArray();}}
 static string Hash(byte[] b)=>Convert.ToHexString(SHA256.HashData(b)).ToLowerInvariant();
 public ContentPackage(string root,string? mapOverride=null) {
  Root=Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));Descriptor=ReadJson<PackageDescriptor>("content/original/package.json","package",false);
  if(Descriptor.Schema!=1||Descriptor.LegacyFallback||Descriptor.Movement.Authority!="tile"||Descriptor.Movement.Directions!=8||Descriptor.Movement.InterpolationMilliseconds!=200||Descriptor.Movement.MaxStepHeight!=2)throw new ContentException("Unsupported package schema or movement contract. Legacy fallback is prohibited.");
  using(var integrity=JsonDocument.Parse(ReadBytes(Descriptor.Integrity,"integrity",false))){if(integrity.RootElement.GetProperty("algorithm").GetString()!="sha256")throw new ContentException("Unsupported integrity algorithm.");hashes=integrity.RootElement.GetProperty("files").EnumerateObject().ToDictionary(x=>x.Name,x=>x.Value.GetString()!,StringComparer.Ordinal);}
  foreach(var entry in hashes){if(entry.Value.Length!=64||!entry.Value.All(Uri.IsHexDigit))throw new ContentException("Invalid asset digest: "+entry.Key);SafePath(entry.Key);}
  Definitions=ReadJson<Definitions>(Descriptor.Definitions,"definitions");Text=ReadJson<Dictionary<string,string>>(Descriptor.Text,"text");Audio=ReadJson<List<AudioCue>>(Descriptor.Audio,"audio-catalog");Visuals=JsonDocument.Parse(ReadBytes(Descriptor.Visuals,"visual-manifest"));
  AdventureCatalog=Descriptor.Adventure==""?new():ReadJson<AdventureCatalog>(Descriptor.Adventure,"adventure");AdventureRules.ValidateCatalog(this);
  terrain=Index(Definitions.Terrain,"terrain");statics=Index(Definitions.Statics,"statics");
  Map=ReadJson<WorldMap>(Descriptor.Map,"map");ValidateDefinitions();ValidateMap(Map);
  foreach(var image in RuntimeImages())if(!hashes.ContainsKey(image))throw new ContentException("Undeclared runtime visual: "+image);
  if(mapOverride!=null)LoadMapOverride(mapOverride);
  Fingerprint=Hash(System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(Descriptor,Json)+string.Join('\n',hashes.OrderBy(x=>x.Key).Select(x=>x.Key+":"+x.Value))));
 }
 Dictionary<string,Definition> Index(List<Definition> defs,string kind){try{return defs.ToDictionary(d=>d.Id,StringComparer.Ordinal);}catch(ArgumentException){throw new ContentException("Duplicate "+kind+" definition ID.");}}
 string SafePath(string relative){
  if(string.IsNullOrWhiteSpace(relative)||Path.IsPathRooted(relative)||relative.Contains('\\')||relative.Split('/').Any(s=>s is ".." or "." or ""))throw new ContentException("Invalid package path: "+relative);
  var ext=Path.GetExtension(relative).ToLowerInvariant();if(new[]{".mul",".uop",".idx",".def",".rle",".ttf",".ico"}.Contains(ext)||Path.GetFileName(relative).StartsWith("cliloc",StringComparison.OrdinalIgnoreCase))throw new ContentException("Legacy/undeclared resource family rejected: "+relative);
  string p=Path.GetFullPath(Path.Combine(Root,relative));if(!p.StartsWith(Path.EndsInDirectorySeparator(Root)?Root:Root+Path.DirectorySeparatorChar,StringComparison.Ordinal))throw new ContentException("Package path escapes root.");
  // Prevent a package-internal symlink from bypassing the root boundary.
  var walk=new DirectoryInfo(Path.GetDirectoryName(p)!);while(walk.FullName!=Root){if(walk.LinkTarget!=null)throw new ContentException("Package symlink rejected: "+relative);walk=walk.Parent??throw new ContentException("Package root not found.");}if(new FileInfo(p).LinkTarget!=null)throw new ContentException("Package symlink rejected: "+relative);return p;
 }
 public byte[] ReadBytes(string relative,string kind,bool verify=true){
  string p=SafePath(relative);if(!File.Exists(p))throw new ContentException("Missing original content: "+relative);
  byte[] bytes=File.ReadAllBytes(p);string digest=Hash(bytes);if(verify&&(!hashes.TryGetValue(relative,out var expected)||digest!=expected))throw new ContentException("Original content integrity check failed: "+relative);
  lock(gate)reads.Add(new(){Path=relative,Kind=kind,Bytes=bytes.Length,Sha256=digest});return bytes;
 }
 T ReadJson<T>(string path,string kind,bool verify=true){try{return JsonSerializer.Deserialize<T>(ReadBytes(path,kind,verify),Json)??throw new ContentException("Empty JSON: "+path);}catch(JsonException e){throw new ContentException("Invalid "+kind+": "+e.Message);}}
 public string Localize(string key)=>Text.TryGetValue(key,out var s)?s:throw new ContentException("Missing original text key: "+key);
 public string ImagePath(string name){if(!Visuals.RootElement.GetProperty("images").TryGetProperty(name,out var d))throw new ContentException("Unknown original visual: "+name);return "art/compiled/"+d.GetProperty("image").GetString();}
 public string ActorPath(string name){if(!Visuals.RootElement.GetProperty("actors").TryGetProperty(name,out var d))throw new ContentException("Unknown original actor: "+name);return "art/compiled/"+d.GetProperty("image").GetString();}
 public bool AcceptsMapHash(string value){if(value==MapHash)return true;if(Map.Objects.Count!=0||Map.Regions.Count!=0||Map.Roofs.Count!=0)return false;var node=System.Text.Json.Nodes.JsonNode.Parse(JsonSerializer.Serialize(Map,Json))!.AsObject();node.Remove("regions");node.Remove("roofs");node.Remove("objects");return value==Hash(System.Text.Encoding.UTF8.GetBytes(node.ToJsonString(Json)));}
 public bool AcceptsFingerprint(string value)=>value==Fingerprint||Descriptor.CompatibleFingerprints.Contains(value,StringComparer.Ordinal);
 public IEnumerable<string> RuntimeImages()=>Visuals.RootElement.GetProperty("images").EnumerateObject().Select(x=>"art/compiled/"+x.Value.GetProperty("image").GetString()).Concat(Visuals.RootElement.GetProperty("actors").EnumerateObject().Select(x=>"art/compiled/"+x.Value.GetProperty("image").GetString())).Append("art/compiled/"+Visuals.RootElement.GetProperty("font").GetProperty("image").GetString()).Distinct();
 void ValidateDefinitions(){
  Localize(Descriptor.TitleKey);var all=new HashSet<string>(StringComparer.Ordinal);
  foreach(var list in new[]{Definitions.Terrain,Definitions.Statics}){
   var numbers=new HashSet<int>();foreach(var d in list){if(!all.Add(d.Id)||!numbers.Add(d.NumericId)||d.NumericId<0||d.NumericId>=(list==Definitions.Terrain?16384:65536)||d.Height<0||d.Height>127)throw new ContentException("Invalid/duplicate original tile definition: "+d.Id);Localize(d.NameKey);ImagePath(d.Visual);}}
  foreach(var item in Definitions.Items){if(!all.Add(item.Id)||item.Weight<0||item.ContainerSlots<0||item.ContainerSlots>128||item.ContainerWeight<0||item.Price<1||item.Price>10000||item.ContainerSlots>0&&(item.Stackable||item.Equipment!=null))throw new ContentException("Invalid/duplicate item: "+item.Id);Localize(item.NameKey);ImagePath(item.GroundVisual);ImagePath(item.InventoryVisual);if(item.Equipment!=null&&(item.Equipment.Slot is not ("hat" or "sword" or "shield")||item.Equipment.VariantValue<1||item.Equipment.VariantValue>(item.Equipment.Slot=="hat"?2:1)))throw new ContentException("Invalid equipment mapping: "+item.Id);}
  foreach(var spell in Definitions.Spells){if(!all.Add(spell.Id))throw new ContentException("Duplicate spell: "+spell.Id);Localize(spell.NameKey);ImagePath(spell.Visual);if(spell.RulesEnabled&&!CombatRules.Spells.Any(s=>s.Id==spell.Id))throw new ContentException("Unsupported spell rules.");}
  if(Definitions.Features.Any(x=>x.Value)||Definitions.Abilities.Count>0||Definitions.Prefabs.Count>0)throw new ContentException("Unsupported legacy/out-of-scope feature enabled.");
  var cueIds=new HashSet<string>();foreach(var cue in Audio){if(!cueIds.Add(cue.Id)||!hashes.ContainsKey(cue.Path)||!cue.Path.EndsWith(".wav",StringComparison.Ordinal))throw new ContentException("Invalid audio event: "+cue.Id);}
 }
 public void ValidateMap(WorldMap map){
  if(map is null||map.Cells is null||map.Statics is null||map.Spawn is null||map.Cells.Any(c=>c is null||c.Terrain is null)||map.Statics.Any(o=>o is null||o.Id is null||o.Definition is null)||map.NameKey is null)throw new ContentException("Incomplete map snapshot.");
  if(map.Schema!=1||map.Width<8||map.Height<8||map.Width>512||map.Height>512||map.Width%8!=0||map.Height%8!=0||map.ChunkSize!=8||map.Cells.Count!=map.Width*map.Height)throw new ContentException("Invalid map dimensions/chunk/cell count.");
  map.InvalidateSpatial();Localize(map.NameKey);foreach(var cell in map.Cells)if(!terrain.ContainsKey(cell.Terrain)||cell.Z< -128||cell.Z>127)throw new ContentException("Invalid terrain/elevation in map.");
  var ids=new HashSet<string>();foreach(var obj in map.Statics)if(!ids.Add(obj.Id)||!statics.ContainsKey(obj.Definition)||!map.Contains(obj.X,obj.Y)||obj.Z< -128||obj.Z>127)throw new ContentException("Invalid/duplicate placed object: "+obj.Id);
  if(!map.Contains(map.Spawn.X,map.Spawn.Y)||!CanStand(map,map.Spawn.X,map.Spawn.Y)||map.Spawn.Z!=map.Get(map.Spawn.X,map.Spawn.Y).Z)throw new ContentException("Map spawn is blocked or out of bounds.");
  WorldAuthoring.Validate(this,map);
 }
 public bool CanStand(WorldMap map,int x,int y){if(!map.Contains(x,y)||terrain[map.Get(x,y).Terrain].Impassable)return false;int z=map.Get(x,y).Z;return !map.ObjectsAt(x,y).Any(p=>statics[p.Definition].Impassable&&p.Z+statics[p.Definition].Height>z&&p.Z<z+16);}
 public bool CanStep(WorldMap map,int x,int y,int dx,int dy){if(dx is < -1 or > 1||dy is < -1 or > 1||(dx==0&&dy==0)||!map.Contains(x,y)||!CanStand(map,x+dx,y+dy))return false;int z=map.Get(x,y).Z;if(Math.Abs(map.Get(x+dx,y+dy).Z-z)>Descriptor.Movement.MaxStepHeight)return false;return dx==0||dy==0||(CanStand(map,x+dx,y)&&CanStand(map,x,y+dy)&&Math.Abs(map.Get(x+dx,y).Z-z)<=2&&Math.Abs(map.Get(x,y+dy).Z-z)<=2);}
 public void LoadMapOverride(string path){if(!Path.GetExtension(path).Equals(".json",StringComparison.OrdinalIgnoreCase))throw new ContentException("Map override must be original JSON.");byte[] bytes=File.ReadAllBytes(path);WorldMap map=JsonSerializer.Deserialize<WorldMap>(bytes,Json)??throw new ContentException("Empty map override.");ValidateMap(map);Map=map;lock(gate)reads.Add(new(){Path=Path.GetFullPath(path),Kind="explicit-map-override",Bytes=bytes.Length,Sha256=Hash(bytes)});}
 public void SaveMap(WorldMap map,string path){ValidateMap(map);string p=Path.GetFullPath(path);Directory.CreateDirectory(Path.GetDirectoryName(p)!);string temp=p+".writing";File.WriteAllText(temp,JsonSerializer.Serialize(map,Json));File.Move(temp,p,true);}
 public void VerifyAll(){foreach(var p in hashes.Keys)ReadBytes(p,"integrity-validation");}
 public void SaveAudit(string path,string role){lock(gate){Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);string temp=path+".writing";File.WriteAllText(temp,JsonSerializer.Serialize(new{role,package=Descriptor.Id,fingerprint=Fingerprint,mapHash=MapHash,legacyFallback=false,loadedFiles=Accesses},Json));File.Move(temp,path,true);}}
 public void Dispose()=>Visuals.Dispose();
}

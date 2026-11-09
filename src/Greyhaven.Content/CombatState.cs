// SPDX-License-Identifier: MIT
namespace Greyhaven.Content;
public sealed class StatusState
{
    public double RootUntil {get;set;} public double WardUntil {get;set;} public double SlowUntil {get;set;}
    public double PoisonUntil {get;set;} public double BurnUntil {get;set;} public double RenewUntil {get;set;}
    public double NextPulse {get;set;}
}
public sealed class EnemyState
{
    public string Id {get;set;}=""; public string Kind {get;set;}="";
    public int X {get;set;} public int Y {get;set;} public int Z {get;set;} public int HomeX {get;set;} public int HomeY {get;set;}
    public int Facing {get;set;}=3;public int Health {get;set;} public string Behavior {get;set;}="Idle";
    public double MoveAt {get;set;} public double AttackAt {get;set;} public double ActionUntil {get;set;} public double FleeUntil {get;set;} public double SpecialAt {get;set;}
    public bool LootSpawned {get;set;} public StatusState Status {get;set;}=new();
}
public sealed class PendingCast
{
    public string Spell {get;set;}="";public string TargetId {get;set;}=""; public int X {get;set;}public int Y {get;set;}
    public double StartedAt {get;set;} public double ResolvesAt {get;set;}
}
public sealed class CombatEffect
{
    public string Id {get;set;}="";public string Kind {get;set;}="";public string TargetId {get;set;}="";
    public int X {get;set;}public int Y {get;set;} public int ToX {get;set;}public int ToY {get;set;}
    public int Damage {get;set;} public double Born {get;set;}public double Ends {get;set;}public bool Resolved {get;set;}
}
public sealed class CombatState
{
    public int Version {get;set;}public int Health {get;set;}=100;public int Mana {get;set;}=60;public int Stamina {get;set;}=100;
    public bool Dead {get;set;}public bool WarMode {get;set;}public string TargetId {get;set;}="";public string CorpseId {get;set;}="";
    public double SwingAt {get;set;}public double ThrowAt {get;set;}public double SpellAt {get;set;}public double PotionAt {get;set;}
    public double ActionUntil {get;set;}public double GraceUntil {get;set;}public double NextRegen {get;set;}
    public string Action {get;set;}="idle";public int Kills {get;set;}
    public StatusState Status {get;set;}=new();public PendingCast? Cast {get;set;}
    public List<EnemyState> Enemies {get;set;}=[];public List<CombatEffect> Effects {get;set;}=[];
}
public sealed record EnemyProfile(string Id,string Name,string Actor,int Health,int Damage,int Armor,int Range,double Speed,double Delay,int Aggro);
public sealed record SpellProfile(string Id,string Name,int Cost,double CastTime,int Range,string Description);
public static class CombatRules
{
    public const string CorpseDefinition="greyhaven.item.fallen-pack";
    public static readonly EnemyProfile[] Enemies=[
        new("tidehound","Tidehound","tidehound",32,6,0,1,.35,1.3,5),
        new("raider","Shore Raider","shore-raider",50,9,1,1,.5,1.6,5),
        new("slinger","Reef Slinger","reef-slinger",36,7,0,5,.65,2.1,6),
        new("hexer","Salt Hexer","salt-hexer",42,8,0,5,.6,2.5,6),
        new("brineguard","Brineguard","brineguard",80,12,3,1,.8,2.2,4)];
    public static readonly SpellProfile[] Spells=[
        new("greyhaven.spell.dawn","Dawn",6,.45,0,"Heal 18 and cleanse poison."),
        new("greyhaven.spell.storm","Storm",14,1.0,6,"Lightning: 16 damage in radius 2; slows."),
        new("greyhaven.spell.ember","Ember",8,.65,6,"Fire bolt: 18 damage and a short burn."),
        new("greyhaven.spell.renew","Renew",10,.8,0,"Restore health over 5 seconds."),
        new("greyhaven.spell.ward","Ward",10,.5,0,"Halve incoming damage for 8 seconds."),
        new("greyhaven.spell.venom","Venom",9,.6,5,"Poison bolt: 6 damage and poison."),
        new("greyhaven.spell.bind","Bind",8,.5,5,"Root an enemy for 4 seconds."),
        new("greyhaven.spell.gate","Gate",16,.9,5,"Blink to a visible, clear tile.")];
    public static EnemyProfile Enemy(string id)=>Enemies.FirstOrDefault(e=>e.Id==id)??throw new ContentException("Unknown enemy kind.");
    public static SpellProfile Spell(string id)=>Spells.FirstOrDefault(s=>s.Id==id)??throw new ContentException("Unknown spell.");
    public static int Distance(int x,int y,int tx,int ty)=>(int)Math.Min(int.MaxValue,Math.Max(Math.Abs((long)x-tx),Math.Abs((long)y-ty)));
    public static bool InCamp(ContentPackage p,int x,int y)=>Distance(x,y,p.Map.Spawn.X,p.Map.Spawn.Y)<=3||WorldAuthoring.Safe(p.Map,x,y);
    public static bool HostileSpell(string id)=>id is "greyhaven.spell.storm" or "greyhaven.spell.ember" or "greyhaven.spell.venom" or "greyhaven.spell.bind";
    public static bool LineOfSight(ContentPackage p,int x,int y,int tx,int ty,SessionState? state=null)
    {
        if(!p.Map.Contains(x,y)||!p.Map.Contains(tx,ty)||Distance(x,y,tx,ty)>16)return false;
        bool Stand(int a,int b)=>p.CanStand(p.Map,a,b)&&(state==null||!WorldAuthoring.Blocks(p,state,a,b));
        int ox=x,oy=y;int dx=Math.Abs(tx-x),dy=Math.Abs(ty-y),sx=Math.Sign(tx-x),sy=Math.Sign(ty-y),err=dx-dy;
        while(x!=tx||y!=ty){int e=err*2,nx=x,ny=y;if(e> -dy){err-=dy;nx+=sx;}if(e<dx){err+=dx;ny+=sy;}
            if(nx!=x&&ny!=y&&(!Stand(nx,y)||!Stand(x,ny)))return false;
            x=nx;y=ny;if(!Stand(x,y))return false;
            if(Math.Abs(p.Map.Get(x,y).Z-p.Map.Get(ox,oy).Z)>8)return false;
        }return true;
    }
    public static void Validate(ContentPackage p,SessionState state)
    {
        var c=state.Combat??throw new ContentException("Missing combat state.");
        if(c.Version is <0 or >1||c.Health<0||c.Health>100||c.Mana<0||c.Mana>60||c.Stamina<0||c.Stamina>100||c.Dead!=(c.Health==0)||c.Kills<0||c.Kills>100000||c.Enemies is null||c.Effects is null||c.Enemies.Count>64||c.Effects.Count>64||c.TargetId is null||c.CorpseId is null||c.Action is not ("idle" or "attack" or "cast"))throw new ContentException("Invalid combat state.");
        void Times(params double[] values){if(values.Any(t=>!double.IsFinite(t)||t<0||t>state.ElapsedSeconds+3600))throw new ContentException("Invalid combat timer.");}
        void Status(StatusState s){if(s is null)throw new ContentException("Invalid status state.");Times(s.RootUntil,s.WardUntil,s.SlowUntil,s.PoisonUntil,s.BurnUntil,s.RenewUntil,s.NextPulse);}
        Times(c.SwingAt,c.ThrowAt,c.SpellAt,c.PotionAt,c.ActionUntil,c.GraceUntil,c.NextRegen);Status(c.Status);
        var ids=new HashSet<string>();var occupied=new HashSet<(int,int)>();
        foreach(var e in c.Enemies){if(e is null||string.IsNullOrWhiteSpace(e.Id)||e.Id.Length>100||!ids.Add(e.Id))throw new ContentException("Invalid enemy identity.");var d=Enemy(e.Kind);if(e.Health<0||e.Health>d.Health||!p.CanStand(p.Map,e.X,e.Y)||e.Z!=p.Map.Get(e.X,e.Y).Z||!p.CanStand(p.Map,e.HomeX,e.HomeY)||e.Facing<0||e.Facing>7||e.Behavior is not ("Idle" or "Chase" or "Attack" or "Flee" or "Return" or "Dead" or "Rooted")||e.Health>0&&!occupied.Add((e.X,e.Y)))throw new ContentException("Invalid enemy state.");Times(e.MoveAt,e.AttackAt,e.ActionUntil,e.FleeUntil,e.SpecialAt);Status(e.Status);}
        if(c.TargetId!=""&&!ids.Contains(c.TargetId))throw new ContentException("Unknown combat target.");
        if(c.Cast is PendingCast cast){Spell(cast.Spell);Times(cast.StartedAt,cast.ResolvesAt);if(cast.ResolvesAt<cast.StartedAt||!p.Map.Contains(cast.X,cast.Y)||cast.TargetId is null||cast.TargetId!=""&&!ids.Contains(cast.TargetId)||c.Dead)throw new ContentException("Invalid pending cast.");}
        var effectIds=new HashSet<string>();foreach(var e in c.Effects){if(e is null||!effectIds.Add(e.Id)||e.Kind is not ("hit" or "heal" or "ward" or "bind" or "gate" or "storm" or "ember" or "venom" or "stone" or "hex" or "death" or "fizzle")||e.TargetId is null||e.TargetId!=""&&e.TargetId!="player"&&!ids.Contains(e.TargetId)||!p.Map.Contains(e.X,e.Y)||!p.Map.Contains(e.ToX,e.ToY)||e.Damage<0||e.Damage>100)throw new ContentException("Invalid combat effect.");Times(e.Born,e.Ends);if(e.Ends<e.Born)throw new ContentException("Invalid effect duration.");}
    }
}

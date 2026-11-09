// GPL-2.0: authoritative combat and AI hosted by the ServUO-linked world.
using System.Text.Json;
using Greyhaven.Content;
namespace Greyhaven.Server;
public static class CombatSimulation
{
    static readonly (int dx,int dy)[] Directions=[(0,-1),(1,0),(0,1),(-1,0),(1,-1),(1,1),(-1,1),(-1,-1)];
    public static void Initialize(ContentPackage p,SessionState s)
    {
        if(s.Combat.Version==1)return;
        s.Combat=new(){Version=1,NextRegen=s.ElapsedSeconds+1};
        if(s.Quickbar.All(b=>b=="")){string Item(string suffix)=>s.Items.FirstOrDefault(i=>i.Definition.EndsWith(suffix)&&InventoryRules.Carried(s,i))?.Id??"";string tonic=Item("healing-tonic"),crystal=Item("tide-crystal");s.Quickbar=["spell:greyhaven.spell.dawn","spell:greyhaven.spell.ember","spell:greyhaven.spell.bind","spell:greyhaven.spell.ward",tonic==""?"":"item:"+tonic,crystal==""?"":"item:"+crystal,"spell:greyhaven.spell.renew","spell:greyhaven.spell.storm","spell:greyhaven.spell.venom","spell:greyhaven.spell.gate"];}

        if(p.Map.Objects.Any(o=>o.Kind=="encounter")){
            foreach(var o in p.Map.Objects.Where(o=>o.Kind=="encounter")){var d=CombatRules.Enemy(o.Archetype);s.Combat.Enemies.Add(new(){Id=o.Id,Kind=d.Id,X=o.X,Y=o.Y,Z=p.Map.Get(o.X,o.Y).Z,HomeX=o.X,HomeY=o.Y,Health=d.Health,MoveAt=s.ElapsedSeconds+2,AttackAt=s.ElapsedSeconds+1});}return;
        }
        var preferred=new[]{(24,19),(22,11),(9,10),(16,8),(7,20)};
        var used=new HashSet<(int,int)>();
        for(int i=0;i<CombatRules.Enemies.Length;i++)
        {
            var spots=Enumerable.Range(0,p.Map.Width*p.Map.Height).Select(n=>(x:n%p.Map.Width,y:n/p.Map.Width)).Where(t=>p.CanStand(p.Map,t.x,t.y)&&!CombatRules.InCamp(p,t.x,t.y)&&CombatRules.Distance(t.x,t.y,s.X,s.Y)>=7&&!used.Contains(t));
            var chosen=spots.OrderBy(t=>CombatRules.Distance(t.x,t.y,preferred[i].Item1,preferred[i].Item2)).ThenBy(t=>t.y).ThenBy(t=>t.x).Cast<(int x,int y)?>().FirstOrDefault();
            if(chosen is not { } cell)continue;used.Add(cell);var d=CombatRules.Enemies[i];
            s.Combat.Enemies.Add(new(){Id="reach.enemy."+d.Id,Kind=d.Id,X=cell.x,Y=cell.y,Z=p.Map.Get(cell.x,cell.y).Z,HomeX=cell.x,HomeY=cell.y,Health=d.Health,MoveAt=s.ElapsedSeconds+2,AttackAt=s.ElapsedSeconds+1});
        }
    }
    public static bool Handles(Command c,SessionState s)=>c.Type is "attack" or "throw" or "cast" or "target" or "war" or "recover" or "reclaim" or "cancel-cast" or "drink" || c.Type=="use"&&InventoryRules.Find(s,c.ItemId)?.Definition.EndsWith("healing-tonic")==true;
    public static (SessionState state,string message) Apply(ContentPackage p,SessionState original,Command command)
    {
        if(original.Paused)throw new ContentException("Campaign paused.");
        var s=JsonSerializer.Deserialize<SessionState>(Wire.Encode(original),ContentPackage.Json)!;var c=s.Combat;double now=s.ElapsedSeconds;string message;ProgressionSimulation.Stop(s);
        EnemyState Target(string id)=>c.Enemies.FirstOrDefault(e=>e.Id==id&&e.Health>0)??throw new ContentException("Choose a living enemy.");
        if(command.Type=="recover")
        {
            if(!c.Dead)throw new ContentException("You have not fallen.");
            s.X=p.Map.Spawn.X;s.Y=p.Map.Spawn.Y;s.Z=p.Map.Spawn.Z;c.Dead=false;c.Health=65;c.Mana=45;c.Stamina=100;c.Status=new();c.GraceUntil=now+10;c.TargetId="";c.WarMode=false;c.Cast=null;
            message="Recovered at camp. Your fallen pack remains where you fell. Ten seconds of protection.";Fx(s,"heal",s.X,s.Y,s.X,s.Y,0,"",.8);
        }
        else if(c.Dead)throw new ContentException("You have fallen. Recover at camp before acting.");
        else if(command.Type=="target")
        {
            if(command.TargetId==""){c.TargetId="";message="Target cleared.";}
            else{var e=Target(command.TargetId);if(CombatRules.Distance(s.X,s.Y,e.X,e.Y)>12)throw new ContentException("That enemy is too far away to target.");c.TargetId=e.Id;message="Target: "+WorldAuthoring.EnemyName(p,e);}
        }
        else if(command.Type=="war") {c.WarMode=command.Destination=="peace"?false:!c.WarMode;if(!c.WarMode)c.TargetId="";message=c.WarMode?"War mode: select an enemy or double-click to attack.":"Peace mode.";}
        else if(command.Type=="cancel-cast") {if(c.Cast==null)throw new ContentException("No spell is being cast.");Interrupt(s);message="Spell cancelled; spent mana is not refunded.";}
        else if(command.Type=="reclaim")
        {
            var pack=InventoryRules.Find(s,c.CorpseId)??throw new ContentException("There is no fallen pack to reclaim.");
            if(!InventoryRules.Reachable(s,pack))throw new ContentException("Move within two tiles of your fallen pack.");
            var contents=s.Items.Where(i=>i.Where=="container"&&i.ParentId==pack.Id).Select(i=>i.Id).ToList();
            foreach(var id in contents){var item=InventoryRules.Find(s,id)!;var d=p.Definitions.Items.First(d=>d.Id==item.Definition);bool equip=d.Equipment!=null&&!s.Items.Any(i=>i.Where=="equipped"&&p.Definitions.Items.First(x=>x.Id==i.Definition).Equipment!.Slot==d.Equipment.Slot);s=InventoryTransactions.Apply(p,s,new(){Type="transfer",ItemId=id,Destination=equip?"equipped":"bag",X=Math.Clamp(item.X,42,225),Y=Math.Clamp(item.Y,100,305)}).state;}
            c=s.Combat;s.Items.RemoveAll(i=>i.Id==pack.Id);c.CorpseId="";message="Reclaimed your fallen pack.";
        }
        else if(command.Type is "drink" or "use")
        {
            if(c.PotionAt>now)throw new ContentException("Wait before drinking another tonic.");
            if(c.Health>=100)throw new ContentException("You are already at full health.");
            var item=command.ItemId!=""?InventoryRules.Find(s,command.ItemId):s.Items.FirstOrDefault(i=>i.Definition.EndsWith("healing-tonic")&&i.QuestId==""&&InventoryRules.Carried(s,i));
            if(item==null||!item.Definition.EndsWith("healing-tonic")||!InventoryRules.Carried(s,item))throw new ContentException("Carry a healing tonic to drink it.");
            Consume(s,item);c.Health=Math.Min(100,c.Health+30);c.PotionAt=now+5;Fx(s,"heal",s.X,s.Y,s.X,s.Y,0,"",.8);message="Drank a tonic: restored 30 health.";
        }
        else if(command.Type=="cast")
        {
            var spell=CombatRules.Spell(command.ItemId);if(!p.Definitions.Spells.Any(d=>d.Id==spell.Id&&d.RulesEnabled))throw new ContentException("This spell is not enabled in the package.");
            if(c.Cast!=null||c.SpellAt>now)throw new ContentException("Finish the current spell or wait for recovery.");
            if(c.Mana<spell.Cost)throw new ContentException("Not enough mana.");
            int x=s.X,y=s.Y;string id="";
            if(spell.Range>0)
            {
                x=command.X;y=command.Y;
                if(command.TargetId!=""){var e=Target(command.TargetId);id=e.Id;x=e.X;y=e.Y;}
                ValidateSpellTarget(p,s,spell,x,y,id);
            }
            if(spell.Range>0)s.Facing=Face(x-s.X,y-s.Y);ProgressionSimulation.Stop(s);c.Mana-=spell.Cost;c.Cast=new(){Spell=spell.Id,TargetId=id,X=x,Y=y,StartedAt=now,ResolvesAt=now+spell.CastTime};c.Action="cast";c.ActionUntil=now+spell.CastTime;c.SpellAt=now+spell.CastTime+.35;message="Casting "+spell.Name+"...";
            if(CombatRules.HostileSpell(spell.Id))c.GraceUntil=0;
        }
        else if(command.Type is "attack" or "throw")
        {
            var e=Target(command.TargetId!=""?command.TargetId:c.TargetId);
            if(CombatRules.InCamp(p,s.X,s.Y))throw new ContentException("The starting camp is a sanctuary. Fight beyond its boundary.");
            if(c.Cast!=null)throw new ContentException("Finish or cancel your spell first.");
            if(command.Type=="throw")
            {
                if(c.ThrowAt>now||c.Stamina<6)throw new ContentException("Wait or recover stamina before throwing.");
                if(CombatRules.Distance(s.X,s.Y,e.X,e.Y)>6||!CombatRules.LineOfSight(p,s.X,s.Y,e.X,e.Y,s))throw new ContentException("The throw is blocked or out of range.");
                var ammo=command.ItemId==""?s.Items.FirstOrDefault(i=>i.Definition.EndsWith("tide-crystal")&&i.QuestId==""&&InventoryRules.Carried(s,i)):InventoryRules.Find(s,command.ItemId);if(ammo==null||!ammo.Definition.EndsWith("tide-crystal")||!InventoryRules.Carried(s,ammo))throw new ContentException("Carry a tide crystal to throw.");s.Facing=Face(e.X-s.X,e.Y-s.Y);Consume(s,ammo);c.Stamina-=6;c.ThrowAt=now+1.4;c.TargetId=e.Id;c.Action="attack";c.ActionUntil=now+.6;c.GraceUntil=0;ProgressionSimulation.Stop(s);Fx(s,"stone",s.X,s.Y,e.X,e.Y,ProgressionSimulation.WeaponHit(s,e,"throwing")?ProgressionSimulation.Damage(s,14):0,e.Id,.2+CombatRules.Distance(s.X,s.Y,e.X,e.Y)*.06);message="Threw a tide crystal.";
            }
            else {c.WarMode=true;c.TargetId=e.Id;c.GraceUntil=0;Melee(p,s,e);message="Attacking "+CombatRules.Enemy(e.Kind).Name+". Close to melee range.";}
        }
        else throw new ContentException("Unsupported combat action.");
        InventoryRules.Validate(p,s);CombatRules.Validate(p,s);if(command.Type is not ("target" or "attack"))InventoryTransactions.Log(s,message);s.Revision++;return (s,message);
    }
    static void ValidateSpellTarget(ContentPackage p,SessionState s,SpellProfile spell,int x,int y,string id)
    {
        if(!p.Map.Contains(x,y)||!WorldAuthoring.CanStand(p,s,x,y)||CombatRules.Distance(s.X,s.Y,x,y)>spell.Range||!CombatRules.LineOfSight(p,s.X,s.Y,x,y,s))throw new ContentException("Spell target is blocked or out of range.");
        if(CombatRules.HostileSpell(spell.Id))
        {
            if(CombatRules.InCamp(p,s.X,s.Y)||CombatRules.InCamp(p,x,y))throw new ContentException("Hostile magic is forbidden in camp.");
            if(spell.Id!="greyhaven.spell.storm"&&(id==""||!s.Combat.Enemies.Any(e=>e.Id==id&&e.Health>0)))throw new ContentException("That spell needs a living enemy target.");
        }
        if(spell.Id=="greyhaven.spell.gate"&&s.Combat.Enemies.Any(e=>e.Health>0&&e.X==x&&e.Y==y))throw new ContentException("That tile is occupied.");
    }
    public static void Interrupt(SessionState s)
    {
        if(s.Combat.Cast!=null){s.Combat.Cast=null;s.Combat.Action="idle";Fx(s,"fizzle",s.X,s.Y,s.X,s.Y,0,"",.5);}
    }
    static void Consume(SessionState s,WorldItem i)
    {
        if(i.QuestId!="")throw new ContentException("Unmark this quest item before using it as ammunition or medicine.");if(--i.Amount>0)return;s.Items.Remove(i);for(int n=0;n<s.Quickbar.Count;n++)if(s.Quickbar[n]=="item:"+i.Id)s.Quickbar[n]="";
    }
    static void Fx(SessionState s,string kind,int x,int y,int tx,int ty,int damage,string target,double life)
    {
        var effects=s.Combat.Effects;if(effects.Count>=64){var old=effects.FirstOrDefault(e=>e.Resolved||e.Damage==0);if(old!=null)effects.Remove(old);else return;}
        effects.Add(new(){Id=Guid.NewGuid().ToString("N"),Kind=kind,X=x,Y=y,ToX=tx,ToY=ty,Damage=damage,TargetId=target,Born=s.ElapsedSeconds,Ends=s.ElapsedSeconds+life,Resolved=damage==0});
    }
    static int Face(int dx,int dy)=>(Math.Sign(dx),Math.Sign(dy)) switch{(-1,-1)=>0,(0,-1)=>1,(1,-1)=>2,(1,0)=>3,(1,1)=>4,(0,1)=>5,(-1,1)=>6,_=>7};
    static void Melee(ContentPackage p,SessionState s,EnemyState e)
    {
        var c=s.Combat;double now=s.ElapsedSeconds;
        if(c.SwingAt>now||c.Stamina<3||CombatRules.Distance(s.X,s.Y,e.X,e.Y)>1||!CombatRules.LineOfSight(p,s.X,s.Y,e.X,e.Y,s))return;
        var weapon=s.Items.FirstOrDefault(i=>i.Where=="equipped"&&p.Definitions.Items.First(d=>d.Id==i.Definition).Equipment?.Slot=="sword");bool heavy=weapon!=null&&p.Definitions.Items.First(d=>d.Id==weapon.Definition).Equipment!.TwoHanded;
        int damage=weapon==null?5:heavy?18:12;c.Stamina-=3;c.SwingAt=now+(heavy?1.5:1.0);c.Action="attack";c.ActionUntil=now+.6;s.Facing=Face(e.X-s.X,e.Y-s.Y);ProgressionSimulation.Stop(s);if(ProgressionSimulation.WeaponHit(s,e,weapon==null?"wrestling":"swords")){ProgressionSimulation.CheckRange(s,"tactics",0,100);ProgressionSimulation.CheckRange(s,"anatomy",0,100);HitEnemy(p,s,e,ProgressionSimulation.Damage(s,damage),"Melee");}else{Fx(s,"fizzle",e.X,e.Y,e.X,e.Y,0,"",.3);}
    }
    static void HitEnemy(ContentPackage p,SessionState s,EnemyState e,int damage,string source)
    {
        if(e.Health<=0)return;int actual=Math.Max(1,damage-CombatRules.Enemy(e.Kind).Armor);e.Health=Math.Max(0,e.Health-actual);Fx(s,"hit",e.X,e.Y,e.X,e.Y,0,"",.45);
        if(e.Health==0)
        {
            e.Behavior="Dead";e.LootSpawned=true;s.Combat.Kills++;if(s.Combat.TargetId==e.Id)s.Combat.TargetId="";InventoryTransactions.Log(s,WorldAuthoring.EnemyName(p,e)+" defeated.");
            if(s.Items.Count<InventoryRules.MaxItems){string id="loot."+e.Id;if(!s.Items.Any(i=>i.Id==id))s.Items.Add(new(){Id=id,Definition=InventoryRules.Currency,Amount=e.Kind=="brineguard"?25:8,Where="ground",X=e.X,Y=e.Y,Z=e.Z});}
        }
        else if(e.Behavior is "Idle" or "Return")e.Behavior="Chase";
    }
    public static void EnvironmentalDamage(ContentPackage p,SessionState s,int damage)=>HitPlayer(p,s,damage);
    static void HitPlayer(ContentPackage p,SessionState s,int damage)
    {
        var c=s.Combat;if(c.Dead||c.GraceUntil>s.ElapsedSeconds||CombatRules.InCamp(p,s.X,s.Y))return;
        ProgressionSimulation.Stop(s);if(s.Adventure.BandageAt>s.ElapsedSeconds)s.Adventure.BandageSlips=Math.Min(50,s.Adventure.BandageSlips+1);bool shield=s.Items.Any(i=>i.Where=="equipped"&&p.Definitions.Items.First(d=>d.Id==i.Definition).Equipment?.Slot=="shield");int actual=Math.Max(1,damage-(shield?2:0));if(c.Status.WardUntil>s.ElapsedSeconds)actual=(actual+1)/2;
        c.Health=Math.Max(0,c.Health-actual);Interrupt(s);Fx(s,"hit",s.X,s.Y,s.X,s.Y,0,"",.5);
        if(c.Health>0)return;
        c.Dead=true;c.WarMode=false;c.Cast=null;c.TargetId="";c.Action="idle";c.Status=new();
        var roots=s.Items.Where(i=>i.Where is "bag" or "equipped").ToList();
        if(roots.Count>0&&s.Items.Count<InventoryRules.MaxItems&&p.Definitions.Items.Any(d=>d.Id==CombatRules.CorpseDefinition))
        {
            string id="fallen."+Guid.NewGuid().ToString("N");s.Items.Add(new(){Id=id,Definition=CombatRules.CorpseDefinition,Where="ground",X=s.X,Y=s.Y,Z=s.Z});
            foreach(var i in roots){i.Where="container";i.ParentId=id;i.X=Math.Clamp(i.X,42,225);i.Y=Math.Clamp(i.Y,100,305);}c.CorpseId=id;
        }
        else{foreach(var i in roots){i.Where="ground";i.ParentId="";i.X=s.X;i.Y=s.Y;i.Z=s.Z;}c.CorpseId="";}
        Fx(s,"death",s.X,s.Y,s.X,s.Y,0,"",2);InventoryTransactions.Log(s,"You fell. Recover at camp, then reclaim your belongings at the fallen pack.");
    }
    static void ResolveCast(ContentPackage p,SessionState s)
    {
        var c=s.Combat;var cast=c.Cast!;c.Cast=null;var d=CombatRules.Spell(cast.Spell);int x=cast.X,y=cast.Y;var e=c.Enemies.FirstOrDefault(e=>e.Id==cast.TargetId&&e.Health>0);
        if(e!=null){x=e.X;y=e.Y;}
        try{if(d.Range>0)ValidateSpellTarget(p,s,d,x,y,cast.TargetId);}
        catch(ContentException){Fx(s,"fizzle",s.X,s.Y,s.X,s.Y,0,"",.6);InventoryTransactions.Log(s,d.Name+" fizzled; target no longer valid.");return;}
        if(!ProgressionSimulation.Spell(s,d.Name)){Fx(s,"fizzle",s.X,s.Y,s.X,s.Y,0,"",.6);InventoryTransactions.Log(s,d.Name+" fails to take shape; practice improves Magery.");return;}
        double now=s.ElapsedSeconds;
        switch(d.Name)
        {
            case "Dawn":c.Health=Math.Min(100,c.Health+18);c.Status.PoisonUntil=0;Fx(s,"heal",s.X,s.Y,s.X,s.Y,0,"",.9);break;
            case "Renew":c.Status.RenewUntil=now+5;c.Status.NextPulse=now+1;Fx(s,"heal",s.X,s.Y,s.X,s.Y,0,"",1);break;
            case "Ward":c.Status.WardUntil=now+8;Fx(s,"ward",s.X,s.Y,s.X,s.Y,0,"",1);break;
            case "Storm":foreach(var enemy in c.Enemies.Where(t=>t.Health>0&&CombatRules.Distance(t.X,t.Y,x,y)<=2&&CombatRules.LineOfSight(p,x,y,t.X,t.Y,s)).ToList()){HitEnemy(p,s,enemy,ProgressionSimulation.MagicDamage(s,16),"Storm");enemy.Status.SlowUntil=now+3;}Fx(s,"storm",x,y,x,y,0,"",1);break;
            case "Ember":if(e!=null)Fx(s,"ember",s.X,s.Y,x,y,ProgressionSimulation.MagicDamage(s,18),e.Id,.35);break;
            case "Venom":if(e!=null)Fx(s,"venom",s.X,s.Y,x,y,ProgressionSimulation.MagicDamage(s,6),e.Id,.35);break;
            case "Bind":if(e!=null){e.Status.RootUntil=now+4;e.Behavior="Rooted";Fx(s,"bind",x,y,x,y,0,"",1);}break;
            case "Gate":Fx(s,"gate",s.X,s.Y,x,y,0,"",.8);s.X=x;s.Y=y;s.Z=p.Map.Get(x,y).Z;break;
        }
        InventoryTransactions.Log(s,d.Name+" released.");
    }
    public static void Advance(ContentPackage p,SessionState s,double dt,Func<int,int,int,int,bool> canStep)
    {
        if(s.Paused||dt<=0)return;if(!double.IsFinite(dt)||dt>1)throw new ContentException("Invalid simulation interval.");s.ElapsedSeconds+=dt;var c=s.Combat;double now=s.ElapsedSeconds;
        if(c.Version==0)Initialize(p,s);
        if(c.Cast!=null&&c.Cast.ResolvesAt<=now&&!c.Dead)ResolveCast(p,s);
        foreach(var effect in c.Effects.Where(e=>!e.Resolved&&e.Ends<=now).ToList())
        {
            effect.Resolved=true;
            if(effect.TargetId=="player"){if(CombatRules.LineOfSight(p,effect.X,effect.Y,s.X,s.Y,s)&&ProgressionSimulation.Incoming(s,effect.Kind=="hex")){HitPlayer(p,s,effect.Damage);if(effect.Kind=="hex"&&!c.Dead&&c.GraceUntil<=now&&!CombatRules.InCamp(p,s.X,s.Y)){c.Status.PoisonUntil=now+4;c.Status.NextPulse=now+1;}}}
            else{var target=c.Enemies.FirstOrDefault(e=>e.Id==effect.TargetId&&e.Health>0);if(target!=null&&CombatRules.LineOfSight(p,effect.X,effect.Y,target.X,target.Y,s)){HitEnemy(p,s,target,effect.Damage,effect.Kind);if(target.Health>0){if(effect.Kind=="ember")target.Status.BurnUntil=now+4;if(effect.Kind=="venom")target.Status.PoisonUntil=now+5;target.Status.NextPulse=now+1;}}}
        }
        c.Effects.RemoveAll(e=>e.Resolved&&now>e.Ends+.6);
        if(now+1e-6>=c.Status.NextPulse&&!c.Dead){double pulse=c.Status.NextPulse;if(c.Status.PoisonUntil>0&&pulse<=c.Status.PoisonUntil+1e-6)HitPlayer(p,s,3);if(c.Status.BurnUntil>0&&pulse<=c.Status.BurnUntil+1e-6)HitPlayer(p,s,2);if(c.Status.RenewUntil>0&&pulse<=c.Status.RenewUntil+1e-6)c.Health=Math.Min(100,c.Health+4);c.Status.NextPulse=Math.Max(pulse+1,now+.001);}
        if(now>=c.NextRegen&&!c.Dead){c.Mana=Math.Min(60,c.Mana+2);c.Stamina=Math.Min(100,c.Stamina+3);if(CombatRules.InCamp(p,s.X,s.Y))c.Health=Math.Min(100,c.Health+3);c.NextRegen=Math.Max(c.NextRegen+1,now+.001);}
        if(c.WarMode&&!c.Dead&&c.Cast==null&&!CombatRules.InCamp(p,s.X,s.Y)){var target=c.Enemies.FirstOrDefault(e=>e.Id==c.TargetId&&e.Health>0);if(target!=null)Melee(p,s,target);}
        foreach(var e in c.Enemies.Where(e=>e.Health>0).ToList())
        {
            var d=CombatRules.Enemy(e.Kind);
            if(now+1e-6>=e.Status.NextPulse){double pulse=e.Status.NextPulse;if(e.Status.PoisonUntil>0&&pulse<=e.Status.PoisonUntil+1e-6)HitEnemy(p,s,e,3,"Poison");if(e.Health>0&&e.Status.BurnUntil>0&&pulse<=e.Status.BurnUntil+1e-6)HitEnemy(p,s,e,2,"Burn");e.Status.NextPulse=Math.Max(pulse+1,now+.001);}if(e.Health<=0)continue;
            int distance=CombatRules.Distance(e.X,e.Y,s.X,s.Y);bool returning=c.Dead||c.GraceUntil>now||CombatRules.InCamp(p,s.X,s.Y)||CombatRules.Distance(e.HomeX,e.HomeY,s.X,s.Y)>8;
            bool aggro=!returning&&(e.Behavior is not "Idle"||distance<=d.Aggro&&CombatRules.LineOfSight(p,e.X,e.Y,s.X,s.Y,s));
            if(returning||!aggro){e.Behavior=CombatRules.Distance(e.X,e.Y,e.HomeX,e.HomeY)>0?"Return":"Idle";if(e.X==e.HomeX&&e.Y==e.HomeY){e.Health=Math.Min(d.Health,e.Health+1);continue;}MoveToward(p,s,e,e.HomeX,e.HomeY,now,canStep,false);continue;}
            if(e.Health<d.Health/4&&e.Kind is "slinger" or "raider"&&e.FleeUntil==0)e.FleeUntil=now+4;
            if(e.Status.RootUntil>now)e.Behavior="Rooted";
            else if(e.FleeUntil>now){e.Behavior="Flee";MoveToward(p,s,e,s.X,s.Y,now,canStep,true);continue;}
            else if(d.Range>1&&distance<3){e.Behavior="Chase";MoveToward(p,s,e,s.X,s.Y,now,canStep,true);}
            else if(distance>d.Range||!CombatRules.LineOfSight(p,e.X,e.Y,s.X,s.Y,s)){e.Behavior="Chase";MoveToward(p,s,e,s.X,s.Y,now,canStep,false);}
            else e.Behavior="Attack";
            distance=CombatRules.Distance(e.X,e.Y,s.X,s.Y);
            if(distance<=d.Range&&now>=e.AttackAt&&CombatRules.LineOfSight(p,e.X,e.Y,s.X,s.Y,s))
            {
                e.AttackAt=now+d.Delay;e.Facing=Face(s.X-e.X,s.Y-e.Y);e.ActionUntil=now+.65;
                if(e.Kind=="hexer"&&now>=e.SpecialAt){c.Status.RootUntil=now+2;e.SpecialAt=now+7;Fx(s,"bind",s.X,s.Y,s.X,s.Y,0,"",.8);}
                else if(d.Range>1)Fx(s,e.Kind=="hexer"?"hex":"stone",e.X,e.Y,s.X,s.Y,d.Damage,"player",.35);
                else if(ProgressionSimulation.Incoming(s,false))HitPlayer(p,s,d.Damage);
            }
        }
    }
    static void MoveToward(ContentPackage p,SessionState s,EnemyState e,int tx,int ty,double now,Func<int,int,int,int,bool> canStep,bool away)
    {
        if(now<e.MoveAt||e.Status.RootUntil>now)return;var d=CombatRules.Enemy(e.Kind);e.MoveAt=now+d.Speed*(e.Status.SlowUntil>now?2:1);
        bool Valid(int x,int y,int dx,int dy)=>canStep(x,y,dx,dy)&&!CombatRules.InCamp(p,x+dx,y+dy)&&!(x+dx==s.X&&y+dy==s.Y)&&!s.Combat.Enemies.Any(o=>o.Id!=e.Id&&o.Health>0&&o.X==x+dx&&o.Y==y+dy)&&CombatRules.Distance(x+dx,y+dy,e.HomeX,e.HomeY)<=8;
        (int dx,int dy)? step=null;
        if(away){step=Directions.Where(v=>Valid(e.X,e.Y,v.dx,v.dy)).OrderByDescending(v=>CombatRules.Distance(e.X+v.dx,e.Y+v.dy,tx,ty)).Cast<(int,int)?>().FirstOrDefault();}
        else
        {
            // Bounded breadth-first routing around terrain, statics and other actors.
            var seen=new HashSet<(int,int)>{(e.X,e.Y)};var queue=new Queue<(int x,int y,int dx,int dy)>();
            foreach(var v in Directions)if(Valid(e.X,e.Y,v.dx,v.dy)){queue.Enqueue((e.X+v.dx,e.Y+v.dy,v.dx,v.dy));seen.Add((e.X+v.dx,e.Y+v.dy));}
            int best=CombatRules.Distance(e.X,e.Y,tx,ty),visited=0;
            while(queue.Count>0&&visited++<256){var n=queue.Dequeue();int distance=CombatRules.Distance(n.x,n.y,tx,ty);if(distance<best){best=distance;step=(n.dx,n.dy);if(distance<=1)break;}foreach(var v in Directions)if(!seen.Contains((n.x+v.dx,n.y+v.dy))&&Valid(n.x,n.y,v.dx,v.dy)){seen.Add((n.x+v.dx,n.y+v.dy));queue.Enqueue((n.x+v.dx,n.y+v.dy,n.dx,n.dy));}}
        }
        if(step is { } move){e.X+=move.dx;e.Y+=move.dy;e.Z=p.Map.Get(e.X,e.Y).Z;e.Facing=Face(move.dx,move.dy);}
    }
}

// SPDX-License-Identifier: MIT
using Greyhaven.Content;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
namespace Greyhaven.Presentation;
public sealed partial class OriginalGame
{
    sealed class EnemyVisual {public Vector2 From,To;public double At;public double Duration=.5;public Vector2 Position(double t)=>Vector2.Lerp(From,To,Math.Clamp((float)((t-At)/Duration),0,1));}
    readonly Dictionary<string,EnemyVisual> enemyViews=[];
    readonly HashSet<string> heardEffects=[];
    double snapshotClock,nextPoll,enemyClickTime,lastActionUntil;string lastJournalEvent="";string clickedEnemy="",rangedItem="";bool rangedAiming;
    double CombatTime=>state.ElapsedSeconds+(state.Paused?0:Math.Max(0,clock-snapshotClock));
    void CombatSnapshot()
    {
        if(state.Combat.ActionUntil!=lastActionUntil&&state.Combat.ActionUntil>state.ElapsedSeconds&&state.Combat.Action=="attack")Play("swing");lastActionUntil=state.Combat.ActionUntil;
        snapshotClock=clock;if(state.Combat.Dead){StopInput();dragging=null;pendingItem=null;targetSpell="";rangedAiming=false;}
        var latest=state.Journal.LastOrDefault();if(latest!=null){string key=latest.Time+":"+latest.Text;if(lastJournalEvent!=key){lastJournalEvent=key;message=latest.Text;}}
        foreach(var e in state.Combat.Enemies){if(!enemyViews.TryGetValue(e.Id,out var v)){v=new(){From=new(e.X,e.Y),To=new(e.X,e.Y),At=clock};enemyViews[e.Id]=v;}if(v.To!=new Vector2(e.X,e.Y)){v.From=v.Position(clock);v.To=new(e.X,e.Y);v.At=clock;v.Duration=CombatRules.Enemy(e.Kind).Speed*(e.Status.SlowUntil>state.ElapsedSeconds?2:1);}}
        foreach(var fx in state.Combat.Effects)if(heardEffects.Add(fx.Id))PlayProduction(fx.Kind switch{"hit"=>"impact","death"=>"death","heal"=>"heal","ember"=>"ember","venom"=>"venom","storm"=>"storm","ward"=>"ward","bind"=>"bind","gate"=>"portal",_=>"cast"},Math.Clamp((fx.ToX-state.X-fx.ToY+state.Y)*.06f,-.8f,.8f));
        if(heardEffects.Count>500){heardEffects.Clear();foreach(var fx in state.Combat.Effects)heardEffects.Add(fx.Id);}
    }
    EnemyState? EnemyAt(Vector2 p)=>state.Combat.Enemies.Where(e=>e.Health>0).OrderByDescending(e=>e.X+e.Y).FirstOrDefault(e=>{var v=enemyViews.TryGetValue(e.Id,out var visual)?visual.Position(clock):new Vector2(e.X,e.Y);var point=Project(v.X,v.Y,e.Z);return p.X>point.X-35&&p.X<point.X+35&&p.Y>point.Y-100&&p.Y<point.Y+15;});
    string CombatHeroClip()=>state.Combat.Cast!=null?"cast":state.Combat.ActionUntil>CombatTime?state.Combat.Action:interpolation<1?"walk":"idle";
    bool CombatBegin(Vector2 point)
    {
        if(At(point)!=null||QuickSlot(point)>=0||point.Y<65||point.Y>=752)return false;
        var enemy=EnemyAt(point);var tile=Unproject(point);
        if(targetSpell!="")
        {
            var command=new Command{Type="cast",ItemId=targetSpell,TargetId=enemy?.Id??"",X=tile.X,Y=tile.Y};
            if(SendInventory(command))targetSpell="";return true;
        }
        if(rangedAiming){if(enemy==null){message="Choose a living enemy for the throw.";return true;}if(SendInventory(new(){Type="throw",TargetId=enemy.Id,ItemId=rangedItem})){rangedAiming=false;rangedItem="";}return true;}
        if(enemy!=null){bool twice=clickedEnemy==enemy.Id&&clock-enemyClickTime<.35;clickedEnemy=enemy.Id;enemyClickTime=clock;SendInventory(new(){Type=twice?"attack":"target",TargetId=enemy.Id});return true;}
        return false;
    }
    void CombatInput(KeyboardState keys)
    {
        if(state.Paused||TextCapture)return;
        if(Press(keys,Keys.Tab))SendInventory(new(){Type="war"});
        if(Press(keys,Keys.Space))SendInventory(new(){Type="attack",TargetId=state.Combat.TargetId});
        if(Press(keys,Keys.F)){if(state.Combat.TargetId!="")SendInventory(new(){Type="throw",TargetId=state.Combat.TargetId});else{rangedAiming=true;rangedItem="";message="Select an enemy to throw a tide crystal.";}}
        if(Press(keys,Keys.H))SendInventory(new(){Type="drink"});
        if(Press(keys,Keys.C))SendInventory(new(){Type="cancel-cast"});
        if(Press(keys,Keys.R))SendInventory(new(){Type=state.Combat.Dead?"recover":"reclaim"});
    }
    void PollCombat(){if(!editor&&session?.Busy==false&&session.Error==null&&clock>=nextPoll){if(session.Send(new(){Type="poll"}))nextPoll=clock+.125;}}
    void AddEnemies(List<(float depth,Action draw)> objects)
    {
        foreach(var e in state.Combat.Enemies)
        {
            var enemy=e;var d=CombatRules.Enemy(e.Kind);var v=enemyViews.TryGetValue(e.Id,out var visual)?visual.Position(clock):new Vector2(e.X,e.Y);var point=Project(v.X,v.Y,e.Z);
            objects.Add((v.X+v.Y+.03f,()=>{
                if(enemy.Health<=0){Image("corpse-marker",point.X-18,point.Y-25,36,30);var actor=package.Visuals.RootElement.GetProperty("actors").GetProperty(d.Actor);var first=actor.GetProperty("directions").GetProperty("SE").GetProperty("idle").GetProperty("start").GetInt32();batch.Draw(textures[package.ActorPath(d.Actor)],new Rectangle((int)point.X-45,(int)point.Y-27,90,35),new Rectangle(first%16*128,first/16*128,128,128),ColorOf(95,100,100,190));return;}
                bool walking=visual!=null&&clock-visual.At<visual.Duration;string clip=enemy.ActionUntil>CombatTime?(enemy.Kind=="hexer"?"cast":"attack"):walking?"walk":"idle";Actor(d.Actor,enemy.Facing,clip,point.X,point.Y);
                bool selected=state.Combat.TargetId==enemy.Id;if(selected){var color=Color.OrangeRed;Line(point+new Vector2(-23,0),point+new Vector2(0,13),color);Line(point+new Vector2(0,13),point+new Vector2(23,0),color);}
                if(selected||enemy.Behavior is not ("Idle" or "Return")||CombatRules.Distance(enemy.X,enemy.Y,state.X,state.Y)<8){int x=(int)point.X-48,y=(int)point.Y-118;Rect(x,y,96,5,ColorOf(40,24,27));Rect(x,y,(int)(96f*enemy.Health/d.Health),5,Color.IndianRed);Text(WorldAuthoring.EnemyName(package,enemy),(int)point.X-WorldAuthoring.EnemyName(package,enemy).Length*9/2,y-21,selected?Color.Gold:ColorOf(235,215,196));}
                if(enemy.Status.RootUntil>CombatTime)Text("BOUND",(int)point.X-22,(int)point.Y-35,Color.Cyan);
            }));
        }
    }
    void DrawCombatEffects()
    {
        foreach(var fx in state.Combat.Effects)
        {
            if(CombatTime>fx.Ends+.6)continue;
            var start=Project(fx.X,fx.Y,map.Get(fx.X,fx.Y).Z)-new Vector2(0,55);var end=Project(fx.ToX,fx.ToY,map.Get(fx.ToX,fx.ToY).Z)-new Vector2(0,35);
            Color color=fx.Kind switch{"ember"=>Color.OrangeRed,"venom"=>Color.LimeGreen,"heal"=>Color.LightGreen,"ward"=>Color.Gold,"death"=>Color.DarkRed,"storm"=>Color.Cyan,_=>Color.LightSkyBlue};
            float progress=Math.Clamp((float)((CombatTime-fx.Born)/Math.Max(.1,fx.Ends-fx.Born)),0,1);
            if(fx.Damage>0&&!fx.Resolved){var at=Vector2.Lerp(start,end,progress);Line(start,at,color);if(fx.Kind=="stone")Image("crystal",at.X-8,at.Y-8,16,16);else DrawSpriteEffect(fx.Kind,at,progress,.5f,MathF.Atan2(end.Y-start.Y,end.X-start.X));}
            else{DrawSpriteEffect(fx.Kind,end,progress,fx.Kind=="storm"?1.2f:.85f);float radius=8+progress*(fx.Kind=="storm"?55:25);for(int i=0;i<24;i++){float a=i*MathF.Tau/24,b=(i+1)*MathF.Tau/24;Line(end+new Vector2(MathF.Cos(a)*radius,MathF.Sin(a)*radius*.5f),end+new Vector2(MathF.Cos(b)*radius,MathF.Sin(b)*radius*.5f),color);}if(fx.Kind=="hit")Text("HIT",(int)end.X-13,(int)(end.Y-25-progress*15),Color.Salmon);}
        }
    }
    void Bar(int x,int y,int w,int value,int maximum,string name,Color color){Rect(x,y,w,21,ColorOf(18,28,31,235));Rect(x+1,y+1,(w-2)*value/maximum,19,color);Text($"{name} {value}/{maximum}",x+5,y+1);}
    void DrawCombat()
    {
        var c=state.Combat;Bar(20,72,165,c.Health,100,"HP",ColorOf(111,42,45));Bar(193,72,165,c.Mana,60,"MP",ColorOf(38,64,125));Bar(366,72,165,c.Stamina,100,"STA",ColorOf(65,93,54));
        Rect(540,72,440,24,ColorOf(19,32,36,235));Text(c.WarMode?"WAR | Space melee | F throw | H tonic":"PEACE | Tab war | H tonic | R reclaim",550,75,c.WarMode?Color.Salmon:Color.Gold);
        Text($"Defeated {c.Kills}/{c.Enemies.Count}",1040,74,Color.Gold);
        var target=c.Enemies.FirstOrDefault(e=>e.Id==c.TargetId);if(target!=null){var d=CombatRules.Enemy(target.Kind);Bar(20,105,260,target.Health,d.Health,WorldAuthoring.EnemyName(package,target),ColorOf(106,41,43));Text(target.Behavior,28,131,Color.Gold);}
        if(c.Cast is PendingCast cast){var spell=CombatRules.Spell(cast.Spell);float progress=Math.Clamp((float)((CombatTime-cast.StartedAt)/(cast.ResolvesAt-cast.StartedAt)),0,1);Rect(310,600,440,48,ColorOf(14,27,31,240));Rect(315,631,(int)(430*progress),8,Color.Cyan);Text("Casting "+spell.Name+" | C cancels",325,608,Color.Gold);}
        var active=new List<string>();if(c.Status.RootUntil>CombatTime)active.Add("ROOTED");if(c.Status.WardUntil>CombatTime)active.Add("WARD");if(c.Status.RenewUntil>CombatTime)active.Add("RENEW");if(c.Status.PoisonUntil>CombatTime)active.Add("POISON");if(c.GraceUntil>CombatTime)active.Add("PROTECTED");if(active.Count>0)Text(string.Join(" | ",active),20,155,Color.LightGreen);
        if(c.Dead){Rect(285,255,720,220,ColorOf(35,22,30,245));Text("YOU HAVE FALLEN",320,280,Color.Salmon);Wrap("Your equipment and backpack contents remain at your fallen pack. Press R to recover at camp, then return to retrieve them.",320,325,70);Text("R: recover at camp     Esc: pause / save",320,425,Color.Gold);}
        if(rangedAiming)Text("THROW: choose an enemy | Esc cancels",20,186,Color.Cyan);
    }
    void DrawSpellDetails(Panel p)
    {
        for(int i=0;i<CombatRules.Spells.Length;i++){var s=CombatRules.Spells[i];Text($"{s.Cost}mp {s.CastTime:0.##}s",p.X+75+i%2*170,p.Y+77+i/2*45,ColorOf(75,58,41));}
        if(At(pointer)?.Id=="book")for(int i=0;i<CombatRules.Spells.Length;i++){var at=new Vector2(p.X+50+i%2*170,p.Y+68+i/2*45);if(Vector2.Distance(at,pointer)<26){var s=CombatRules.Spells[i];Rect(30,560,650,60,ColorOf(15,28,32,245));Text(s.Name+" | "+s.Description,42,572,Color.Gold);Text($"Mana {s.Cost} | Cast {s.CastTime:0.##}s | Range {s.Range}",42,596);}}
    }
    bool combatSmokeDone;int combatSmokeStep;double combatSmokeTime,nextSmokeAction;string smokeEnemy="";
    void PrepareCombatScene(){if(!options.CombatScene&&!options.CombatSmoke)return;foreach(var p in panels)p.Visible=p.Id=="book";var book=panels.First(p=>p.Id=="book");book.X=885;book.Y=135;help=false;}
    void RunCombatSmoke(float dt)
    {
        if(!options.CombatSmoke||combatSmokeDone)return;combatSmokeTime+=dt;if(combatSmokeTime>30)throw new ContentException("Combat native smoke timed out at "+combatSmokeStep+": "+message);
        if(session?.Busy!=false||clock<nextSmokeAction)return;
        if(state.Paused){SendInventory(new(){Type="pause",Destination="resume"});return;}
        var enemy=state.Combat.Enemies.FirstOrDefault(e=>e.Id==smokeEnemy);
        void Aim(string spell,EnemyState? target){targetSpell="greyhaven.spell."+spell;pointer=target==null?Project(state.X,state.Y):Project(target.X,target.Y,target.Z)-new Vector2(0,45);CombatBegin(pointer);}
        switch(combatSmokeStep)
        {
            case 0:if(state.X<map.Spawn.X+4){SendInventory(new(){Type="step",Dx=1,Dy=0});nextSmokeAction=clock+.23;return;}smokeEnemy=state.Combat.Enemies.First(e=>e.Kind=="tidehound").Id;combatSmokeStep++;return;
            case 1:Aim("bind",enemy);combatSmokeStep++;return;
            case 2:if(enemy==null||enemy.Status.RootUntil<=state.ElapsedSeconds||state.Combat.SpellAt>state.ElapsedSeconds)return;Aim("ember",enemy);combatSmokeStep++;return;
            case 3:if(enemy==null||enemy.Health>=32||state.Combat.Cast!=null)return;rangedAiming=true;rangedItem="";pointer=Project(enemy.X,enemy.Y,enemy.Z)-new Vector2(0,45);CombatBegin(pointer);combatSmokeStep++;return;
            case 4:if(enemy?.Health>0)return;if(state.Combat.SpellAt>state.ElapsedSeconds)return;Aim("dawn",null);combatSmokeStep++;return;
            case 5:if(state.Combat.Cast!=null)return;if(state.Combat.Kills<1||!state.Items.Any(i=>i.Id=="loot."+smokeEnemy))throw new ContentException("Combat native smoke did not produce a defeated enemy and loot.");combatSmokeDone=true;Console.WriteLine("Native combat routes: tile approach, Bind targeting, Ember cast and impact, crystal projectile, enemy defeat/loot and self-heal passed.");return;
        }
    }

}

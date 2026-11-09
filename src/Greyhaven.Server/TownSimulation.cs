// GPL-2.0: authoritative tile routines, with home/wander/follow-style stepping informed by ServUO BaseAI.
using Greyhaven.Content;
namespace Greyhaven.Server;
public static class TownSimulation {
 static readonly (int x,int y)[] Dirs=[(1,0),(-1,0),(0,1),(0,-1)];
 public static void Advance(ContentPackage p,SessionState s){var a=s.Adventure;if(s.ElapsedSeconds<a.NextNpcAt)return;a.NextNpcAt=s.ElapsedSeconds+.25;int hour=AdventureRules.Minute(s)/60;foreach(var r in p.AdventureCatalog.Routines){var n=AdventureRules.Npc(s,r.NpcId);if(n==null||s.ElapsedSeconds<n.MoveAt||s.ElapsedSeconds<n.HoldUntil)continue;if(CombatRules.Distance(s.X,s.Y,n.X,n.Y)<=3){n.HoldUntil=s.ElapsedSeconds+2;continue;}int gx=hour>=22||hour<7?r.HomeX:hour>=18?r.SocialX:r.WorkX,gy=hour>=22||hour<7?r.HomeY:hour>=18?r.SocialY:r.WorkY;n.Activity=hour>=22||hour<7?"resting":hour>=18?"socializing":"working";
  // Small daytime walks make the routine visible without waiting for a whole world day.
  if(hour>=7&&hour<18&&(int)(s.ElapsedSeconds/30)%2==1){var alt=Dirs[(int)(s.ElapsedSeconds/60)%4];if(p.CanStand(p.Map,gx+alt.x,gy+alt.y)){gx+=alt.x;gy+=alt.y;}}
  if(!p.CanStand(p.Map,gx,gy)||!WorldAuthoring.Safe(p.Map,gx,gy))continue;n.MoveAt=s.ElapsedSeconds+.8;if(n.X==gx&&n.Y==gy)continue;
  bool Free(int x,int y)=>!(s.X==x&&s.Y==y)&&!a.Npcs.Any(o=>o.Id!=n.Id&&o.X==x&&o.Y==y)&&!s.Combat.Enemies.Any(e=>e.Health>0&&e.X==x&&e.Y==y)&&WorldAuthoring.Safe(p.Map,x,y)&&!p.Map.Objects.Any(o=>o.X==x&&o.Y==y&&(o.Kind=="gate"||o.Kind=="door"&&o.RequiredItemId!="")&&WorldAuthoring.Progress(s,o.Id)?.Active!=true);
  var q=new Queue<(int x,int y,int fx,int fy)>();var seen=new HashSet<(int,int)>{(n.X,n.Y)};q.Enqueue((n.X,n.Y,0,0));(int x,int y)? step=null;int tested=0;
  while(q.TryDequeue(out var at)&&tested++<4096){if(at.x==gx&&at.y==gy){step=(at.fx,at.fy);break;}foreach(var d in Dirs){int x=at.x+d.x,y=at.y+d.y;if(p.CanStep(p.Map,at.x,at.y,d.x,d.y)&&Free(x,y)&&seen.Add((x,y)))q.Enqueue((x,y,at.fx==0&&at.fy==0?d.x:at.fx,at.fx==0&&at.fy==0?d.y:at.fy));}}
  if(step is not {} move)continue;int nx=n.X+move.x,ny=n.Y+move.y;foreach(var door in p.Map.Objects.Where(o=>o.Kind=="door"&&o.RequiredItemId==""&&o.X==nx&&o.Y==ny)){var state=WorldAuthoring.Progress(s,door.Id);if(state!=null)state.Active=true;}n.X=nx;n.Y=ny;n.Facing=move switch{(1,0)=>3,(-1,0)=>7,(0,1)=>5,_=>1};n.Activity="walking";
 }}
}

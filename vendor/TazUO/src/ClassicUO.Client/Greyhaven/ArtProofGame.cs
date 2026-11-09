#nullable disable
// SPDX-License-Identifier: MIT
// Original Greyhaven Phase 1 proof; no Ultima data APIs are called.
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Greyhaven.ArtProof
{
 public sealed class ArtProofGame : Game
 {
  readonly GraphicsDeviceManager graphics;readonly string root,capture;
  JsonDocument document;JsonElement manifest;readonly Dictionary<string,Texture2D> textures=new();SpriteBatch batch;Texture2D pixel;RenderTarget2D target;
  readonly string[] dirs={"N","NE","E","SE","S","SW","W","NW"},clips={"idle","walk","attack","cast"};
  int tx,ty,fx,fy,direction=3,clip,hat=1,sword=1,shield=1,draws;float progress=1;double time;bool paused,monster,armor,teal,pivots,right;
  KeyboardState previousKeys;MouseState previousMouse;Vector2 pointer;
  string message="Drag items between ground, backpack, and paperdoll.";
  readonly List<Panel> panels=new(){new("book",610,100,360,310),new("doll",1010,80,240,400),new("bag",960,420,270,360)};
  readonly List<Item> items=new(){new("leather","hat-leather","hat",1,"equipped"),new("wizard","hat-wizard","hat",2,"bag",68,128),new("blade","sword","sword",1,"equipped"),new("buckler","shield","shield",1,"equipped"),new("tonic","potion","",0,"bag",127,136),new("gold","coins","",0,"bag",173,207),new("gem","crystal","",0,"ground",2,1),new("lore","scroll","",0,"bag",80,236)};
  Panel moving;Item dragging;Vector2 offset;
  sealed class Panel {public string Id;public int X,Y,W,H;public Panel(string id,int x,int y,int w,int h){Id=id;X=x;Y=y;W=w;H=h;}public bool Contains(Vector2 p)=>p.X>=X&&p.X<X+W&&p.Y>=Y&&p.Y<Y+H;}
  sealed class Item {public string Id,Icon,Slot,Where;public int Value,X,Y;public Item(string id,string icon,string slot,int value,string where,int x=0,int y=0){Id=id;Icon=icon;Slot=slot;Value=value;Where=where;X=x;Y=y;}}
  public ArtProofGame(string assets,string capturePath=null){root=Path.GetFullPath(assets);capture=capturePath;graphics=new GraphicsDeviceManager(this){PreferredBackBufferWidth=1280,PreferredBackBufferHeight=800,SynchronizeWithVerticalRetrace=true};IsMouseVisible=true;Window.Title="Greyhaven - original art and animation proof";Window.AllowUserResizing=true;}
  protected override void LoadContent(){document=JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"manifest.json")));manifest=document.RootElement;batch=new SpriteBatch(GraphicsDevice);pixel=new Texture2D(GraphicsDevice,1,1);pixel.SetData(new[]{Color.White});target=new RenderTarget2D(GraphicsDevice,1280,800,false,SurfaceFormat.Color,DepthFormat.None);
   foreach(var a in manifest.GetProperty("actors").EnumerateObject())Load(a.Value.GetProperty("image").GetString());foreach(var a in manifest.GetProperty("images").EnumerateObject())Load(a.Value.GetProperty("image").GetString());Load("images/font.png");}
  void Load(string name){if(textures.ContainsKey(name))return;using var f=File.OpenRead(Path.Combine(root,name));textures[name]=Texture2D.FromStream(GraphicsDevice,f);}
  Vector2 Project(float x,float y)=>new(420+(x-y)*22,350+(x+y)*22);
  Point Unproject(Vector2 p)=>new((int)MathF.Round(((p.X-420)+(p.Y-350))/44),(int)MathF.Round(((p.Y-350)-(p.X-420))/44));
  Panel At(Vector2 p)=>panels.LastOrDefault(g=>g.Contains(p));
  string Actor=>monster?"tidehound":teal?"armor-teal":$"{(armor?"armor":"humanoid")}-h{hat}-s{sword}-b{shield}";
  bool Press(KeyboardState k,Keys key)=>k.IsKeyDown(key)&&!previousKeys.IsKeyDown(key);
  void Step(int dx,int dy){if(paused||progress<1||(dx==0&&dy==0)||Math.Abs(tx+dx)>6||Math.Abs(ty+dy)>6)return;fx=tx;fy=ty;tx+=dx;ty+=dy;progress=0;direction=(dx,dy) switch {(-1,-1)=>0,(0,-1)=>1,(1,-1)=>2,(1,0)=>3,(1,1)=>4,(0,1)=>5,(-1,1)=>6,_=>7};}
  Vector2 ItemPosition(Item item){if(item.Where=="ground")return Project(item.X,item.Y);var p=panels.First(g=>g.Id==(item.Where=="bag"?"bag":"doll"));return item.Where=="bag"?new(p.X+item.X,p.Y+item.Y):new(p.X+23,p.Y+(item.Slot=="hat"?100:item.Slot=="sword"?165:230));}
  void Begin(Vector2 p){var panel=At(p);dragging=items.LastOrDefault(i=>Vector2.Distance(ItemPosition(i),p)<23&&(panel==null?i.Where=="ground":panel.Id=="bag"?i.Where=="bag":panel.Id=="doll"&&i.Where=="equipped"));if(dragging!=null)return;if(panel!=null&&p.Y<panel.Y+30){moving=panel;offset=p-new Vector2(panel.X,panel.Y);panels.Remove(panel);panels.Add(panel);}}
  void Drop(Vector2 p){if(moving!=null){moving=null;return;}if(dragging==null)return;var item=dragging;dragging=null;var panel=At(p);bool ok=false;
   if(panel?.Id=="bag"){item.Where="bag";item.X=Math.Clamp((int)p.X-panel.X,42,panel.W-42);item.Y=Math.Clamp((int)p.Y-panel.Y,100,panel.H-50);ok=true;}
   else if(panel?.Id=="doll"&&item.Slot!=""){var old=items.FirstOrDefault(i=>i!=item&&i.Where=="equipped"&&i.Slot==item.Slot);if(old!=null){old.Where="bag";old.X=60;old.Y=170;}item.Where="equipped";ok=true;}
   else if(panel==null&&p.Y>70){var tile=Unproject(p);if(Math.Abs(tile.X)<=6&&Math.Abs(tile.Y)<=6){item.Where="ground";item.X=tile.X;item.Y=tile.Y;ok=true;}}
   hat=items.FirstOrDefault(i=>i.Where=="equipped"&&i.Slot=="hat")?.Value??0;sword=items.FirstOrDefault(i=>i.Where=="equipped"&&i.Slot=="sword")?.Value??0;shield=items.FirstOrDefault(i=>i.Where=="equipped"&&i.Slot=="shield")?.Value??0;
   message=ok?$"{item.Id}: {item.Where}":"Drop cancelled; item returned to its previous location.";}
  protected override void Update(GameTime gameTime){var k=Keyboard.GetState();var mouse=Mouse.GetState();pointer=new(mouse.X*1280f/Math.Max(1,GraphicsDevice.PresentationParameters.BackBufferWidth),mouse.Y*800f/Math.Max(1,GraphicsDevice.PresentationParameters.BackBufferHeight));
   if(k.IsKeyDown(Keys.Escape))Exit();if(Press(k,Keys.Space))paused=!paused;if(Press(k,Keys.Tab))monster=!monster;if(Press(k,Keys.O)){armor=!armor;teal=false;}if(Press(k,Keys.T))teal=!teal;if(Press(k,Keys.P))pivots=!pivots;if(Press(k,Keys.C))clip=(clip+1)%4;if(Press(k,Keys.Q))direction=(direction+7)%8;if(Press(k,Keys.E))direction=(direction+1)%8;
   if(!paused){time+=gameTime.ElapsedGameTime.TotalSeconds;progress=Math.Min(1,progress+(float)gameTime.ElapsedGameTime.TotalSeconds/.2f);}
   int dx=(k.IsKeyDown(Keys.D)||k.IsKeyDown(Keys.Right)?1:0)-(k.IsKeyDown(Keys.A)||k.IsKeyDown(Keys.Left)?1:0),dy=(k.IsKeyDown(Keys.S)||k.IsKeyDown(Keys.Down)?1:0)-(k.IsKeyDown(Keys.W)||k.IsKeyDown(Keys.Up)?1:0);
   right=mouse.RightButton==ButtonState.Pressed;if(right&&At(pointer)==null){var dest=Unproject(pointer);dx=Math.Sign(dest.X-tx);dy=Math.Sign(dest.Y-ty);}Step(dx,dy);
   if(IsActive){if(mouse.LeftButton==ButtonState.Pressed&&previousMouse.LeftButton==ButtonState.Released)Begin(pointer);if(moving!=null){moving.X=Math.Clamp((int)(pointer.X-offset.X),0,1280-moving.W);moving.Y=Math.Clamp((int)(pointer.Y-offset.Y),58,800-moving.H);}if(mouse.LeftButton==ButtonState.Released&&previousMouse.LeftButton==ButtonState.Pressed)Drop(pointer);}else{dragging=null;moving=null;}
   previousKeys=k;previousMouse=mouse;base.Update(gameTime);}
  static Color C(int r,int g,int b,int a=255)=>new(r,g,b,a);
  void Rect(int x,int y,int w,int h,Color color)=>batch.Draw(pixel,new Rectangle(x,y,w,h),color);
  void Text(string value,int x,int y,Color? color=null){var font=textures["images/font.png"];for(int i=0;i<value.Length;i++){int c=value[i]-32;if(c<0||c>=96)c=31;batch.Draw(font,new Rectangle(x+i*9,y,9,18),new Rectangle(c%16*9,c/16*18,9,18),color??C(231,224,204));}}
  void Image(string name,int x,int y,int w=0,int h=0){var d=manifest.GetProperty("images").GetProperty(name);var t=textures[d.GetProperty("image").GetString()];batch.Draw(t,new Rectangle(x,y,w==0?t.Width:w,h==0?t.Height:h),Color.White);}
  void DrawActor(string name,int d,string animation,float x,float y,float scale=1){var a=manifest.GetProperty("actors").GetProperty(name);var cs=a.GetProperty("directions").GetProperty(dirs[d]);if(!cs.TryGetProperty(animation,out var c))c=cs.GetProperty("attack");int index=c.GetProperty("start").GetInt32()+(int)(time*c.GetProperty("fps").GetInt32())%c.GetProperty("count").GetInt32();batch.Draw(textures[a.GetProperty("image").GetString()],new Rectangle((int)(x-64*scale),(int)(y-112*scale),(int)(128*scale),(int)(128*scale)),new Rectangle(index%16*128,index/16*128,128,128),Color.White);
   if(pivots){Rect((int)x-5,(int)y,11,1,Color.Gold);Rect((int)x,(int)y-5,1,11,Color.Gold);}}
  protected override void Draw(GameTime gameTime){GraphicsDevice.SetRenderTarget(target);GraphicsDevice.Clear(C(21,37,44));batch.Begin(SpriteSortMode.Deferred,BlendState.NonPremultiplied,SamplerState.PointClamp,null,null);
   Rect(0,0,1280,58,C(29,48,53));Text("GREYHAVEN | ORIGINAL ART PROOF",22,16,C(218,185,129));Text("O Outfit  T Teal  C Clip  Q/E Facing  TAB Monster  SPACE Pause  P Pivots",22,39,C(171,193,193));Text($"Tile {tx}, {ty} | {Actor} | {(paused?"PAUSED":clips[clip])}",22,78,C(171,193,193));
   for(int n=-12;n<=12;n++)for(int x=-6;x<=6;x++){int y=n-x;if(y< -6||y>6)continue;var p=Project(x,y);Image(Math.Abs(x)>5||Math.Abs(y)>5?"water":x==1||y==-1?"dirt":"grass",(int)p.X-22,(int)p.Y-22);}
   var objects=new List<(float x,float y,Action<Vector2> draw)>();void Prop(string name,int x,int y){objects.Add((x,y,p=>{var t=textures[manifest.GetProperty("images").GetProperty(name).GetProperty("image").GetString()];Image(name,(int)p.X-t.Width/2,(int)p.Y-t.Height+10);}));}
   Prop("tree",-4,-1);Prop("wall-left",2,-3);Prop("door",3,-3);Prop("chest",-3,2);Prop("lantern",3,1);Prop("shrub",4,3);objects.Add((-2,2,p=>DrawActor("tidehound",5,"idle",p.X,p.Y)));
   foreach(var it in items.Where(i=>i.Where=="ground"&&i!=dragging)){var item=it;objects.Add((it.X,it.Y,p=>Image(item.Icon,(int)p.X-18,(int)p.Y-18,36,36)));}
   float hx=fx+(tx-fx)*progress,hy=fy+(ty-fy)*progress;objects.Add((hx,hy,p=>DrawActor(Actor,direction,progress<1?"walk":clips[clip],p.X,p.Y)));
   foreach(var o in objects.OrderBy(o=>o.x+o.y))o.draw(Project(o.x,o.y));Text("EIGHT FACING INSPECTOR",22,573,C(218,185,129));for(int d=0;d<8;d++){DrawActor(Actor,d,clips[clip],55+d*105,690,.85f);Text(dirs[d],47+d*105,705,C(171,193,193));}
   foreach(var p in panels){Rect(p.X+7,p.Y+8,p.W,p.H,C(0,0,0,120));Image(p.Id=="bag"?"backpack":p.Id=="doll"?"paperdoll":"spellbook",p.X,p.Y,p.W,p.H);Rect(p.X,p.Y,p.W,30,C(21,34,40));Text(p.Id=="bag"?"BACKPACK":p.Id=="doll"?"PAPERDOLL":"THE TIDAL GRIMOIRE",p.X+12,p.Y+5,C(222,194,141));
    if(p.Id=="doll"){DrawActor(monster?"humanoid-h1-s1-b1":Actor,direction,"idle",p.X+130,p.Y+285,1.5f);Text("The Wayfarer",p.X+55,p.Y+368,C(26,34,30));}
    if(p.Id=="book"){string[] names={"dawn","storm","ember","renew","ward","venom","bind","gate"};for(int i=0;i<8;i++){int col=i%2,row=i/2;Image("spell-"+names[i],p.X+35+col*190,p.Y+61+row*52,36,36);Text(names[i],p.X+77+col*190,p.Y+72+row*52,C(61,48,36));}}
    foreach(var it in items.Where(i=>i!=dragging&&((p.Id=="bag"&&i.Where=="bag")||(p.Id=="doll"&&i.Where=="equipped")))){var q=ItemPosition(it);Image(it.Icon,(int)q.X-18,(int)q.Y-18,36,36);}}
   if(dragging!=null)Image(dragging.Icon,(int)pointer.X-20,(int)pointer.Y-20,40,40);Text("WASD/Arrows: tiles | Right-drag: walk | Drag panel title: move gump",22,748,C(171,193,193));Text(message,22,774,C(218,185,129));batch.End();
   GraphicsDevice.SetRenderTarget(null);GraphicsDevice.Clear(Color.Black);batch.Begin(SpriteSortMode.Deferred,BlendState.Opaque,SamplerState.LinearClamp,null,null);batch.Draw(target,new Rectangle(0,0,GraphicsDevice.PresentationParameters.BackBufferWidth,GraphicsDevice.PresentationParameters.BackBufferHeight),Color.White);batch.End();
   if(capture!=null&&++draws>=5){using var f=File.Create(capture);target.SaveAsPng(f,1280,800);Console.WriteLine("Greyhaven original-content native capture: "+capture);Exit();}base.Draw(gameTime);}
  protected override void UnloadContent(){foreach(var t in textures.Values)t.Dispose();target?.Dispose();pixel?.Dispose();batch?.Dispose();document?.Dispose();base.UnloadContent();}
 }
}

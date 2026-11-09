// SPDX-License-Identifier: MIT
using System.Text.Json;
using Greyhaven.Content;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
namespace Greyhaven.Presentation;
public sealed partial class OriginalGame
{
    WorldItem? pendingItem, inspected;
    Vector2 pendingAt;
    double lastClick;
    string lastItem = "", draggedSpell = "";
    bool splitDrag, writingNote, replaceQuantity;
    Command? quantityCommand;
    int quantityMax = 1, journalOffset, tradeRow, tradePage;
    bool selling;
    string inputText = "";
    bool TextCapture => quantityCommand != null || writingNote;
    sealed class SavedPanel { public string Id {get;set;}="";public int X {get;set;}public int Y {get;set;}public bool Visible {get;set;} }
    string LayoutPath => Path.Combine(options.Root,"UserData","Interface","gumps.json");
    void LoadInterface()
    {
        TextInputEXT.TextInput += OnText;
        try { if (File.Exists(LayoutPath)) foreach (var saved in JsonSerializer.Deserialize<List<SavedPanel>>(File.ReadAllText(LayoutPath),ContentPackage.Json) ?? []) {var p=panels.FirstOrDefault(p=>p.Id==saved.Id);if(p!=null){p.X=Math.Clamp(saved.X,0,1280-p.W);p.Y=Math.Clamp(saved.Y,70,752-p.H);p.Visible=saved.Visible;}} } catch (Exception e) when(e is IOException or JsonException){message="Interface layout reset: "+e.Message;}
        PrepareInventoryScene();PrepareCombatScene();
    }
    void SaveInterface()
    {
        TextInputEXT.TextInput -= OnText;
        try {Directory.CreateDirectory(Path.GetDirectoryName(LayoutPath)!);string temp=LayoutPath+".writing";File.WriteAllText(temp,JsonSerializer.Serialize(panels.Where(p=>!p.Id.StartsWith("container:")).Select(p=>new SavedPanel{Id=p.Id,X=p.X,Y=p.Y,Visible=p.Visible}),ContentPackage.Json));File.Move(temp,LayoutPath,true);}catch(IOException e){Console.Error.WriteLine("Interface layout: "+e.Message);}
    }
    void OnText(char c) {if(!TextCapture)return;if(c>=32&&c<=126&&(writingNote||char.IsAsciiDigit(c))){if(!writingNote&&replaceQuantity){inputText="";replaceQuantity=false;}if(inputText.Length<(writingNote?200:4))inputText+=c;}}
    void StartInput(){replaceQuantity=!writingNote;TextInputEXT.StartTextInput();dragging=null;pendingItem=null;moving=null;}
    void StopInput(){writingNote=false;quantityCommand=null;TextInputEXT.StopTextInput();}
    bool SendInventory(Command c){if(session?.Send(c)==true)return true;message="Wait for the current transaction and try again.";Play("cancel");return false;}
    bool IsContainer(Panel p)=>p.Id=="bag"||p.Id.StartsWith("container:",StringComparison.Ordinal);
    string ContainerId(Panel p)=>p.Id=="bag"?"":p.Id[10..];
    bool InPanel(WorldItem i,Panel p)=>p.Id=="bag"?i.Where=="bag":p.Id=="doll"?i.Where=="equipped":p.Id.StartsWith("container:")&&i.Where=="container"&&i.ParentId==ContainerId(p);
    WorldItem? HitItem(Vector2 point)
    {
        var panel=At(point);
        return state.Items.LastOrDefault(i=>InventoryRules.Reachable(state,i)&&(panel==null?i.Where=="ground":InPanel(i,panel))&&Vector2.Distance(ItemPosition(i),point)<23);
    }
    void OpenContainer(WorldItem item)
    {
        if(!InventoryRules.Reachable(state,item)){message="That container is out of reach.";return;}
        string id="container:"+item.Id;var p=panels.FirstOrDefault(p=>p.Id==id);
        if(p==null){p=new(id,Math.Clamp((int)pointer.X-130,20,900),Math.Clamp((int)pointer.Y-80,80,390),270,360);panels.Add(p);}else{p.Visible=true;panels.Remove(p);panels.Add(p);}Play("open");
    }
    void ActivateItem(WorldItem item)
    {
        if(item.Definition.EndsWith("bandage")){SendInventory(new(){Type="bandage"});return;}
        if(!InventoryRules.Carried(state,item)&&!InventoryRules.Reachable(state,item)){message="That item is out of reach.";return;}
        if(itemDefs[item.Definition].ContainerSlots>0)OpenContainer(item);
        else if(itemDefs[item.Definition].Equipment!=null)SendInventory(new(){Type="use",ItemId=item.Id});
        else if(item.Definition.EndsWith("healing-tonic"))SendInventory(new(){Type="use",ItemId=item.Id});
        else if(item.Definition.EndsWith("tide-crystal")){rangedAiming=true;rangedItem=item.Id;message="Choose an enemy to throw this crystal.";}
        else{inspected=item;message=package.Localize(itemDefs[item.Definition].NameKey)+" inspected.";}
    }
    void ActivateSlot(int slot)
    {
        string binding=state.Quickbar[slot];
        if(binding.StartsWith("item:")){var item=InventoryRules.Find(state,binding[5..]);if(item==null||!InventoryRules.Carried(state,item))message="Shortcut item is no longer carried.";else ActivateItem(item);}
        else if(binding.StartsWith("spell:")){targetSpell=binding[6..];message="Choose a target for the spell.";}
    }
    int QuickSlot(Vector2 p)=>p.X>=24&&p.X<584&&p.Y>=692&&p.Y<742?(int)((p.X-24)/56):-1;
    bool UpdateInventory(KeyboardState k,MouseState mouse,float dt)
    {
        foreach(var p in panels.Where(p=>p.Id.StartsWith("container:")).ToList()){var i=InventoryRules.Find(state,ContainerId(p));if(i==null||!InventoryRules.Reachable(state,i))p.Visible=false;}
        if(TextCapture)
        {
            if(Press(k,Keys.Escape)){StopInput();message="Cancelled.";return true;}
            if(Press(k,Keys.Back)){if(replaceQuantity){inputText="";replaceQuantity=false;}else if(inputText.Length>0)inputText=inputText[..^1];}
            if(Press(k,Keys.Enter))
            {
                if(writingNote){if(SendInventory(new(){Type="note",Text=inputText})){journalOffset=0;StopInput();}}
                else if(int.TryParse(inputText,out int n)&&n>0&&n<=quantityMax){var command=quantityCommand!;command.Amount=n;command.ExpectedRevision=command.Type=="trade"?state.Revision:-1;if(SendInventory(command))StopInput();}
                else message="Choose a quantity from 1 to "+quantityMax+".";
            }
            return true;
        }
        if(state.Paused)return false;
        if(Press(k,Keys.T))Toggle("trade");
        if(Press(k,Keys.N)){var journal=panels.First(p=>p.Id=="journal");journal.Visible=true;writingNote=true;inputText="";StartInput();return true;}
        for(int i=0;i<10;i++)if(Press(k,i==9?Keys.D0:Keys.D1+i))ActivateSlot(i);
        if(pendingItem!=null&&mouse.LeftButton==ButtonState.Pressed&&Vector2.Distance(pendingAt,pointer)>5){dragging=pendingItem;pendingItem=null;Play("pickup");}
        if(mouse.LeftButton==ButtonState.Released&&oldMouse.LeftButton==ButtonState.Pressed){pendingItem=null;if(draggedSpell!=""){int q=QuickSlot(pointer);if(q>=0){SendInventory(new(){Type="bind",Slot=q,TargetId=draggedSpell});targetSpell="";}draggedSpell="";}}
        if(mouse.RightButton==ButtonState.Pressed&&oldMouse.RightButton==ButtonState.Released){int q=QuickSlot(pointer);if(q>=0)SendInventory(new(){Type="bind",Slot=q});}
        int wheel=mouse.ScrollWheelValue-oldMouse.ScrollWheelValue;
        var under=At(pointer);if(under?.Id=="trade"&&wheel!=0){tradePage=Math.Clamp(tradePage+(wheel>0?-1:1),0,45);tradeRow=0;}
        if(under?.Id=="journal"&&wheel!=0)journalOffset=Math.Clamp(journalOffset+(wheel>0?3:-3),0,Math.Max(0,state.Journal.Count-8));
        return false;
    }
    bool InventoryBegin(Vector2 p)
    {
        int q=QuickSlot(p);if(q>=0){ActivateSlot(q);return true;}
        var panel=At(p);
        if(panel?.Id=="journal"){if(p.Y>panel.Y+panel.H-45){writingNote=true;inputText="";StartInput();}return true;}
        if(panel?.Id=="trade")
        {
            if(p.Y<panel.Y+75){selling=p.X>panel.X+panel.W/2;tradeRow=0;tradePage=0;return true;}
            var entries=TradeItems();int row=(int)(p.Y-panel.Y-88)/32;
            if(row>=0&&row<entries.Count&&p.Y<panel.Y+panel.H-45){tradeRow=row;return true;}
            if(p.Y>panel.Y+panel.H-45&&entries.Count>0)
            {
                var entry=entries[Math.Clamp(tradeRow,0,entries.Count-1)];if(entry.amount<1){message="That item is out of stock.";return true;}var def=itemDefs[entry.definition];
                quantityCommand=new(){Type="trade",Destination=selling?"sell":"buy",ItemId=selling?entry.id:"",TargetId=selling?"":entry.definition};
                quantityMax=def.Stackable?Math.Min(999,entry.amount):1;inputText="1";StartInput();
            }
            return true;
        }
        var item=HitItem(p);
        if(item!=null)
        {
            inspected=item;
            if(lastItem==item.Id&&clock-lastClick<.35){pendingItem=null;ActivateItem(item);lastItem="";return true;}
            lastItem=item.Id;lastClick=clock;pendingItem=item;pendingAt=p;splitDrag=(Keyboard.GetState().IsKeyDown(Keys.LeftShift)||Keyboard.GetState().IsKeyDown(Keys.RightShift))&&item.Amount>1;
            message=package.Localize(itemDefs[item.Definition].NameKey)+" | "+item.Amount+" | weight "+InventoryRules.TotalWeight(package,state,item);return true;
        }
        if(panel?.Id=="doll"&&p.Y>panel.Y+310&&p.Y<panel.Y+340){Toggle("bag");return true;}
        if(panel?.Id=="book")for(int i=0;i<package.Definitions.Spells.Count;i++){var icon=new Vector2(panel.X+50+i%2*170,panel.Y+68+i/2*45);if(Vector2.Distance(icon,p)<24){draggedSpell=package.Definitions.Spells[i].Id;break;}}
        if(panel==null&&Vector2.Distance(Project(map.Spawn.X,map.Spawn.Y-1)+new Vector2(0,-45),p)<55){Toggle("trade");return true;}
        return false;
    }
    bool InventoryDrop(Vector2 p)
    {
        if(dragging==null)return false;
        var item=dragging;dragging=null;lastItem="";
        int q=QuickSlot(p);if(q>=0){SendInventory(new(){Type="bind",Slot=q,ItemId=item.Id});return true;}
        var panel=At(p);var target=HitItem(p);Command c=new(){Type="transfer",ItemId=item.Id};
        if(target?.Id==item.Id)target=null;
        if(target!=null&&itemDefs[target.Definition].ContainerSlots>0){c.Destination="container";c.TargetId=target.Id;c.X=70;c.Y=140;}
        else if(target!=null&&itemDefs[target.Definition].Stackable&&target.Definition==item.Definition){c.Destination="stack";c.TargetId=target.Id;}
        else if(panel!=null&&IsContainer(panel)){c.Destination=panel.Id=="bag"?"bag":"container";c.TargetId=ContainerId(panel);c.X=(int)p.X-panel.X;c.Y=(int)p.Y-panel.Y;}
        else if(panel?.Id=="doll"){c.Destination="equipped";int y=(int)p.Y-panel.Y;c.TargetId=p.X<panel.X+55?(y<135?"hat":y<205?"sword":y<280?"shield":""):"";}
        else if(panel?.Id=="trade"){message="Use the merchant Sell tab and confirm the offer.";return true;}
        else if(panel==null&&p.Y>70&&p.Y<752){var tile=Unproject(p);c.Destination="ground";c.X=tile.X;c.Y=tile.Y;}
        else{message="Drop cancelled.";return true;}
        if(splitDrag){quantityCommand=c;quantityMax=item.Amount;inputText=Math.Max(1,item.Amount/2).ToString();StartInput();}else SendInventory(c);
        return true;
    }
    List<(string id,string definition,int amount)> TradeItems()=>selling?state.Items.Where(i=>i.Where!="equipped"&&i.Definition!=InventoryRules.Currency&&InventoryRules.Carried(state,i)&&!state.Items.Any(c=>c.Where=="container"&&c.ParentId==i.Id)).Skip(tradePage*11).Take(11).Select(i=>(i.Id,i.Definition,i.Amount)).ToList():state.MerchantStock.Skip(tradePage*11).Take(11).Select(s=>("",s.Definition,s.Amount)).ToList();
    string PanelTitle(Panel p)=>p.Id.StartsWith("container:")?WorldAuthoring.ItemName(package,InventoryRules.Find(state,ContainerId(p))!).ToUpperInvariant():p.Id=="trade"?"NESSA - OUTFITTER":package.Localize(p.Id=="bag"?"ui.inventory":p.Id=="doll"?"ui.paperdoll":p.Id=="book"?"ui.spellbook":"ui.journal");
    void DrawPanelExtras(Panel p)
    {
        if(IsContainer(p))
        {
            var contents=state.Items.Where(i=>InPanel(i,p)).ToList();decimal weight=contents.Sum(i=>InventoryRules.TotalWeight(package,state,i));
            int cap=p.Id=="bag"?InventoryRules.BackpackSlots:itemDefs[InventoryRules.Find(state,ContainerId(p))!.Definition].ContainerSlots;
            Text($"{contents.Count}/{cap} objects | {weight:0.##} wt",p.X+20,p.Y+38,ColorOf(230,210,156));
        }
        if(p.Id=="doll")
        {
            foreach(var (slot,y) in new[]{("HAT",100),("HAND",165),("OFF",230)}){Rect(p.X+3,p.Y+y-23,45,45,ColorOf(18,30,33,190));Text(slot,p.X+4,p.Y+y+23,Color.Gold);}
            Rect(p.X+14,p.Y+340,p.W-28,54,ColorOf(24,38,41,235));Text(state.Adventure.Enabled?$"STR {state.Adventure.Strength} DEX {state.Adventure.Dexterity} INT {state.Adventure.Intelligence}":"STR 10 DEX 10 INT 10",p.X+20,p.Y+344);
            Rect(p.X+18,p.Y+311,p.W-36,26,ColorOf(24,38,41,210));Text("Open backpack",p.X+45,p.Y+315,Color.Gold);
            Text($"Weight {InventoryRules.CarriedWeight(package,state):0.##}/100",p.X+20,p.Y+370,Color.Gold);
        }
        if(p.Id=="journal")
        {
            Text("Mouse wheel: history   N: new note",p.X+15,p.Y+42,Color.Gold);
            int start=Math.Max(0,state.Journal.Count-8-journalOffset);
            for(int n=0;n<Math.Min(8,state.Journal.Count-start);n++){var j=state.Journal[start+n];string prefix=j.Note?"NOTE ":TimeSpan.FromSeconds(j.Time).ToString(@"mm\:ss")+" ";Wrap(prefix+j.Text,p.X+15,p.Y+75+n*39,60,j.Note?ColorOf(233,210,157):ColorOf(185,206,204));}
            Rect(p.X+12,p.Y+p.H-40,p.W-24,30,ColorOf(72,73,48));Text("Add a journal note",p.X+24,p.Y+p.H-34);
        }
        if(p.Id=="trade")
        {
            Rect(p.X+8,p.Y+36,p.W/2-12,32,!selling?ColorOf(102,83,48):ColorOf(40,55,59));Rect(p.X+p.W/2,p.Y+36,p.W/2-12,32,selling?ColorOf(102,83,48):ColorOf(40,55,59));Text("BUY",p.X+25,p.Y+43);Text("SELL",p.X+p.W/2+25,p.Y+43);
            var entries=TradeItems();for(int i=0;i<entries.Count;i++){var row=entries[i];var def=itemDefs[row.definition];if(i==tradeRow)Rect(p.X+8,p.Y+84+i*32,p.W-16,31,ColorOf(76,74,46));Image(def.InventoryVisual,p.X+13,p.Y+85+i*32,28,28);string name=package.Localize(def.NameKey);Text(name.Length>20?name[..20]:name,p.X+48,p.Y+91+i*32);Text($"{row.amount}  {(selling?Math.Max(1,def.Price/2):def.Price)}m",p.X+p.W-115,p.Y+91+i*32);}
            Rect(p.X+8,p.Y+p.H-39,p.W-16,30,ColorOf(102,83,48));Text("Review quantity / confirm",p.X+20,p.Y+p.H-33);Text($"Silver: {InventoryRules.Marks(state)} | Wheel: page {tradePage+1}",p.X+15,p.Y+p.H-67,Color.Gold);
        }
    }
    void DrawInventory()
    {
        for(int i=0;i<10;i++){int x=24+i*56;Rect(x,692,52,50,ColorOf(24,38,41,235));Text(i==9?"0":(i+1).ToString(),x+3,694,Color.Gold);string binding=state.Quickbar[i];if(binding.StartsWith("item:")){var item=InventoryRules.Find(state,binding[5..]);if(item!=null){Image(itemDefs[item.Definition].InventoryVisual,x+15,705,30,30);if(!InventoryRules.Carried(state,item))Text("!",x+38,698,Color.Salmon);}}else if(binding.StartsWith("spell:")){var spell=package.Definitions.Spells.FirstOrDefault(s=>s.Id==binding[6..]);if(spell!=null)Image(spell.Visual,x+15,705,30,30);}}
        if(help){Rect(18,649,670,42,ColorOf(18,30,33,220));Text("Shift-drag: split | Double-click: use/open | T: trade | N: note",24,654);Text("Drag item/spell to quickbar | Right-click a shortcut to clear",24,675);}
        if(draggedSpell!=""){var spell=package.Definitions.Spells.First(s=>s.Id==draggedSpell);Image(spell.Visual,pointer.X-18,pointer.Y-18,36,36);}
        var hovered=HitItem(pointer);
        if(hovered!=null&&dragging==null&&!TextCapture)
        {
            var d=itemDefs[hovered.Definition];int x=Math.Clamp((int)pointer.X+20,5,860),y=Math.Clamp((int)pointer.Y+25,75,550);Rect(x,y,410,112,ColorOf(13,25,29,245));Text(WorldAuthoring.ItemName(package,hovered),x+12,y+10,Color.Gold);Text($"Amount {hovered.Amount} | Total wt {InventoryRules.TotalWeight(package,state,hovered):0.##}",x+12,y+34);Text(d.ContainerSlots>0?$"Container: {d.ContainerSlots} objects / {d.ContainerWeight} wt":d.Equipment is Equipment e?$"{e.Slot} | STR {e.RequiredStrength} INT {e.RequiredIntelligence}"+(e.TwoHanded?" | 2H":""):$"Trade: buy {d.Price} / sell {Math.Max(1,d.Price/2)}",x+12,y+58);Text(hovered.QuestId!=""?"QUEST ITEM | right-click to unmark":"Double-click to use | right-click: quest mark",x+12,y+82,ColorOf(158,184,184));
        }
        if(TextCapture)
        {
            Rect(280,240,720,270,ColorOf(17,29,33,250));Text(writingNote?"NEW JOURNAL NOTE":"CHOOSE QUANTITY",305,265,Color.Gold);Wrap(inputText+"_",305,312,72);
            if(quantityCommand?.Type=="trade"){var c=quantityCommand;string def=c.Destination=="buy"?c.TargetId:InventoryRules.Find(state,c.ItemId)?.Definition??"";if(itemDefs.TryGetValue(def,out var d)){int.TryParse(inputText,out int n);Text($"{c.Destination.ToUpperInvariant()} | unit {(c.Destination=="buy"?d.Price:Math.Max(1,d.Price/2))}m | total {n*(c.Destination=="buy"?d.Price:Math.Max(1,d.Price/2))}m",305,385,Color.Gold);}}
            Text(writingNote?"Enter: save note | Esc: cancel":$"1 - {quantityMax} | Enter: confirm | Esc: cancel",305,450);
        }
    }
    int smokeStep;bool smokeDone;double smokeSeconds;
    void PrepareInventoryScene()
    {
        if(options.InventoryScene==null)return;
        foreach(var p in panels)p.Visible=p.Id is "bag" or "doll";
        if(options.InventoryScene=="trade"){var p=panels.First(p=>p.Id=="trade");p.Visible=true;p.X=45;p.Y=105;}
        if(options.InventoryScene=="journal"){var p=panels.First(p=>p.Id=="journal");p.Visible=true;p.X=30;p.Y=110;}
        if(options.InventoryScene=="containers"){var item=state.Items.FirstOrDefault(i=>i.Definition.EndsWith("field-satchel"));if(item!=null){OpenContainer(item);var panel=panels.Last();panel.X=410;panel.Y=355;}}
    }
    void RunInventorySmoke(float dt)
    {
        if(!options.InventorySmoke||smokeDone)return;
        smokeSeconds+=dt;if(smokeSeconds>15)throw new ContentException("Native inventory smoke timed out at "+smokeStep);
        if(session?.Busy!=false)return;
        if(state.Paused){SendInventory(new(){Type="pause",Destination="resume"});return;}
        WorldItem Item(string suffix)=>state.Items.First(i=>i.Definition.EndsWith(suffix));
        void Require(bool ok,string why){if(!ok)throw new ContentException("Native inventory route failed: "+why);}
        void Drag(WorldItem i,Vector2 target){pointer=ItemPosition(i);InventoryBegin(pointer);dragging=pendingItem;pendingItem=null;Require(dragging!=null,"item hit testing");pointer=target;InventoryDrop(target);}
        switch(smokeStep)
        {
            case 0:
                foreach(var p in panels)p.Visible=p.Id is "bag" or "doll";
                Drag(Item("healing-tonic"),ItemPosition(Item("field-satchel")));break;
            case 1:
                Require(Item("healing-tonic").Where=="container","drop into closed satchel");OpenContainer(Item("field-satchel"));var satchel=panels.Last();satchel.X=410;satchel.Y=340;
                var bag=panels.First(p=>p.Id=="bag");pointer=ItemPosition(Item("healing-tonic"));InventoryBegin(pointer);dragging=pendingItem;pendingItem=null;splitDrag=true;InventoryDrop(new(bag.X+110,bag.Y+270));Require(quantityCommand!=null,"split prompt");inputText="";OnText('3');UpdateInventory(new KeyboardState(Keys.Enter),Mouse.GetState(),0);break;
            case 2:
                Require(state.Items.Where(i=>i.Definition.EndsWith("healing-tonic")).Sum(i=>i.Amount)==8,"split conservation");Require(Item("healing-tonic").Amount==5,"nested remainder");splitDrag=false;Drag(Item("healing-tonic"),new(48,715));break;
            case 3:
                Require(state.Quickbar[0]=="item:"+Item("healing-tonic").Id,"shortcut binding from nested item");var doll=panels.First(p=>p.Id=="doll");Drag(Item("wizard-hat"),new(doll.X+23,doll.Y+100));break;
            case 4:
                Require(Item("wizard-hat").Where=="equipped"&&Item("trail-hat").Where=="bag","paperdoll replacement");var trade=panels.First(p=>p.Id=="trade");trade.Visible=true;selling=false;tradeRow=TradeItems().FindIndex(i=>i.definition.EndsWith("healing-tonic"));InventoryBegin(new(trade.X+60,trade.Y+trade.H-20));Require(quantityCommand?.Type=="trade","trade confirmation");inputText="";OnText('2');UpdateInventory(new KeyboardState(Keys.Enter),Mouse.GetState(),0);break;
            case 5:
                Require(InventoryRules.Marks(state)==109,"merchant payment");writingNote=true;inputText="";StartInput();foreach(char ch in "Packed the satchel and traded with Nessa.")OnText(ch);UpdateInventory(new KeyboardState(Keys.Enter),Mouse.GetState(),0);break;
            case 6:
                Require(state.Journal.Last().Note,"journal note saved");panels.First(p=>p.Id=="trade").Visible=false;smokeDone=true;Console.WriteLine("Native inventory routes: container drop, split dialog, quickbar, paperdoll, merchant confirmation and note input passed.");break;
        }
        smokeStep++;
    }

}

// GPL-2.0: authoritative inventory transactions owned by the ServUO-linked host.
using System.Text.Json;
using Greyhaven.Content;
namespace Greyhaven.Server;
public static class InventoryTransactions
{
    public static bool Handles(string type) => type is "transfer" or "use" or "bind" or "note" or "trade" or "outfit";
    public static void Log(SessionState s, string text, bool note = false)
    {
        s.Journal.Add(new() { Text = text, Time = s.ElapsedSeconds, Note = note });
        while (s.Journal.Count > 200) s.Journal.RemoveAt(0);
    }
    public static void Initialize(ContentPackage p, SessionState s, bool upgrading = false)
    {
        if (s.InventoryVersion >= 1) return;
        s.InventoryVersion = 1;
        foreach (var i in s.Items)
        {
            if (!upgrading) i.Amount = i.Definition == InventoryRules.Currency ? 125 : i.Definition.EndsWith("healing-tonic") ? 8 : i.Definition.EndsWith("tide-crystal") ? 3 : i.Definition.EndsWith("sealed-scroll") ? 2 : 1;
        }
        var satchel = p.Definitions.Items.FirstOrDefault(d => d.Id.EndsWith("field-satchel"));
        var chest = p.Definitions.Items.FirstOrDefault(d => d.Id.EndsWith("sea-chest"));
        // Phase 3 saves receive the new container specimens once, without changing existing possessions.
        if (satchel != null && !s.Items.Any(i => i.Definition == satchel.Id)) s.Items.Add(new() { Id = "reach.satchel", Definition = satchel.Id, X = 200, Y = 285 });
        if (chest != null && !s.Items.Any(i => i.Definition == chest.Id)) s.Items.Add(new() { Id = "reach.chest", Definition = chest.Id, Where = "ground", X = p.CanStand(p.Map,s.X-1,s.Y)?s.X-1:s.X, Y = s.Y, Z = p.Map.Get(p.CanStand(p.Map,s.X-1,s.Y)?s.X-1:s.X,s.Y).Z });
        s.MerchantStock = p.Definitions.Items.Where(d => d.Id != InventoryRules.Currency&&!d.Anchored&&(p.Map.Objects.Any(o=>o.Kind=="npc")||!d.Id.EndsWith("bandage"))).Select(d => new StockEntry { Definition = d.Id, Amount = 20 }).ToList();
        Log(s, upgrading ? "Campaign inventory upgraded. Existing possessions retained." : p.Map.Objects.Count>0?"Arrived in Greyhaven. Nessa trades in the square. G opens your atlas; E interacts nearby.":"Arrived at Wayfarer's Reach. Nessa the outfitter trades beside the camp.");
    }
    public static (SessionState state, string message) Apply(ContentPackage p, SessionState original, Command c)
    {
        if(original.Combat.Dead&&c.Type is not ("note" or "bind"))throw new ContentException("Recover at camp before handling items.");
        if (original.Paused) throw new ContentException("Campaign paused.");
        if (c.ExpectedRevision >= 0 && c.ExpectedRevision != original.Revision) throw new ContentException("Inventory changed. Review the offer and try again.");
        var s = JsonSerializer.Deserialize<SessionState>(Wire.Encode(original), ContentPackage.Json)!;
        var defs = p.Definitions.Items.ToDictionary(d => d.Id);
        string Name(WorldItem i) => WorldAuthoring.ItemName(p,i);
        WorldItem Item(string id)
        {
            var i = InventoryRules.Find(s, id) ?? throw new ContentException("Unknown item.");
            if (!InventoryRules.Reachable(s, i)) throw new ContentException("That item is out of reach.");
            return i;
        }
        void Bag(WorldItem i) { i.Where = "bag"; i.ParentId = ""; i.X = 60; i.Y = 190; i.Z = 0; }
        void Remove(WorldItem i)
        {
            s.Items.Remove(i);
            for (int x = 0; x < 10; x++) if (s.Quickbar[x] == "item:" + i.Id) s.Quickbar[x] = "";
        }
        int Amount(WorldItem i)
        {
            int n = c.Amount == 0 ? i.Amount : c.Amount;
            if (n < 1 || n > i.Amount || n > InventoryRules.MaxStack) throw new ContentException("Invalid quantity.");
            return n;
        }
        string message;
        if (c.Type == "transfer" || c.Type == "use")
        {
            var item = Item(c.ItemId); var d = defs[item.Definition];
            if(d.Anchored)throw new ContentException("That fallen pack is anchored. Open it to reclaim the contents.");
            string destination = c.Type == "use" ? item.Where == "equipped" ? "bag" : d.Equipment != null ? "equipped" : "inspect" : c.Destination;
            if (destination == "inspect") return (s, Name(item) + ": " + item.Amount + " item(s), " + InventoryRules.TotalWeight(p, s, item) + " weight.");
            int n = Amount(item);
            if (n < item.Amount)
            {
                if (!d.Stackable) throw new ContentException("This item cannot be split.");
                item.Amount -= n;
                item = new WorldItem { Id = "item." + Guid.NewGuid().ToString("N"), Definition = item.Definition, QuestId=item.QuestId,Label=item.Label,Amount = n, Where = item.Where, ParentId = item.ParentId, X = item.X, Y = item.Y, Z = item.Z };
                s.Items.Add(item);
            }
            if(item.QuestId!=""&&(destination=="ground"||destination=="container"&&InventoryRules.Root(s,Item(c.TargetId)).Where=="ground"))throw new ContentException("Unmark this quest item before leaving it outside your pack.");
            if (destination == "stack")
            {
                var target = Item(c.TargetId);
                if(WorldAuthoring.IsKey(p,item.Id)||WorldAuthoring.IsKey(p,target.Id))throw new ContentException("A named vault key cannot be merged with other items.");
                if (target.QuestId!=item.QuestId||target.Id == item.Id || !d.Stackable || target.Definition != item.Definition || target.Where == "equipped" || target.Amount + n > InventoryRules.MaxStack) throw new ContentException("These stacks cannot be combined.");
                target.Amount += n;
                for (int x = 0; x < 10; x++) if (s.Quickbar[x] == "item:" + item.Id) s.Quickbar[x] = "item:" + target.Id;
                s.Items.Remove(item); message = "Combined " + n + " " + Name(item) + ".";
            }
            else if (destination == "bag" || destination == "container")
            {
                if (destination == "container")
                {
                    var parent = Item(c.TargetId);
                    if (defs[parent.Definition].ContainerSlots == 0) throw new ContentException("That is not a container.");
                    item.ParentId = parent.Id;
                }
                else item.ParentId = "";
                item.Where = destination; item.X = Math.Clamp(c.X, 42, 225); item.Y = Math.Clamp(c.Y, 100, 305); item.Z = 0;
                message = "Placed " + n + " " + Name(item) + (destination == "bag" ? " in backpack." : " in container.");
            }
            else if (destination == "equipped")
            {
                var e = d.Equipment ?? throw new ContentException("That item cannot be worn.");
                if (c.TargetId != "" && c.TargetId != e.Slot) throw new ContentException("That equipment does not fit this slot.");
                if (e.RequiredStrength > (s.Adventure.Enabled?s.Adventure.Strength:InventoryRules.Strength) || e.RequiredIntelligence > (s.Adventure.Enabled?s.Adventure.Intelligence:InventoryRules.Intelligence)) throw new ContentException("Equipment requirements are not met.");
                foreach (var old in s.Items.Where(i => i.Id != item.Id && i.Where == "equipped" && (defs[i.Definition].Equipment!.Slot == e.Slot || e.TwoHanded && defs[i.Definition].Equipment!.Slot == "shield" || e.Slot == "shield" && defs[i.Definition].Equipment!.TwoHanded)).ToList()) Bag(old);
                item.Where = "equipped"; item.ParentId = ""; item.Z = 0; message = "Equipped " + Name(item) + ".";
            }
            else if (destination == "ground")
            {
                if (!p.Map.Contains(c.X, c.Y) || !p.CanStand(p.Map, c.X, c.Y) || Math.Max(Math.Abs((long)c.X - s.X), Math.Abs((long)c.Y - s.Y)) > 2) throw new ContentException("Invalid or unreachable ground target.");
                item.Where = "ground"; item.ParentId = ""; item.X = c.X; item.Y = c.Y; item.Z = p.Map.Get(c.X, c.Y).Z; message = "Placed " + Name(item) + " on ground.";
            }
            else throw new ContentException("Invalid drop target.");
        }
        else if (c.Type == "bind")
        {
            if (c.Slot < 0 || c.Slot >= 10) throw new ContentException("Unknown quickbar slot.");
            string binding = "";
            if (c.ItemId != "") { var item = Item(c.ItemId); if (!InventoryRules.Carried(s, item)) throw new ContentException("Quickbar items must be carried."); binding = "item:" + item.Id; }
            else if (c.TargetId != "") { if (!p.Definitions.Spells.Any(x => x.Id == c.TargetId)) throw new ContentException("Unknown spell."); binding = "spell:" + c.TargetId; }
            s.Quickbar[c.Slot] = binding; message = binding == "" ? "Quickbar slot cleared." : "Quickbar shortcut assigned.";
        }
        else if (c.Type == "note")
        {
            string note = new((c.Text ?? "").Where(ch => ch >= 32 && ch <= 126).Take(240).ToArray());
            if (string.IsNullOrWhiteSpace(note)) throw new ContentException("Write a note first.");
            Log(s, note.Trim(), true); message = "Journal note saved.";
        }
        else if (c.Type == "outfit") { s.Outfit = 1 - s.Outfit; message = "Changed travel outfit."; }
        else if (c.Type == "trade")
        {
            if (!InventoryRules.MerchantReachable(p, s)) throw new ContentException("Nessa is too far away to trade.");
            if (c.Amount < 1 || c.Amount > 999) throw new ContentException("Choose a trade quantity from 1 to 999.");
            var stock = s.MerchantStock.FirstOrDefault(x => x.Definition == (c.Destination == "buy" ? c.TargetId : Item(c.ItemId).Definition)) ?? throw new ContentException("Nessa does not trade that item.");
            var d = defs[stock.Definition]; if (d.Id == InventoryRules.Currency) throw new ContentException("Currency is not merchandise.");
            int price = checked(c.Amount * (c.Destination == "buy" ? d.Price : Math.Max(1, d.Price / 2)));
            if (!d.Stackable && c.Amount > 1) throw new ContentException("Trade non-stackable equipment one piece at a time.");
            if (c.Destination == "buy")
            {
                if (stock.Amount < c.Amount || InventoryRules.Marks(s) < price) throw new ContentException("Insufficient stock or silver marks.");
                int owing = price;
                foreach (var coin in s.Items.Where(i => i.Definition == InventoryRules.Currency && InventoryRules.Carried(s, i)).ToList()) { int pay = Math.Min(coin.Amount, owing); coin.Amount -= pay; owing -= pay; if (coin.Amount == 0) Remove(coin); if (owing == 0) break; }
                stock.Amount -= c.Amount;
                var bought = new WorldItem { Id = "item." + Guid.NewGuid().ToString("N"), Definition = d.Id, Amount = c.Amount, X = 70, Y = 150 };
                s.Items.Add(bought); message = "Bought " + c.Amount + " " + p.Localize(d.NameKey) + " for " + price + " marks.";
            }
            else if (c.Destination == "sell")
            {
                var sold = Item(c.ItemId);
                if(sold.QuestId!="")throw new ContentException("Marked quest items cannot be sold.");
                if(WorldAuthoring.IsKey(p,sold.Id))throw new ContentException("Keep the vault key; Nessa will not buy it.");
                if (!InventoryRules.Carried(s, sold) || sold.Where == "equipped" || sold.Amount < c.Amount || s.Items.Any(i => i.Where == "container" && i.ParentId == sold.Id) || stock.Amount + c.Amount > InventoryRules.MaxStack) throw new ContentException("Only carried, unequipped, empty merchandise can be sold.");
                sold.Amount -= c.Amount; if (sold.Amount == 0) Remove(sold); stock.Amount += c.Amount;
                int remaining = price;
                foreach (var coin in s.Items.Where(i => i.Definition == InventoryRules.Currency && InventoryRules.Carried(s, i))) { int n = Math.Min(InventoryRules.MaxStack - coin.Amount, remaining); coin.Amount += n; remaining -= n; if (remaining == 0) break; }
                while (remaining > 0) { int n = Math.Min(remaining, InventoryRules.MaxStack); s.Items.Add(new() { Id = "item." + Guid.NewGuid().ToString("N"), Definition = InventoryRules.Currency, Amount = n, X = 70, Y = 150 }); remaining -= n; }
                message = "Sold " + c.Amount + " " + p.Localize(d.NameKey) + " for " + price + " marks.";
            }
            else throw new ContentException("Invalid trade direction.");
        }
        else throw new ContentException("Unsupported inventory command.");
        InventoryRules.Validate(p, s);
        if (c.Type is not ("bind" or "note")) Log(s, message);
        s.Revision++;
        return (s, message);
    }
}

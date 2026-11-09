// SPDX-License-Identifier: MIT
namespace Greyhaven.Content;

// One graph and one validator are shared by transactions, saves and presentation.
public static class InventoryRules
{
    public const int MaxStack = 9999, MaxItems = 512, BackpackSlots = 125, MaxDepth = 8;
    public const decimal CarryLimit = 100;
    public const int Strength = 10, Intelligence = 10;
    public const string Currency = "greyhaven.item.silver-marks";
    public static WorldItem? Find(SessionState s, string id) => s.Items.FirstOrDefault(i => i.Id == id);
    public static WorldItem Root(SessionState s, WorldItem item)
    {
        var seen = new HashSet<string>();
        while (item.Where == "container")
        {
            if (!seen.Add(item.Id) || seen.Count > MaxDepth+1) throw new ContentException("Container nesting is too deep or circular.");
            item = Find(s, item.ParentId) ?? throw new ContentException("Container parent is missing.");
        }
        if(seen.Count>MaxDepth&&item.Definition!=CombatRules.CorpseDefinition)throw new ContentException("Container nesting is too deep.");
        return item;
    }
    public static bool Carried(SessionState s, WorldItem i) => Root(s, i).Where is "bag" or "equipped";
    public static bool Reachable(SessionState s, WorldItem i)
    {
        var root = Root(s, i);
        return root.Where != "ground" || Math.Max(Math.Abs((long)root.X - s.X), Math.Abs((long)root.Y - s.Y)) <= 2;
    }
    public static decimal TotalWeight(ContentPackage p, SessionState s, WorldItem i)
    {
        decimal weight = p.Definitions.Items.First(d => d.Id == i.Definition).Weight * i.Amount;
        foreach (var child in s.Items.Where(c => c.Where == "container" && c.ParentId == i.Id)) weight += TotalWeight(p, s, child);
        return weight;
    }
    public static decimal CarriedWeight(ContentPackage p, SessionState s) => s.Items.Where(i => i.Where is "bag" or "equipped").Sum(i => TotalWeight(p, s, i));
    public static int Marks(SessionState s) => s.Items.Where(i => i.Definition == Currency && Carried(s, i)).Sum(i => i.Amount);
    public static bool MerchantReachable(ContentPackage p, SessionState s) => p.Map.Objects.Any(o=>o.Kind=="npc"&&o.Role=="merchant")?p.Map.Objects.Any(o=>o.Kind=="npc"&&o.Role=="merchant"&&CombatRules.Distance(s.X,s.Y,AdventureRules.Position(s,o).x,AdventureRules.Position(s,o).y)<=2):Math.Max(Math.Abs((long)s.X-p.Map.Spawn.X),Math.Abs((long)s.Y-(p.Map.Spawn.Y-1)))<=2;
    public static void Validate(ContentPackage p, SessionState s)
    {
        if(s.InventoryVersion is < 0 or > 1)throw new ContentException("Unsupported inventory version.");
        if (s.Items is null || s.Items.Count > MaxItems) throw new ContentException("Too many inventory objects.");
        var defs = p.Definitions.Items.ToDictionary(d => d.Id);
        var ids = new HashSet<string>(); var slots = new HashSet<string>();
        foreach (var i in s.Items)
        {
            if (i is null || i.QuestId is null || i.Label is null || i.Label.Length>80 || i.QuestId!=""&&!p.AdventureCatalog.Quests.Any(q=>q.Id==i.QuestId) || string.IsNullOrWhiteSpace(i.Id) || i.Id.Length > 100 || !ids.Add(i.Id) || i.Definition is null || !defs.TryGetValue(i.Definition, out var d)) throw new ContentException("Invalid saved item identity.");
            if(d.Anchored&&i.Where!="ground")throw new ContentException("A fallen pack must stay on the ground.");
            if (i.Amount < 1 || i.Amount > MaxStack || !d.Stackable && i.Amount != 1) throw new ContentException("Invalid stack quantity.");
            if (i.ParentId is null || i.Where != "container" && i.ParentId != "") throw new ContentException("Unexpected container parent.");
            if (i.Where == "ground")
            {
                if (!p.CanStand(p.Map, i.X, i.Y) || i.Z != p.Map.Get(i.X, i.Y).Z) throw new ContentException("Invalid ground item.");
            }
            else if (i.Where is "bag" or "container")
            {
                if (i.X < 42 || i.X > 225 || i.Y < 100 || i.Y > 305) throw new ContentException("Invalid container position.");
                if (i.Where == "container")
                {
                    var parent = Find(s, i.ParentId);
                    if (parent == null || !defs.TryGetValue(parent.Definition, out var pd) || pd.ContainerSlots == 0) throw new ContentException("That object cannot contain items.");
                }
            }
            else if (i.Where == "equipped")
            {
                var e = d.Equipment;
                if (e == null || !slots.Add(e.Slot) || e.RequiredStrength > (s.Adventure.Enabled?s.Adventure.Strength:Strength) || e.RequiredIntelligence > (s.Adventure.Enabled?s.Adventure.Intelligence:Intelligence)) throw new ContentException("Equipment requirements or slot conflict.");
            }
            else throw new ContentException("Invalid item location.");
        }
        foreach (var i in s.Items) Root(s, i); // Detect cycles before recursive weight traversal.
        if (s.Items.Count(i => i.Where == "bag") > BackpackSlots) throw new ContentException("Backpack is full.");
        foreach (var i in s.Items.Where(i => defs[i.Definition].ContainerSlots > 0))
        {
            var d = defs[i.Definition];
            if (s.Items.Count(c => c.Where == "container" && c.ParentId == i.Id) > d.ContainerSlots || TotalWeight(p, s, i) - d.Weight > d.ContainerWeight) throw new ContentException("Container capacity exceeded.");
        }
        if (CarriedWeight(p, s) > CarryLimit) throw new ContentException("That would exceed your carrying capacity.");
        if (s.Items.Any(i => i.Where == "equipped" && defs[i.Definition].Equipment!.TwoHanded) && slots.Contains("shield")) throw new ContentException("A two-handed weapon needs both hands.");
        if (s.Quickbar is null || s.Quickbar.Count != 10 || s.Quickbar.Any(b => b is null || b.Length > 120 || b != "" && !b.StartsWith("item:", StringComparison.Ordinal) && !b.StartsWith("spell:", StringComparison.Ordinal))) throw new ContentException("Invalid quickbar.");
        if(s.Quickbar.Any(b=>b.StartsWith("spell:")&&!p.Definitions.Spells.Any(d=>d.Id==b[6..])||b.StartsWith("item:")&&!ids.Contains(b[5..])))throw new ContentException("Unknown quickbar binding.");
        if (s.Journal is null || s.Journal.Count > 200 || s.Journal.Any(j => j is null || j.Text is null || j.Text.Length > 240 || j.Time < 0 || !double.IsFinite(j.Time))) throw new ContentException("Invalid journal.");
        if (s.MerchantStock is null || s.MerchantStock.Count > defs.Count || s.MerchantStock.Any(x => x is null || x.Definition is null || !defs.ContainsKey(x.Definition) || x.Amount < 0 || x.Amount > MaxStack) || s.MerchantStock.Select(x => x.Definition).Distinct().Count() != s.MerchantStock.Count) throw new ContentException("Invalid merchant stock.");
        if (s.Outfit is < 0 or > 1) throw new ContentException("Invalid outfit.");
    }
}

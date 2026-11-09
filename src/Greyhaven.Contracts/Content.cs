namespace Greyhaven.Contracts;

// Standalone original contracts. No dependency on GPL server implementation.
public readonly record struct TilePosition(ushort X, ushort Y, sbyte Z);
public enum Facing : byte { North, NorthEast, East, SouthEast, South, SouthWest, West, NorthWest }
public enum VisualDomain { Terrain, Static, Body, Equipment, Gump, Icon, Sound }
public sealed record VisualReference(string ContentId, VisualDomain Domain);
public sealed record ItemDefinition(string Id, string NameKey, string GroundVisualId,
    string InventoryVisualId, string? EquipmentVisualId, string? PaperdollVisualId,
    decimal UnitWeight, bool Stackable);
public sealed record TileDefinition(string Id, string VisualId, bool Impassable,
    bool Surface, byte Height);
public sealed record AnimationClip(string Id, Facing Facing, int Frames, int FrameMilliseconds);
public sealed record LegacyVisualMapping(string ContentId, VisualDomain Domain, ushort LegacyId);
// Legacy IDs are scoped by domain and remain stable across package versions.
// The bridge is transitional: authoring uses ContentId, not graphic numbers.
public interface IContentCatalog
{
    ItemDefinition GetItem(string id);
    TileDefinition GetTile(string id);
    VisualReference GetVisual(string id);
}

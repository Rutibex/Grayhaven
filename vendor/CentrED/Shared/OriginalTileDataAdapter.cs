// SPDX-License-Identifier: MIT
namespace CentrED;
public static class OriginalTileDataAdapter {
 public static void Verify(Greyhaven.Content.ContentPackage package){
  var native=new TileDataProvider(package);
  foreach(var d in package.Definitions.Terrain)
   if(native.LandTiles[d.NumericId].IsImpassable!=d.Impassable||native.LandTiles[d.NumericId].IsWet!=d.Wet||native.LandTiles[d.NumericId].Name!=package.Localize(d.NameKey))throw new Greyhaven.Content.ContentException("CentrED terrain adapter mismatch.");
  foreach(var d in package.Definitions.Statics)
   if(native.StaticTiles[d.NumericId].Height!=d.Height||native.StaticTiles[d.NumericId].IsImpassable!=d.Impassable)throw new Greyhaven.Content.ContentException("CentrED static adapter mismatch.");
 }
}

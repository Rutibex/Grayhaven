// GPL-2.0: ServUO provider seam. No original package parser is copied into the server fork.
using System;
namespace Server {
 public static class OriginalContentBridge {
  public static bool Enabled { get; private set; }
  public static LandData[] LandDefinitions { get; private set; }
  public static ItemData[] StaticDefinitions { get; private set; }
  public static Func<int,int,LandTile[]> LandBlock { get; private set; }
  public static Func<int,int,StaticTile[][][]> StaticBlock { get; private set; }
  public static void Install(LandData[] land,ItemData[] statics,Func<int,int,LandTile[]> landBlock,Func<int,int,StaticTile[][][]> staticBlock) {
   if(Enabled)throw new InvalidOperationException("Original content provider is already installed.");
   LandDefinitions=land;StaticDefinitions=statics;LandBlock=landBlock;StaticBlock=staticBlock;Enabled=true;
  }
 }
}

// SPDX-License-Identifier: MIT
namespace CentrED;
public static class GreyhavenBootstrap {
 [STAThread] public static void Main(string[] args){
  if(args.Contains("--original-content")){Greyhaven.Presentation.OriginalBoot.Run(args,true,OriginalTileDataAdapter.Verify);return;}
  Application.Main(args);
 }
}

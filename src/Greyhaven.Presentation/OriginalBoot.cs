// SPDX-License-Identifier: MIT
using Greyhaven.Content;
namespace Greyhaven.Presentation;
public static class OriginalBoot {
 public static void Run(string[] args,bool editor=false,Action<ContentPackage>? verifyAdapter=null){
  var options=BootOptions.Parse(args);using var package=new ContentPackage(options.Root,options.Map);verifyAdapter?.Invoke(package);
  if(args.Contains("--edit-smoke")){
   if(!editor||options.Export==null)throw new ContentException("Editor --edit-smoke requires --export PATH.");
   var map=package.Map.Clone();map.Get(15,19).Terrain="greyhaven.terrain.stone";map.Get(15,19).Z=2;map.Statics.Add(new(){Id="editor.smoke.lantern",Definition="greyhaven.static.lantern",X=15,Y=19,Z=2});package.SaveMap(map,options.Export);package.LoadMapOverride(options.Export);verifyAdapter?.Invoke(package);
  }
  if(options.Check||args.Contains("--edit-smoke")){package.VerifyAll();package.SaveAudit(options.DefaultAudit(editor?"editor":"client"),editor?"CentrED-original-editor":"TazUO-original-client");Console.WriteLine(Wire.Encode(new{ok=true,role=editor?"editor":"client",fingerprint=package.Fingerprint,mapHash=package.MapHash,legacyFallback=false,files=package.Accesses.Count}));return;}
  var selection=BootOptions.Value(args,"--campaign-menu");if(selection!=null){using var menu=new CampaignMenu(package,selection,options.Capture);menu.Run();return;}
  using var game=new OriginalGame(package,options,editor);game.Run();package.SaveAudit(options.DefaultAudit(editor?"editor":"client"),editor?"CentrED-original-editor":"TazUO-original-client");
 }
}

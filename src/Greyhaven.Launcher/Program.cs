// SPDX-License-Identifier: MIT
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Greyhaven.Content;
Process? ownedServer=null;string? ready=null;var logs=new List<Task>();var writers=new List<StreamWriter>();var started=new List<Process>();string sessionId=Guid.NewGuid().ToString("N")[..8];
try {
 var options=BootOptions.Parse(args);string runtime=Path.Combine(options.Root,"Runtime"),logDir=Path.Combine(options.Root,"UserData","Logs");Directory.CreateDirectory(logDir);
 string suffix=OperatingSystem.IsWindows()?".exe":"";
 Process Start(string name,IEnumerable<string> arguments,bool log=true){var start=new ProcessStartInfo(Path.Combine(runtime,name+suffix)){UseShellExecute=false,CreateNoWindow=true,WorkingDirectory=options.Root,RedirectStandardOutput=log,RedirectStandardError=log};foreach(var a in arguments)start.ArgumentList.Add(a);var process=Process.Start(start)??throw new Exception("Could not start "+name);started.Add(process);if(log){var writer=new StreamWriter(Path.Combine(logDir,name+"-"+sessionId+"-"+Guid.NewGuid().ToString("N")[..6]+".log")){AutoFlush=true};writers.Add(writer);object sync=new();logs.Add(Task.Run(async()=>{string? line;while((line=await process.StandardOutput.ReadLineAsync())!=null)lock(sync)writer.WriteLine(line);}));logs.Add(Task.Run(async()=>{string? line;while((line=await process.StandardError.ReadLineAsync())!=null)lock(sync)writer.WriteLine(line);}));}return process;}
 var common=new List<string>{"--content-root",options.Root};string? map=options.Map;
 if(args.Contains("--edited-map")){map=Path.Combine(options.Root,"UserData","Maps","reach-edit.json");if(!File.Exists(map))throw new ContentException("No edited map yet. Open Map-Editor.cmd and press Ctrl+S to export one.");}
 if(map!=null)common.AddRange(["--map",Path.GetFullPath(map)]);
 if(options.ProductionScene!=null)common.AddRange(["--production-scene",options.ProductionScene]);if(options.ProductionSmoke)common.Add("--production-smoke");
 if(options.AdventureScene!=null)common.AddRange(["--adventure-scene",options.AdventureScene]);if(options.AdventureSmoke)common.Add("--adventure-smoke");
 if(options.WorkshopSmoke)common.Add("--workshop-smoke");if(options.WorldScene!=null)common.AddRange(["--world-scene",options.WorldScene]);
 if(options.SaveRoot!=null)common.AddRange(["--save-root",Path.GetFullPath(options.SaveRoot)]);if(options.StartAt!=null)common.AddRange(["--start-at",options.StartAt]);if(options.ViewAt!=null)common.AddRange(["--view-at",options.ViewAt]);
 if(options.InventoryScene!=null)common.AddRange(["--inventory-scene",options.InventoryScene]);if(options.InventorySmoke)common.Add("--inventory-smoke");if(options.CombatSmoke)common.Add("--combat-smoke");if(options.CombatScene)common.Add("--combat-scene");
 if(options.Capture!=null)common.AddRange(["--capture",options.Capture]);
 if(args.Contains("--editor")){var edit=Start("Greyhaven.Editor",common);await edit.WaitForExitAsync();if(edit.ExitCode!=0)throw new Exception("Editor could not boot. See the editor log in UserData/Logs.");}
 else if(args.Contains("--art-proof")){var proof=Start("Greyhaven.Client",common.Concat(["--art-proof"]));await proof.WaitForExitAsync();if(proof.ExitCode!=0)throw new Exception("Art proof could not boot. See the client log in UserData/Logs.");}
 else {
  bool direct=args.Contains("--campaign");
  while(true){
   string selectionPath=Path.Combine(logDir,"selection-"+Guid.NewGuid().ToString("N")+".json");CampaignSelection selection;
   if(direct)selection=new(){Campaign=BootOptions.Value(args,"--campaign")!,Load=BootOptions.Value(args,"--load")??"latest"};
   else{var menu=Start("Greyhaven.Client",common.Concat(["--campaign-menu",selectionPath]));await menu.WaitForExitAsync();if(menu.ExitCode!=0)throw new Exception("Campaign menu could not boot. See client log.");if(!File.Exists(selectionPath))break;selection=JsonSerializer.Deserialize<CampaignSelection>(File.ReadAllText(selectionPath),ContentPackage.Json)!;File.Delete(selectionPath);}
   var sessionArgs=new List<string>(common);var store=new CampaignStore(options.SaveRoot??options.Root,selection.Campaign);string? snapshot=null;
   if(selection.Load!="new"){var save=selection.Load=="latest"?store.Latest():store.Read(selection.Load);using var content=new ContentPackage(options.Root,options.Map);content.ValidateMap(save.Map);snapshot=Path.Combine(logDir,"campaign-map-"+Guid.NewGuid().ToString("N")+".json");content.SaveMap(save.Map,snapshot);int index=sessionArgs.IndexOf("--map");if(index>=0)sessionArgs.RemoveRange(index,2);sessionArgs.AddRange(["--map",snapshot]);}
   ready=Path.Combine(logDir,"host-"+Guid.NewGuid().ToString("N")+".json");var hostArgs=sessionArgs.Concat(["--campaign",selection.Campaign,"--port","0","--ready-file",ready]);if(selection.Load!="new")hostArgs=hostArgs.Concat(["--load",selection.Load]);ownedServer=Start("Greyhaven.Server",hostArgs);int port=0;
   for(int i=0;i<600;i++){if(ownedServer.HasExited)throw new Exception("The campaign host could not start. See server log in UserData/Logs.");if(File.Exists(ready)){try{using var d=JsonDocument.Parse(File.ReadAllText(ready));port=d.RootElement.GetProperty("port").GetInt32();break;}catch(JsonException){}}await Task.Delay(50);}
   if(port==0)throw new Exception("Timed out starting the campaign host.");var client=Start("Greyhaven.Client",sessionArgs.Concat(["--connect","127.0.0.1:"+port]));await client.WaitForExitAsync();
   using(var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(30))){try{await ownedServer.WaitForExitAsync(timeout.Token);}catch(OperationCanceledException){ownedServer.Kill(true);await ownedServer.WaitForExitAsync();}}
   if(client.ExitCode!=0)throw new Exception("Client could not boot. See client log in UserData/Logs.");if(ownedServer.ExitCode!=0)throw new Exception("Campaign host could not save cleanly. See server log in UserData/Logs.");File.Delete(ready);ready=null;if(snapshot!=null)File.Delete(snapshot);ownedServer=null;if(direct||options.Capture!=null)break;
  }

 }
}catch(Exception e){Console.Error.WriteLine(e.Message);if(OperatingSystem.IsWindows())Native.MessageBox(IntPtr.Zero,e.Message,"Greyhaven",0x10);Environment.ExitCode=1;}
finally {
 if(ownedServer!=null){if(!ownedServer.HasExited){ownedServer.Kill(true);await ownedServer.WaitForExitAsync();}}
 await Task.WhenAll(logs);foreach(var w in writers)w.Dispose();foreach(var p in started)p.Dispose();if(ready!=null&&File.Exists(ready))File.Delete(ready);
}
internal static class Native {[DllImport("user32.dll",CharSet=CharSet.Unicode)]internal static extern int MessageBox(IntPtr window,string text,string title,uint flags);}

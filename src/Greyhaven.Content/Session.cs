// SPDX-License-Identifier: MIT
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
namespace Greyhaven.Content;
public sealed class WorldItem {public string QuestId {get;set;}="";public string Label {get;set;}="";public int Amount {get;set;}=1;public string ParentId {get;set;}="";public string Id {get;set;}="";public string Definition {get;set;}="";public string Where {get;set;}="bag";public int X {get;set;}public int Y {get;set;}public int Z {get;set;}}
public sealed class JournalEntry {public string Text {get;set;}="";public double Time {get;set;}public bool Note {get;set;}}
public sealed class StockEntry {public string Definition {get;set;}="";public int Amount {get;set;}}
public sealed class SessionState {public AdventureProgress Adventure {get;set;}=new();public WorldProgress World {get;set;}=new();public CombatState Combat {get;set;}=new();public int InventoryVersion {get;set;} public int Outfit {get;set;}public List<string> Quickbar {get;set;}=Enumerable.Repeat("",10).ToList();public List<JournalEntry> Journal {get;set;}=[];public List<StockEntry> MerchantStock {get;set;}=[];public bool Paused {get;set;} public double ElapsedSeconds {get;set;} public int X {get;set;}public int Y {get;set;}public int Z {get;set;}public int Facing {get;set;}=3;public int Revision {get;set;}public List<WorldItem> Items {get;set;}=[];}
public sealed class Command {public int Amount {get;set;} public string TargetId {get;set;}="";public int Slot {get;set;}public string Text {get;set;}="";public int ExpectedRevision {get;set;}=-1;public string Type {get;set;}="";public int Protocol {get;set;}=1;public string Fingerprint {get;set;}="";public string MapHash {get;set;}="";public int Dx {get;set;}public int Dy {get;set;}public string ItemId {get;set;}="";public string Destination {get;set;}="";public int X {get;set;}public int Y {get;set;}}
public sealed class Reply {public string Type {get;set;}="";public bool Ok {get;set;}public string Message {get;set;}="";public string Fingerprint {get;set;}="";public string MapHash {get;set;}="";public SessionState? State {get;set;}}
public static class Wire {
 public static string Encode<T>(T value)=>JsonSerializer.Serialize(value,new JsonSerializerOptions(ContentPackage.Json){WriteIndented=false});
 public static async Task<T> Read<T>(StreamReader reader,CancellationToken ct=default){string? line=await reader.ReadLineAsync(ct);if(line==null)throw new IOException("Original-content connection closed.");if(line.Length>1024*1024)throw new IOException("Protocol message too large.");return JsonSerializer.Deserialize<T>(line,ContentPackage.Json)??throw new IOException("Empty protocol message.");}
 public static async Task Write<T>(StreamWriter writer,T value){await writer.WriteLineAsync(Encode(value));await writer.FlushAsync();}
}
public sealed class OriginalSession : IDisposable {
 readonly TcpClient client=new(){NoDelay=true};readonly StreamReader reader;readonly StreamWriter writer;int busy;public bool Busy=>Volatile.Read(ref busy)!=0;public string? Error {get;private set;}public SessionState State {get;private set;}public bool Connected=>Error==null;public event Action<Reply>? Received;
 public OriginalSession(ContentPackage package,string endpoint){
  var split=endpoint.Split(':');if(split.Length!=2||!int.TryParse(split[1],out int port)||port<1||port>65535)throw new ContentException("Use --connect HOST:PORT.");
  using(var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(5)))client.ConnectAsync(split[0],port,timeout.Token).AsTask().GetAwaiter().GetResult();
  reader=new(client.GetStream(),Encoding.UTF8,false,8192,true);writer=new(client.GetStream(),new UTF8Encoding(false),8192,true);
  Wire.Write(writer,new Command{Type="hello",Fingerprint=package.Fingerprint,MapHash=package.MapHash}).GetAwaiter().GetResult();using var helloTimeout=new CancellationTokenSource(TimeSpan.FromSeconds(5));var reply=Wire.Read<Reply>(reader,helloTimeout.Token).GetAwaiter().GetResult();
  if(!reply.Ok||reply.State==null||reply.Fingerprint!=package.Fingerprint||reply.MapHash!=package.MapHash){Dispose();throw new ContentException("Server rejected content handshake: "+reply.Message);}State=reply.State;
 }
 public bool Send(Command command){if(Error!=null||Interlocked.CompareExchange(ref busy,1,0)!=0)return false;_ =Task.Run(async()=>{try{await Wire.Write(writer,command);using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(5));var r=await Wire.Read<Reply>(reader,timeout.Token);if(r.State!=null)State=r.State;Received?.Invoke(r);}catch(Exception e){Error=e.Message;Received?.Invoke(new Reply{Type="connection-error",Message=e.Message});}finally{Interlocked.Exchange(ref busy,0);}});return true;}
 public void Dispose(){client.Dispose();reader?.Dispose();writer?.Dispose();}
}

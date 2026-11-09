// SPDX-License-Identifier: MIT
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
namespace Greyhaven.Content;
public sealed class CampaignSave {
 public int Schema {get;set;}=1; public string Campaign {get;set;}=""; public string Fingerprint {get;set;}=""; public string MapHash {get;set;}="";
 public DateTime SavedUtc {get;set;} public WorldMap Map {get;set;}=new(); public SessionState State {get;set;}=new();
}
public sealed class SaveEnvelope {public string Sha256 {get;set;}="";public string Payload {get;set;}="";}
public sealed class CampaignStore {
 readonly string directory; public string Campaign {get;} public string? RecoveryNotice {get;private set;}
 public CampaignStore(string root,string campaign){if(campaign is not ("slot1" or "slot2" or "slot3"))throw new ContentException("Choose campaign slot1, slot2 or slot3.");Campaign=campaign;directory=Path.Combine(root,"UserData","Campaigns",campaign);}
 string PathFor(string kind){if(kind is not ("manual" or "quick" or "autosave" or "exit"))throw new ContentException("Unknown save kind.");return Path.Combine(directory,kind+".json");}
 public FileStream AcquireHostLock(){Directory.CreateDirectory(directory);try{return new FileStream(Path.Combine(directory,"host.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);}catch(IOException){throw new ContentException("This campaign is already open in another host.");}}
 public bool Exists=>Directory.Exists(directory)&&Directory.EnumerateFiles(directory,"*.json*").Any();
 public CampaignSave Read(string kind){string path=PathFor(kind);try{return ReadFile(path);}catch(Exception e)when(e is IOException or JsonException or ContentException){try{var backup=ReadFile(path+".bak");RecoveryNotice="Recovered previous valid "+kind+" save; primary was unreadable.";return backup;}catch(Exception){throw new ContentException("Cannot load "+kind+": "+e.Message+". Your files have been retained.");}}}
 CampaignSave ReadFile(string path){if(new FileInfo(path).Length>16*1024*1024)throw new ContentException("Save is too large.");var bytes=File.ReadAllBytes(path);if(bytes.Length>16*1024*1024)throw new ContentException("Save is too large.");var envelope=JsonSerializer.Deserialize<SaveEnvelope>(bytes,ContentPackage.Json)??throw new ContentException("Empty save.");if(envelope.Payload is null||envelope.Sha256!=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(envelope.Payload))))throw new ContentException("Save checksum mismatch.");var save=JsonSerializer.Deserialize<CampaignSave>(envelope.Payload,ContentPackage.Json)??throw new ContentException("Empty campaign.");if(save.Schema!=1||save.Campaign!=Campaign||save.Map is null||save.State is null||save.State.Items is null)throw new ContentException("Unsupported campaign save.");return save;}
 public CampaignSave Latest(){var saves=new List<CampaignSave>();foreach(var kind in new[]{"manual","quick","autosave","exit"}){try{saves.Add(Read(kind));}catch(ContentException){}}return saves.OrderByDescending(s=>s.SavedUtc).FirstOrDefault()??throw new ContentException("No readable saves in this campaign. Start in an empty slot; damaged files are retained.");}
 public void Write(ContentPackage package,SessionState state,string kind){var save=new CampaignSave{Campaign=Campaign,Fingerprint=package.Fingerprint,MapHash=package.MapHash,SavedUtc=DateTime.UtcNow,Map=package.Map.Clone(),State=JsonSerializer.Deserialize<SessionState>(Wire.Encode(state),ContentPackage.Json)!};var payload=Wire.Encode(save);var envelope=new SaveEnvelope{Payload=payload,Sha256=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)))};string path=PathFor(kind);Directory.CreateDirectory(directory);string temp=path+".writing";
  using(var file=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None)){var data=Encoding.UTF8.GetBytes(Wire.Encode(envelope));file.Write(data);file.Flush(true);}if(File.Exists(path)){try{ReadFile(path);File.Copy(path,path+".bak",true);}catch(Exception e)when(e is IOException or JsonException or ContentException){/* Preserve the last known-good backup. */}}File.Move(temp,path,true);
 }
 public static void Validate(ContentPackage p,CampaignSave save){p.ValidateMap(save.Map);if(!p.AcceptsFingerprint(save.Fingerprint)||!p.AcceptsMapHash(save.MapHash))throw new ContentException("This save belongs to a different content package or map.");var s=save.State;if(s is null||s.Items is null||s.Items.Count>4096||s.Revision<0||s.ElapsedSeconds<0||!double.IsFinite(s.ElapsedSeconds)||s.Facing<0||s.Facing>7||!p.CanStand(p.Map,s.X,s.Y)||s.Z!=p.Map.Get(s.X,s.Y).Z)throw new ContentException("Invalid saved player state.");InventoryRules.Validate(p,s);CombatRules.Validate(p,s);WorldAuthoring.ValidateProgress(p,s);AdventureRules.Validate(p,s);}
}

using Greyhaven.ArtProof;
string Option(string key,string fallback=null){int i=Array.IndexOf(args,key);return i>=0&&i+1<args.Length?args[i+1]:fallback;}
string root=Option("--assets",Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..","art","compiled")));
try{using var game=new ArtProofGame(root,Option("--capture"));game.Run();}
catch(Exception ex){File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"art-proof-error.log"),ex.ToString());throw;}

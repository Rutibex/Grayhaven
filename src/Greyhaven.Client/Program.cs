// SPDX-License-Identifier: MIT
using Greyhaven.Content;
using Greyhaven.Presentation;
try {
 if(args.Contains("--art-proof")){var o=BootOptions.Parse(args);using var game=new Greyhaven.ArtProof.ArtProofGame(Path.Combine(o.Root,"art/compiled"),o.Capture);game.Run();}
 else OriginalBoot.Run(args);
}catch(Exception e){Console.Error.WriteLine("Greyhaven client: "+e.Message);File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"client-error.log"),e.ToString());Environment.ExitCode=1;}

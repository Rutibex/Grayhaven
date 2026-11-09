// SPDX-License-Identifier: MIT
try {Greyhaven.Presentation.OriginalBoot.Run(args,true,CentrED.OriginalTileDataAdapter.Verify);}
catch(Exception e){Console.Error.WriteLine("Greyhaven editor: "+e.Message);File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"editor-error.log"),e.ToString());Environment.ExitCode=1;}

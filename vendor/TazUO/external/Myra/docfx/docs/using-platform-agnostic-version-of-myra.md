Myra.PlatformAgnostic is version of Myra that isnt tied to any game engine.

It is available at the nuget: https://www.nuget.org/packages/Myra.PlatformAgnostic/

In order to use it, it is required to implement interfaces [IMyraPlatform](https://github.com/rds1983/Myra/blob/master/src/Myra/Platform/IMyraPlatform.cs) and [IMyraRenderer](https://github.com/rds1983/Myra/blob/master/src/Myra/Platform/IMyraRenderer.cs). 

For the latter, among methods DrawSprite and DrawQuad, only one should be implemented. The other could throw NotImplementedException. The property RendererType should return which method is implemented.

Samples that work through DrawSprite:
https://github.com/rds1983/Myra/tree/master/samples/Myra.Samples.PlatformAgnostic
https://github.com/rds1983/Myra/tree/master/samples/Myra.Samples.Silk.NET.TrippyGL


Sample that work through DrawQuad:
https://github.com/rds1983/Myra/tree/master/samples/Myra.Samples.Silk.NET
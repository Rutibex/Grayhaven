Myra uses [FontStashSharp](https://github.com/FontStashSharp/FontStashSharp) for the text rendering.

Sample code for setting font:
```c#
byte[] ttfData = File.ReadAllBytes("DroidSans.ttf");

// Ordinary DynamicSpriteFont
FontSystem ordinaryFontSystem = new FontSystem();
ordinaryFontSystem.AddFont(ttfData);
_label1.Font = ordinaryFontSystem.GetFont(32);

// Stroked DynamicSpriteFont
FontSystemSettings strokedSettings = new FontSystemSettings
{
    Effect = FontSystemEffect.Stroked,
    EffectAmount = 1
};
FontSystem strokedFontSystem = new FontSystem(strokedSettings);
strokedFontSystem.AddFont(ttfData);
_label2.Font = strokedFontSystem.GetFont(24);

// Blurry DynamicSpriteFont
FontSystemSettings blurrySettings = new FontSystemSettings
{
    Effect = FontSystemEffect.Blurry,
    EffectAmount = 1
};
FontSystem blurryFontSystem = new FontSystem(blurrySettings);
blurryFontSystem.AddFont(ttfData);
_label3.Font = blurryFontSystem.GetFont(48);

// StaticSpriteFont in AngelCode BMFont Format(https://www.angelcode.com/products/bmfont/)
string fntData = File.ReadAllText("comicSans48.fnt");
_label4.Font = StaticSpriteFont.FromBMFont(fntData, atlasFileName => File.OpenRead(atlasFileName), GraphicsDevice);
```

It is equivalent to the following MML:
```xml
<Project>
  <Project.ExportOptions />
  <Panel Background="#4BD961FF">
    <VerticalStackPanel HorizontalAlignment="Center" VerticalAlignment="Center" >
      <Label Text="Hello, World!" Font="DroidSans.ttf:32"  />
      <Label Text="Hello, World!" Font="DroidSans.ttf:Stroked:1:24"  />
      <Label Text="Hello, World!" Font="DroidSans.ttf:Blurry:2:48"  />
      <Label Text="Hello, World!" Font="comicSans48.fnt"  />
    </VerticalStackPanel>
  </Panel>
</Project>
```

Which would result to the following:

![alt text](~/images/fonts.png)

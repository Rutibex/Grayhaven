## Overview
A complete UI stylesheet consists of following essential files: stylesheet file in XML format(.xmms), texture atlas file in [MyraTexturePacker](https://github.com/rds1983/MyraTexturePacker) XML format(.xmat), font(s) in TTF/OTF/TTC or AngelCode FNT format, texture atlas image in BMP, TGA, PNG, JPG, GIF or PSD format.

Default UI stylesheet is example of all those files: https://github.com/rds1983/Myra/tree/master/src/Myra/Resources

Below is the detailed explanation of the [default_ui_skin.xmms](https://github.com/rds1983/Myra/blob/master/src/Myra/Resources/default_ui_skin.xmms) contents.

## Images
Notice how the stylesheet explicitly references [texture atlas](images.md#textureregionatlas) in the following string:
```xml
  <Stylesheet TextureRegionAtlas="default_ui_skin.xmat">
```

Now if any style references an image, it is being resolved through the texture atlas.

I.e. 
```xml
    <TextBoxStyle Background="textfield" TextColor="white" DisabledTextColor="grey" Font="default-font" Cursor="cursor" Selection="selection" />
``` 
This style references 3 images: 'textfield', 'cursor' and 'selection'. All of them are present in the texture atlas.
Also it references two colors by their names ('white' and 'grey').

It is possible to set style properties of type [IBrush](images.md) to explicit color values.

I.e. see VerticalMenuStyle declaration:
```
    <VerticalMenuStyle Background="button" Border="#1BA1E2" BorderThickness="1"
                       SelectionHoverBackground="button-over" SelectionBackground="button-down"
                       SpecialCharColor="red">
```
VerticalMenuStyle Border brush is explicitly set to color "#1BA1E2".

In order to find what properties can be set for each style see following code: https://github.com/rds1983/Myra/tree/master/src/Myra/Graphics2D/UI/Styles

Every widget style corresponds to some style class and loading itself is done through reflection.

I.e. above VerticalMenuStyle is defined in [MenuStyle.cs](https://github.com/rds1983/Myra/blob/master/src/Myra/Graphics2D/UI/Styles/MenuStyle.cs). MenuStyle inherits [WidgetStyle](https://github.com/rds1983/Myra/blob/master/src/Myra/Graphics2D/UI/Styles/WidgetStyle.cs). So VerticalMenuStyle has properies of both these classes.

## Fonts
This is how stylesheet references and resolves fonts:
```xml
  <Fonts UsedSpace="0, 0, 1024, 160">
    <Font Id="default-font" File="Inter-Regular.ttf" Size="20"/>
  </Fonts>
```
`UsedSpace` declares rectangle in the texture atlas that stores all of the UI images. Now the font system will use the texture atlas to store the font glyphs, omitting the that rectangle.

It's important to note that `UsedSpace` property is optional. If it is not declared, then the font system will create separate texture to store the font glyphs. Which would result in texture swaps(the renderer would have to switch between the texture containing UI images and the texture containing font glyphs). Hence it is recommended to use `UsedSpace`. See FontStashSharp's [How To Use Existing Texture As Font Glyphs Atlas](https://github.com/rds1983/FontStashSharp/wiki/How-To-Use-Existing-Texture-As-Font-Glyphs-Atlas) for more details.

## Loading
Actual stylesheet loading is done through [AssetManagementBase](https://github.com/rds1983/AssetManagementBase).

The loading code is located here: https://github.com/rds1983/Myra/blob/master/src/Myra/DefaultAssets.cs

Since the default stylesheet files are stored as resources the AssetManager creation code is:
```c#
  private static readonly AssetManager _assetManager = new AssetManager(MyraEnvironment.GraphicsDevice, new ResourceAssetResolver(typeof(DefaultAssets).Assembly, "Resources."));
```
And the actual stylesheet loading code:
```c#
  _uiStylesheet = _assetManager.Load<Stylesheet>("default_ui_skin.xml");
```

## Myra.Samples.CustomUIStylesheet
Myra.Samples.CustomUIStylesheet is another example of full Myra stylesheet.
Again stylesheet files are stored as resources: https://github.com/rds1983/Myra/tree/master/samples/Myra.Samples.CustomUIStylesheet/Resources

The major difference with the default stylesheet is that custom stylesheet uses static font in AngelCode .FNT format:
```xml
  <Fonts>
    <Font Id="commodore-64" File="commodore-64.fnt"/>
  </Fonts>
```

It's important to notice that it uses single [underlying image](https://github.com/rds1983/Myra/blob/master/samples/Myra.Samples.CustomUIStylesheet/Resources/ui_stylesheet_atlas.png) to store both texture atlas images and font glyphs. It's good solution performance-wise as the renderer doesnt need to switch between textures.

Now if you view [commodore-64.fnt](https://github.com/rds1983/Myra/blob/master/samples/Myra.Samples.CustomUIStylesheet/Resources/commodore-64.fnt), you would see how .fnt references the texture region in default_ui_skin.xmat with id 'default' as the image with character glyphs: 
```
file="ui_stylesheet.xmat:commodore-64"
```

And the loading code is:
```c#
  protected override void LoadContent()
  {
      base.LoadContent();

      MyraEnvironment.Game = this;

      // Create asset manager
      var assetManager = new AssetManager(GraphicsDevice, new ResourceAssetResolver(typeof(CustomUIStylesheetGame).Assembly, "Resources"));

      // Load stylesheet
      Stylesheet.Current = assetManager.Load<Stylesheet>("ui_stylesheet.xml");
      ...
    }
```

  _**Note**. The default style is being applied to the widget in the moment of the creation. Therefore all changes 
to **Stylesheet.Current** should be done before the UI is created._
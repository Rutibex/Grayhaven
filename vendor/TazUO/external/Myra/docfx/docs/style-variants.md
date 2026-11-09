Every widget can have more than one possible style defined in XML.

I.e. consider following fragment from the [default stylesheet](https://github.com/rds1983/Myra/blob/master/src/Myra/Resources/default_ui_skin.xml):
```XML
  <ButtonStyles>
    <ButtonStyle Background="button" OverBackground="button-over" PressedBackground="button-down" />
    <ButtonStyle Id="blue" Background="button-blue" OverBackground="button-blue-down" PressedBackground="button-blue-down" />
  </ButtonStyles>
```
It defines two button styles: default(without Id) and "blue" style.

In order to create button with default style, simply call its constructor without parameters:
```c#
  var button = new TextButton
  {
    Text = "My button"
  };
```
Or through [MML](MML.md):
```xml
  <TextButton Text="My button"/>
```

In order to create button with named style, pass it to constructor. I.e.
```c#
  var button = new TextButton("blue")
  {
    Text = "My button"
  };
```
Or pass it to StyleName property when using [MML](MML.md):
```xml
  <TextButton StyleName="blue" Text="My button"/>
```
  _**Note**. It's also possible to create a widget without any applied style. If you want to archieve that, then pass null to constructor: `var button = new TextButton(null);`_

# MyraPad Support

![alt text](~/images/style-variants.gif)
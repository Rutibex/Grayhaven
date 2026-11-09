Restyling through code is quite easy. There's a static object **Stylesheet.Current** that contains all default widget styles.  
I.e. following code will make all Labels - green:
```c#
  Stylesheet.Current.LabelStyle.TextColor = Color.Green;
```

  _**Note**. The default style is being applied to the widget in the moment of the creation. Therefore all changes 
to **Stylesheet.Current** should be done before the UI is created._










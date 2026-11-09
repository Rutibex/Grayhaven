# Layout Properties
Every widget has following properties related to the layout:

Name|Type|Default|Description
----|----|-------|-----------
Left/Top|int|0|X/Y Addition
Width/Height|int?|null|Width/Height of the widget, if set to null, then it is automatically calculated
HorizontalAlignment/VerticalAlignment|enum|Depends on a widget, it's either Stretch or Left/Top|How control is horizontally/vertically aligned in the container
Margin/Border/Padding|Thickness||[Margin, Border, Padding](margin-border-padding.md)

**Note**. Properties which names start with 'Grid' are used only in the Grid container.

# Panel
Panel is simple container. Following code demonstrates usage of layout properties with it:
```C#
    var panel = new Panel();
    var positionedText = new Label();
    positionedText.Text = "Positioned Text";
    positionedText.Left = 50;
    positionedText.Top = 100;
    panel.Widgets.Add(positionedText);

    var paddedCenteredButton = new TextButton();
    paddedCenteredButton.Text = "Padded Centered Button";
    paddedCenteredButton.HorizontalAlignment = HorizontalAlignment.Center;
    paddedCenteredButton.VerticalAlignment = VerticalAlignment.Center;
    panel.Widgets.Add(paddedCenteredButton);

    var rightBottomText = new Label();
    rightBottomText.Text = "Right Bottom Text";
    rightBottomText.Left = -30;
    rightBottomText.Top = -20;
    rightBottomText.HorizontalAlignment = HorizontalAlignment.Right;
    rightBottomText.VerticalAlignment = VerticalAlignment.Bottom;
    panel.Widgets.Add(rightBottomText);

    var fixedSizeButton = new TextButton();
    fixedSizeButton.Text = "Fixed Size Button";
    fixedSizeButton.Width = 110;
    fixedSizeButton.Height = 80;
    panel.Widgets.Add(fixedSizeButton);
```

It is equivalent to the following [MML](MML.md):
```xml
<Project>
  <Panel>
    <Label Text="Positioned Text" Left="50" Top="100" />
    <TextButton Text="Padded Centered Button" HorizontalAlignment="Center" VerticalAlignment="Center" />
    <Label Text="Right Bottom Text" Left="-30" Top="-20" HorizontalAlignment="Right" VerticalAlignment="Bottom" />
    <TextButton Text="Fixed Size Button" Width="110" Height="80" />
  </Panel>
</Project>
```

It would result in following UI:

![alt text](~/images/basic-layout-and-panel.png)
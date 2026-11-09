# Basics
HorizontalStackPanel/VerticalStackPanel are containers that layout children horizontally/vertically in one line/row.
I.e. following code layouts 3 widgets in one line:
```c#
    var horizontalStackPanel = new HorizontalStackPanel
    {
        horizontalStackPanel.ShowGridLines = true,
        horizontalStackPanel.Spacing = 8
    };

    var textBlock1 = new Label();
    textBlock1.Text = "First Text";
    horizontalStackPanel.Widgets.Add(textBlock1);

    var textButton1 = new TextButton();
    textButton1.Text = "Second Button";
    horizontalStackPanel.Widgets.Add(textButton1);

    var checkStackPanel1 = new CheckBox();
    checkStackPanel1.Text = "Third Checkbox";

    horizontalStackPanel.Widgets.Add(checkStackPanel1);
```
It is equivalent to the following [MML](MML.md):
```xml
    <Project>
      <HorizontalStackPanel ShowGridLines="True" Spacing="8">
        <Label Text="First Text" />
        <TextButton Text="Second Button" />
        <CheckBox Text="Third Checkbox" />
      </HorizontalStackPanel>
    </Project>
```
It would result in following:

![alt text](~/images/stackpanel-layout1.png)

Note. There are white lines separating cells, because "ShowGridLines" is set to "True". It's useful property to debug the StackPanel behavior.

# Proportions
It is possible to specify proportions for StackPanel's widgets through the attached properties.

I.e. following code will make 2nd widget fill all available height:
```c#
    var verticalStackPanel = new VerticalStackPanel
    {
        ShowGridLines = true,
        Spacing = 8
    };

    var textBlock1 = new Label();
    textBlock1.Text = "First Text";
    verticalStackPanel.Widgets.Add(textBlock1);

    var textButton1 = new TextButton();
    textButton1.Text = "Second Button";
    textButton1.VerticalAlignment = VerticalAlignment.Center;
    StackPanel.SetProportionType(textButton1, ProportionType.Fill);
    verticalStackPanel.Widgets.Add(textButton1);

    var checkStackPanel1 = new CheckBox();
    checkStackPanel1.Text = "Third Checkbox";
    verticalStackPanel.Widgets.Add(checkStackPanel1);
```
It is equivalent to the following [MML](MML.md):
```xml
<Project>
  <Project.ExportOptions />
  <VerticalStackPanel ShowGridLines="True" Spacing="8">
    <Label Text="First Text" />
    <TextButton Text="Second Button" VerticalAlignment="Center" StackPanel.ProportionType="Fill" />
    <CheckBox Text="Third Checkbox" />
  </VerticalStackPanel>
</Project>
```
It would result in following:

![alt text](~/images/stackpanel-layout2.png)

# Default Proportions

It is possible to set default value for proportions.
I.e.:
```xml
  <HorizontalStackPanel ColumnSpacing="8" RowSpacing="8">
      <HorizontalStackPanel.DefaultProportion Type="Auto" />
  </HorizontalStackPanel>
```
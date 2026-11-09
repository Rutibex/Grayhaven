Myra uses following box model:

![alt text](~/images/margin-border-padding1.png)

Background is being rendered at Content+Padding.

[Thickness](https://github.com/rds1983/Myra/blob/master/src/Myra/Graphics2D/Thickness.cs) is structure that sets size of all four sides. It could be initialized either with one parameter(one size for all sides), two parameters(first for Left and Right, second for Top and Bottom) and four parameters(Left, Top, Right, Bottom).

Consider following code:
```c#
    var topPanel = new Panel
    {
        Background = new SolidBrush("#ADD8E6FF")
    };

    var childPanel = new Panel
    {
        Margin = new Thickness(36),
        BorderThickness = new Thickness(24, 0),
        Padding = new Thickness(20, 20, 0, 0),
        Background = new SolidBrush("#FFA500FF"),
        Border = new SolidBrush("#008000")
    };

    var label1 = new Label
    {
        Text = "Some Text",
        TextColor = Color.Red
    };

    childPanel.Widgets.Add(label1);
    topPanel.Widgets.Add(childPanel);
```
It would be equivalent to following [MML](MML.md):
```xml
    <Project>
      <Panel Background="#ADD8E6FF">
        <Panel Margin="36" BorderThickness="24, 0" Padding="20, 20, 0, 0" Background="#FFA500FF" Border="#008000">
          <Label Text="Some Text" TextColor="#FF0000FF" />
        </Panel>
      </Panel>
    </Project>
```
And would result in following image:

![alt text](~/images/margin-border-padding2.png)

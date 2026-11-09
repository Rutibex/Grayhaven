By default Myra translates Keys to chars in the TextBox widget, which basically limits accepted chars to ASCII only.

Add following code to the Game.LoadContent to make Myra accept any chars by utilizing MonoGame TextInput:
```c#
    // Inform Myra that external text input is available
    // So it stops translating Keys to chars
    Desktop.HasExternalTextInput = true;

    // Provide that text input
    Window.TextInput += (s, a) =>
    {
        Desktop.OnChar(a.Character);
    };
```
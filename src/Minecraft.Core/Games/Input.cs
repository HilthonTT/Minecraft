using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Minecraft.Core.Games;

public sealed class Input : IDisposable
{
    private readonly NativeWindow _window;
    private readonly List<char> _pending = [];
    private readonly List<char> _typedThisFrame = [];

    public Input(NativeWindow window)
    {
        _window = window;
        _window.TextInput += HandleTextInput;
    }

    public Vector2 MousePosition => _window.MouseState.Position;
    public Vector2 MouseDelta => _window.MouseState.Delta;
    public Vector2 ScrollDelta => _window.MouseState.ScrollDelta;

    public IReadOnlyList<char> TypedCharacters => _typedThisFrame;

    public string ClipboardText
    {
        get => _window.ClipboardString ?? string.Empty;
        set => _window.ClipboardString = value;
    }

    public bool IsControlDown => OnKeyDown(Keys.LeftControl) || OnKeyDown(Keys.RightControl);

    public bool IsShiftDown => OnKeyDown(Keys.LeftShift) || OnKeyDown(Keys.RightShift);

    public void Update()
    {
        _typedThisFrame.Clear();
        _typedThisFrame.AddRange(_pending);
        _pending.Clear();
    }

    public bool OnKeyDown(Keys key) => _window.KeyboardState.IsKeyDown(key);
    public bool OnKeyPress(Keys key) => _window.KeyboardState.IsKeyPressed(key);

    public bool OnMousePress(MouseButton b) => _window.MouseState.IsButtonPressed(b);

    public bool OnMouseDown(MouseButton b) => _window.MouseState.IsButtonDown(b);

    public bool OnMouseRelease(MouseButton b) => _window.MouseState.IsButtonReleased(b);

    private void HandleTextInput(TextInputEventArgs e) => _pending.AddRange(e.AsString);

    public void Dispose() => _window.TextInput -= HandleTextInput;
}

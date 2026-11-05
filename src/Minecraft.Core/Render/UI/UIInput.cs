using Minecraft.Core.Games;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Minecraft.Core.Render.UI;

public readonly record struct UIInput(
    Vector2 Mouse,
    bool Pressed,
    bool Down,
    bool Released,
    bool DoubleClicked,
    bool RightPressed,
    float Scroll)
{
    private const double DoubleClickSeconds = 0.4;
    private const float DoubleClickDistance = 6F;

    private static double _lastClickAt = double.NegativeInfinity;
    private static Vector2 _lastClickPosition;

    public static UIInput Idle => new(new Vector2(-100000F), false, false, false, false, false, 0F);

    public static UIInput Capture(bool windowFocused)
    {
        Vector2 mouse = UIScale.ToCanvas(Game.Input.MousePosition);

        if (!windowFocused)
        {
            return Idle with { Mouse = mouse };
        }

        bool pressed = Game.Input.OnMousePress(MouseButton.Left);
        bool doubleClicked = false;

        if (pressed)
        {
            double now = UIMotion.NowSeconds;
            doubleClicked = now - _lastClickAt <= DoubleClickSeconds &&
                            (mouse - _lastClickPosition).Length <= DoubleClickDistance;

            _lastClickAt = doubleClicked ? double.NegativeInfinity : now;
            _lastClickPosition = mouse;
        }

        return new UIInput(
            mouse,
            pressed,
            Game.Input.OnMouseDown(MouseButton.Left),
            Game.Input.OnMouseRelease(MouseButton.Left),
            doubleClicked,
            Game.Input.OnMousePress(MouseButton.Right),
            Game.Input.ScrollDelta.Y);
    }

    public static bool KeyPressed(Keys key) => Game.Input.OnKeyPress(key);

    public static bool EnterPressed => KeyPressed(Keys.Enter) || KeyPressed(Keys.KeyPadEnter);

    public static bool ControlDown => Game.Input.IsControlDown;

    public static bool ShiftDown => Game.Input.IsShiftDown;
}

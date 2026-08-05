using Minecraft.Core.Games;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Minecraft.Core.Render.UI;

/// <summary>
/// A key that fires once when it goes down and then keeps firing while it is held, the way a text field is
/// expected to behave when backspace is held down.
/// </summary>
public sealed class HeldKey
{
    private const float SecondsBeforeRepeating = 0.4F;
    private const float SecondsBetweenRepeats = 0.04F;

    private readonly Keys _key;

    private DateTime _nextRepeatAt;

    public HeldKey(Keys key)
    {
        _key = key;
    }

    public bool HasFired()
    {
        if (Game.Input.OnKeyPress(_key))
        {
            _nextRepeatAt = DateTime.Now.AddSeconds(SecondsBeforeRepeating);
            return true;
        }

        if (!Game.Input.OnKeyDown(_key) || DateTime.Now < _nextRepeatAt)
        {
            return false;
        }

        _nextRepeatAt = DateTime.Now.AddSeconds(SecondsBetweenRepeats);
        return true;
    }
}

using Minecraft.Core.Games;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Minecraft.Core.Render.UI;

public sealed class UITextEditKeys
{
    private readonly HeldKey _backspaceKey = new(Keys.Backspace);
    private readonly HeldKey _deleteKey = new(Keys.Delete);
    private readonly HeldKey _leftKey = new(Keys.Left);
    private readonly HeldKey _rightKey = new(Keys.Right);

    public bool Apply(TextEditBuffer buffer)
    {
        int before = buffer.Version;

        bool control = Game.Input.IsControlDown;
        bool shift = Game.Input.IsShiftDown;

        if (_leftKey.HasFired())
        {
            buffer.MoveLeft(control, shift);
        }

        if (_rightKey.HasFired())
        {
            buffer.MoveRight(control, shift);
        }

        if (Game.Input.OnKeyPress(Keys.Home))
        {
            buffer.MoveHome(shift);
        }

        if (Game.Input.OnKeyPress(Keys.End))
        {
            buffer.MoveEnd(shift);
        }

        if (_backspaceKey.HasFired())
        {
            buffer.Backspace(control);
        }

        if (_deleteKey.HasFired())
        {
            buffer.Delete(control);
        }

        bool alt = Game.Input.OnKeyDown(Keys.LeftAlt) || Game.Input.OnKeyDown(Keys.RightAlt);

        if (control && !alt)
        {
            ApplyShortcuts(buffer);
        }
        else if (Game.Input.TypedCharacters.Count > 0)
        {
            buffer.Insert(new string([.. Game.Input.TypedCharacters]));
        }

        return buffer.Version != before;
    }

    private static void ApplyShortcuts(TextEditBuffer buffer)
    {
        if (Game.Input.OnKeyPress(Keys.A))
        {
            buffer.SelectAll();
        }

        if (Game.Input.OnKeyPress(Keys.C) && buffer.HasSelection)
        {
            Game.Input.ClipboardText = buffer.SelectedText;
        }

        if (Game.Input.OnKeyPress(Keys.X) && buffer.HasSelection)
        {
            Game.Input.ClipboardText = buffer.Cut();
        }

        if (Game.Input.OnKeyPress(Keys.V))
        {
            buffer.Insert(Game.Input.ClipboardText);
        }
    }
}

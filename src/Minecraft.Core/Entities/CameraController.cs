using Minecraft.Core.Games;
using OpenTK.Mathematics;

namespace Minecraft.Core.Entities;

/// <summary>
/// Turns mouse movement into camera pitch and yaw. The cursor is held grabbed while the game has the
/// controls, so the mouse reports a raw delta and never runs into the edge of the screen.
/// </summary>
public sealed class CameraController
{
    /// <summary>
    /// Looking straight up or down would make the view matrix degenerate, so the pitch stops just short.
    /// </summary>
    private const float MaxPitchRadians = MathF.PI / 2.0F - 0.1F;

    /// <summary>
    /// How many frames of mouse movement are dropped after the cursor is grabbed again. Grabbing warps the
    /// cursor, and the delta that leaves behind is not something the player did.
    /// </summary>
    private const int FramesIgnoredAfterGrab = 2;

    private readonly Game _game;

    private int _framesToIgnore;

    public Camera Camera { get; private set; }

    public CameraController(Game game, Camera camera)
    {
        _game = game;
        Camera = camera;
    }

    public void ControlCamera(Camera camera)
    {
        Camera = camera;
    }

    /// <summary>Drops the mouse movement of the next few frames, for when the cursor was just grabbed.</summary>
    public void DiscardPendingMouseLook() => _framesToIgnore = FramesIgnoredAfterGrab;

    public void Update()
    {
        Camera.Update();

        // The cursor stays grabbed while the chat is open, but the view is left alone, so that writing a
        // message does not also turn the player around. A menu is the same: it has the controls, not the
        // player.
        if (!_game.Window.IsFocused || !_game.IsGameplayInputEnabled)
        {
            return;
        }

        if (_framesToIgnore > 0)
        {
            _framesToIgnore--;
            return;
        }

        UpdateCameraPitchAndYaw();
    }

    private void UpdateCameraPitchAndYaw()
    {
        Vector2 delta = -_game.Window.MouseState.Delta * Constants.PLAYER_MOUSE_SENSIVITY;

        float newYaw = (Camera.Yaw + delta.X) % (MathF.PI * 2.0F);
        float newPitch = Math.Clamp(Camera.Pitch + delta.Y, -MaxPitchRadians, MaxPitchRadians);
        Camera.SetPitchAndYaw(newPitch, newYaw);
    }
}

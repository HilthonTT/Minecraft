using Minecraft.Core.Games;
using OpenTK.Mathematics;

namespace Minecraft.Core.Entities;

/// <summary>
/// Turns mouse movement into camera pitch and yaw. The cursor is held grabbed while the window has focus,
/// so the mouse reports a raw delta and never runs into the edge of the screen.
/// </summary>
public sealed class CameraController
{
    /// <summary>
    /// Looking straight up or down would make the view matrix degenerate, so the pitch stops just short.
    /// </summary>
    private const float MaxPitchRadians = MathF.PI / 2.0F - 0.1F;

    private readonly GameWindow _window;

    public Camera Camera { get; private set; }

    public CameraController(GameWindow window, Camera camera)
    {
        _window = window;
        Camera = camera;
    }

    public void ControlCamera(Camera camera)
    {
        Camera = camera;
    }

    public void Update()
    {
        Camera.Update();

        // The cursor stays grabbed while the chat is open, but the view is left alone, so that writing a
        // message does not also turn the player around.
        if (!_window.IsFocused || _window.IsChatOpen)
        {
            return;
        }

        UpdateCameraPitchAndYaw();
    }

    private void UpdateCameraPitchAndYaw()
    {
        Vector2 delta = -_window.MouseState.Delta * Constants.PLAYER_MOUSE_SENSIVITY;

        float newYaw = (Camera.Yaw + delta.X) % (MathF.PI * 2.0F);
        float newPitch = Math.Clamp(Camera.Pitch + delta.Y, -MaxPitchRadians, MaxPitchRadians);
        Camera.SetPitchAndYaw(newPitch, newYaw);
    }
}

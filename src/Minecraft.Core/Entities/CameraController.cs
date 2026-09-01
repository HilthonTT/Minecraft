using Minecraft.Core.Games;
using OpenTK.Mathematics;

namespace Minecraft.Core.Entities;

public sealed class CameraController
{
    private const float MaxPitchRadians = MathF.PI / 2.0F - 0.1F;

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

    public void DiscardPendingMouseLook() => _framesToIgnore = FramesIgnoredAfterGrab;

    public void Update()
    {
        Camera.Update();

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
        float sensitivity = Constants.PLAYER_MOUSE_SENSIVITY * _game.Settings.MouseSensitivity;
        Vector2 delta = -_game.Window.MouseState.Delta * sensitivity;

        float newYaw = (Camera.Yaw + delta.X) % (MathF.PI * 2.0F);
        float newPitch = Math.Clamp(Camera.Pitch + delta.Y, -MaxPitchRadians, MaxPitchRadians);
        Camera.SetPitchAndYaw(newPitch, newYaw);
    }
}

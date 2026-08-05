using Minecraft.Core.Logging;
using Minecraft.Core.Network;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace Minecraft.Core.Games;

public sealed class GameWindow : OpenTK.Windowing.Desktop.GameWindow
{
    private readonly Game _game;

    public GameWindow(StartArgs startArgs)
        : base(
            GameWindowSettings.Default,
            new NativeWindowSettings
            {
                // A dedicated server draws nothing, so it gets a token window rather than a real one.
                ClientSize = startArgs.RunMode == RunMode.Server ? new Vector2i(320, 240) : new Vector2i(1280, 720),
                Title = "Minecraft OpenGL",
                APIVersion = new Version(3, 3),
                Profile = ContextProfile.Compatability,
                StartVisible = startArgs.RunMode != RunMode.Server,
            })
    {
        _game = new Game(startArgs);
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        Logger.Info("OpenGL version: " + GL.GetString(StringName.Version));
        _game.OnStartGame(this);
    }

    protected override void OnUnload()
    {
        _game.OnCloseGame();
        base.OnUnload();
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);

        GL.Viewport(0, 0, e.Width, e.Height);
        _game.OnWindowResize(e.Width, e.Height);
    }

    protected override void OnUpdateFrame(FrameEventArgs e)
    {
        base.OnUpdateFrame(e);

        _game.OnUpdateGame(e.Time);
    }

    protected override void OnRenderFrame(FrameEventArgs e)
    {
        base.OnRenderFrame(e);

        _game.OnRenderGame();
        SwapBuffers();
    }
}

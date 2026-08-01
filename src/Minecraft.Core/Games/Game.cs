using Minecraft.Core.Entities.Player;
using Minecraft.Core.Network;
using Minecraft.Core.Render;
using Minecraft.Core.Render.UI;
using Minecraft.Core.Utilities;
using Minecraft.Core.Worlds;
using Minecraft.Core.Worlds.Blocks;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common;

namespace Minecraft.Core.Games;

/// <summary>
/// Owns everything the game is made of and drives it from the window's callbacks. Which of the pieces exist
/// depends on the run mode: a dedicated server has no renderer or local player, a pure client has no server.
/// </summary>
public sealed class Game
{
    private readonly StartArgs _startArgs;

    public static Input Input { get; private set; } = null!;

    public GameWindow Window { get; private set; } = null!;
    public MasterRenderer MasterRenderer { get; private set; } = null!;
    public ClientPlayer ClientPlayer { get; private set; } = null!;
    public FPSCounter AverageFPSCounter { get; private set; } = null!;
    public Client Client { get; private set; } = null!;
    public WorldClient World { get; private set; } = null!;
    public Server Server { get; private set; } = null!;

    public bool IsServer { get; }
    public RunMode RunMode { get; }
    public float CurrentFPS { get; private set; }

    /// <summary>The world the server half of this process loads and saves.</summary>
    public string WorldName { get; }

    /// <summary>Seed for a newly created world, or null to pick one at random.</summary>
    public int? WorldSeed { get; }

    public Game(StartArgs startArgs)
    {
        _startArgs = startArgs;
        RunMode = startArgs.RunMode;
        IsServer = RunMode is RunMode.ClientServer or RunMode.Server;
        WorldName = startArgs.WorldName;
        WorldSeed = startArgs.Seed;
    }

    public void OnStartGame(GameWindow window)
    {
        Window = window;
        window.VSync = VSyncMode.On;

        BlockRegistry.RegisterBlocks();

        Input = new Input(window);

        AverageFPSCounter = new FPSCounter();

        if (RunMode != RunMode.Server)
        {
            // The cursor is grabbed so mouse look gets a raw delta and never leaves the window.
            window.CursorState = CursorState.Grabbed;

            FontRegistry.Initialize();

            ClientPlayer = new ClientPlayer(this);
            MasterRenderer = new MasterRenderer(this);
        }

        if (IsServer)
        {
            Server = new Server(this, true);
            Server.Start(_startArgs.IP, _startArgs.Port);
        }

        if (RunMode != RunMode.Server)
        {
            World = new WorldClient(this);
            ClientPlayer.World = World;

            Client = new Client(this);
            Client.ConnectWith(_startArgs.IP, _startArgs.Port);
        }
    }

    public void OnCloseGame()
    {
        if (RunMode != RunMode.Server)
        {
            MasterRenderer.CleanUp();
            Input.Dispose();
        }

        // The client goes first: stopping the server closes the sockets underneath it, and a read already
        // in flight would then fail on a disposed stream.
        Client?.Stop();
        Server?.Stop();
    }

    public void OnUpdateGame(double deltaTimeSeconds)
    {
        // A frame that took no measurable time would divide by zero here and make every rate infinite.
        float elapsedSeconds = deltaTimeSeconds <= 0 ? 0.0001F : (float)deltaTimeSeconds;

        CurrentFPS = 1.0F / elapsedSeconds;

        AverageFPSCounter.IncrementFrameCounter();
        AverageFPSCounter.AddElapsedTime(elapsedSeconds);

        if (IsServer)
        {
            Server.World.Update(elapsedSeconds);
            Server.Update(elapsedSeconds);
        }

        if (RunMode == RunMode.Server)
        {
            return;
        }

        Client.Update(elapsedSeconds);
        World.Update(elapsedSeconds);
        MasterRenderer.EndFrameUpdate(World);

        // Updated last so that a press is visible for the whole frame that observed it.
        Input.Update();
    }

    public void OnRenderGame()
    {
        if (RunMode != RunMode.Server)
        {
            MasterRenderer.Render(World);
            return;
        }

        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
    }

    public void OnWindowResize(int newWidth, int newHeight)
    {
        if (RunMode != RunMode.Server && ClientPlayer != null)
        {
            ClientPlayer.Camera.SetWindowSize(newWidth, newHeight);
        }
    }
}

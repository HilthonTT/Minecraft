using Minecraft.Core.Audio;
using Minecraft.Core.Entities.Player;
using Minecraft.Core.Inventories;
using Minecraft.Core.Inventories.Crafting;
using Minecraft.Core.Inventories.Items;
using Minecraft.Core.Logging;
using Minecraft.Core.Network;
using Minecraft.Core.Network.Packets;
using Minecraft.Core.Render;
using Minecraft.Core.Render.UI;
using Minecraft.Core.Worlds;
using Minecraft.Core.Worlds.Blocks;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Minecraft.Core.Games;

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
    public MenuController Menu { get; private set; } = null!;

    public GameSettings Settings { get; }

    public SoundDirector SoundDirector { get; private set; } = null!;

    public bool IsServer => RunMode is RunMode.ClientServer or RunMode.Server;

    private AudioEngine _audioEngine = null!;

    public RunMode RunMode { get; private set; }

    public GameState State { get; private set; } = GameState.MainMenu;

    public float CurrentFPS { get; private set; }

    public bool IsChatOpen => MasterRenderer?.IngameCanvas.IsTyping ?? false;

    public bool IsPlaying => State == GameState.Playing;

    public bool IsGameplayInputEnabled => State == GameState.Playing && !IsChatOpen;

    public string WorldName { get; private set; }

    public int? WorldSeed { get; private set; }

    public GameMode? WorldGameMode { get; private set; }

    public bool FreshWorld { get; private set; }

    public Game(StartArgs startArgs)
    {
        _startArgs = startArgs;
        RunMode = startArgs.RunMode;
        WorldName = startArgs.WorldName;
        WorldSeed = startArgs.Seed;
        WorldGameMode = startArgs.GameMode;
        FreshWorld = startArgs.FreshWorld;

        Settings = GameSettings.Load();
        Settings.OnChangedHandler += ApplySettings;
    }

    private void ApplySettings()
    {
        if (RunMode == RunMode.Server)
        {
            return;
        }

        _audioEngine.MasterVolume = Settings.MasterVolume;
        ClientPlayer.ApplyFieldOfViewSetting();
        SendViewDistanceToServer();
    }

    private void SendViewDistanceToServer()
    {
        Client?.WritePacket(new PlayerSettingsPacket(Settings.RenderDistanceChunks));
    }

    public void OnStartGame(GameWindow window)
    {
        Window = window;
        window.VSync = VSyncMode.On;

        BlockRegistry.RegisterBlocks();

        ItemRegistry.RegisterItems();
        RecipeRegistry.RegisterRecipes();

        Input = new Input(window);

        AverageFPSCounter = new FPSCounter();

        if (RunMode == RunMode.Server)
        {
            if (!StartSession(_startArgs.IP, _startArgs.Port))
            {
                Logger.Error("Failed to start the server. Closing.");
                window.Close();
            }

            return;
        }

        FontRegistry.Initialize();

        ClientPlayer = new ClientPlayer(this);
        MasterRenderer = new MasterRenderer(this);

        _audioEngine = new AudioEngine { MasterVolume = Settings.MasterVolume };
        SoundDirector = new SoundDirector(this, _audioEngine, new SoundRegistry());
        Menu = new MenuController(
            this,
            _startArgs.IP + ":" + _startArgs.Port,
            _startArgs.Port,
            _startArgs.WorldName);

        if (!_startArgs.ShowMenu && StartSession(_startArgs.IP, _startArgs.Port))
        {
            return;
        }

        EnterState(GameState.MainMenu);
        Menu.ShowMainMenu();
    }

    public bool StartHostedGame(string worldName, int? seed, GameMode? gameMode)
    {
        WorldName = worldName;
        WorldSeed = seed;
        WorldGameMode = gameMode;

        FreshWorld = false;

        RunMode = RunMode.ClientServer;
        return StartSession(_startArgs.IP, _startArgs.Port);
    }

    public bool StartMultiplayer(string host, int port)
    {
        RunMode = RunMode.Client;
        return StartSession(host, port);
    }

    public void Pause()
    {
        if (State != GameState.Playing)
        {
            return;
        }

        EnterState(GameState.Paused);
        Menu.ShowPauseMenu();
    }

    public void Resume()
    {
        if (State != GameState.Paused)
        {
            return;
        }

        Menu.Hide();
        EnterState(GameState.Playing);
    }

    public void OpenInventory()
    {
        OpenInventoryWithBench(2);
    }

    public void OpenCraftingTable()
    {
        OpenInventoryWithBench(3);
    }

    private void OpenInventoryWithBench(int benchSize)
    {
        if (State != GameState.Playing || IsChatOpen)
        {
            return;
        }

        MasterRenderer.InventoryCanvas.OpenWithBench(benchSize);
        EnterState(GameState.Inventory);
    }

    public void CloseInventory()
    {
        if (State != GameState.Inventory)
        {
            return;
        }

        foreach (ItemStack leftover in MasterRenderer.InventoryCanvas.ReturnBenchContents())
        {
            ClientPlayer.ThrowAway(leftover);
        }

        ClientPlayer.ThrowAway(ClientPlayer.Inventory.ReturnCursorStack());
        EnterState(GameState.Playing);
    }

    public void QuitToTitle()
    {
        EndSession();
        EnterState(GameState.MainMenu);
        Menu.ShowMainMenu();
    }

    public void OnCloseGame()
    {
        EndSession();

        if (RunMode != RunMode.Server)
        {
            Settings.Save();

            MasterRenderer.CleanUp();
            _audioEngine.Dispose();
            Input.Dispose();
        }
    }

    public void OnUpdateGame(double deltaTimeSeconds)
    {
        float elapsedSeconds = deltaTimeSeconds <= 0 ? 0.0001F : (float)deltaTimeSeconds;

        CurrentFPS = 1.0F / elapsedSeconds;

        AverageFPSCounter.IncrementFrameCounter();
        AverageFPSCounter.AddElapsedTime(elapsedSeconds);

        elapsedSeconds = MathF.Min(elapsedSeconds, Constants.MAX_FRAME_TIME_SECONDS);

        if (RunMode != RunMode.Server)
        {
            HandleEscape();
            HandleInventoryKey();

            if (State != GameState.Playing)
            {
                Menu.Update();
            }
        }

        if (State != GameState.MainMenu)
        {
            UpdateSession(elapsedSeconds);
        }

        if (RunMode != RunMode.Server)
        {
            Input.Update();
        }
    }

    private void UpdateSession(float elapsedSeconds)
    {
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
        MasterRenderer.EndFrameUpdate(elapsedSeconds);
        SoundDirector.Update(elapsedSeconds, World);
        MasterRenderer.Particles.Update(elapsedSeconds, World);
    }

    public void OnRenderGame()
    {
        if (RunMode == RunMode.Server)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            return;
        }

        if (State == GameState.MainMenu)
        {
            MasterRenderer.RenderInterfaceOnly();
            return;
        }

        MasterRenderer.Render(World);
    }

    public void OnWindowResize(int newWidth, int newHeight)
    {
        if (RunMode != RunMode.Server && ClientPlayer is not null)
        {
            ClientPlayer.Camera.SetWindowSize(newWidth, newHeight);
        }
    }

    private void HandleEscape()
    {
        if (!Input.OnKeyPress(Keys.Escape) || IsChatOpen)
        {
            return;
        }

        switch (State)
        {
            case GameState.Playing:
                Pause();
                break;

            case GameState.Inventory:
                CloseInventory();
                break;

            case GameState.Paused:
                if (!Menu.OnEscape())
                {
                    Resume();
                }

                break;

            default:
                Menu.OnEscape();
                break;
        }
    }

    private void HandleInventoryKey()
    {
        if (!Input.OnKeyPress(Keys.E) || IsChatOpen)
        {
            return;
        }

        switch (State)
        {
            case GameState.Playing:
                OpenInventory();
                break;

            case GameState.Inventory:
                CloseInventory();
                break;
        }
    }

    private bool StartSession(string ip, int port)
    {
        if (IsServer)
        {
            Server = new Server(this, true);
            if (!Server.Start(port))
            {
                EndSession();
                return false;
            }
        }

        if (RunMode != RunMode.Server)
        {
            World = new WorldClient(this);
            ClientPlayer.World = World;

            Client = new Client(this);
            if (!Client.ConnectWith(ip, port))
            {
                EndSession();
                return false;
            }

            SendViewDistanceToServer();
        }

        EnterState(GameState.Playing);
        return true;
    }

    private void EndSession()
    {
        Client?.Stop();
        Server?.Stop();
        Client = null!;
        Server = null!;

        World = null!;

        if (RunMode != RunMode.Server)
        {
            MasterRenderer?.UnloadWorld();
            SoundDirector?.OnWorldUnloaded();
            ClientPlayer?.ResetForNewSession();
        }
    }

    private void EnterState(GameState state)
    {
        State = state;

        if (RunMode == RunMode.Server)
        {
            return;
        }

        Window.CursorState = state == GameState.Playing ? CursorState.Grabbed : CursorState.Normal;

        MasterRenderer.IngameCanvas.IsEnabled = state != GameState.MainMenu;

        MasterRenderer.HotbarCanvas.IsEnabled = state == GameState.Playing;
        MasterRenderer.InventoryCanvas.IsEnabled = state == GameState.Inventory;

        if (state == GameState.Playing)
        {
            MasterRenderer.DiscardPendingMouseLook();
        }
    }
}

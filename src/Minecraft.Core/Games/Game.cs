using Minecraft.Core.Audio;
using Minecraft.Core.Entities.Player;
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

/// <summary>
/// Owns everything the game is made of and drives it from the window's callbacks. Which of the pieces exist
/// depends on the run mode and on <see cref="State"/>: a dedicated server has no renderer or local player, a
/// pure client has no server, and while the main menu is up there is no world at all.
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
    public MenuController Menu { get; private set; } = null!;

    /// <summary>
    /// What this player has set the game up to look, sound and feel like. Read off disk before anything that
    /// is built from it, and every change to it applied straight away through <see cref="ApplySettings"/>.
    /// </summary>
    public GameSettings Settings { get; }

    /// <summary>Null on a dedicated server, which has nobody to hear anything.</summary>
    public SoundDirector SoundDirector { get; private set; } = null!;

    /// <summary>Whether this process is running a world of its own, rather than only joining somebody else's.</summary>
    public bool IsServer => RunMode is RunMode.ClientServer or RunMode.Server;

    /// <summary>
    /// How the current session is set up. Started from the run mode the game was launched with, and set
    /// again whenever a session is started from the menu, since that is what decides between hosting and
    /// joining.
    /// </summary>
    private AudioEngine _audioEngine = null!;

    public RunMode RunMode { get; private set; }

    public GameState State { get; private set; } = GameState.MainMenu;

    public float CurrentFPS { get; private set; }

    /// <summary>
    /// Whether the chat input line is open. While it is, keys belong to the chat rather than to the controls,
    /// which is why more than the chat itself has to be able to ask.
    /// </summary>
    public bool IsChatOpen => MasterRenderer?.IngameCanvas.IsTyping ?? false;

    /// <summary>Whether a world is loaded and nothing is covering it, which is when the chat may be opened.</summary>
    public bool IsPlaying => State == GameState.Playing;

    /// <summary>Whether the keyboard and mouse belong to the player rather than to a menu or the chat.</summary>
    public bool IsGameplayInputEnabled => State == GameState.Playing && !IsChatOpen;

    /// <summary>The world the server half of this process loads and saves.</summary>
    public string WorldName { get; private set; }

    /// <summary>Seed for a newly created world, or null to pick one at random.</summary>
    public int? WorldSeed { get; private set; }

    /// <summary>
    /// Which mode a newly created world is played in, or null to take the default. Like the seed, this only
    /// ever decides a world that does not exist yet: an existing one carries its own.
    /// </summary>
    public GameMode? WorldGameMode { get; private set; }

    /// <summary>Whether the world is discarded and regenerated when the server starts.</summary>
    public bool FreshWorld { get; private set; }

    public Game(StartArgs startArgs)
    {
        _startArgs = startArgs;
        RunMode = startArgs.RunMode;
        WorldName = startArgs.WorldName;
        WorldSeed = startArgs.Seed;
        WorldGameMode = startArgs.GameMode;
        FreshWorld = startArgs.FreshWorld;

        // Loaded here rather than at start up, since the camera and the audio engine are built from it.
        Settings = GameSettings.Load();
        Settings.OnChangedHandler += ApplySettings;
    }

    /// <summary>
    /// Takes a changed setting everywhere it has to go. Called for every change, including each step of a
    /// slider being dragged, so all of it is cheap: the one thing that is not — telling the server how far
    /// this player can see — is only reached when the distance itself has actually moved.
    /// </summary>
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

    /// <summary>
    /// Tells the server how much of the world to stream. The server owns which chunks are loaded, so this is
    /// the only way a render distance chosen here reaches the thing that acts on it.
    /// </summary>
    private void SendViewDistanceToServer()
    {
        Client?.WritePacket(new PlayerSettingsPacket(Settings.RenderDistanceChunks));
    }

    public void OnStartGame(GameWindow window)
    {
        Window = window;
        window.VSync = VSyncMode.On;

        BlockRegistry.RegisterBlocks();

        Input = new Input(window);

        AverageFPSCounter = new FPSCounter();

        // A dedicated server has nobody at the keyboard to pick anything from a menu, so it goes straight
        // into hosting the world it was pointed at.
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

        // Launching straight into a game is what the launch profiles and any scripted run expect, so the
        // menu can be skipped from the start arguments.
        if (!_startArgs.ShowMenu && StartSession(_startArgs.IP, _startArgs.Port))
        {
            return;
        }

        EnterState(GameState.MainMenu);
        Menu.ShowMainMenu();
    }

    /// <summary>
    /// Hosts the named world in this process and joins it, creating it from the given seed if nothing is
    /// saved under that name. The server it starts listens on every interface, so the same world is both the
    /// singleplayer game and one other players can join. Reports whether that worked.
    /// </summary>
    /// <param name="seed">Seeds a world being created. Null leaves it to be picked at random.</param>
    /// <param name="gameMode">The mode a world being created is played in. Null takes the default.</param>
    public bool StartHostedGame(string worldName, int? seed, GameMode? gameMode)
    {
        WorldName = worldName;
        WorldSeed = seed;
        WorldGameMode = gameMode;

        // A world started from the menu is never discarded first. The menu offers a name nothing is saved
        // under when a new world is what is wanted, so deleting one here could only ever lose a game.
        FreshWorld = false;

        RunMode = RunMode.ClientServer;
        return StartSession(_startArgs.IP, _startArgs.Port);
    }

    /// <summary>Joins a world somebody else is hosting. Reports whether the server could be reached.</summary>
    public bool StartMultiplayer(string host, int port)
    {
        RunMode = RunMode.Client;
        return StartSession(host, port);
    }

    /// <summary>Opens the pause menu, which is what Escape does while a world is loaded.</summary>
    public void Pause()
    {
        if (State != GameState.Playing)
        {
            return;
        }

        EnterState(GameState.Paused);
        Menu.ShowPauseMenu();
    }

    /// <summary>Closes the pause menu and hands the controls back to the player.</summary>
    public void Resume()
    {
        if (State != GameState.Paused)
        {
            return;
        }

        Menu.Hide();
        EnterState(GameState.Playing);
    }

    /// <summary>
    /// Opens the inventory over the world, which stays running behind it. Unlike the pause menu this releases
    /// the cursor without stopping anything, since the screen is worked with the mouse.
    /// </summary>
    public void OpenInventory()
    {
        if (State != GameState.Playing || IsChatOpen)
        {
            return;
        }

        EnterState(GameState.Inventory);
    }

    /// <summary>Closes the inventory, putting whatever was being carried on the cursor back into it.</summary>
    public void CloseInventory()
    {
        if (State != GameState.Inventory)
        {
            return;
        }

        ClientPlayer.Inventory.ReturnCursorStack();
        EnterState(GameState.Playing);
    }

    /// <summary>Leaves the world, saving it on the way out, and returns to the main menu.</summary>
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
        // A frame that took no measurable time would divide by zero here and make every rate infinite.
        float elapsedSeconds = deltaTimeSeconds <= 0 ? 0.0001F : (float)deltaTimeSeconds;

        CurrentFPS = 1.0F / elapsedSeconds;

        AverageFPSCounter.IncrementFrameCounter();
        AverageFPSCounter.AddElapsedTime(elapsedSeconds);

        // The counters above report the frame as it really was, but the simulation is only ever advanced by
        // a bounded amount, so that a stutter cannot move anything further in one step than it can be
        // simulated over.
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

        // The world keeps running while the pause menu is up. Stopping it would also stop the connection it
        // is fed by, and a server hearing nothing from a client eventually drops it.
        if (State != GameState.MainMenu)
        {
            UpdateSession(elapsedSeconds);
        }

        if (RunMode != RunMode.Server)
        {
            // Updated last so that a press is visible for the whole frame that observed it.
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

        // With no world loaded there is nothing to draw behind the menu, so only the interface is.
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

    /// <summary>
    /// Escape opens the pause menu while playing and closes it again while paused. An open chat swallows it
    /// first, since there it is what closes the input line.
    /// </summary>
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
                // The options are reached from the pause menu, so Escape there has to step back to it rather
                // than drop straight into the world with a screen still up over it.
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

    /// <summary>
    /// The inventory key opens the screen and closes it again, so the same key gets a player back out of it
    /// without having to find Escape. An open chat swallows it, since there it is a letter being typed.
    /// </summary>
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

    /// <summary>
    /// Brings up a world: the server half if this process hosts one, then the client half that joins it.
    /// Anything that was built is torn down again if the connection cannot be made, so a failed attempt
    /// leaves the game exactly as it was.
    /// </summary>
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

            // Straight after the join request, so the first chunks the server streams are already the right
            // number of them rather than the default followed by a correction.
            SendViewDistanceToServer();
        }

        EnterState(GameState.Playing);
        return true;
    }

    /// <summary>Tears the current world down, if there is one, and leaves the game ready to start another.</summary>
    private void EndSession()
    {
        // The client goes first: stopping the server closes the sockets underneath it, and a read already
        // in flight would then fail on a disposed stream.
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

        // The cursor is grabbed while playing so mouse look gets a raw delta and never leaves the window,
        // and released again for anything that has to be clicked on.
        Window.CursorState = state == GameState.Playing ? CursorState.Grabbed : CursorState.Normal;

        MasterRenderer.IngameCanvas.IsEnabled = state != GameState.MainMenu;

        // The bar belongs to a world being played rather than to the interface. The pause menu is a screen
        // over a game that has been stopped, and the inventory screen ends with the same nine slots drawn
        // larger, so in both cases showing it as well would only be showing it twice.
        MasterRenderer.HotbarCanvas.IsEnabled = state == GameState.Playing;
        MasterRenderer.InventoryCanvas.IsEnabled = state == GameState.Inventory;

        if (state == GameState.Playing)
        {
            // Grabbing the cursor recentres it, and the jump that leaves in the mouse delta would otherwise
            // spin the camera on the first frame back.
            MasterRenderer.DiscardPendingMouseLook();
        }
    }
}

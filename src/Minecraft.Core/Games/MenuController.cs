using Minecraft.Core.Network;
using Minecraft.Core.Render.UI.Presets;
using Minecraft.Core.Worlds.Generation;
using Minecraft.Core.Worlds.Storage;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Minecraft.Core.Games;

/// <summary>
/// Which menu screen is up and what the buttons on it do. The screens themselves only report what was
/// clicked; turning that into starting, leaving or closing a game happens here.
/// </summary>
public sealed class MenuController
{
    private enum Screen
    {
        /// <summary>No menu at all, which is what being in a world looks like.</summary>
        None,
        Main,
        Multiplayer,
        WorldSetup,
        Pause,
    }

    private readonly Game _game;
    private readonly string _savesRoot;
    private readonly string _defaultWorldName;

    private readonly UICanvasMainMenu _mainMenu;
    private readonly UICanvasMultiplayer _multiplayerMenu;
    private readonly UICanvasWorldSetup _worldSetup;
    private readonly UICanvasPauseMenu _pauseMenu;

    private Screen _screen = Screen.None;

    /// <summary>Which screen the world setup was opened from, and so where backing out of it goes.</summary>
    private Screen _worldSetupOpenedFrom = Screen.Main;

    public MenuController(Game game, string defaultServerAddress, int hostPort, string defaultWorldName)
    {
        _game = game;
        _savesRoot = Server.SavesDirectory;
        _defaultWorldName = defaultWorldName;

        _mainMenu = new UICanvasMainMenu(game);
        _multiplayerMenu = new UICanvasMultiplayer(
            game,
            defaultServerAddress,
            NetworkAddresses.LocalAddress + ":" + hostPort);
        _worldSetup = new UICanvasWorldSetup(game, _savesRoot);
        _pauseMenu = new UICanvasPauseMenu(game);

        // Registered once and left there. Which of them is drawn is decided by the canvas being enabled,
        // so switching screens does not rebuild anything.
        game.MasterRenderer.AddCanvas(_mainMenu);
        game.MasterRenderer.AddCanvas(_multiplayerMenu);
        game.MasterRenderer.AddCanvas(_worldSetup);
        game.MasterRenderer.AddCanvas(_pauseMenu);
    }

    public void ShowMainMenu() => SetScreen(Screen.Main);

    public void ShowPauseMenu() => SetScreen(Screen.Pause);

    public void Hide() => SetScreen(Screen.None);

    /// <summary>Escape steps back out of a screen that was opened from another one.</summary>
    public void OnEscape()
    {
        if (_screen is Screen.Multiplayer or Screen.WorldSetup)
        {
            GoBack();
        }
    }

    public void Update()
    {
        UICanvasMenu? screen = GetCanvas(_screen);
        if (screen is null)
        {
            return;
        }

        // A click while the window is not focused is the click that focused it, and should not also press
        // whatever happened to be under the cursor.
        bool mousePressed = _game.Window.IsFocused && Game.Input.OnMousePress(MouseButton.Left);
        Vector2 mousePosition = Game.Input.MousePosition;

        Act(screen.HandleInput(mousePosition, mousePressed));
    }

    private void Act(MenuAction action)
    {
        switch (action)
        {
            case MenuAction.Singleplayer:
                OpenWorldSetup(Screen.Main, "Singleplayer");
                break;

            case MenuAction.Multiplayer:
                SetScreen(Screen.Multiplayer);
                break;

            case MenuAction.Host:
                OpenWorldSetup(Screen.Multiplayer, "Host Game");
                break;

            case MenuAction.Play:
                Play();
                break;

            case MenuAction.Connect:
                Connect();
                break;

            case MenuAction.Back:
                GoBack();
                break;

            case MenuAction.Resume:
                _game.Resume();
                break;

            case MenuAction.QuitToTitle:
                _game.QuitToTitle();
                break;

            case MenuAction.QuitGame:
                _game.Window.Close();
                break;
        }
    }

    /// <summary>
    /// Opens the screen that names the world and picks its seed. It is the same screen either way round: a
    /// hosted world always accepts other players, so singleplayer and hosting are the same thing seen from
    /// different sides, and only the heading says which side that was.
    /// </summary>
    private void OpenWorldSetup(Screen openedFrom, string title)
    {
        _worldSetupOpenedFrom = openedFrom;

        _worldSetup.SetTitle(title);

        // Offered a name nothing is saved under, so that arriving here and pressing play generates a world
        // from the chosen seed instead of quietly reopening the last one, where a seed decides nothing.
        _worldSetup.Prepare(WorldStorage.SuggestUnusedWorldName(_savesRoot, _defaultWorldName));

        SetScreen(Screen.WorldSetup);
    }

    private void Play()
    {
        string worldName = _worldSetup.WorldName.Trim();
        if (worldName.Length == 0)
        {
            _worldSetup.SetStatus("Give the world a name before playing it.", isError: true);
            return;
        }

        if (_game.StartHostedGame(worldName, SeedParser.Parse(_worldSetup.SeedText)))
        {
            SetScreen(Screen.None);
            return;
        }

        _worldSetup.SetStatus(
            "Could not open the world. Is another copy of the game already running?",
            isError: true);
    }

    private void GoBack()
    {
        SetScreen(_screen == Screen.WorldSetup ? _worldSetupOpenedFrom : Screen.Main);
    }

    private void Connect()
    {
        if (!TryParseAddress(_multiplayerMenu.Address, out string host, out int port))
        {
            _multiplayerMenu.SetStatus("Enter an address such as 127.0.0.1:25565", isError: true);
            return;
        }

        if (_game.StartMultiplayer(host, port))
        {
            SetScreen(Screen.None);
            return;
        }

        _multiplayerMenu.SetStatus(
            "No server answered at " + host + ":" + port + ". Somebody has to host one first.",
            isError: true);
    }

    /// <summary>
    /// Reads a <c>host</c> or <c>host:port</c> address. Leaving the port off is common enough to be worth
    /// allowing, and falls back to the one the game listens on by default.
    /// </summary>
    private static bool TryParseAddress(string address, out string host, out int port)
    {
        host = string.Empty;
        port = ArgsParser.DefaultPort;

        string trimmed = address.Trim();
        if (trimmed.Length == 0)
        {
            return false;
        }

        int separator = trimmed.LastIndexOf(':');
        if (separator < 0)
        {
            host = trimmed;
            return true;
        }

        host = trimmed[..separator].Trim();
        string portText = trimmed[(separator + 1)..].Trim();

        if (host.Length == 0 || !int.TryParse(portText, out port) || port is < 1 or > 65535)
        {
            return false;
        }

        return true;
    }

    private void SetScreen(Screen screen)
    {
        _screen = screen;

        _mainMenu.IsEnabled = screen == Screen.Main;
        _multiplayerMenu.IsEnabled = screen == Screen.Multiplayer;
        _worldSetup.IsEnabled = screen == Screen.WorldSetup;
        _pauseMenu.IsEnabled = screen == Screen.Pause;

        GetCanvas(screen)?.OnShown();
    }

    private UICanvasMenu? GetCanvas(Screen screen) => screen switch
    {
        Screen.Main => _mainMenu,
        Screen.Multiplayer => _multiplayerMenu,
        Screen.WorldSetup => _worldSetup,
        Screen.Pause => _pauseMenu,
        _ => null,
    };
}

using Minecraft.Core.Network;
using Minecraft.Core.Render.UI.Presets;
using Minecraft.Core.Worlds.Generation;
using Minecraft.Core.Worlds.Storage;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Minecraft.Core.Games;

public sealed class MenuController
{
    private enum Screen
    {
        None,
        Main,
        Multiplayer,
        WorldList,
        WorldSetup,
        RenameWorld,
        DeleteWorld,
        Pause,
        Options,
    }

    private readonly Game _game;
    private readonly string _savesRoot;
    private readonly string _defaultWorldName;

    private readonly UICanvasMainMenu _mainMenu;
    private readonly UICanvasMultiplayer _multiplayerMenu;
    private readonly UICanvasWorldList _worldList;
    private readonly UICanvasWorldSetup _worldSetup;
    private readonly UICanvasRenameWorld _renameWorld;
    private readonly UICanvasDeleteWorld _deleteWorld;
    private readonly UICanvasPauseMenu _pauseMenu;
    private readonly UICanvasOptions _options;

    private Screen _screen = Screen.None;

    private Screen _worldListOpenedFrom = Screen.Main;

    private Screen _optionsOpenedFrom = Screen.Main;

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
        _worldList = new UICanvasWorldList(game);
        _worldSetup = new UICanvasWorldSetup(game, _savesRoot);
        _renameWorld = new UICanvasRenameWorld(game);
        _deleteWorld = new UICanvasDeleteWorld(game);
        _pauseMenu = new UICanvasPauseMenu(game);
        _options = new UICanvasOptions(game);

        game.MasterRenderer.AddCanvas(_mainMenu);
        game.MasterRenderer.AddCanvas(_multiplayerMenu);
        game.MasterRenderer.AddCanvas(_worldList);
        game.MasterRenderer.AddCanvas(_worldSetup);
        game.MasterRenderer.AddCanvas(_renameWorld);
        game.MasterRenderer.AddCanvas(_deleteWorld);
        game.MasterRenderer.AddCanvas(_pauseMenu);
        game.MasterRenderer.AddCanvas(_options);
    }

    public void ShowMainMenu() => SetScreen(Screen.Main);

    public void ShowPauseMenu() => SetScreen(Screen.Pause);

    public void Hide() => SetScreen(Screen.None);

    public bool OnEscape()
    {
        if (_screen is Screen.None or Screen.Main or Screen.Pause)
        {
            return false;
        }

        GoBack();
        return true;
    }

    public void Update()
    {
        UICanvasMenu? screen = GetCanvas(_screen);
        if (screen is null)
        {
            return;
        }

        bool mousePressed = _game.Window.IsFocused && Game.Input.OnMousePress(MouseButton.Left);
        Vector2 mousePosition = Game.Input.MousePosition;

        Act(screen.HandleInput(mousePosition, mousePressed));
    }

    private void Act(MenuAction action)
    {
        switch (action)
        {
            case MenuAction.Singleplayer:
                OpenWorldList(Screen.Main, "Singleplayer");
                break;

            case MenuAction.Multiplayer:
                SetScreen(Screen.Multiplayer);
                break;

            case MenuAction.Options:
                _optionsOpenedFrom = _screen;
                SetScreen(Screen.Options);
                break;

            case MenuAction.Host:
                OpenWorldList(Screen.Multiplayer, "Host Game");
                break;

            case MenuAction.PlaySelected:
                Play(_worldList.SelectedWorld, seed: null, gameMode: null);
                break;

            case MenuAction.CreateWorld:
                _worldSetup.SetTitle(_worldList.Title);
                _worldSetup.Prepare(WorldSaves.SuggestUnusedWorldName(_savesRoot, _defaultWorldName));
                SetScreen(Screen.WorldSetup);
                break;

            case MenuAction.Play:
                Play(_worldSetup.WorldName, SeedParser.Parse(_worldSetup.SeedText), _worldSetup.GameMode);
                break;

            case MenuAction.RenameSelected:
                _renameWorld.Prepare(_worldList.SelectedWorld);
                SetScreen(Screen.RenameWorld);
                break;

            case MenuAction.DeleteSelected:
                _deleteWorld.Prepare(_worldList.SelectedWorld);
                SetScreen(Screen.DeleteWorld);
                break;

            case MenuAction.Confirm:
                Confirm();
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

    private void OpenWorldList(Screen openedFrom, string title)
    {
        _worldListOpenedFrom = openedFrom;
        _worldList.SetTitle(title);
        SetScreen(Screen.WorldList);
    }

    private void Play(string worldName, int? seed, GameMode? gameMode)
    {
        string trimmed = worldName.Trim();
        if (trimmed.Length == 0)
        {
            _worldSetup.SetStatus("Give the world a name before playing it.", isError: true);
            return;
        }

        if (_game.StartHostedGame(trimmed, seed, gameMode))
        {
            SetScreen(Screen.None);
            return;
        }

        GetCanvas(_screen)?.SetStatus(
            "Could not open the world. Is another copy of the game already running?",
            isError: true);
    }

    private void Confirm()
    {
        switch (_screen)
        {
            case Screen.RenameWorld:
                Rename();
                break;

            case Screen.DeleteWorld:
                Delete();
                break;
        }
    }

    private void Rename()
    {
        string newName = _renameWorld.NewName.Trim();
        if (newName.Length == 0)
        {
            _renameWorld.SetStatus("Give the world a name.", isError: true);
            return;
        }

        string oldName = _renameWorld.CurrentName;
        WorldRenameResult result = WorldSaves.TryRenameWorld(_savesRoot, oldName, newName);

        switch (result)
        {
            case WorldRenameResult.NameTaken:
                _renameWorld.SetStatus("A world called '" + newName + "' already exists.", isError: true);
                return;

            case WorldRenameResult.SourceMissing:
                _renameWorld.SetStatus("'" + oldName + "' is no longer there.", isError: true);
                return;

            case WorldRenameResult.Failed:
                _renameWorld.SetStatus("Could not rename it. Something else may have it open.", isError: true);
                return;
        }

        SetScreen(Screen.WorldList);

        if (result == WorldRenameResult.Renamed)
        {
            _worldList.SetStatus("Renamed '" + oldName + "'.");
        }
    }

    private void Delete()
    {
        string worldName = _deleteWorld.WorldName;

        if (!WorldSaves.TryDeleteWorld(_savesRoot, worldName))
        {
            _deleteWorld.SetStatus("Could not delete it. Something else may have it open.", isError: true);
            return;
        }

        SetScreen(Screen.WorldList);
        _worldList.SetStatus("Deleted '" + worldName + "'.");
    }

    private void GoBack() => SetScreen(ParentOf(_screen));

    private Screen ParentOf(Screen screen) => screen switch
    {
        Screen.WorldList => _worldListOpenedFrom,
        Screen.WorldSetup => Screen.WorldList,
        Screen.RenameWorld => Screen.WorldList,
        Screen.DeleteWorld => Screen.WorldList,
        Screen.Options => _optionsOpenedFrom,
        _ => Screen.Main,
    };

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
        Screen previous = _screen;
        _screen = screen;

        _mainMenu.IsEnabled = screen == Screen.Main;
        _multiplayerMenu.IsEnabled = screen == Screen.Multiplayer;
        _worldList.IsEnabled = screen == Screen.WorldList;
        _worldSetup.IsEnabled = screen == Screen.WorldSetup;
        _renameWorld.IsEnabled = screen == Screen.RenameWorld;
        _deleteWorld.IsEnabled = screen == Screen.DeleteWorld;
        _pauseMenu.IsEnabled = screen == Screen.Pause;
        _options.IsEnabled = screen == Screen.Options;

        if (previous == Screen.Options)
        {
            _game.Settings.Save();
        }

        if (screen == Screen.WorldList)
        {
            _worldList.SetWorlds(WorldSaves.ListWorlds(_savesRoot));
        }

        GetCanvas(screen)?.OnShown();
    }

    private UICanvasMenu? GetCanvas(Screen screen) => screen switch
    {
        Screen.Main => _mainMenu,
        Screen.Multiplayer => _multiplayerMenu,
        Screen.WorldList => _worldList,
        Screen.WorldSetup => _worldSetup,
        Screen.RenameWorld => _renameWorld,
        Screen.DeleteWorld => _deleteWorld,
        Screen.Pause => _pauseMenu,
        Screen.Options => _options,
        _ => null,
    };
}

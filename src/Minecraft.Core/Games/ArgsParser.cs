using Minecraft.Core.Logging;
using Minecraft.Core.Network;

namespace Minecraft.Core.Games;

public static class ArgsParser
{
    public const string Usage = """
        Usage: Minecraft.App [key=value ...]

          mode=client|server|clientserver   Defaults to clientserver (singleplayer).
          ip=<address>                      Defaults to 127.0.0.1.
          port=<1-65535>                    Defaults to 25565.
          world=<name>                      Save directory under saves/. Defaults to world.
          seed=<number>                     Seeds a new world. Ignored if the world already exists.
          gamemode=survival|creative        Mode for a new world. Ignored if the world already exists.
                                            Defaults to survival.
          fresh=true|false                  Deletes the world first, so each launch generates new
                                            terrain. Defaults to false.
          menu=true|false                   Open on the main menu. Turn it off to start playing
                                            straight away with the settings above. Defaults to true.
          loglevel=packet|info|warn|error   Defaults to error.
        """;

    public const RunMode DefaultRunMode = RunMode.ClientServer;

    public const string DefaultIP = "127.0.0.1";

    public const int DefaultPort = 25565;

    public const LogLevel DefaultLogLevel = LogLevel.Error;

    public const string DefaultWorldName = "world";

    public const GameMode DefaultGameMode = GameMode.Survival;

    private static readonly string[] _knownKeys =
        ["mode", "ip", "port", "world", "seed", "gamemode", "fresh", "menu", "loglevel"];

    public static StartArgs ParseProgramArgs(string[] args)
    {
        Dictionary<string, string> parsedArgs = [];
        foreach (string arg in args)
        {
            string[] keyValue = arg.Split('=', 2);

            if (keyValue.Length != 2)
            {
                Console.Error.WriteLine("Ignoring start argument '" + arg + "'. Arguments are of the form key=value.");
                continue;
            }

            string key = keyValue[0].Trim().ToLowerInvariant();

            if (!_knownKeys.Contains(key))
            {
                Console.Error.WriteLine("Ignoring unknown start argument '" + key + "'.");
                continue;
            }

            parsedArgs[key] = keyValue[1].Trim();
        }

        return new StartArgs
        {
            RunMode = GetRunMode(parsedArgs),
            IP = GetIp(parsedArgs),
            Port = GetPort(parsedArgs),
            LogLevel = GetLogLevel(parsedArgs),
            WorldName = GetWorldName(parsedArgs),
            Seed = GetSeed(parsedArgs),
            GameMode = GetGameMode(parsedArgs),
            FreshWorld = GetFreshWorld(parsedArgs),
            ShowMenu = GetShowMenu(parsedArgs),
        };
    }

    private static bool GetShowMenu(Dictionary<string, string> startArgs)
    {
        if (!startArgs.TryGetValue("menu", out string? value))
        {
            return true;
        }

        if (!bool.TryParse(value, out bool showMenu))
        {
            throw new ArgumentException("Invalid menu '" + value + "'. Expected true or false.");
        }

        return showMenu;
    }

    private static bool GetFreshWorld(Dictionary<string, string> startArgs)
    {
        if (!startArgs.TryGetValue("fresh", out string? value))
        {
            return false;
        }

        if (!bool.TryParse(value, out bool fresh))
        {
            throw new ArgumentException("Invalid fresh '" + value + "'. Expected true or false.");
        }

        return fresh;
    }

    private static RunMode GetRunMode(Dictionary<string, string> startArgs)
    {
        if (!startArgs.TryGetValue("mode", out string? value))
        {
            return DefaultRunMode;
        }

        return value.ToLowerInvariant() switch
        {
            "client" => RunMode.Client,
            "server" => RunMode.Server,
            "clientserver" => RunMode.ClientServer,
            _ => throw new ArgumentException(
                "Invalid run mode '" + value + "'. Expected client, server or clientserver."),
        };
    }

    private static string GetIp(Dictionary<string, string> startArgs)
    {
        if (!startArgs.TryGetValue("ip", out string? value) || value.Length == 0)
        {
            return DefaultIP;
        }

        return value;
    }

    private static int GetPort(Dictionary<string, string> startArgs)
    {
        if (!startArgs.TryGetValue("port", out string? value))
        {
            return DefaultPort;
        }

        if (!int.TryParse(value, out int port) || port is < 1 or > 65535)
        {
            throw new ArgumentException("Invalid port '" + value + "'. Expected a number between 1 and 65535.");
        }

        return port;
    }

    private static string GetWorldName(Dictionary<string, string> startArgs)
    {
        if (!startArgs.TryGetValue("world", out string? value) || value.Length == 0)
        {
            return DefaultWorldName;
        }

        return value;
    }

    private static int? GetSeed(Dictionary<string, string> startArgs)
    {
        if (!startArgs.TryGetValue("seed", out string? value))
        {
            return null;
        }

        if (!int.TryParse(value, out int seed))
        {
            throw new ArgumentException("Invalid seed '" + value + "'. Expected a whole number.");
        }

        return seed;
    }

    private static GameMode? GetGameMode(Dictionary<string, string> startArgs)
    {
        if (!startArgs.TryGetValue("gamemode", out string? value))
        {
            return null;
        }

        if (!Enum.TryParse(value, ignoreCase: true, out GameMode gameMode) || !Enum.IsDefined(gameMode))
        {
            throw new ArgumentException(
                "Invalid game mode '" + value + "'. Expected survival or creative.");
        }

        return gameMode;
    }

    private static LogLevel GetLogLevel(Dictionary<string, string> startArgs)
    {
        if (!startArgs.TryGetValue("loglevel", out string? value))
        {
            return DefaultLogLevel;
        }

        if (!Enum.TryParse(value, ignoreCase: true, out LogLevel logLevel) || !Enum.IsDefined(logLevel))
        {
            throw new ArgumentException(
                "Invalid log level '" + value + "'. Expected packet, info, warn or error.");
        }

        return logLevel;
    }
}

using Minecraft.Core.Logging;
using Minecraft.Core.Network;

namespace Minecraft.Core.Games;

public sealed class ArgsParser
{
    /// <summary>A description of every start argument, shown when one of them is invalid.</summary>
    public const string Usage = """
        Usage: Minecraft.App [key=value ...]

          mode=client|server|clientserver   Defaults to clientserver (singleplayer).
          ip=<address>                      Defaults to 127.0.0.1.
          port=<1-65535>                    Defaults to 25565.
          loglevel=packet|info|warn|error   Defaults to error.
        """;

    /// <summary>Singleplayer, so that launching with no arguments at all lands somewhere playable.</summary>
    public const RunMode DefaultRunMode = RunMode.ClientServer;

    public const string DefaultIP = "127.0.0.1";

    public const int DefaultPort = 25565;

    public const LogLevel DefaultLogLevel = LogLevel.Error;

    private static readonly string[] _knownKeys = ["mode", "ip", "port", "loglevel"];

    /// <summary>
    /// Parses <c>key=value</c> start arguments. Every argument is optional; anything left out falls back to
    /// the defaults above, which run a singleplayer game on the loopback address.
    /// </summary>
    public StartArgs ParseProgramArgs(string[] args)
    {
        Dictionary<string, string> parsedArgs = [];
        foreach (string arg in args)
        {
            string[] keyValue = arg.Split('=', 2);

            // These go straight to the console rather than through the logger: the log level is itself one
            // of the arguments being parsed here, and at its default it would swallow them.
            if (keyValue.Length != 2)
            {
                Console.Error.WriteLine("Ignoring start argument '" + arg + "'. Arguments are of the form key=value.");
                continue;
            }

            string key = keyValue[0].Trim().ToLowerInvariant();

            // A misspelled key would otherwise be silently replaced by its default, which is confusing.
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
        };
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

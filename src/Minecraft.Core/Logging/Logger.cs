using System.Diagnostics;

namespace Minecraft.Core.Logging;

public static class Logger
{
    private static LogLevel _logLevel = LogLevel.Error;

    public static void SetLogLevel(LogLevel logLevel)
    {
        _logLevel = logLevel;
    }

    private static void Print(string message, LogLevel level)
    {
        string line = $"[{DateTime.Now:HH:mm:ss}][{level}] {message}";
        Console.WriteLine(line);
        Debug.WriteLine(line);
    }

    public static void Info(string message)
    {
        if (LogLevel.Info >= _logLevel)
        {
            Print(message, LogLevel.Info);
        }
    }

    public static void Warn(string message)
    {
        if (LogLevel.Warn >= _logLevel)
        {
            Print(message, LogLevel.Warn);
        }
    }

    public static void Error(string message)
    {
        if (LogLevel.Error >= _logLevel)
        {
            Print(message, LogLevel.Error);
        }
    }

    public static void Packet(string message)
    {
        if (LogLevel.Packet >= _logLevel)
        {
            Print(message, LogLevel.Packet);
        }
    }
}

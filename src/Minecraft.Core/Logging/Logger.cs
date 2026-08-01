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
        // The message is concatenated, never used as a format string: log output such as a shader
        // info log can legitimately contain braces and would otherwise throw a FormatException.
        string line = $"[{DateTime.Now:HH:mm:ss}][{level}] {message}";
        Console.WriteLine(line);
        // Also emitted to the debugger, so the log is visible in the IDE's output window and not only
        // in the console window the game was launched from.
        System.Diagnostics.Debug.WriteLine(line);
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

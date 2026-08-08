using Minecraft.Core.Games;
using Minecraft.Core.Logging;

StartArgs startArgs;
try
{
    startArgs = ArgsParser.ParseProgramArgs(args);
}
catch (ArgumentException e)
{
    // A bad start argument is a usage mistake, so report it plainly instead of as a crash.
    Console.Error.WriteLine(e.Message);
    Console.Error.WriteLine();
    Console.Error.WriteLine(ArgsParser.Usage);
    return 1;
}

Logger.SetLogLevel(startArgs.LogLevel);

using var window = new GameWindow(startArgs);
window.Run();

return 0;

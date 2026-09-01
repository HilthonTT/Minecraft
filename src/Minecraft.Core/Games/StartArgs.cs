using Minecraft.Core.Logging;
using Minecraft.Core.Network;

namespace Minecraft.Core.Games;

public struct StartArgs
{
    public RunMode RunMode;
    public string IP;
    public int Port;
    public LogLevel LogLevel;

    public string WorldName;

    public int? Seed;

    public GameMode? GameMode;

    public bool FreshWorld;

    public bool ShowMenu;
}

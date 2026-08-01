using Minecraft.Core.Logging;
using Minecraft.Core.Network;

namespace Minecraft.Core.Games;

public struct StartArgs
{
    public RunMode RunMode;
    public string IP;
    public int Port;
    public LogLevel LogLevel;

    /// <summary>The name of the world directory under <c>saves/</c>.</summary>
    public string WorldName;

    /// <summary>Seeds a newly created world. Ignored when the world already exists.</summary>
    public int? Seed;
}

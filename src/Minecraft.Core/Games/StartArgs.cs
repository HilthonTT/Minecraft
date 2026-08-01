using Minecraft.Core.Logging;
using Minecraft.Core.Network;

namespace Minecraft.Core.Games;

public struct StartArgs
{
    public RunMode RunMode;
    public string IP;
    public int Port;
    public LogLevel LogLevel;
}

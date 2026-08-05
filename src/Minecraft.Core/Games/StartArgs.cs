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

    /// <summary>
    /// Deletes the named world before loading it, so every launch generates new terrain. Combined with no
    /// explicit <see cref="Seed"/> this gives a different random seed each time.
    /// </summary>
    public bool FreshWorld;

    /// <summary>
    /// Whether the game opens on the main menu. Turning it off starts a session straight away using the
    /// run mode and address given here, which is what a scripted launch wants. Ignored by a dedicated
    /// server, which never shows a menu.
    /// </summary>
    public bool ShowMenu;
}

namespace Minecraft.Core.Entities.Player;

/// <summary>
/// What one connected player has asked of the server. Held per session rather than globally, since two
/// players on the same world may want to see different distances of it.
/// </summary>
public struct PlayerSettings
{
    /// <summary>How far out from the player, in chunks, the server streams and keeps the world loaded.</summary>
    public int ViewDistance;
}

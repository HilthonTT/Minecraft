namespace Minecraft.Core.Games;

/// <summary>
/// Which of the two ways a world is played. Written into <c>level.dat</c> as its name and into packets as its
/// underlying number, so existing entries keep their order and new ones go on the end.
/// </summary>
public enum GameMode
{
    /// <summary>
    /// Blocks are spent when they are placed and take time to break, breaking one leaves something behind to
    /// pick up, and the player can be hurt and killed. There is no flying.
    /// </summary>
    Survival,

    /// <summary>
    /// The world as a drawing board: an endless supply of every block, blocks break the instant they are
    /// struck and leave nothing behind, nothing can hurt the player, and a double jump takes off.
    /// </summary>
    Creative,
}

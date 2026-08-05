namespace Minecraft.Core.Games;

/// <summary>
/// What the game is currently doing. It decides which pieces exist, which of them are updated and drawn,
/// and whether the keyboard and mouse belong to the player or to a menu.
/// </summary>
public enum GameState
{
    /// <summary>No world is loaded and one of the menu screens is up.</summary>
    MainMenu,

    /// <summary>A world is loaded and the controls belong to the player.</summary>
    Playing,

    /// <summary>A world is loaded, but the pause menu is up and the controls belong to it.</summary>
    Paused,
}

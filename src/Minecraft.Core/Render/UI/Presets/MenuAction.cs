namespace Minecraft.Core.Render.UI.Presets;

/// <summary>
/// What a menu screen was asked to do. The screens only report it: acting on it means starting or leaving a
/// world, which is the game's business rather than the canvas's.
/// </summary>
public enum MenuAction
{
    None,

    /// <summary>Start a world hosted by this process.</summary>
    Singleplayer,

    /// <summary>Open the screen that asks which server to join.</summary>
    Multiplayer,

    /// <summary>Join the server the multiplayer screen is pointed at.</summary>
    Connect,

    /// <summary>Start a world in this process for other players to join.</summary>
    Host,

    /// <summary>Step back to the screen this one was opened from.</summary>
    Back,

    /// <summary>Close the pause menu and hand the controls back to the player.</summary>
    Resume,

    /// <summary>Leave the world and return to the main menu.</summary>
    QuitToTitle,

    /// <summary>Close the game.</summary>
    QuitGame,
}

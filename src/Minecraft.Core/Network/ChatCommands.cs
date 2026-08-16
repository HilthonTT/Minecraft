using Minecraft.Core.Entities.Player;
using Minecraft.Core.Games;
using Minecraft.Core.Network.Packets;
using Minecraft.Core.Network.Session;

namespace Minecraft.Core.Network;

/// <summary>
/// The commands a player can type into the chat.
/// <para>
/// Run on the server, because everything they change is the server's: a client typing <c>/gamemode</c> is
/// asking, the same way clicking a block is asking, and what comes back is the mode it has been put into. A
/// reply goes to the one player who asked rather than to the room, since nobody else was told the question.
/// </para>
/// </summary>
public static class ChatCommands
{
    /// <summary>What marks a line as a command rather than something to say.</summary>
    public const char Prefix = '/';

    /// <summary>
    /// Runs the message as a command if it is one, and reports whether it was. A message that is not a
    /// command is left alone for the chat to broadcast.
    /// </summary>
    public static bool TryHandle(Game game, ServerSession session, string message)
    {
        string trimmed = message.Trim();
        if (trimmed.Length < 2 || trimmed[0] != Prefix)
        {
            return false;
        }

        string[] parts = trimmed[1..].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return false;
        }

        switch (parts[0].ToLowerInvariant())
        {
            case "gamemode":
            case "gm":
                RunGameMode(game, session, parts);
                return true;

            default:
                Reply(session, "Unknown command '" + parts[0] + "'. Try /gamemode survival or /gamemode creative.");
                return true;
        }
    }

    private static void RunGameMode(Game game, ServerSession session, string[] parts)
    {
        if (session.Player is not ServerPlayer player)
        {
            return;
        }

        if (parts.Length < 2)
        {
            Reply(session, "You are in " + Describe(player.GameMode) + " mode. Use /gamemode survival or /gamemode creative.");
            return;
        }

        if (!TryParseGameMode(parts[1], out GameMode gameMode))
        {
            Reply(session, "'" + parts[1] + "' is not a game mode. Try survival or creative.");
            return;
        }

        if (player.GameMode == gameMode)
        {
            Reply(session, "You are already in " + Describe(gameMode) + " mode.");
            return;
        }

        player.SetGameMode(gameMode);
        session.WritePacket(new PlayerGameModePacket(gameMode));
        session.WritePacket(new PlayerHealthPacket(player.Health, wasHurt: false));

        // Written back onto the world as well, so that a world reopens in the mode it was left in rather
        // than in the one it happened to be created in. On a server with several players that also decides
        // what the next person to join arrives in, which is the closest this game has to a world setting.
        game.Server.World.DefaultGameMode = gameMode;

        Reply(session, "Game mode set to " + Describe(gameMode) + ".");
    }

    private static bool TryParseGameMode(string text, out GameMode gameMode)
    {
        switch (text.ToLowerInvariant())
        {
            case "s":
            case "0":
            case "survival":
                gameMode = GameMode.Survival;
                return true;

            case "c":
            case "1":
            case "creative":
                gameMode = GameMode.Creative;
                return true;

            default:
                gameMode = GameMode.Survival;
                return false;
        }
    }

    private static string Describe(GameMode gameMode) => gameMode.ToString().ToLowerInvariant();

    /// <summary>
    /// Answers the one player who asked. An empty sender is what marks a line as coming from the game rather
    /// than from somebody, which is how the chat tells the two apart on the way in.
    /// </summary>
    private static void Reply(ServerSession session, string message)
    {
        session.WritePacket(new ChatPacket(string.Empty, message));
    }
}

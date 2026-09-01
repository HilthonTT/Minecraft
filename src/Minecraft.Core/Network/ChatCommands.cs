using Minecraft.Core.Entities.Player;
using Minecraft.Core.Games;
using Minecraft.Core.Network.Packets;
using Minecraft.Core.Network.Session;

namespace Minecraft.Core.Network;

public static class ChatCommands
{
    public const char Prefix = '/';

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

    private static void Reply(ServerSession session, string message)
    {
        session.WritePacket(new ChatPacket(string.Empty, message));
    }
}

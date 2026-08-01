using Minecraft.Core.Games;
using Minecraft.Core.Utilities.Vectors;
using Minecraft.Core.Worlds;
using Minecraft.Core.Worlds.Chunks;
using OpenTK.Mathematics;
using System.Text;

namespace Minecraft.Core.Render.UI.Presets;

/// <summary>The F2 debug readout: position, chunk, lighting and performance information.</summary>
public sealed class UICanvasDebug : UICanvas
{
    private readonly Game _game;
    private readonly UIText _debugText;

    public UICanvasDebug(Game game)
        : base(
            Vector3.Zero,
            Vector3.Zero,
            game.Window.ClientSize.X,
            game.Window.ClientSize.Y,
            RenderSpace.Screen)
    {
        _game = game;

        _debugText = new UIText(
            this,
            FontRegistry.GetFont(FontType.Arial),
            new Vector2(0, 0),
            new Vector2(0.4F, 0.4F),
            string.Empty);
        AddComponentToRender(_debugText);
    }

    public override void Update()
    {
        Vector3 position = _game.ClientPlayer.Position;
        Vector3 velocity = _game.ClientPlayer.Velocity;
        Vector3 acceleration = _game.ClientPlayer.Acceleration;

        Vector3i playerGridPos = position.ToBlockPos();
        Vector3i chunkLocalPos = playerGridPos.ToChunkLocal();
        Vector2 chunkPos = World.GetChunkPosition(position.X, position.Z);
        _game.World.LoadedChunks.TryGetValue(chunkPos, out Chunk? currentChunk);

        var builder = new StringBuilder();

        builder.AppendLine($"Focused={_game.Window.IsFocused} Vsync={_game.Window.VSync}");
        builder.AppendLine(
            $"Position X={position.X:0.00} Y={position.Y:0.00} Z={position.Z:0.00}" +
            $" Grid Position X={playerGridPos.X} Y={playerGridPos.Y} Z={playerGridPos.Z}" +
            $" ChunkLocal X={chunkLocalPos.X} Y={chunkLocalPos.Y} Z={chunkLocalPos.Z}");
        builder.AppendLine($"Velocity X={velocity.X:0.00} Y={velocity.Y:0.00} Z={velocity.Z:0.00}");
        builder.AppendLine($"Acceleration X={acceleration.X:0.00} Y={acceleration.Y:0.00} Z={acceleration.Z:0.00}");
        builder.AppendLine($"Chunk X={(int)chunkPos.X} Z={(int)chunkPos.Y} Section Y={(int)(position.Y / 16)}");

        if (currentChunk is not null)
        {
            builder.AppendLine(
                $"Light sources in chunk={currentChunk.LightSourceBlocks.Count}" +
                $" Desired strength={_game.MasterRenderer.DebugHelper.LightDebug.DesiredLightLevel}" +
                $" Debug={_game.MasterRenderer.DebugHelper.RenderBlockLightAreas}");

            AppendLightInfo(builder, currentChunk, chunkLocalPos);
        }

        builder.AppendLine($"FPS={_game.CurrentFPS:0} AVG FPS={_game.AverageFPSCounter.GetAverageFPS()}");
        builder.AppendLine($"Block={_game.ClientPlayer.MouseOverObject?.BlockstateHit}");
        builder.AppendLine(
            $"Time={_game.World.Environment.CurrentTime:0.00}/{_game.World.Environment.TimeInDay}");
        builder.AppendLine($"IsServer={_game.IsServer}");
        builder.AppendLine($"Mem={GC.GetTotalMemory(false) / 1000000}MB");

        _debugText.Text = builder.ToString();
    }

    private void AppendLightInfo(StringBuilder builder, Chunk currentChunk, Vector3i chunkLocalPos)
    {
        if (chunkLocalPos.Y >= 0 && chunkLocalPos.Y < Constants.MAX_BUILD_HEIGHT)
        {
            builder.AppendLine(
                $"Light at feet R={currentChunk.LightMap.GetRedBlockLightAt(chunkLocalPos)}" +
                $" G={currentChunk.LightMap.GetGreenBlockLightAt(chunkLocalPos)}" +
                $" B={currentChunk.LightMap.GetBlueBlockLightAt(chunkLocalPos)}" +
                $" Sun={currentChunk.LightMap.GetSunLightIntensityAt(chunkLocalPos)}");
        }

        if (_game.ClientPlayer.MouseOverObject is null)
        {
            return;
        }

        Vector3i intersectedBlockPos = _game.ClientPlayer.MouseOverObject.IntersectedBlockPos;
        Vector2 mouseOverChunkPos = World.GetChunkPosition(intersectedBlockPos.X, intersectedBlockPos.Z);
        if (!_game.World.LoadedChunks.TryGetValue(mouseOverChunkPos, out Chunk? cursorChunk))
        {
            return;
        }

        Vector3i mouseBlockLocalPos = intersectedBlockPos.ToChunkLocal();

        builder.AppendLine(
            $"Light at mouse R={cursorChunk.LightMap.GetRedBlockLightAt(mouseBlockLocalPos)}" +
            $" G={cursorChunk.LightMap.GetGreenBlockLightAt(mouseBlockLocalPos)}" +
            $" B={cursorChunk.LightMap.GetBlueBlockLightAt(mouseBlockLocalPos)}");

        // Read from the chunk the block is actually in, which need not be the one the player stands in.
        bool isTopBlock = cursorChunk.TopMostBlocks[mouseBlockLocalPos.X, mouseBlockLocalPos.Z] == mouseBlockLocalPos.Y;
        builder.AppendLine($"Is Top Block={isTopBlock}");
    }
}

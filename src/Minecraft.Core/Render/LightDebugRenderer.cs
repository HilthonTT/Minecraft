using Minecraft.Core.Games;
using Minecraft.Core.Utilities;
using Minecraft.Core.Worlds;
using Minecraft.Core.Worlds.Chunks;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Minecraft.Core.Render;

public sealed class LightDebugRenderer
{
    private const uint MinLightLevel = 1;
    private const uint MaxLightLevel = 15;

    private readonly Game _game;
    private readonly WireframeRenderer _wireframeRenderer;

    public uint DesiredLightLevel { get; private set; } = MinLightLevel;

    public LightDebugRenderer(Game game, WireframeRenderer wireframeRenderer)
    {
        _game = game;
        _wireframeRenderer = wireframeRenderer;
    }

    public void RenderLightArea()
    {
        if (_game.IsGameplayInputEnabled)
        {
            if (Game.Input.OnKeyPress(Keys.Down) && DesiredLightLevel > MinLightLevel)
            {
                DesiredLightLevel--;
            }

            if (Game.Input.OnKeyPress(Keys.Up) && DesiredLightLevel < MaxLightLevel)
            {
                DesiredLightLevel++;
            }
        }

        Vector2 chunkPos = World.GetChunkPosition(_game.ClientPlayer.Position.X, _game.ClientPlayer.Position.Z);
        if (!_game.World.LoadedChunks.TryGetValue(chunkPos, out Chunk? chunk))
        {
            return;
        }

        for (uint x = 0; x < 16; x++)
        {
            for (uint z = 0; z < 16; z++)
            {
                for (uint y = 0; y < Constants.MAX_BUILD_HEIGHT; y++)
                {
                    if (chunk.LightMap.GetSunLightIntensityAt(x, y, z) != DesiredLightLevel)
                    {
                        continue;
                    }

                    var translation = new Vector3(x + chunk.GridX * 16, y, z + chunk.GridZ * 16);
                    float green = MathUtils.ConvertRange(0, MaxLightLevel, 0, 0.85F, DesiredLightLevel) + 0.15F;
                    _wireframeRenderer.RenderWireframeAt(3, translation, Vector3.One, new Vector3(0, green, 0));
                }
            }
        }
    }
}

using OpenTK.Mathematics;

namespace Minecraft.Core.Textures;

public sealed class TextureAtlas : Texture
{
    private readonly float _cellUVSize;

    public TextureAtlas(int textureId, int atlasSizeInPixels, int atlasCellSizeInPixels)
        : base(textureId, atlasSizeInPixels, atlasSizeInPixels)
    {
        int cellsPerRow = atlasSizeInPixels / atlasCellSizeInPixels;
        _cellUVSize = 1.0F / cellsPerRow;
    }

    public Vector2[] GetTextureCoords(float atlasGridX, float atlasGridY)
    {
        float xMin = atlasGridX * _cellUVSize;
        float yMin = atlasGridY * _cellUVSize;
        float xMax = atlasGridX * _cellUVSize + _cellUVSize;
        float yMax = atlasGridY * _cellUVSize + _cellUVSize;

        return
        [
            new Vector2(xMax, yMax),
            new Vector2(xMin, yMax),
            new Vector2(xMin, yMin),
            new Vector2(xMax, yMin),
        ];
    }

    public Vector2[] GetTextureCoords(Vector2 atlatGrid)
    {
        return GetTextureCoords(atlatGrid.X, atlatGrid.Y);
    }
}

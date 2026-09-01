using Minecraft.Core.Textures;
using System.Collections.ObjectModel;

namespace Minecraft.Core.Render.UI;

public sealed class Font
{
    public Texture FontMapTexture { get; private set; }
    public int DesiredPixelLineHeight { get; private set; }
    public ReadOnlyDictionary<int, Character> FontChars { get; private set; }

    public Font(string fontFilePath, string fontMapFilePath, int fontMapWidth, int fontMapHeight)
    {
        int fontMapTextureid = TextureLoader.LoadTexture(fontMapFilePath, smooth: true);
        FontMapTexture = new Texture(fontMapTextureid, fontMapWidth, fontMapHeight);

        var charBuilder = new CharacterBuilder();
        FontChars = new ReadOnlyDictionary<int, Character>(charBuilder.BuildFont(FontMapTexture, fontFilePath));
        DesiredPixelLineHeight = FontChars.Aggregate((l, r) => l.Value.Height > r.Value.Height ? l : r).Value.Height;
    }

    public float MeasureWidth(string text, float horizontalScale)
    {
        float width = 0;
        foreach (char character in text)
        {
            if (FontChars.TryGetValue(character, out Character fontCharacter))
            {
                width += (int)(fontCharacter.XAdvance * horizontalScale);
            }
        }

        return width;
    }

    public (float Top, float Bottom) MeasureVerticalBounds(string text, float verticalScale)
    {
        float top = float.MaxValue;
        float bottom = float.MinValue;

        foreach (char character in text)
        {
            if (!FontChars.TryGetValue(character, out Character fontCharacter))
            {
                continue;
            }

            top = MathF.Min(top, fontCharacter.YOffset * verticalScale);
            bottom = MathF.Max(bottom, (fontCharacter.YOffset + fontCharacter.Height) * verticalScale);
        }

        return top > bottom ? (0, DesiredPixelLineHeight * verticalScale) : (top, bottom);
    }
}

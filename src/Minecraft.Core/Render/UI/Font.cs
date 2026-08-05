using Minecraft.Core.Textures;
using Minecraft.Core.Utilities;
using System.Collections.ObjectModel;

namespace Minecraft.Core.Render.UI;

public sealed class Font
{
    public Texture FontMapTexture { get; private set; }
    public int DesiredPixelLineHeight { get; private set; }
    public ReadOnlyDictionary<int, Character> FontChars { get; private set; }

    public Font(string fontFilePath, string fontMapFilePath, int fontMapWidth, int fontMapHeight)
    {
        // Glyphs are authored far larger than they are drawn, so the font map is filtered rather than
        // point sampled, which would otherwise drop whole rows of pixels out of small text.
        int fontMapTextureid = TextureLoader.LoadTexture(fontMapFilePath, smooth: true);
        FontMapTexture = new Texture(fontMapTextureid, fontMapWidth, fontMapHeight);

        var charBuilder = new CharacterBuilder();
        FontChars = new ReadOnlyDictionary<int, Character>(charBuilder.BuildFont(FontMapTexture, fontFilePath));
        DesiredPixelLineHeight = FontChars.Aggregate((l, r) => l.Value.Height > r.Value.Height ? l : r).Value.Height;
    }

    /// <summary>
    /// How wide a run of text ends up in canvas pixels. Advances are truncated per character exactly as the
    /// mesh builder does, so a width measured here lines up with what is drawn down to the pixel.
    /// </summary>
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

    /// <summary>
    /// How far below the top edge of the component drawing it a run of text starts and ends, in canvas
    /// pixels. Glyphs hang below that edge by an offset of their own, so anything centring text against the
    /// line height alone leaves it sitting low in its box.
    /// </summary>
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

        // Text with nothing drawable in it, which still has to be given somewhere to sit.
        return top > bottom ? (0, DesiredPixelLineHeight * verticalScale) : (top, bottom);
    }
}

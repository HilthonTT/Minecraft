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
}

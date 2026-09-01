using System.Collections.ObjectModel;
using Minecraft.Core.Utilities;

namespace Minecraft.Core.Render.UI;

public static class FontRegistry
{
    private static ReadOnlyDictionary<FontType, Font> _fonts =
        new(new Dictionary<FontType, Font>());

    public static void Initialize()
    {
        RegisterFonts();
    }

    public static Font GetFont(FontType fontType)
    {
        if (!_fonts.TryGetValue(fontType, out Font? font))
        {
            throw new KeyNotFoundException();
        }
        return font;
    }

    private static void RegisterFonts()
    {
        Dictionary<FontType, Font> registry = new Dictionary<FontType, Font>
            {
                { FontType.Arial, new Font(Assets.Path("Resources/arial.fnt"), Assets.Path("Resources/arial.png"), 512, 512) },
            };
        _fonts = new ReadOnlyDictionary<FontType, Font>(registry);
    }
}

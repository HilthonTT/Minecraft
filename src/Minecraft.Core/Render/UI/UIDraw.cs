using Minecraft.Core.Textures;
using OpenTK.Mathematics;

namespace Minecraft.Core.Render.UI;

public static class UIDraw
{
    public static UIImage Rect(UICanvas canvas, Vector3 color, float transparency = 1F, Texture? texture = null)
    {
        var image = new UIImage(canvas, Vector2.Zero, Vector2.Zero, texture ?? UITextures.White)
        {
            Color = color,
            Transparency = transparency,
        };

        canvas.AddComponentToRender(image);
        return image;
    }

    public static void Place(UIImage image, Vector2 position, Vector2 size)
    {
        image.PixelPositionInCanvas = position;
        image.Dimension = new Vector2(Math.Max(0F, size.X), Math.Max(0F, size.Y));
    }

    public static void Place(UIImage image, float left, float top, float width, float height)
    {
        Place(image, new Vector2(left, top), new Vector2(width, height));
    }
}

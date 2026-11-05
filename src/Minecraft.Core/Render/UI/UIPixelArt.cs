namespace Minecraft.Core.Render.UI;

public static class UIPixelArt
{
    public const int IconSize = 9;

    public const int SceneWidth = 384;
    public const int SceneHeight = 216;

    private static readonly string[] _heart =
    [
        ".XX...XX.",
        "XooX.XooX",
        "XoooXoooX",
        "XoooooooX",
        "XoooooooX",
        ".XoooooX.",
        "..XoooX..",
        "...XoX...",
        "....X....",
    ];

    private static readonly string[] _shank =
    [
        "......XX.",
        ".....XooX",
        "....XooX.",
        "..XXXXX..",
        ".XoooooX.",
        "XooooooX.",
        "XoooooX..",
        "XooooX...",
        ".XXXX....",
    ];

    public static byte[] Heart(bool leftHalfOnly) => Stamp(_heart, leftHalfOnly);

    public static byte[] Shank(bool leftHalfOnly) => Stamp(_shank, leftHalfOnly);

    public static byte[] VerticalGradient(int height, float top, float bottom)
    {
        byte[] pixels = new byte[height * 4];
        for (int row = 0; row < height; row++)
        {
            float t = height == 1 ? 0F : row / (float)(height - 1);
            byte value = ToByte(top + ((bottom - top) * t));

            pixels[(row * 4) + 0] = value;
            pixels[(row * 4) + 1] = value;
            pixels[(row * 4) + 2] = value;
            pixels[(row * 4) + 3] = 255;
        }

        return pixels;
    }

    public static byte[] Vignette(int size, float innerRadius, float strength)
    {
        byte[] pixels = new byte[size * size * 4];
        float centre = (size - 1) / 2F;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - centre) / centre;
                float dy = (y - centre) / centre;
                float distance = MathF.Sqrt((dx * dx) + (dy * dy)) / MathF.Sqrt(2F);

                float t = Math.Clamp((distance - innerRadius) / (1F - innerRadius), 0F, 1F);
                float alpha = t * t * (3F - (2F * t)) * strength;

                int index = ((y * size) + x) * 4;
                pixels[index + 3] = Math.Max((byte)1, ToByte(alpha));
            }
        }

        return pixels;
    }

    public static byte[] Scene(int width, int height, int seed)
    {
        byte[] pixels = new byte[width * height * 4];

        int horizon = (int)(height * 0.58F);

        for (int y = 0; y < height; y++)
        {
            float t = y / (float)horizon;
            (float r, float g, float b) sky = Mix((0.20F, 0.42F, 0.78F), (0.62F, 0.80F, 0.96F), Math.Clamp(t, 0F, 1F));
            for (int x = 0; x < width; x++)
            {
                Put(pixels, width, x, y, sky);
            }
        }

        PaintSun(pixels, width, height, (int)(width * 0.78F), (int)(height * 0.18F), Math.Max(4, height / 18));
        PaintClouds(pixels, width, height, seed);

        PaintRidge(pixels, width, height, seed + 11, horizon - (height * 0.16F), height * 0.10F, 0.035F,
            (0.47F, 0.58F, 0.72F), null, 0);
        PaintRidge(pixels, width, height, seed + 23, horizon - (height * 0.04F), height * 0.07F, 0.06F,
            (0.33F, 0.52F, 0.36F), null, 0);
        PaintRidge(pixels, width, height, seed + 37, horizon + (height * 0.10F), height * 0.06F, 0.09F,
            (0.47F, 0.34F, 0.22F), (0.36F, 0.65F, 0.25F), 3);
        PaintTrees(pixels, width, height, seed + 41, horizon + (height * 0.10F), height * 0.06F, 0.09F);
        PaintRidge(pixels, width, height, seed + 53, horizon + (height * 0.28F), height * 0.05F, 0.12F,
            (0.38F, 0.27F, 0.17F), (0.30F, 0.56F, 0.20F), 4);
        PaintStones(pixels, width, height, seed + 67, horizon + (height * 0.28F), height * 0.05F, 0.12F);

        return pixels;
    }

    public static float Noise1D(int seed, float x)
    {
        int cell = (int)MathF.Floor(x);
        float fraction = x - cell;
        float smooth = fraction * fraction * (3F - (2F * fraction));

        float a = Hash(seed, cell);
        float b = Hash(seed, cell + 1);
        return a + ((b - a) * smooth);
    }

    public static float Hash(int seed, int value)
    {
        unchecked
        {
            uint h = (uint)(value * 374761393) + ((uint)seed * 668265263U);
            h = (h ^ (h >> 13)) * 1274126177U;
            h ^= h >> 16;
            return (h & 0xFFFFFF) / (float)0xFFFFFF;
        }
    }

    private static float RidgeHeight(int seed, int x, float baseline, float amplitude, float frequency)
    {
        float noise = (Noise1D(seed, x * frequency) * 0.7F) + (Noise1D(seed + 5, x * frequency * 2.7F) * 0.3F);
        return baseline - (noise * amplitude);
    }

    private static void PaintRidge(
        byte[] pixels,
        int width,
        int height,
        int seed,
        float baseline,
        float amplitude,
        float frequency,
        (float R, float G, float B) body,
        (float R, float G, float B)? top,
        int topThickness)
    {
        for (int x = 0; x < width; x++)
        {
            int surface = Math.Clamp((int)RidgeHeight(seed, x / 4 * 4, baseline, amplitude, frequency), 0, height - 1);

            for (int y = surface; y < height; y++)
            {
                bool isTop = top is not null && y - surface < topThickness;
                (float r, float g, float b) colour = isTop ? top!.Value : body;

                float grain = (Hash(seed + 97, (x * 7919) + (y * 104729)) - 0.5F) * 0.08F;
                Put(pixels, width, x, y, (colour.r + grain, colour.g + grain, colour.b + grain));
            }
        }
    }

    private static void PaintTrees(
        byte[] pixels,
        int width,
        int height,
        int seed,
        float baseline,
        float amplitude,
        float frequency)
    {
        for (int x = 6; x < width - 6; x += 4)
        {
            if (Hash(seed, x) > 0.10F)
            {
                continue;
            }

            int ground = (int)RidgeHeight(seed - 4, x / 4 * 4, baseline, amplitude, frequency);
            int trunkHeight = 6 + (int)(Hash(seed + 1, x) * 4);

            for (int y = ground - trunkHeight; y < ground; y++)
            {
                for (int dx = 0; dx < 2; dx++)
                {
                    Put(pixels, width, x + dx, y, (0.36F, 0.26F, 0.15F));
                }
            }

            int crownTop = ground - trunkHeight - 6;
            for (int y = crownTop; y < ground - trunkHeight + 2; y++)
            {
                for (int dx = -4; dx < 6; dx++)
                {
                    float shade = Hash(seed + 3, ((x + dx) * 31) + y) * 0.08F;
                    Put(pixels, width, x + dx, y, (0.16F + shade, 0.42F + shade, 0.14F + shade));
                }
            }
        }
    }

    private static void PaintStones(
        byte[] pixels,
        int width,
        int height,
        int seed,
        float baseline,
        float amplitude,
        float frequency)
    {
        for (int x = 0; x < width; x += 4)
        {
            int surface = (int)RidgeHeight(seed - 14, x / 4 * 4, baseline, amplitude, frequency);
            for (int y = surface + 12; y < height; y += 4)
            {
                if (Hash(seed, (x * 131) + y) > 0.10F)
                {
                    continue;
                }

                for (int dy = 0; dy < 4; dy++)
                {
                    for (int dx = 0; dx < 4; dx++)
                    {
                        Put(pixels, width, x + dx, y + dy, (0.45F, 0.45F, 0.47F));
                    }
                }
            }
        }
    }

    private static void PaintSun(byte[] pixels, int width, int height, int centreX, int centreY, int radius)
    {
        for (int y = centreY - radius; y <= centreY + radius; y++)
        {
            for (int x = centreX - radius; x <= centreX + radius; x++)
            {
                if (x >= 0 && x < width && y >= 0 && y < height)
                {
                    Put(pixels, width, x, y, (1.0F, 0.96F, 0.74F));
                }
            }
        }
    }

    private static void PaintClouds(byte[] pixels, int width, int height, int seed)
    {
        int band = (int)(height * 0.12F);
        for (int x = 0; x < width; x++)
        {
            for (int y = band; y < band + (height * 0.12F); y++)
            {
                float density = Noise1D(seed + 7, (x / 8) * 0.35F) + (Noise1D(seed + 9, y * 0.4F) * 0.2F);
                if (density > 0.72F && (y - band) % 12 < 6)
                {
                    Put(pixels, width, x, y, (0.95F, 0.97F, 1.0F));
                }
            }
        }
    }

    private static byte[] Stamp(string[] shape, bool leftHalfOnly)
    {
        byte[] pixels = new byte[IconSize * IconSize * 4];
        int columns = leftHalfOnly ? (IconSize / 2) + 1 : IconSize;

        for (int y = 0; y < IconSize; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                char cell = shape[y][x];
                if (cell == '.')
                {
                    continue;
                }

                int index = ((y * IconSize) + x) * 4;
                byte value = cell == 'X' ? (byte)0 : (byte)255;
                pixels[index + 0] = value;
                pixels[index + 1] = value;
                pixels[index + 2] = value;
                pixels[index + 3] = 255;
            }
        }

        return pixels;
    }

    private static (float R, float G, float B) Mix((float R, float G, float B) a, (float R, float G, float B) b, float t)
    {
        return (a.R + ((b.R - a.R) * t), a.G + ((b.G - a.G) * t), a.B + ((b.B - a.B) * t));
    }

    private static void Put(byte[] pixels, int width, int x, int y, (float R, float G, float B) colour)
    {
        int height = pixels.Length / 4 / width;
        if (x < 0 || x >= width || y < 0 || y >= height)
        {
            return;
        }

        int index = ((y * width) + x) * 4;
        pixels[index + 0] = ToByte(colour.R);
        pixels[index + 1] = ToByte(colour.G);
        pixels[index + 2] = ToByte(colour.B);
        pixels[index + 3] = 255;
    }

    private static byte ToByte(float value) => (byte)Math.Clamp((int)MathF.Round(value * 255F), 0, 255);
}

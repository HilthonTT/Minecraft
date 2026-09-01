using Minecraft.Core.Logging;
using Minecraft.Core.Textures;

namespace Minecraft.Core.Render.UI;

public sealed class CharacterBuilder
{
    public Dictionary<int, Character> BuildFont(Texture fontTexture, string fontFilePath)
    {
        Dictionary<int, Character> fontChars = [];

        foreach (string line in ReadFileLines(fontFilePath))
        {
            string[] splitLines = line.Split(separator: Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries);
            if (splitLines.Length == 0 || splitLines[0] != "char")
            {
                continue;
            }

            Character charc = new();
            for (int i = 1; i < splitLines.Length; i++)
            {
                string[] attributeValue = splitLines[i].Split('=');
                if (attributeValue.Length != 2)
                {
                    Logger.Error("Error parsing attribute value combo from font file " + fontFilePath);
                }

                string attribute = attributeValue[0];
                int value = int.Parse(attributeValue[1]);

                if (attribute == "id")
                {
                    charc.ID = value;
                }
                else if (attribute == "x")
                {
                    charc.XTextureMin = (float)value / fontTexture.PixelWidth;
                }
                else if (attribute == "y")
                {
                    charc.YTextureMin = (float)value / fontTexture.PixelHeight;
                }
                else if (attribute == "width")
                {
                    charc.XTextureOffset = (float)value / fontTexture.PixelWidth;
                    charc.Width = value;
                }
                else if (attribute == "height")
                {
                    charc.YTextureOffset = (float)value / fontTexture.PixelHeight;
                    charc.Height = value;
                }
                else if (attribute == "xoffset")
                {
                    charc.XOffset = value;
                }
                else if (attribute == "yoffset")
                {
                    charc.YOffset = value;
                }
                else if (attribute == "xadvance")
                {
                    charc.XAdvance = value;
                }
            }

            fontChars.Add(charc.ID, charc);
        }

        return fontChars;
    }

    private static string[] ReadFileLines(string fontFilePath)
    {
        try
        {
            return File.ReadAllLines(fontFilePath);
        }
        catch (Exception e)
        {
            Logger.Error("Filed to load font file " + fontFilePath + ". Error message: " + e.Message);
            return [];
        }
    }
}

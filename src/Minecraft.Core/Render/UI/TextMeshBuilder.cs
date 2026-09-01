using OpenTK.Mathematics;

namespace Minecraft.Core.Render.UI;

public sealed class TextMeshBuilder
{
    public float[] GetVerticesForText(UIText textComponent)
    {
        int charCount = textComponent.Text.Count(c => c != '\n');
        float[] allVertices = new float[charCount * 6 * 3];

        float pixelToNdcX = 2.0F / textComponent.ParentCanvas.PixelWidth;
        float pixelToNdcY = 2.0F / textComponent.ParentCanvas.PixelHeight;

        float xNdc = textComponent.PixelPositionInCanvas.X * pixelToNdcX - 1;
        float yNdc = 1 - textComponent.PixelPositionInCanvas.Y * pixelToNdcY;

        int xPointer = 0;
        int yPointer = 0;
        int charPointer = 0;
        int charCounter = 0;
        for (int j = 0; j < textComponent.Text.Length; j++)
        {
            char c = textComponent.Text[charPointer++];
            if (c == '\n')
            {
                yPointer -= (int)(textComponent.Font.DesiredPixelLineHeight * textComponent.Scale.Y);
                xPointer = 0;
                continue;
            }

            textComponent.Font.FontChars.TryGetValue(c, out Character charc);

            float cxPointer = xNdc + (xPointer * pixelToNdcX);
            float cyPointer = yNdc + (yPointer * pixelToNdcY);

            float cwidth = charc.Width * textComponent.Scale.X * pixelToNdcX;
            float cHeight = -charc.Height * textComponent.Scale.Y * pixelToNdcY;
            float cxOffset = charc.XOffset * textComponent.Scale.X * pixelToNdcX;
            float cyOffset = -charc.YOffset * textComponent.Scale.Y * pixelToNdcY;
            Vector3 topLeft = new(cxPointer + cxOffset, cyPointer + cyOffset, 0);
            Vector3 bottomLeft = new(cxPointer + cxOffset, cyPointer + cyOffset + cHeight, 0);
            Vector3 bottomRight = new(cxPointer + cxOffset + cwidth, cyPointer + cyOffset + cHeight, 0);
            Vector3 topRight = new(cxPointer + cxOffset + cwidth, cyPointer + cyOffset, 0);

            int i = charCounter * 18;
            charCounter++;
            allVertices[i + 0] = bottomLeft.X; allVertices[i + 1] = bottomLeft.Y; allVertices[i + 2] = bottomLeft.Z;
            allVertices[i + 3] = bottomRight.X; allVertices[i + 4] = bottomRight.Y; allVertices[i + 5] = bottomRight.Z;
            allVertices[i + 6] = topRight.X; allVertices[i + 7] = topRight.Y; allVertices[i + 8] = topRight.Z;
            allVertices[i + 9] = bottomLeft.X; allVertices[i + 10] = bottomLeft.Y; allVertices[i + 11] = bottomLeft.Z;
            allVertices[i + 12] = topRight.X; allVertices[i + 13] = topRight.Y; allVertices[i + 14] = topRight.Z;
            allVertices[i + 15] = topLeft.X; allVertices[i + 16] = topLeft.Y; allVertices[i + 17] = topLeft.Z;
            xPointer += (int)(charc.XAdvance * textComponent.Scale.X);
        }

        return allVertices;
    }

    public float[] GetTexturesForText(UIText textComponent)
    {
        int charCount = textComponent.Text.Count(c => c != '\n');
        float[] allTextures = new float[charCount * 6 * 2];
        int charPointer = 0;
        int charCounter = 0;
        for (int j = 0; j < textComponent.Text.Length; j++)
        {
            char c = textComponent.Text[charPointer++];
            textComponent.Font.FontChars.TryGetValue(c, out Character charc);
            if (c == '\n')
            {
                continue;
            }

            int i = charCounter * 12;
            charCounter++;
            allTextures[i + 0] = charc.XTextureMin; allTextures[i + 1] = charc.YTextureMin + charc.YTextureOffset;
            allTextures[i + 2] = charc.XTextureMin + charc.XTextureOffset; allTextures[i + 3] = charc.YTextureMin + charc.YTextureOffset;
            allTextures[i + 4] = charc.XTextureMin + charc.XTextureOffset; allTextures[i + 5] = charc.YTextureMin;
            allTextures[i + 6] = charc.XTextureMin; allTextures[i + 7] = charc.YTextureMin + charc.YTextureOffset;
            allTextures[i + 8] = charc.XTextureMin + charc.XTextureOffset; allTextures[i + 9] = charc.YTextureMin;
            allTextures[i + 10] = charc.XTextureMin; allTextures[i + 11] = charc.YTextureMin;
        }
        return allTextures;
    }
}

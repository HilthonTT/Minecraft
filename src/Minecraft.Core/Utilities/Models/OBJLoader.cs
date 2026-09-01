using Minecraft.Core.Logging;
using System.Globalization;

namespace Minecraft.Core.Utilities.Models;

public static class OBJLoader
{
    private readonly static char[] _splitCharacters = [' '];
    private readonly static char[] _faceParamaterSplitter = ['/'];

    public static ModelData Load(string fileName)
    {
        try
        {
            using var streamReader = new StreamReader(fileName);
            return Load(streamReader);
        }
        catch (Exception e)
        {
            Logger.Error($"Could not load model '{fileName}': {e.Message}");
        }

        return new ModelData
        {
            positions = [],
            normals = [],
            textureCoordinates = [],
            indices = [],
        };
    }

    private static ModelData Load(TextReader textReader)
    {
        List<float> vertices = [];
        List<float> normals = [];
        List<float> texCoords = [];
        List<int> indices = [];

        string? line;
        while ((line = textReader.ReadLine()) is not null)
        {
            string[] parameters = line.Split(_splitCharacters, StringSplitOptions.RemoveEmptyEntries);
            if (parameters.Length == 0)
            {
                continue;
            }

            switch (parameters[0])
            {
                case "v":
                    vertices.Add(float.Parse(parameters[1], CultureInfo.InvariantCulture));
                    vertices.Add(float.Parse(parameters[2], CultureInfo.InvariantCulture));
                    vertices.Add(float.Parse(parameters[3], CultureInfo.InvariantCulture));
                    break;
                case "f":
                    if (parameters.Length == 4)
                    {
                        ParseFace(parameters[1], vertices, indices);
                        ParseFace(parameters[2], vertices, indices);
                        ParseFace(parameters[3], vertices, indices);
                    }
                    else
                    {
                        Logger.Warn("Skipping a face with " + (parameters.Length - 1) + " vertices; only triangles are supported.");
                    }
                    break;
                default:
                    break;
            }
        }

        return new ModelData()
        {
            positions = [.. vertices],
            normals = [.. normals],
            textureCoordinates = [.. texCoords],
            indices = [.. indices],
        };
    }

    private static void ParseFace(string faceParameter, List<float> vertices, List<int> indices)
    {
        string[] parameters = faceParameter.Split(_faceParamaterSplitter);
        int vertexIndex = int.Parse(parameters[0]);

        if (vertexIndex < 0)
        {
            vertexIndex = (vertices.Count / 3) + vertexIndex;
        }
        else
        {
            vertexIndex--;
        }
        indices.Add(vertexIndex);
    }
}

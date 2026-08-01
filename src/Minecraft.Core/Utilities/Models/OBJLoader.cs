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

        // Empty rather than default, so a failed load cannot hand null arrays to whoever asked for the
        // model. The result draws nothing instead of throwing far away from the actual problem.
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
        // Kept local so a failed load cannot leak half a model into the next one.
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

            // Only positions and triangular faces are consumed. Anything else a modelling package may
            // have written is skipped rather than treated as an error, so that a richer file still loads.
            switch (parameters[0])
            {
                case "v": // vertex position
                    vertices.Add(float.Parse(parameters[1], CultureInfo.InvariantCulture));
                    vertices.Add(float.Parse(parameters[2], CultureInfo.InvariantCulture));
                    vertices.Add(float.Parse(parameters[3], CultureInfo.InvariantCulture));
                    break;
                case "f": // face
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
            // OBJ indices are 1 based; a negative one counts back from the last vertex read so far.
            // The list holds three floats per vertex, so the vertex count is a third of its length.
            vertexIndex = (vertices.Count / 3) + vertexIndex;
        }
        else
        {
            vertexIndex--;
        }
        indices.Add(vertexIndex);
    }
}

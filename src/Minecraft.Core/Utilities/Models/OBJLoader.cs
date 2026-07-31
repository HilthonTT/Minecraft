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
            Console.WriteLine(e);
        }
        return new ModelData();
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

            switch (parameters[0])
            {
                case "p": // point
                    throw new NotImplementedException();
                case "v": // vertex
                    float x = float.Parse(parameters[1]);
                    float y = float.Parse(parameters[2]);
                    float z = float.Parse(parameters[3]);
                    vertices.Add(x);
                    vertices.Add(y);
                    vertices.Add(z);
                    break;
                case "vt": // texCoord
                    throw new NotImplementedException();
                case "vn": // normal
                    throw new NotImplementedException();
                case "f":
                    switch (parameters.Length)
                    {
                        case 4:
                            ParseFace(parameters[1], vertices, indices);
                            ParseFace(parameters[2], vertices, indices);
                            ParseFace(parameters[3], vertices, indices);
                            break;
                        case 5:
                            throw new NotImplementedException();
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

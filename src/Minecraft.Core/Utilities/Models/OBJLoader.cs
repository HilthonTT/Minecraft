namespace Minecraft.Core.Utilities.Models;

public static class OBJLoader
{
    private readonly static char[] _splitCharacters = [' '];
    private readonly static char[] _faceParamaterSplitter = ['/'];

    private readonly static List<float> _vertices = [];
    private readonly static List<float> _normals = [];
    private readonly static List<float> _texCoords = [];
    private readonly static List<int> _indices = [];

    public static ModelData Load(string fileName)
    {
        var m = new ModelData();
        try
        {
            using var streamReader = new StreamReader(fileName);
            m = Load(streamReader);
            streamReader.Close();
            return m;
        }
        catch (Exception e) 
        {
            Console.WriteLine(e);
        }
        return m;
    }

    private static ModelData Load(TextReader textReader)
    {
        string? line;
        while ((line = textReader.ReadLine()) is not null)
        {
            line = line.Trim(_splitCharacters);
            line = line.Replace("  ", " ");
            string[] parameters = line.Split(_splitCharacters);
            switch (parameters[0])
            {
                case "p": // point
                    throw new NotImplementedException();
                case "v": // vertex
                    float x = float.Parse(parameters[1]);
                    float y = float.Parse(parameters[2]);
                    float z = float.Parse(parameters[3]);
                    _vertices.Add(x);
                    _vertices.Add(y);
                    _vertices.Add(z);
                    break;
                case "vt": // texCoord
                    throw new NotImplementedException();
                case "vn": // normal
                    throw new NotImplementedException();
                case "f":
                    switch (parameters.Length)
                    {
                        case 4:
                            ParseFace(parameters[1]);
                            ParseFace(parameters[2]);
                            ParseFace(parameters[3]);
                            break;
                        case 5:
                            throw new NotImplementedException();
                    }
                    break;
                default:
                    break;
            }
        }

        var model = new ModelData()
        {
            positions = _vertices.ToArray(),
            normals = _normals.ToArray(),
            textureCoordinates = _texCoords.ToArray(),
            indices = _indices.ToArray()
        };

        _vertices.Clear();
        _normals.Clear();
        _texCoords.Clear();
        _indices.Clear();

        return model;
    }

    private static void ParseFace(string faceParameter)
    {
        string[] parameters = faceParameter.Split(_faceParamaterSplitter);
        int vertexIndex = int.Parse(parameters[0]);

        if (vertexIndex < 0)
        {
            vertexIndex = _vertices.Count + vertexIndex;
        }
        else
        {
            vertexIndex--;
        }
        _indices.Add(vertexIndex);
    }
}

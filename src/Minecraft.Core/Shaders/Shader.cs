using Minecraft.Core.Logging;
using Minecraft.Core.Utilities;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Minecraft.Core.Shaders;

public abstract class Shader
{
    private readonly int _programId;
    private readonly int _vertexShaderId;
    private readonly int _fragmentShaderId;

    protected Shader(string vertexFile, string fragmentFile)
    {
        _vertexShaderId = LoadShader(vertexFile, ShaderType.VertexShader);
        _fragmentShaderId = LoadShader(fragmentFile, ShaderType.FragmentShader);
        _programId = GL.CreateProgram();

        GL.AttachShader(_programId, _vertexShaderId);
        GL.AttachShader(_programId, _fragmentShaderId);
        BindAttributes();

        GL.LinkProgram(_programId);
        GL.GetProgram(_programId, GetProgramParameterName.LinkStatus, out int linked);
        if (linked == 0)
        {
            // A program that failed to link still has a valid id, so nothing downstream would notice;
            // it simply draws nothing and every uniform location comes back as -1.
            Logger.Error($"Could not link shader program for '{vertexFile}' and '{fragmentFile}'.");
            Logger.Error(GL.GetProgramInfoLog(_programId));
        }

        GL.ValidateProgram(_programId);
        GetAllUniformLocations();
    }

    protected abstract void BindAttributes();

    protected abstract void GetAllUniformLocations();

    protected int GetUniformLocation(string uniform)
    {
        return GL.GetUniformLocation(_programId, uniform);
    }

    public void Start()
    {
        GL.UseProgram(_programId);
    }

    public void Stop()
    {
        GL.UseProgram(0);
    }

    public void CleanUp()
    {
        Stop();
        GL.DetachShader(_programId, _vertexShaderId);
        GL.DetachShader(_programId, _fragmentShaderId);
        GL.DeleteShader(_vertexShaderId);
        GL.DeleteShader(_fragmentShaderId);
        GL.DeleteProgram(_programId);
    }

    protected void BindAttribute(int attribute, string variableName)
    {
        GL.BindAttribLocation(_programId, attribute, variableName);
    }

    public void LoadFloat(int location, float value)
    {
        GL.Uniform1(location, value);
    }

    public void LoadInt(int location, int value)
    {
        GL.Uniform1(location, value);
    }

    public void LoadVector(int location, Vector3 vector)
    {
        GL.Uniform3(location, vector);
    }

    public void LoadMatrix(int location, Matrix4 matrix)
    {
        GL.UniformMatrix4(location, false, ref matrix);
    }

    public void LoadTexture(
        int uniformLocation,
        int textureUnitLayout,
        int textureId,
        TextureTarget target = TextureTarget.Texture2D)
    {
        GL.Uniform1(uniformLocation, textureUnitLayout);
        GL.ActiveTexture(TextureUnit.Texture0 + textureUnitLayout);
        GL.BindTexture(target, textureId);
    }

    /// <summary> Loads and compiles a shader stage. The path is relative to the output directory. </summary>
    public static int LoadShader(string file, ShaderType type)
    {
        string source = File.ReadAllText(Assets.Path(file));
        int shaderId = GL.CreateShader(type);

        GL.ShaderSource(shaderId, source);
        GL.CompileShader(shaderId);

        GL.GetShader(shaderId, ShaderParameter.CompileStatus, out int success);
        if (success == 0)
        {
            Logger.Error("Could not compile shader: " + file);
            Logger.Error(GL.GetShaderInfoLog(shaderId));
        }

        return shaderId;
    }
}

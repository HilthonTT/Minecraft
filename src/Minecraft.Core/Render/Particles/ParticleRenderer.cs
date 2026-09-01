using Minecraft.Core.Entities;
using Minecraft.Core.Shaders.BasicShader;
using Minecraft.Core.Textures;
using Minecraft.Core.Worlds;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Minecraft.Core.Render.Particles;

public sealed class ParticleRenderer
{
    private const int VerticesPerParticle = 6;
    private const int MaxVertices = ParticleSystem.Capacity * VerticesPerParticle;

    private static readonly int[] _cornerOrder = [0, 1, 2, 0, 2, 3];

    private readonly BasicShader _shader;
    private readonly TextureAtlas _textureAtlas;

    private readonly float[] _positions = new float[MaxVertices * 3];
    private readonly float[] _normals = new float[MaxVertices * 3];
    private readonly float[] _uvs = new float[MaxVertices * 2];
    private readonly uint[] _lights = new uint[MaxVertices];

    private readonly int _vao;
    private readonly int _positionBuffer;
    private readonly int _normalBuffer;
    private readonly int _uvBuffer;
    private readonly int _lightBuffer;

    public ParticleRenderer(BasicShader shader, TextureAtlas textureAtlas)
    {
        _shader = shader;
        _textureAtlas = textureAtlas;

        _vao = GL.GenVertexArray();
        GL.BindVertexArray(_vao);

        _positionBuffer = CreateStreamBuffer(attribute: 0, componentsPerVertex: 3, sizeof(float));
        _normalBuffer = CreateStreamBuffer(attribute: 1, componentsPerVertex: 3, sizeof(float));
        _uvBuffer = CreateStreamBuffer(attribute: 2, componentsPerVertex: 2, sizeof(float));
        _lightBuffer = CreateStreamBuffer(attribute: 3, componentsPerVertex: 1, sizeof(uint), isInteger: true);

        GL.BindVertexArray(0);
    }

    public void Render(ParticleSystem system, Camera camera, World world, Vector3 fogColor, float fogStart, float fogEnd)
    {
        int vertexCount = BuildGeometry(system, camera);
        if (vertexCount == 0)
        {
            return;
        }

        _shader.Start();
        _shader.LoadTexture(_shader.LocationTextureAtlas, 0, _textureAtlas.Id);
        _shader.LoadMatrix(_shader.LocationTransformationMatrix, Matrix4.Identity);
        _shader.LoadMatrix(_shader.LocationViewMatrix, camera.CurrentViewMatrix);
        _shader.LoadMatrix(_shader.LocationProjectionMatrix, camera.CurrentProjectionMatrix);
        _shader.LoadVector(_shader.LocationSunColor, world.Environment.GetCurrentSunColor());
        _shader.LoadVector(_shader.LocationAmbientColor, world.Environment.AmbientColor);
        _shader.LoadVector(_shader.LocationCameraPosition, camera.Position);
        _shader.LoadVector(_shader.LocationFogColor, fogColor);
        _shader.LoadFloat(_shader.LocationFogStart, fogStart);
        _shader.LoadFloat(_shader.LocationFogEnd, fogEnd);
        _shader.LoadFloat(_shader.LocationMaterialAlpha, 1.0F);

        GL.BindVertexArray(_vao);
        Upload(_positionBuffer, _positions, vertexCount * 3, sizeof(float));
        Upload(_normalBuffer, _normals, vertexCount * 3, sizeof(float));
        Upload(_uvBuffer, _uvs, vertexCount * 2, sizeof(float));
        Upload(_lightBuffer, _lights, vertexCount, sizeof(uint));

        GL.Disable(EnableCap.CullFace);
        GL.DrawArrays(PrimitiveType.Triangles, 0, vertexCount);
        GL.Enable(EnableCap.CullFace);

        GL.BindVertexArray(0);
    }

    private int BuildGeometry(ParticleSystem system, Camera camera)
    {
        Vector3 right = camera.Right;
        Vector3 up = Vector3.Normalize(Vector3.Cross(camera.Forward, right));

        Vector3 normal = -camera.Forward;

        int vertex = 0;
        Span<Vector3> corners = stackalloc Vector3[4];
        Span<Vector2> uvs = stackalloc Vector2[4];

        foreach (Particle particle in system.Particles)
        {
            if (!particle.IsAlive || vertex + VerticesPerParticle > MaxVertices)
            {
                continue;
            }

            float half = particle.Size / 2F;
            Vector3 across = right * half;
            Vector3 above = up * half;

            corners[0] = particle.Position + across - above;
            corners[1] = particle.Position - across - above;
            corners[2] = particle.Position - across + above;
            corners[3] = particle.Position + across + above;

            uvs[0] = new Vector2(particle.UVMax.X, particle.UVMax.Y);
            uvs[1] = new Vector2(particle.UVMin.X, particle.UVMax.Y);
            uvs[2] = new Vector2(particle.UVMin.X, particle.UVMin.Y);
            uvs[3] = new Vector2(particle.UVMax.X, particle.UVMin.Y);

            foreach (int corner in _cornerOrder)
            {
                _positions[(vertex * 3) + 0] = corners[corner].X;
                _positions[(vertex * 3) + 1] = corners[corner].Y;
                _positions[(vertex * 3) + 2] = corners[corner].Z;

                _normals[(vertex * 3) + 0] = normal.X;
                _normals[(vertex * 3) + 1] = normal.Y;
                _normals[(vertex * 3) + 2] = normal.Z;

                _uvs[(vertex * 2) + 0] = uvs[corner].X;
                _uvs[(vertex * 2) + 1] = uvs[corner].Y;

                _lights[vertex] = particle.PackedLight;
                vertex++;
            }
        }

        return vertex;
    }

    private static int CreateStreamBuffer(int attribute, int componentsPerVertex, int bytesPerComponent, bool isInteger = false)
    {
        int buffer = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, buffer);
        GL.BufferData(
            BufferTarget.ArrayBuffer,
            MaxVertices * componentsPerVertex * bytesPerComponent,
            IntPtr.Zero,
            BufferUsageHint.StreamDraw);

        if (isInteger)
        {
            GL.VertexAttribIPointer(attribute, componentsPerVertex, VertexAttribIntegerType.UnsignedInt, 0, IntPtr.Zero);
        }
        else
        {
            GL.VertexAttribPointer(attribute, componentsPerVertex, VertexAttribPointerType.Float, false, 0, 0);
        }

        GL.EnableVertexAttribArray(attribute);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        return buffer;
    }

    private static void Upload<T>(int buffer, T[] data, int elementCount, int bytesPerElement)
        where T : struct
    {
        GL.BindBuffer(BufferTarget.ArrayBuffer, buffer);

        GL.BufferData(BufferTarget.ArrayBuffer, data.Length * bytesPerElement, IntPtr.Zero, BufferUsageHint.StreamDraw);
        GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, elementCount * bytesPerElement, data);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
    }

    public void CleanUp()
    {
        GL.DeleteBuffer(_positionBuffer);
        GL.DeleteBuffer(_normalBuffer);
        GL.DeleteBuffer(_uvBuffer);
        GL.DeleteBuffer(_lightBuffer);
        GL.DeleteVertexArray(_vao);
    }
}

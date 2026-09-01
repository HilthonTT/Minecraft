using Minecraft.Core.Entities;
using Minecraft.Core.Entities.Mobs;
using Minecraft.Core.Games;
using Minecraft.Core.Shaders.EntityShader;
using Minecraft.Core.Shapes;
using Minecraft.Core.Worlds;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Minecraft.Core.Render;

public sealed class EntityRenderer
{
    private const float HurtFlashStrength = 0.35F;

    private readonly Game _game;
    private readonly EntityShader _shader = new();
    private readonly EntityMeshRegistry _meshRegistry = new();

    public EntityRenderer(Game game)
    {
        _game = game;
    }

    public void Render(World world, Camera camera, in FogState fog)
    {
        _shader.Start();
        _shader.LoadMatrix(_shader.LocationViewMatrix, camera.CurrentViewMatrix);

        _shader.LoadVector(_shader.LocationCameraPosition, camera.Position);
        _shader.LoadVector(_shader.LocationFogColor, fog.Color);
        _shader.LoadFloat(_shader.LocationFogStart, fog.Start);
        _shader.LoadFloat(_shader.LocationFogEnd, fog.End);

        int boundSkinTextureId = -1;

        float loadedHurtFlash = -1F;

        foreach (Entity entity in world.LoadedEntities.Values)
        {
            if (entity.ID == _game.ClientPlayer.ID && !IsClientPlayerBodyVisible(camera))
            {
                continue;
            }

            if (!_meshRegistry.Models.TryGetValue(entity.EntityType, out EntityMesh entityMesh))
            {
                continue;
            }

            if (boundSkinTextureId != entityMesh.SkinTextureId)
            {
                _shader.LoadTexture(_shader.LocationSkinTexture, 0, entityMesh.SkinTextureId);
                boundSkinTextureId = entityMesh.SkinTextureId;
            }

            float hurtFlash = entity is Mob { IsHurt: true } ? HurtFlashStrength : 0F;
            if (loadedHurtFlash != hurtFlash)
            {
                _shader.LoadFloat(_shader.LocationHurtFlash, hurtFlash);
                loadedHurtFlash = hurtFlash;
            }

            entityMesh.Mesh.BindVAO();
            _shader.LoadMatrix(_shader.LocationTransformationMatrix, GetEntityTransformation(entity));
            GL.DrawArrays(PrimitiveType.Triangles, 0, entityMesh.Mesh.IndicesCount);
        }
    }

    public void UploadProjectionMatrix(Matrix4 projectionMatrix)
    {
        _shader.Start();
        _shader.LoadMatrix(_shader.LocationProjectionMatrix, projectionMatrix);
        _shader.Stop();
    }

    public void CleanUp() => _shader.CleanUp();

    private bool IsClientPlayerBodyVisible(Camera camera)
    {
        return _game.ClientPlayer.IsBodyVisible || camera != _game.ClientPlayer.Camera;
    }

    private static Matrix4 GetEntityTransformation(Entity entity)
    {
        var pivot = new Vector3(entity.Width / 2.0F, 0, entity.Length / 2.0F);

        return Matrix4.CreateTranslation(-pivot) *
               Matrix4.CreateRotationY(entity.Yaw) *
               Matrix4.CreateTranslation(entity.Position + pivot);
    }
}

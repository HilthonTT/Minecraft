using Minecraft.Core.Entities;
using Minecraft.Core.Games;
using Minecraft.Core.Shaders.RealisticShader;
using Minecraft.Core.Shaders.Skydome;
using Minecraft.Core.Utilities;
using Minecraft.Core.Utilities.Models;
using OpenTK.Graphics.OpenGL;

namespace Minecraft.Core.Render;

public sealed class Skydome
{
    private SkydomeShader _skydomeShader = new();
    private VAOModel _skydomeModel;
    private Game _game;

    public Skydome(Game game)
    {
        _game = game;

        ModelData model = OBJLoader.Load(Assets.Path("Resources/sphere.obj"));
        _skydomeModel = new VAOModel(model.positions, model.indices);
    }

    public void Render(RealisticPipeline? realistic = null)
    {
        GL.DepthMask(false);
        GL.Disable(EnableCap.CullFace);

        SkydomeShader shader = realistic?.SkyShader ?? _skydomeShader;
        shader.Start();

        Camera activeCamera = _game.MasterRenderer.GetActiveCamera();
        shader.LoadMatrix(shader.LocationProjectionMatrix, activeCamera.CurrentViewMatrix.ClearTranslation() * activeCamera.CurrentProjectionMatrix);

        var environment = _game.World.Environment;
        shader.LoadInt(shader.LocationCurrentTime, (int)environment.CurrentTime);
        shader.LoadVector(shader.LocationSunPosition, environment.SunPosition);
        shader.LoadVector(shader.LocationTopSkyColor, environment.GetCurrentTopSkyColor());
        shader.LoadVector(shader.LocationBottomSkyColor, environment.GetCurrentBottomSkyColor());
        shader.LoadVector(shader.LocationHorizonColor, environment.GetCurrentHorizonColor());
        shader.LoadVector(shader.LocationSunColor, environment.GetCurrentSunColor());
        shader.LoadVector(shader.LocationSunGlowColor, environment.GetCurrentSunGlowColor());
        shader.LoadVector(shader.LocationMoonColor, environment.GetCurrentMoonColor());
        shader.LoadVector(shader.LocationMoonGlowColor, environment.GetCurrentMoonGlowColor());

        if (realistic is not null)
        {
            RealisticSkyShader realisticShader = realistic.SkyShader;
            realisticShader.LoadVector(realisticShader.LocationSunPosition, realistic.Lighting.SunDirection);
            realisticShader.LoadFloat(realisticShader.LocationAnimationTime, realistic.AnimationTime);
            realisticShader.LoadFloat(realisticShader.LocationDaylight, realistic.Lighting.Daylight);
        }

        shader.LoadTexture(shader.LocationDitherTexture, 0, _game.MasterRenderer.DitherTextureId);

        _skydomeModel.BindVAO();
        GL.DrawElements(PrimitiveType.Triangles, _skydomeModel.IndicesCount, DrawElementsType.UnsignedInt, 0);
        VAOModel.UnbindVAO();
        shader.Stop();

        GL.Enable(EnableCap.CullFace);
        GL.CullFace(TriangleFace.Back);
        GL.DepthMask(true);
    }

    public void CleanUp()
    {
        _skydomeModel.CleanUp();
        _skydomeShader.CleanUp();
    }
}

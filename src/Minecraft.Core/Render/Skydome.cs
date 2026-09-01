using Minecraft.Core.Games;
using Minecraft.Core.Shaders.Skydome;
using Minecraft.Core.Utilities;
using Minecraft.Core.Utilities.Models;
using OpenTK.Graphics.OpenGL;
using Minecraft.Core.Entities;

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

    public void Render()
    {
        GL.DepthMask(false);
        GL.Disable(EnableCap.CullFace);
        _skydomeShader.Start();

        Camera activeCamera = _game.MasterRenderer.GetActiveCamera();
        _skydomeShader.LoadMatrix(_skydomeShader.LocationProjectionMatrix, activeCamera.CurrentViewMatrix.ClearTranslation() * activeCamera.CurrentProjectionMatrix);

        var environment = _game.World.Environment;
        _skydomeShader.LoadInt(_skydomeShader.LocationCurrentTime, (int)environment.CurrentTime);
        _skydomeShader.LoadVector(_skydomeShader.LocationSunPosition, environment.SunPosition);
        _skydomeShader.LoadVector(_skydomeShader.LocationTopSkyColor, environment.GetCurrentTopSkyColor());
        _skydomeShader.LoadVector(_skydomeShader.LocationBottomSkyColor, environment.GetCurrentBottomSkyColor());
        _skydomeShader.LoadVector(_skydomeShader.LocationHorizonColor, environment.GetCurrentHorizonColor());
        _skydomeShader.LoadVector(_skydomeShader.LocationSunColor, environment.GetCurrentSunColor());
        _skydomeShader.LoadVector(_skydomeShader.LocationSunGlowColor, environment.GetCurrentSunGlowColor());
        _skydomeShader.LoadVector(_skydomeShader.LocationMoonColor, environment.GetCurrentMoonColor());
        _skydomeShader.LoadVector(_skydomeShader.LocationMoonGlowColor, environment.GetCurrentMoonGlowColor());

        _skydomeShader.LoadTexture(_skydomeShader.LocationDitherTexture, 0, _game.MasterRenderer.DitherTextureId);

        _skydomeModel.BindVAO();
        GL.DrawElements(PrimitiveType.Triangles, _skydomeModel.IndicesCount, DrawElementsType.UnsignedInt, 0);
        VAOModel.UnbindVAO();
        _skydomeShader.Stop();

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

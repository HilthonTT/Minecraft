using Minecraft.Core.Entities;
using Minecraft.Core.Logging;
using Minecraft.Core.Shaders.UIShader;
using OpenTK.Mathematics;

namespace Minecraft.Core.Render.UI;

public sealed class UIRenderer
{
    private readonly Dictionary<RenderSpace, List<UICanvas>> _canvasses = [];
    private readonly CameraController _cameraController;
    private readonly UIShader _uiShader = new();

    public UIRenderer(CameraController cameraController)
    {
        _cameraController = cameraController;

        foreach (RenderSpace renderSpace in Enum.GetValues<RenderSpace>())
        {
            _canvasses.Add(renderSpace, []);
        }

        cameraController.Camera.OnProjectionChangedHandler += OnCameraProjectionChanged;
    }

    private void OnCameraProjectionChanged(ProjectionMatrixInfo projectionInfo)
    {
        foreach (UICanvas canvas in _canvasses[RenderSpace.Screen])
        {
            canvas.SetDimensions(projectionInfo.WindowPixelWidth, projectionInfo.WindowPixelHeight);
        }
    }

    public void AddCanvas(UICanvas canvas)
    {
        if (!_canvasses.TryGetValue(canvas.RenderSpace, out List<UICanvas>? spaceCanvasses))
        {
            Logger.Error("Failed to add canvas of unknown render space " + canvas.RenderSpace);
            return;
        }

        spaceCanvasses.Add(canvas);
    }

    public void RemoveCanvas(UICanvas canvas)
    {
        if (!_canvasses.TryGetValue(canvas.RenderSpace, out List<UICanvas>? spaceCanvasses))
        {
            Logger.Error("Failed to remove canvas of unknown render space " + canvas.RenderSpace);
            return;
        }

        spaceCanvasses.Remove(canvas);
    }

    public void RemoveCanvassesIn(RenderSpace renderSpace)
    {
        if (_canvasses.TryGetValue(renderSpace, out List<UICanvas>? spaceCanvasses))
        {
            spaceCanvasses.Clear();
        }
    }

    public void Render()
    {
        foreach (KeyValuePair<RenderSpace, List<UICanvas>> spaceCanvasses in _canvasses)
        {
            for (int i = spaceCanvasses.Value.Count - 1; i >= 0; i--)
            {
                UICanvas canvas = spaceCanvasses.Value[i];

                if (canvas.IsEnabled)
                {
                    canvas.Update();
                }

                canvas.Clean();
            }
        }

        Draw(overlays: false);
    }

    public void RenderOverlays() => Draw(overlays: true);

    private void Draw(bool overlays)
    {
        _uiShader.Start();

        foreach (KeyValuePair<RenderSpace, List<UICanvas>> spaceCanvasses in _canvasses)
        {
            if (spaceCanvasses.Key == RenderSpace.Screen)
            {
                _uiShader.LoadMatrix(_uiShader.LocationViewMatrix, Matrix4.Identity);
                _uiShader.LoadMatrix(_uiShader.LocationProjectionMatrix, Matrix4.Identity);
            }
            else
            {
                _uiShader.LoadMatrix(_uiShader.LocationViewMatrix, _cameraController.Camera.CurrentViewMatrix);
                _uiShader.LoadMatrix(_uiShader.LocationProjectionMatrix, _cameraController.Camera.CurrentProjectionMatrix);
            }

            foreach (UICanvas canvas in spaceCanvasses.Value)
            {
                if (canvas.IsEnabled && canvas.IsOverlay == overlays)
                {
                    canvas.Render(_uiShader);
                }
            }
        }

        _uiShader.Stop();
    }

    public void CleanUp()
    {
        _uiShader.CleanUp();
    }
}

using Minecraft.Core.Entities;
using Minecraft.Core.Logging;
using Minecraft.Core.Shaders.UIShader;
using OpenTK.Mathematics;

namespace Minecraft.Core.Render.UI;

/// <summary>
/// Draws every registered canvas. Screen space canvases are drawn with identity matrices so their vertices
/// are already in normalised device coordinates, while world space canvases go through the active camera.
/// </summary>
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
        // Screen space canvases are sized in pixels, so they have to follow the window.
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

    /// <summary>
    /// Drops every canvas drawn in the given space. Used when a world is left, since the canvases that live
    /// in it belong to entities that are gone with it.
    /// </summary>
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
            // Iterated by index because a canvas can remove itself from this list while updating.
            for (int i = spaceCanvasses.Value.Count - 1; i >= 0; i--)
            {
                UICanvas canvas = spaceCanvasses.Value[i];

                // A switched off canvas is still cleaned, so that a resize it sat through does not leave it
                // with meshes built for the wrong canvas size once it comes back.
                if (canvas.IsEnabled)
                {
                    canvas.Update();
                }

                canvas.Clean();
            }
        }

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
                if (canvas.IsEnabled)
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

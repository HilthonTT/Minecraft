using Minecraft.Core.Shaders.UIShader;
using Minecraft.Core.Utilities;
using OpenTK.Mathematics;

namespace Minecraft.Core.Render.UI;

public class UICanvas
{
    // A list rather than a set, because components are drawn in the order they were added and one drawn
    // later has to end up on top of the ones before it.
    private readonly List<UIComponent> _components = [];
    private readonly HashSet<UIComponent> _toCleanComponents = new();

    public RenderSpace RenderSpace { get; protected set; }

    /// <summary>
    /// Whether the canvas takes part in updating and drawing. A switched off canvas keeps its meshes, and is
    /// still cleaned, so turning it back on costs nothing and shows it exactly as it was left.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    public int PixelWidth { get; private set; }

    public int PixelHeight { get; private set; }

    public Vector3 Position { get; protected set; }

    public Vector3 Rotation { get; protected set; }

    public UICanvas(Vector3 position, Vector3 rotation, int pixelWidth, int pixelHeight, RenderSpace renderSpace)
    {
        Position = position;
        Rotation = rotation;
        PixelWidth = pixelWidth;
        PixelHeight = pixelHeight;
        RenderSpace = renderSpace;
    }

    public void SetDimensions(int pixelWidth, int pixelHeight)
    {
        if (PixelWidth == pixelWidth && PixelHeight == pixelHeight)
        {
            return;
        }

        PixelWidth = pixelWidth;
        PixelHeight = pixelHeight;

        OnDimensionsChanged();

        // Meshes are built in normalised device coordinates out of canvas pixels, so every one of them is
        // now stale, whether or not the component itself moved.
        foreach (UIComponent component in _components)
        {
            AddComponentToClean(component);
        }
    }

    public void Render(UIShader uiShader)
    {
        Matrix4 transformationMatrix = Matrix4.Identity;
        if (RenderSpace == RenderSpace.World)
        {
            transformationMatrix = MathUtils.CreateRotationAndTranslationMatrix(Position, Rotation);
        }
        uiShader.LoadMatrix(uiShader.LocationTransformationMatrix, transformationMatrix);

        foreach (UIComponent component in _components)
        {
            if (component.IsVisible)
            {
                component.Render(uiShader);
            }
        }
    }

    public void Clean()
    {
        foreach (UIComponent toCleanComp in _toCleanComponents)
        {
            toCleanComp.Clean();
        }
        _toCleanComponents.Clear();
    }

    public void AddComponentToClean(UIComponent component)
    {
        if (!_toCleanComponents.Contains(component))
        {
            _toCleanComponents.Add(component);
        }
    }

    public bool AddComponentToRender(UIComponent component)
    {
        if (_components.Contains(component))
        {
            return false;
        }
        _components.Add(component);
        return true;
    }

    public bool RemoveComponentFromRender(UIComponent component)
    {
        return _components.Remove(component);
    }

    public virtual void Update() { }

    /// <summary>Called after the canvas was resized, for canvases that lay their components out in pixels.</summary>
    protected virtual void OnDimensionsChanged() { }
}

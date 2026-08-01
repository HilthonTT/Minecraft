using Minecraft.Core.Shaders.UIShader;
using Minecraft.Core.Utilities;
using OpenTK.Mathematics;

namespace Minecraft.Core.Render.UI;

public class UICanvas
{
    public RenderSpace RenderSpace { get; protected set; }
    public int PixelWidth { get; private set; }
    public int PixelHeight { get; private set; }
    public Vector3 Position { get; protected set; }
    public Vector3 Rotation { get; protected set; }

    private readonly HashSet<UIComponent> _components = new();
    private readonly HashSet<UIComponent> _toCleanComponents = new();

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
        PixelWidth = pixelWidth;
        PixelHeight = pixelHeight;
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
            component.Render(uiShader);
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
}

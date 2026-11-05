using OpenTK.Mathematics;

namespace Minecraft.Core.Render.UI;

public static class UITheme
{
    public const float TitleScale = 0.78F;
    public const float HeadingScale = 0.40F;
    public const float LabelScale = 0.33F;
    public const float BodyScale = 0.30F;
    public const float SmallScale = 0.26F;
    public const float TinyScale = 0.22F;

    public const float ButtonHeight = 40F;
    public const float FieldHeight = 38F;
    public const float RowHeight = 40F;

    public const float Gap = 8F;
    public const float RowGap = 10F;
    public const float SectionGap = 20F;
    public const float Padding = 18F;
    public const float Margin = 16F;

    public const float BorderWidth = 2F;
    public const float FocusWidth = 2F;
    public const float ShadowOffset = 4F;

    public const float HoverSharpness = 16F;
    public const float SlideSharpness = 22F;

    public const float DimTransparency = 0.62F;
    public const float PanelTransparency = 0.93F;
    public const float ShadowTransparency = 0.35F;

    public static readonly Vector3 Text = new(0.96F, 0.96F, 0.97F);
    public static readonly Vector3 TextMuted = new(0.72F, 0.74F, 0.79F);
    public static readonly Vector3 TextFaint = new(0.50F, 0.52F, 0.57F);
    public static readonly Vector3 TextShadow = new(0.02F, 0.02F, 0.03F);

    public static readonly Vector3 Accent = new(0.47F, 0.80F, 0.34F);
    public static readonly Vector3 AccentDim = new(0.25F, 0.47F, 0.19F);
    public static readonly Vector3 Focus = new(0.98F, 0.92F, 0.52F);

    public static readonly Vector3 Error = new(1.0F, 0.47F, 0.42F);
    public static readonly Vector3 Warning = new(1.0F, 0.76F, 0.36F);
    public static readonly Vector3 Success = new(0.56F, 0.88F, 0.46F);

    public static readonly Vector3 Backdrop = new(0.05F, 0.06F, 0.08F);
    public static readonly Vector3 Dim = Vector3.Zero;
    public static readonly Vector3 Shadow = Vector3.Zero;

    public static readonly Vector3 Panel = new(0.08F, 0.09F, 0.11F);
    public static readonly Vector3 PanelRaised = new(0.12F, 0.13F, 0.16F);
    public static readonly Vector3 PanelBorder = new(0.24F, 0.26F, 0.31F);

    public static readonly Vector3 Outline = new(0.02F, 0.02F, 0.03F);

    public static readonly Vector3 Surface = new(0.27F, 0.29F, 0.34F);
    public static readonly Vector3 SurfaceHover = new(0.38F, 0.42F, 0.49F);
    public static readonly Vector3 SurfacePressed = new(0.20F, 0.22F, 0.26F);
    public static readonly Vector3 SurfaceDisabled = new(0.16F, 0.17F, 0.19F);

    public static readonly Vector3 Primary = new(0.27F, 0.53F, 0.22F);
    public static readonly Vector3 PrimaryHover = new(0.35F, 0.65F, 0.28F);
    public static readonly Vector3 PrimaryPressed = new(0.21F, 0.42F, 0.17F);

    public static readonly Vector3 Danger = new(0.52F, 0.17F, 0.16F);
    public static readonly Vector3 DangerHover = new(0.70F, 0.23F, 0.21F);
    public static readonly Vector3 DangerPressed = new(0.40F, 0.13F, 0.12F);

    public static readonly Vector3 Field = new(0.04F, 0.05F, 0.06F);
    public static readonly Vector3 FieldBorder = new(0.30F, 0.33F, 0.38F);
    public static readonly Vector3 TextSelection = new(0.22F, 0.40F, 0.68F);

    public static readonly Vector3 Slot = new(0.14F, 0.15F, 0.18F);
    public static readonly Vector3 SlotHover = new(0.33F, 0.37F, 0.44F);
    public static readonly Vector3 SlotBorder = new(0.05F, 0.05F, 0.06F);

    public static Vector3 FillFor(UIButtonStyle style, float hover, bool pressed)
    {
        (Vector3 idle, Vector3 hovered, Vector3 down) = style switch
        {
            UIButtonStyle.Primary => (Primary, PrimaryHover, PrimaryPressed),
            UIButtonStyle.Danger => (Danger, DangerHover, DangerPressed),
            _ => (Surface, SurfaceHover, SurfacePressed),
        };

        return pressed ? down : Vector3.Lerp(idle, hovered, Math.Clamp(hover, 0F, 1F));
    }
}

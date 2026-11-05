using OpenTK.Mathematics;

namespace Minecraft.Core.Render.UI;

public sealed class UIToast
{
    private const float SlideSeconds = 0.25F;
    private const float HoldSeconds = 2.6F;
    private const float FadeSeconds = 0.5F;

    private const float HorizontalPadding = 18F;
    private const float VerticalPadding = 12F;
    private const float LineGap = 8F;
    private const float AccentHeight = 3F;
    private const float MinimumWidth = 200F;

    private readonly UICanvas _canvas;
    private readonly UIPanel _panel;
    private readonly UIImage _accent;
    private readonly UILabel _title;
    private readonly UILabel _detail;

    private double _shownAt = double.NegativeInfinity;
    private float _lastOffset = float.NaN;

    public UIToast(UICanvas canvas)
    {
        _canvas = canvas;
        _panel = new UIPanel(canvas, fill: UITheme.PanelRaised);
        _accent = UIDraw.Rect(canvas, UITheme.Accent);
        _title = new UILabel(canvas, UITheme.LabelScale, UITheme.Text);
        _detail = new UILabel(canvas, UITheme.SmallScale, UITheme.TextMuted, string.Empty, shadow: false);

        SetVisible(false);
    }

    public bool IsShowing => Elapsed < SlideSeconds + HoldSeconds + FadeSeconds;

    private float Elapsed => (float)(UIMotion.NowSeconds - _shownAt);

    public void Show(string title, string detail, Vector3 accent)
    {
        _title.Text = title;
        _detail.Text = detail;
        _accent.Color = accent;
        _shownAt = UIMotion.NowSeconds;
        _lastOffset = float.NaN;
    }

    public void Hide() => _shownAt = double.NegativeInfinity;

    public void Update()
    {
        float elapsed = Elapsed;
        float alpha = UIMotion.Envelope(elapsed, SlideSeconds, HoldSeconds, FadeSeconds);

        if (alpha <= 0F)
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);
        SetTransparency(alpha);

        float slide = UIMotion.EaseOutCubic(elapsed / SlideSeconds);
        float offset = MathF.Round((1F - slide) * -24F);
        if (offset != _lastOffset)
        {
            _lastOffset = offset;
            Layout(offset);
        }
    }

    public void Relayout() => _lastOffset = float.NaN;

    private void Layout(float offset)
    {
        bool hasDetail = _detail.Text.Length > 0;
        float contentWidth = Math.Max(_title.Width, hasDetail ? _detail.Width : 0F);
        float width = Math.Max(MinimumWidth, contentWidth + (2 * HorizontalPadding));
        float height = (2 * VerticalPadding) + _title.CapHeight + (hasDetail ? LineGap + _detail.CapHeight : 0F) + AccentHeight;

        float left = (_canvas.PixelWidth - width) / 2F;
        float top = UITheme.Margin + offset;

        _panel.SetBounds(left, top, width, height);
        UIDraw.Place(_accent, left + UITheme.BorderWidth, top + UITheme.BorderWidth, width - (2 * UITheme.BorderWidth), AccentHeight);

        float textTop = top + AccentHeight + VerticalPadding;
        _title.PlaceTop(left + ((width - _title.Width) / 2F), textTop);
        _detail.PlaceTop(left + ((width - _detail.Width) / 2F), textTop + _title.CapHeight + LineGap);
    }

    private void SetVisible(bool isVisible)
    {
        _panel.IsVisible = isVisible;
        _accent.IsVisible = isVisible;
        _title.IsVisible = isVisible;
        _detail.IsVisible = isVisible && _detail.Text.Length > 0;
    }

    private void SetTransparency(float alpha)
    {
        _panel.Transparency = alpha;
        _accent.Transparency = alpha;
        _title.Transparency = alpha;
        _detail.Transparency = alpha;
    }
}

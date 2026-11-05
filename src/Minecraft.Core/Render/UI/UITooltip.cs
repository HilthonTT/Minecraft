using OpenTK.Mathematics;

namespace Minecraft.Core.Render.UI;

public sealed class UITooltip
{
    private const float Padding = 10F;
    private const float LineGap = 8F;
    private const float CursorOffset = 16F;

    private readonly UICanvas _canvas;
    private readonly UIPanel _panel;
    private readonly UILabel _title;
    private readonly UILabel _detail;

    private string _shownTitle = string.Empty;
    private string _shownDetail = string.Empty;
    private Vector2 _shownAt = new(float.NaN);

    public UITooltip(UICanvas canvas)
    {
        _canvas = canvas;
        _panel = new UIPanel(canvas, fill: new Vector3(0.06F, 0.05F, 0.10F), border: new Vector3(0.28F, 0.18F, 0.50F), fillTransparency: 0.95F);
        _title = new UILabel(canvas, UITheme.LabelScale, UITheme.Text);
        _detail = new UILabel(canvas, UITheme.SmallScale, UITheme.TextMuted, string.Empty, shadow: false);

        Hide();
    }

    public static Vector2 Place(Vector2 cursor, Vector2 size, Vector2 bounds, float offset, float margin)
    {
        float left = cursor.X + offset;
        if (left + size.X > bounds.X - margin)
        {
            left = cursor.X - offset - size.X;
        }

        float top = cursor.Y + offset;
        if (top + size.Y > bounds.Y - margin)
        {
            top = cursor.Y - offset - size.Y;
        }

        return new Vector2(
            Math.Clamp(left, margin, Math.Max(margin, bounds.X - margin - size.X)),
            Math.Clamp(top, margin, Math.Max(margin, bounds.Y - margin - size.Y)));
    }

    public void Show(Vector2 cursor, string title, string detail, Vector3 titleColor)
    {
        _title.Color = titleColor;

        if (title == _shownTitle && detail == _shownDetail && cursor == _shownAt && _panel.IsVisible)
        {
            return;
        }

        _shownTitle = title;
        _shownDetail = detail;
        _shownAt = cursor;

        _title.Text = title;
        _detail.Text = detail;

        bool hasDetail = detail.Length > 0;
        float detailWidth = hasDetail ? detail.Split('\n').Max(_detail.Measure) : 0F;
        float width = Math.Max(_title.Width, detailWidth) + (2 * Padding);
        float height = (2 * Padding) + _title.CapHeight + (hasDetail ? LineGap + _detail.BlockHeight + 3F : 0F);

        Vector2 position = Place(
            cursor,
            new Vector2(width, height),
            new Vector2(_canvas.PixelWidth, _canvas.PixelHeight),
            CursorOffset,
            4F);

        _panel.SetBounds(position, new Vector2(width, height));
        _title.PlaceTop(position.X + Padding, position.Y + Padding);
        _detail.PlaceTop(position.X + Padding, position.Y + Padding + _title.CapHeight + LineGap);

        _panel.IsVisible = true;
        _title.IsVisible = true;
        _detail.IsVisible = hasDetail;
    }

    public void Hide()
    {
        _panel.IsVisible = false;
        _title.IsVisible = false;
        _detail.IsVisible = false;
    }
}

using OpenTK.Mathematics;

namespace Minecraft.Core.Render.UI;

public sealed class UITextField : UIWidget
{
    public const float Height = UITheme.FieldHeight;

    private const float TextScale = 0.34F;
    private const float HorizontalPadding = 10F;
    private const float CaretWidth = 2F;
    private const float CaretBlinkSeconds = 1.0F;

    private readonly TextEditBuffer _buffer;
    private readonly UITextEditKeys _keys = new();
    private readonly Font _font;

    private readonly UIImage _border;
    private readonly UIImage _panel;
    private readonly UIImage _selection;
    private readonly UIText _text;
    private readonly UIText _placeholder;
    private readonly UIImage _caret;

    private string _placeholderText = string.Empty;
    private int _firstVisible;
    private bool _isDragging;
    private int _renderedVersion = -1;
    private bool _renderedFocus;
    private double _lastEditAt;

    public UITextField(UICanvas canvas, int maxLength)
        : base(canvas)
    {
        _buffer = new TextEditBuffer(maxLength);
        _font = FontRegistry.GetFont(FontType.Arial);

        _border = UIDraw.Rect(canvas, UITheme.FieldBorder);
        _panel = UIDraw.Rect(canvas, UITheme.Field);
        _selection = UIDraw.Rect(canvas, UITheme.TextSelection);
        _selection.IsVisible = false;

        _placeholder = new UIText(canvas, _font, Vector2.Zero, new Vector2(TextScale, TextScale), string.Empty)
        {
            Color = UITheme.TextFaint,
        };
        canvas.AddComponentToRender(_placeholder);

        _text = new UIText(canvas, _font, Vector2.Zero, new Vector2(TextScale, TextScale), string.Empty)
        {
            Color = UITheme.Text,
        };
        canvas.AddComponentToRender(_text);

        _caret = UIDraw.Rect(canvas, UITheme.Text);
        _caret.IsVisible = false;
    }

    public string Value
    {
        get => _buffer.Text;
        set
        {
            _buffer.SetText(value);
            _firstVisible = 0;
            _lastEditAt = UIMotion.NowSeconds;
            Render();
        }
    }

    public TextEditBuffer Buffer => _buffer;

    public string Placeholder
    {
        get => _placeholderText;
        set
        {
            _placeholderText = value;
            FitPlaceholder();
        }
    }

    public bool HasFocus
    {
        get => IsFocused;
        set => IsFocused = value;
    }

    public override bool WantsHorizontalKeys => true;

    public override bool WantsText => true;

    public override bool TryKeyboardActivate() => false;

    public override bool Update(UIInput input)
    {
        string before = _buffer.Text;

        UpdateMouse(input);

        if (IsFocused && IsEnabled && _keys.Apply(_buffer))
        {
            _lastEditAt = UIMotion.NowSeconds;
        }

        Render();
        return _buffer.Text != before;
    }

    protected override void OnLayout()
    {
        float border = UITheme.BorderWidth;
        UIDraw.Place(_border, Position, Size);
        UIDraw.Place(_panel, Position + new Vector2(border, border), Size - new Vector2(2 * border, 2 * border));
        _caret.Dimension = new Vector2(CaretWidth, GlyphBoxHeight);

        _placeholder.PixelPositionInCanvas = new Vector2(TextLeft, TextTop);
        FitPlaceholder();
        _renderedVersion = -1;
        Render();
    }

    protected override void OnVisibilityChanged(bool isVisible)
    {
        _border.IsVisible = isVisible;
        _panel.IsVisible = isVisible;
        _text.IsVisible = isVisible;
        _placeholder.IsVisible = isVisible && _buffer.Text.Length == 0;
        _selection.IsVisible = false;
        _caret.IsVisible = false;
        _renderedVersion = -1;
    }

    private float TextLeft => Position.X + HorizontalPadding;

    private float TextWidth => Math.Max(1F, Size.X - (2 * HorizontalPadding));

    private float Measure(string text) => _font.MeasureWidth(text, TextScale);

    private void FitPlaceholder()
    {
        _placeholder.Text = Size.X > 0F
            ? UITextLayout.Ellipsize(_placeholderText, TextWidth, Measure)
            : _placeholderText;
    }

    private void UpdateMouse(UIInput input)
    {
        if (!IsEnabled)
        {
            _isDragging = false;
            return;
        }

        if (input.Pressed && Contains(input.Mouse))
        {
            int index = IndexAt(input.Mouse.X);

            if (input.DoubleClicked)
            {
                _buffer.SelectWordAt(index);
            }
            else
            {
                _buffer.MoveTo(index, UIInput.ShiftDown);
            }

            _isDragging = !input.DoubleClicked;
            _lastEditAt = UIMotion.NowSeconds;
            return;
        }

        if (!input.Down)
        {
            _isDragging = false;
            return;
        }

        if (_isDragging)
        {
            _buffer.MoveTo(IndexAt(input.Mouse.X), extend: true);
        }
    }

    private int IndexAt(float pixelX)
    {
        string visible = _buffer.Text[_firstVisible..];
        return _firstVisible + UITextLayout.IndexAtOffset(visible, pixelX - TextLeft, Measure);
    }

    private void Render()
    {
        if (!IsVisible)
        {
            return;
        }

        _border.Color = IsFocused ? UITheme.Focus : UITheme.FieldBorder;
        _placeholder.IsVisible = _buffer.Text.Length == 0 && !IsFocused;

        if (_renderedVersion != _buffer.Version || _renderedFocus != IsFocused)
        {
            _renderedVersion = _buffer.Version;
            _renderedFocus = IsFocused;
            RenderText();
        }

        if (!IsFocused || _buffer.HasSelection)
        {
            _caret.IsVisible = false;
            return;
        }

        double sinceEdit = UIMotion.NowSeconds - _lastEditAt;
        _caret.IsVisible = sinceEdit % CaretBlinkSeconds < CaretBlinkSeconds / 2;
    }

    private void RenderText()
    {
        string value = _buffer.Text;
        float width = TextWidth;

        _firstVisible = UITextLayout.FirstVisibleIndex(value, _buffer.Caret, _firstVisible, width - CaretWidth, Measure);

        string visibleText = value[_firstVisible..];
        while (visibleText.Length > 0 && Measure(visibleText) > width)
        {
            visibleText = visibleText[..^1];
        }

        _text.Text = visibleText;
        _text.PixelPositionInCanvas = new Vector2(TextLeft, TextTop);

        float caretOffset = Measure(value[_firstVisible..Math.Max(_firstVisible, _buffer.Caret)]);
        _caret.PixelPositionInCanvas = new Vector2(TextLeft + caretOffset, GlyphBoxTop);

        if (!IsFocused || !_buffer.HasSelection)
        {
            _selection.IsVisible = false;
            return;
        }

        int start = Math.Max(_firstVisible, _buffer.SelectionStart);
        int end = Math.Max(start, _buffer.SelectionEnd);

        float left = Measure(value[_firstVisible..start]);
        float right = Math.Min(width, Measure(value[_firstVisible..end]));

        _selection.IsVisible = right > left;
        UIDraw.Place(
            _selection,
            new Vector2(TextLeft + left, GlyphBoxTop - 2F),
            new Vector2(right - left, GlyphBoxHeight + 4F));
    }

    private const string HeightReference = "Ag0";

    private float GlyphBoxTop => Position.Y + ((Size.Y - GlyphBoxHeight) / 2.0F);

    private float GlyphBoxHeight
    {
        get
        {
            (float top, float bottom) = _font.MeasureVerticalBounds(HeightReference, TextScale);
            return bottom - top;
        }
    }

    private float TextTop => GlyphBoxTop - _font.MeasureVerticalBounds(HeightReference, TextScale).Top;
}

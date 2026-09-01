using Minecraft.Core.Games;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Text;

namespace Minecraft.Core.Render.UI;

public sealed class UITextField
{
    public const int Height = 40;

    private const float TextScale = 0.36F;
    private const int HorizontalPaddingPixels = 8;
    private const int CaretWidthPixels = 2;
    private const float CaretBlinkSeconds = 1;

    private static readonly Vector3 _panelColor = new(0.10F, 0.11F, 0.13F);
    private static readonly Vector3 _borderColor = new(0.30F, 0.33F, 0.38F);
    private static readonly Vector3 _focusedBorderColor = new(0.55F, 0.62F, 0.72F);
    private static readonly Vector3 _textColor = new(0.95F, 0.95F, 0.95F);

    private readonly int _maxLength;
    private readonly Font _font;
    private readonly UIImage _border;
    private readonly UIImage _panel;
    private readonly UIText _text;
    private readonly UIImage _caret;

    private readonly HeldKey _backspaceKey = new(Keys.Backspace);
    private readonly HeldKey _deleteKey = new(Keys.Delete);
    private readonly HeldKey _caretLeftKey = new(Keys.Left);
    private readonly HeldKey _caretRightKey = new(Keys.Right);

    private Vector2 _position;
    private Vector2 _size;
    private string _value = string.Empty;
    private int _caretIndex;
    private DateTime _lastEditAt = DateTime.Now;

    public bool HasFocus { get; set; }

    public string Value
    {
        get => _value;
        set
        {
            _value = value.Length > _maxLength ? value[.._maxLength] : value;
            _caretIndex = _value.Length;
            _lastEditAt = DateTime.Now;
        }
    }

    public UITextField(UICanvas canvas, int maxLength)
    {
        _maxLength = maxLength;
        _font = FontRegistry.GetFont(FontType.Arial);

        _border = new UIImage(canvas, Vector2.Zero, Vector2.Zero, UITextures.White) { Color = _borderColor };
        canvas.AddComponentToRender(_border);

        _panel = new UIImage(canvas, Vector2.Zero, Vector2.Zero, UITextures.White) { Color = _panelColor };
        canvas.AddComponentToRender(_panel);

        _text = new UIText(canvas, _font, Vector2.Zero, new Vector2(TextScale, TextScale), string.Empty)
        {
            Color = _textColor,
        };
        canvas.AddComponentToRender(_text);

        _caret = new UIImage(canvas, Vector2.Zero, Vector2.Zero, UITextures.White)
        {
            Color = _textColor,
            IsVisible = false,
        };
        canvas.AddComponentToRender(_caret);
    }

    public void SetBounds(Vector2 position, Vector2 size)
    {
        const int borderThickness = 2;

        _position = position;
        _size = size;

        _border.PixelPositionInCanvas = position - new Vector2(borderThickness, borderThickness);
        _border.Dimension = size + new Vector2(2 * borderThickness, 2 * borderThickness);

        _panel.PixelPositionInCanvas = position;
        _panel.Dimension = size;

        _text.PixelPositionInCanvas = new Vector2(position.X + HorizontalPaddingPixels, TextTopPixels);
        _caret.Dimension = new Vector2(CaretWidthPixels, GlyphBoxHeight);
    }

    public bool Contains(Vector2 point)
    {
        return point.X >= _position.X && point.X <= _position.X + _size.X &&
               point.Y >= _position.Y && point.Y <= _position.Y + _size.Y;
    }

    public void Update()
    {
        _border.Color = HasFocus ? _focusedBorderColor : _borderColor;

        if (HasFocus)
        {
            UpdateCaret();
            UpdateTyping();
        }

        UpdateVisibleText();
    }

    private void UpdateCaret()
    {
        if (_caretLeftKey.HasFired() && _caretIndex > 0)
        {
            _caretIndex--;
            _lastEditAt = DateTime.Now;
        }

        if (_caretRightKey.HasFired() && _caretIndex < _value.Length)
        {
            _caretIndex++;
            _lastEditAt = DateTime.Now;
        }

        if (Game.Input.OnKeyPress(Keys.Home))
        {
            _caretIndex = 0;
            _lastEditAt = DateTime.Now;
        }

        if (Game.Input.OnKeyPress(Keys.End))
        {
            _caretIndex = _value.Length;
            _lastEditAt = DateTime.Now;
        }
    }

    private void UpdateTyping()
    {
        if (_backspaceKey.HasFired() && _caretIndex > 0)
        {
            _value = _value.Remove(_caretIndex - 1, 1);
            _caretIndex--;
            _lastEditAt = DateTime.Now;
        }

        if (_deleteKey.HasFired() && _caretIndex < _value.Length)
        {
            _value = _value.Remove(_caretIndex, 1);
            _lastEditAt = DateTime.Now;
        }

        if ((Game.Input.OnKeyDown(Keys.LeftControl) || Game.Input.OnKeyDown(Keys.RightControl)) &&
            Game.Input.OnKeyPress(Keys.V))
        {
            Insert(Game.Input.ClipboardText);
            return;
        }

        IReadOnlyList<char> typed = Game.Input.TypedCharacters;
        if (typed.Count == 0)
        {
            return;
        }

        var builder = new StringBuilder(typed.Count);
        foreach (char character in typed)
        {
            builder.Append(character);
        }

        Insert(builder.ToString());
    }

    private void Insert(string text)
    {
        var cleaned = new StringBuilder(text.Length);
        foreach (char character in text)
        {
            if (!char.IsControl(character))
            {
                cleaned.Append(character);
            }
        }

        int room = _maxLength - _value.Length;
        if (cleaned.Length == 0 || room <= 0)
        {
            return;
        }

        string insertion = cleaned.Length > room ? cleaned.ToString(0, room) : cleaned.ToString();

        _value = _value.Insert(_caretIndex, insertion);
        _caretIndex += insertion.Length;
        _lastEditAt = DateTime.Now;
    }

    private void UpdateVisibleText()
    {
        float textWidth = _size.X - (2 * HorizontalPaddingPixels);

        int firstVisible = 0;
        while (firstVisible < _caretIndex &&
               _font.MeasureWidth(_value[firstVisible.._caretIndex], TextScale) > textWidth - CaretWidthPixels)
        {
            firstVisible++;
        }

        string visibleText = _value[firstVisible..];
        while (visibleText.Length > 0 && _font.MeasureWidth(visibleText, TextScale) > textWidth)
        {
            visibleText = visibleText[..^1];
        }

        _text.Text = visibleText;

        if (!HasFocus)
        {
            _caret.IsVisible = false;
            return;
        }

        float caretOffset = _font.MeasureWidth(_value[firstVisible.._caretIndex], TextScale);
        _caret.PixelPositionInCanvas = new Vector2(_position.X + HorizontalPaddingPixels + caretOffset, GlyphBoxTop);

        double secondsSinceEdit = (DateTime.Now - _lastEditAt).TotalSeconds;
        _caret.IsVisible = secondsSinceEdit % CaretBlinkSeconds < CaretBlinkSeconds / 2;
    }

    private const string HeightReference = "Ag0";

    private float GlyphBoxTop => _position.Y + ((_size.Y - GlyphBoxHeight) / 2.0F);

    private float GlyphBoxHeight
    {
        get
        {
            (float top, float bottom) = _font.MeasureVerticalBounds(HeightReference, TextScale);
            return bottom - top;
        }
    }

    private float TextTopPixels => GlyphBoxTop - _font.MeasureVerticalBounds(HeightReference, TextScale).Top;
}

using Minecraft.Core.Games;
using Minecraft.Core.Network.Packets;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Text;

namespace Minecraft.Core.Render.UI.Presets;

public sealed class UIChat
{
    private const int MaxStoredMessages = 100;

    private const int VisibleLinesClosed = 10;
    private const int VisibleLinesOpen = 20;

    private const float LineVisibleSeconds = 10;
    private const float LineFadeSeconds = 1;

    private const int MaxInputLength = 256;
    private const int MaxRecalledMessages = 32;
    private const int LinesPerScrollStep = 3;

    private const float TextScale = 0.32F;
    private const float ChatWidthFraction = 0.5F;
    private const float MinChatWidthPixels = 320;
    private const float MaxChatWidthPixels = 760;
    private const int MarginPixels = 10;
    private const int HorizontalPaddingPixels = 5;
    private const int VerticalPaddingPixels = 3;
    private const int LogToInputGapPixels = 6;
    private const int CaretWidthPixels = 2;
    private const float CaretBlinkSeconds = 1;
    private const float BackdropTransparency = 0.55F;

    private static readonly Vector3 _messageColor = new(0.95F, 0.95F, 0.95F);
    private static readonly Vector3 _systemColor = new(1.0F, 0.82F, 0.35F);
    private static readonly Vector3 _backdropColor = Vector3.Zero;

    private readonly Game _game;
    private readonly UICanvas _canvas;
    private readonly Font _font;

    private readonly UIImage[] _lineBackdrops = new UIImage[VisibleLinesOpen];
    private readonly UIText[] _lineTexts = new UIText[VisibleLinesOpen];
    private readonly UIImage _inputBackdrop;
    private readonly UIText _inputText;
    private readonly UIImage _inputCaret;

    private readonly HeldKey _backspaceKey = new(Keys.Backspace);
    private readonly HeldKey _deleteKey = new(Keys.Delete);
    private readonly HeldKey _caretLeftKey = new(Keys.Left);
    private readonly HeldKey _caretRightKey = new(Keys.Right);

    private readonly List<ChatLine> _messages = [];

    private readonly List<ChatLine> _lines = [];

    private readonly List<string> _sentMessages = [];

    private float _lineHeightPixels;
    private float _chatWidthPixels;
    private float _textWidthPixels;
    private float _textLeftPixels;
    private float _inputTextTopPixels;

    private string _input = string.Empty;
    private int _caretIndex;

    private int _scrollOffset;

    private int _recallIndex = -1;
    private string _draftBeforeRecall = string.Empty;

    private DateTime _lastInputEditAt = DateTime.Now;

    public bool IsTyping { get; private set; }

    public UIChat(Game game, UICanvas canvas)
    {
        _game = game;
        _canvas = canvas;
        _font = FontRegistry.GetFont(FontType.Arial);

        for (int slot = 0; slot < VisibleLinesOpen; slot++)
        {
            _lineBackdrops[slot] = new UIImage(canvas, Vector2.Zero, Vector2.Zero, UITextures.White)
            {
                Color = _backdropColor,
                IsVisible = false,
            };
            canvas.AddComponentToRender(_lineBackdrops[slot]);
        }

        for (int slot = 0; slot < VisibleLinesOpen; slot++)
        {
            _lineTexts[slot] = new UIText(canvas, _font, Vector2.Zero, new Vector2(TextScale, TextScale), string.Empty)
            {
                IsVisible = false,
            };
            canvas.AddComponentToRender(_lineTexts[slot]);
        }

        _inputBackdrop = new UIImage(canvas, Vector2.Zero, Vector2.Zero, UITextures.White)
        {
            Color = _backdropColor,
            Transparency = BackdropTransparency,
            IsVisible = false,
        };
        canvas.AddComponentToRender(_inputBackdrop);

        _inputText = new UIText(canvas, _font, Vector2.Zero, new Vector2(TextScale, TextScale), string.Empty)
        {
            Color = _messageColor,
            IsVisible = false,
        };
        canvas.AddComponentToRender(_inputText);

        _inputCaret = new UIImage(canvas, Vector2.Zero, Vector2.Zero, UITextures.White)
        {
            Color = _messageColor,
            IsVisible = false,
        };
        canvas.AddComponentToRender(_inputCaret);

        Layout();
    }

    public void AddUserMessage(string sender, string message) => AddLine("<" + sender + "> " + message, _messageColor);

    public void AddSystemMessage(string message) => AddLine(message, _systemColor);

    public void Clear()
    {
        _messages.Clear();
        _lines.Clear();
        _scrollOffset = 0;
    }

    public void Update()
    {
        if (_game.Window.IsFocused && _game.IsPlaying)
        {
            UpdateInput();
        }

        UpdateLog();
        UpdateInputLine();
    }

    public void OnCanvasResized()
    {
        Layout();
        RebuildLines();
    }

    private void UpdateInput()
    {
        if (!IsTyping)
        {
            if (Game.Input.OnKeyPress(Keys.T) ||
                Game.Input.OnKeyPress(Keys.Enter) ||
                Game.Input.OnKeyPress(Keys.KeyPadEnter))
            {
                Open(string.Empty);
            }
            else if (Game.Input.OnKeyPress(Keys.Slash))
            {
                Open("/");
            }

            return;
        }

        if (Game.Input.OnKeyPress(Keys.Escape))
        {
            Close();
            return;
        }

        if (Game.Input.OnKeyPress(Keys.Enter) || Game.Input.OnKeyPress(Keys.KeyPadEnter))
        {
            Send();
            Close();
            return;
        }

        UpdateScrolling();
        UpdateCaret();
        UpdateRecall();
        UpdateTyping();
    }

    private void Open(string initialText)
    {
        IsTyping = true;
        _scrollOffset = 0;
        _recallIndex = -1;
        _draftBeforeRecall = string.Empty;
        SetInput(initialText);
    }

    private void Close()
    {
        IsTyping = false;
        _scrollOffset = 0;
        _recallIndex = -1;
        _draftBeforeRecall = string.Empty;
        SetInput(string.Empty);
    }

    private void Send()
    {
        string message = _input.Trim();
        if (message.Length == 0)
        {
            return;
        }

        _game.Client.WritePacket(new ChatPacket(_game.ClientPlayer.Name, message));

        if (_sentMessages.Count == 0 || _sentMessages[^1] != message)
        {
            _sentMessages.Add(message);
        }

        if (_sentMessages.Count > MaxRecalledMessages)
        {
            _sentMessages.RemoveAt(0);
        }
    }

    private void UpdateScrolling()
    {
        int maxScroll = Math.Max(0, _lines.Count - VisibleLinesOpen);

        int step = 0;
        float wheelDelta = Game.Input.ScrollDelta.Y;
        if (wheelDelta > 0)
        {
            step += LinesPerScrollStep;
        }
        else if (wheelDelta < 0)
        {
            step -= LinesPerScrollStep;
        }

        if (Game.Input.OnKeyPress(Keys.PageUp))
        {
            step += VisibleLinesOpen - 1;
        }

        if (Game.Input.OnKeyPress(Keys.PageDown))
        {
            step -= VisibleLinesOpen - 1;
        }

        _scrollOffset = Math.Clamp(_scrollOffset + step, 0, maxScroll);
    }

    private void UpdateCaret()
    {
        if (_caretLeftKey.HasFired() && _caretIndex > 0)
        {
            _caretIndex--;
            _lastInputEditAt = DateTime.Now;
        }

        if (_caretRightKey.HasFired() && _caretIndex < _input.Length)
        {
            _caretIndex++;
            _lastInputEditAt = DateTime.Now;
        }

        if (Game.Input.OnKeyPress(Keys.Home))
        {
            _caretIndex = 0;
            _lastInputEditAt = DateTime.Now;
        }

        if (Game.Input.OnKeyPress(Keys.End))
        {
            _caretIndex = _input.Length;
            _lastInputEditAt = DateTime.Now;
        }
    }

    private void UpdateRecall()
    {
        if (Game.Input.OnKeyPress(Keys.Up) && _sentMessages.Count > 0)
        {
            if (_recallIndex < 0)
            {
                _draftBeforeRecall = _input;
                _recallIndex = _sentMessages.Count - 1;
            }
            else
            {
                _recallIndex = Math.Max(0, _recallIndex - 1);
            }

            SetInput(_sentMessages[_recallIndex]);
        }

        if (Game.Input.OnKeyPress(Keys.Down) && _recallIndex >= 0)
        {
            if (_recallIndex >= _sentMessages.Count - 1)
            {
                _recallIndex = -1;
                SetInput(_draftBeforeRecall);
            }
            else
            {
                _recallIndex++;
                SetInput(_sentMessages[_recallIndex]);
            }
        }
    }

    private void UpdateTyping()
    {
        if (_backspaceKey.HasFired() && _caretIndex > 0)
        {
            _input = _input.Remove(_caretIndex - 1, 1);
            _caretIndex--;
            _lastInputEditAt = DateTime.Now;
        }

        if (_deleteKey.HasFired() && _caretIndex < _input.Length)
        {
            _input = _input.Remove(_caretIndex, 1);
            _lastInputEditAt = DateTime.Now;
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
        string cleaned = Sanitise(text);
        int room = MaxInputLength - _input.Length;
        if (cleaned.Length == 0 || room <= 0)
        {
            return;
        }

        if (cleaned.Length > room)
        {
            cleaned = cleaned[..room];
        }

        _input = _input.Insert(_caretIndex, cleaned);
        _caretIndex += cleaned.Length;
        _lastInputEditAt = DateTime.Now;
    }

    private void SetInput(string text)
    {
        _input = text.Length > MaxInputLength ? text[..MaxInputLength] : text;
        _caretIndex = _input.Length;
        _lastInputEditAt = DateTime.Now;
    }

    private void AddLine(string text, Vector3 color)
    {
        _messages.Add(new ChatLine(Sanitise(text), color, DateTime.Now));
        if (_messages.Count > MaxStoredMessages)
        {
            _messages.RemoveAt(0);
        }

        int linesBefore = _lines.Count;
        RebuildLines();

        if (_scrollOffset > 0)
        {
            int maxScroll = Math.Max(0, _lines.Count - VisibleLinesOpen);
            _scrollOffset = Math.Clamp(_scrollOffset + (_lines.Count - linesBefore), 0, maxScroll);
        }
    }

    private void RebuildLines()
    {
        _lines.Clear();
        foreach (ChatLine message in _messages)
        {
            foreach (string wrappedLine in WrapText(message.Text))
            {
                _lines.Add(new ChatLine(wrappedLine, message.Color, message.ReceivedAt));
            }
        }
    }

    private List<string> WrapText(string text)
    {
        var wrappedLines = new List<string>();
        if (text.Length == 0)
        {
            wrappedLines.Add(string.Empty);
            return wrappedLines;
        }

        int lineStart = 0;
        while (lineStart < text.Length)
        {
            int fitting = 0;
            float width = 0;
            while (lineStart + fitting < text.Length)
            {
                float characterWidth = _font.MeasureWidth(text[lineStart + fitting].ToString(), TextScale);

                if (fitting > 0 && width + characterWidth > _textWidthPixels)
                {
                    break;
                }

                width += characterWidth;
                fitting++;
            }

            if (lineStart + fitting >= text.Length)
            {
                wrappedLines.Add(text[lineStart..]);
                break;
            }

            int lastSpace = text.LastIndexOf(' ', lineStart + fitting - 1, fitting);
            if (lastSpace > lineStart)
            {
                wrappedLines.Add(text[lineStart..lastSpace]);
                lineStart = lastSpace + 1;
            }
            else
            {
                wrappedLines.Add(text[lineStart..(lineStart + fitting)]);
                lineStart += fitting;
            }
        }

        return wrappedLines;
    }

    private void UpdateLog()
    {
        int visibleLines = IsTyping ? VisibleLinesOpen : VisibleLinesClosed;
        DateTime now = DateTime.Now;

        for (int slot = 0; slot < _lineTexts.Length; slot++)
        {
            int lineIndex = _lines.Count - 1 - _scrollOffset - slot;
            float transparency = 0;

            if (slot < visibleLines && lineIndex >= 0 && lineIndex < _lines.Count)
            {
                transparency = IsTyping ? 1 : GetFadeTransparency(_lines[lineIndex].ReceivedAt, now);
            }

            if (transparency <= 0)
            {
                _lineTexts[slot].IsVisible = false;
                _lineBackdrops[slot].IsVisible = false;
                continue;
            }

            ChatLine line = _lines[lineIndex];
            _lineTexts[slot].Text = line.Text;
            _lineTexts[slot].Color = line.Color;
            _lineTexts[slot].Transparency = transparency;
            _lineTexts[slot].IsVisible = true;

            _lineBackdrops[slot].Transparency = transparency * BackdropTransparency;
            _lineBackdrops[slot].IsVisible = true;
        }
    }

    private static float GetFadeTransparency(DateTime receivedAt, DateTime now)
    {
        float elapsedSeconds = (float)(now - receivedAt).TotalSeconds;
        if (elapsedSeconds <= LineVisibleSeconds)
        {
            return 1;
        }

        return Math.Clamp(1 - ((elapsedSeconds - LineVisibleSeconds) / LineFadeSeconds), 0, 1);
    }

    private void UpdateInputLine()
    {
        _inputBackdrop.IsVisible = IsTyping;
        _inputText.IsVisible = IsTyping;

        if (!IsTyping)
        {
            _inputCaret.IsVisible = false;
            return;
        }

        int firstVisible = 0;
        while (firstVisible < _caretIndex &&
               _font.MeasureWidth(_input[firstVisible.._caretIndex], TextScale) > _textWidthPixels - CaretWidthPixels)
        {
            firstVisible++;
        }

        string visibleText = _input[firstVisible..];
        while (visibleText.Length > 0 && _font.MeasureWidth(visibleText, TextScale) > _textWidthPixels)
        {
            visibleText = visibleText[..^1];
        }

        _inputText.Text = visibleText;

        float caretOffset = _font.MeasureWidth(_input[firstVisible.._caretIndex], TextScale);
        _inputCaret.PixelPositionInCanvas = new Vector2(_textLeftPixels + caretOffset, _inputTextTopPixels);

        double secondsSinceEdit = (DateTime.Now - _lastInputEditAt).TotalSeconds;
        _inputCaret.IsVisible = secondsSinceEdit % CaretBlinkSeconds < CaretBlinkSeconds / 2;
    }

    private void Layout()
    {
        _lineHeightPixels = _font.DesiredPixelLineHeight * TextScale;
        _chatWidthPixels = Math.Clamp(
            _canvas.PixelWidth * ChatWidthFraction, MinChatWidthPixels, MaxChatWidthPixels);

        _chatWidthPixels = Math.Min(_chatWidthPixels, Math.Max(1, _canvas.PixelWidth - (2 * MarginPixels)));
        _textWidthPixels = _chatWidthPixels - (2 * HorizontalPaddingPixels);
        _textLeftPixels = MarginPixels + HorizontalPaddingPixels;

        float inputBoxHeight = _lineHeightPixels + (2 * VerticalPaddingPixels);
        float inputBoxTop = _canvas.PixelHeight - MarginPixels - inputBoxHeight;
        _inputTextTopPixels = inputBoxTop + VerticalPaddingPixels;

        _inputBackdrop.PixelPositionInCanvas = new Vector2(MarginPixels, inputBoxTop);
        _inputBackdrop.Dimension = new Vector2(_chatWidthPixels, inputBoxHeight);

        _inputText.PixelPositionInCanvas = new Vector2(_textLeftPixels, _inputTextTopPixels);
        _inputCaret.Dimension = new Vector2(CaretWidthPixels, _lineHeightPixels);

        float logBottom = inputBoxTop - LogToInputGapPixels;
        for (int slot = 0; slot < VisibleLinesOpen; slot++)
        {
            float lineTop = logBottom - ((slot + 1) * _lineHeightPixels);

            _lineBackdrops[slot].PixelPositionInCanvas = new Vector2(MarginPixels, lineTop);
            _lineBackdrops[slot].Dimension = new Vector2(_chatWidthPixels, _lineHeightPixels);
            _lineTexts[slot].PixelPositionInCanvas = new Vector2(_textLeftPixels, lineTop);
        }
    }

    private static string Sanitise(string text)
    {
        var builder = new StringBuilder(text.Length);
        foreach (char character in text)
        {
            builder.Append(char.IsControl(character) ? ' ' : character);
        }

        return builder.ToString();
    }

    private readonly record struct ChatLine(string Text, Vector3 Color, DateTime ReceivedAt);
}

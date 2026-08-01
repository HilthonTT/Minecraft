using Minecraft.Core.Games;
using Minecraft.Core.Network.Packets;
using Minecraft.Core.Textures;
using Minecraft.Core.Utilities;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Text;

namespace Minecraft.Core.Render.UI.Presets;

/// <summary>The always present in game overlay: the crosshair, the chat log and the chat input line.</summary>
public sealed class UICanvasIngame : UICanvas
{
    private const int MaxChatLines = 10;

    /// <summary>How long the chat stays fully visible after the last message before it fades out.</summary>
    private const float ChatVisibleSeconds = 10;

    private const int CursorSize = 20;

    private readonly Game _game;
    private readonly UIText _chatbox;
    private readonly UIText _inputField;

    private readonly List<string> _messageHistory = [];
    private DateTime _lastTimeChatVisible = DateTime.Now;

    public bool IsTyping { get; private set; }

    public UICanvasIngame(Game game)
        : base(
            Vector3.Zero,
            Vector3.Zero,
            game.Window.ClientSize.X,
            game.Window.ClientSize.Y,
            RenderSpace.Screen)
    {
        _game = game;

        int midX = game.Window.ClientSize.X / 2;
        int midY = game.Window.ClientSize.Y / 2;

        var cursorTexture = new Texture(Assets.Path("Resources/cursor.png"), 512, 512);
        var cursor = new UIImage(
            this,
            new Vector2(midX - CursorSize / 2, midY - CursorSize / 2),
            new Vector2(CursorSize, CursorSize),
            cursorTexture);
        AddComponentToRender(cursor);

        _chatbox = new UIText(
            this,
            FontRegistry.GetFont(FontType.Arial),
            new Vector2(10, midY),
            new Vector2(0.35F, 0.35F),
            string.Empty)
        {
            Color = Vector3.Zero,
        };
        AddComponentToRender(_chatbox);

        _inputField = new UIText(
            this,
            FontRegistry.GetFont(FontType.Arial),
            new Vector2(10, midY - 50),
            new Vector2(0.35F, 0.35F),
            string.Empty)
        {
            Color = Vector3.Zero,
        };
        AddComponentToRender(_inputField);
    }

    public void AddUserMessage(string sender, string message)
    {
        _messageHistory.Add(sender + ": " + message);
        if (_messageHistory.Count > MaxChatLines)
        {
            _messageHistory.RemoveAt(0);
        }

        var builder = new StringBuilder();
        foreach (string chatLine in _messageHistory)
        {
            builder.AppendLine(chatLine);
        }
        _chatbox.Text = builder.ToString();

        _lastTimeChatVisible = DateTime.Now;
        _chatbox.Transparency = 1.0F;
    }

    public override void Update()
    {
        UpdateChatVisibility();

        if (!_game.Window.IsFocused)
        {
            return;
        }

        if (Game.Input.OnKeyPress(Keys.Enter))
        {
            IsTyping = !IsTyping;

            // Closing the input line sends whatever was typed.
            if (!IsTyping)
            {
                if (_inputField.Text.Length > 0)
                {
                    _game.Client.WritePacket(new ChatPacket(_game.ClientPlayer.Name, _inputField.Text));
                }
                _inputField.Text = string.Empty;
            }
        }

        if (!IsTyping)
        {
            return;
        }

        if (Game.Input.OnKeyPress(Keys.Backspace) && _inputField.Text.Length > 0)
        {
            _inputField.Text = _inputField.Text[..^1];
        }

        IReadOnlyList<char> typed = Game.Input.TypedCharacters;
        if (typed.Count == 0)
        {
            return;
        }

        var builder = new StringBuilder(_inputField.Text);
        foreach (char character in typed)
        {
            builder.Append(character);
        }
        _inputField.Text = builder.ToString();
    }

    private void UpdateChatVisibility()
    {
        if (IsTyping)
        {
            _chatbox.Transparency = 1.0F;
            _lastTimeChatVisible = DateTime.Now;
            return;
        }

        // Once the visible window has passed, fade out over the following second.
        float elapsedSeconds = (float)(DateTime.Now - _lastTimeChatVisible).TotalSeconds;
        if (elapsedSeconds > ChatVisibleSeconds)
        {
            _chatbox.Transparency = Math.Clamp(1 - (elapsedSeconds - ChatVisibleSeconds), 0, 1);
        }
    }
}

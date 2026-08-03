using Minecraft.Core.Games;
using Minecraft.Core.Textures;
using Minecraft.Core.Utilities;
using OpenTK.Mathematics;

namespace Minecraft.Core.Render.UI.Presets;

/// <summary>The always present in game overlay: the crosshair and the chat.</summary>
public sealed class UICanvasIngame : UICanvas
{
    private const int CursorSize = 20;

    private readonly UIImage _crosshair;
    private readonly UIChat _chat;

    /// <summary>Whether the chat input line is open, which is when typing takes priority over the controls.</summary>
    public bool IsTyping => _chat.IsTyping;

    public UICanvasIngame(Game game)
        : base(
            Vector3.Zero,
            Vector3.Zero,
            game.Window.ClientSize.X,
            game.Window.ClientSize.Y,
            RenderSpace.Screen)
    {
        var cursorTexture = new Texture(Assets.Path("Resources/cursor.png"), 512, 512);
        _crosshair = new UIImage(
            this,
            GetCrosshairPosition(),
            new Vector2(CursorSize, CursorSize),
            cursorTexture);
        AddComponentToRender(_crosshair);

        _chat = new UIChat(game, this);
    }

    public void AddUserMessage(string sender, string message) => _chat.AddUserMessage(sender, message);

    public void AddSystemMessage(string message) => _chat.AddSystemMessage(message);

    public override void Update() => _chat.Update();

    protected override void OnDimensionsChanged()
    {
        _crosshair.PixelPositionInCanvas = GetCrosshairPosition();
        _chat.OnCanvasResized();
    }

    private Vector2 GetCrosshairPosition() =>
        new((PixelWidth - CursorSize) / 2.0F, (PixelHeight - CursorSize) / 2.0F);
}

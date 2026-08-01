using Minecraft.Core.Entities;
using Minecraft.Core.Entities.Player;
using Minecraft.Core.Games;
using Minecraft.Core.Utilities;
using OpenTK.Mathematics;

namespace Minecraft.Core.Render.UI.Presets;

/// <summary>
/// A name tag floating above another player. It lives in world space and turns to face the local player
/// every frame.
/// </summary>
public sealed class UICanvasEntityName : UICanvas
{
    private static readonly Vector3 _nameTagOffset = new(0, 1, 0);

    private readonly Game _game;
    private readonly ClientPlayer _localPlayer;
    private readonly Entity _otherEntity;

    public UICanvasEntityName(Game game, Entity otherEntity, string text)
        : base(otherEntity.Position + _nameTagOffset, Vector3.Zero, 800, 450, RenderSpace.World)
    {
        _otherEntity = otherEntity;
        _localPlayer = game.ClientPlayer;
        _game = game;

        otherEntity.OnDespawnedHandler += OnEntityDespawned;

        var playerName = new UIText(
            this,
            FontRegistry.GetFont(FontType.Arial),
            new Vector2(0, 0),
            Vector2.One,
            text);
        AddComponentToRender(playerName);
    }

    public override void Update()
    {
        Position = _otherEntity.Position + _nameTagOffset;

        // Signed angle between the world forward axis and the direction to the local player, so the tag
        // always turns the shorter way round.
        Vector3 direction = _localPlayer.Position - _otherEntity.Position;
        float theta = MathF.Atan2(
            direction.X * Vector3.UnitZ.Z - direction.Z * Vector3.UnitZ.X,
            direction.X * Vector3.UnitZ.X + direction.Z * Vector3.UnitZ.Z);

        Rotation = new Vector3(0, MathUtils.RadianToDegree(theta), 0);
    }

    private void OnEntityDespawned()
    {
        _otherEntity.OnDespawnedHandler -= OnEntityDespawned;
        _game.MasterRenderer.RemoveCanvas(this);
    }
}

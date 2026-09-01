using Minecraft.Core.Entities;
using Minecraft.Core.Entities.Player;
using Minecraft.Core.Games;
using Minecraft.Core.Utilities;
using OpenTK.Mathematics;

namespace Minecraft.Core.Render.UI.Presets;

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
            new Vector2(0.5F, 0.5F),
            text);
        AddComponentToRender(playerName);
    }

    public override void Update()
    {
        Position = _otherEntity.Position + _nameTagOffset;

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

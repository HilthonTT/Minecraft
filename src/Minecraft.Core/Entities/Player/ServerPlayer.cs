using Minecraft.Core.Games;
using Minecraft.Core.Inventories;
using Minecraft.Core.Worlds;
using OpenTK.Mathematics;

namespace Minecraft.Core.Entities.Player;

public sealed class ServerPlayer : Player
{
    public int Health { get; private set; } = Constants.PLAYER_MAX_HEALTH;

    public ItemStack HeldItem { get; set; }

    public bool IsAlive => Health > 0;

    public bool IsHurt => _hurtSecondsRemaining > 0F;

    private float _hurtSecondsRemaining;
    private float _secondsSinceLastHurt;
    private float _secondsSinceLastRegen;

    public ServerPlayer(int id, string playerName, World? world, Vector3 position)
        : base(id, playerName, world, position)
    {
    }

    public override void Update(float deltaTime, World world)
    {
        _hurtSecondsRemaining = MathF.Max(_hurtSecondsRemaining - deltaTime, 0F);
        _secondsSinceLastHurt += deltaTime;

        base.Update(deltaTime, world);
    }

    public bool TryRegenerate(float deltaTime)
    {
        if (!IsAlive || Health >= Constants.PLAYER_MAX_HEALTH ||
            _secondsSinceLastHurt < Constants.PLAYER_REGEN_DELAY_SECONDS)
        {
            _secondsSinceLastRegen = 0F;
            return false;
        }

        _secondsSinceLastRegen += deltaTime;
        if (_secondsSinceLastRegen < Constants.PLAYER_REGEN_SECONDS_PER_HEALTH)
        {
            return false;
        }

        _secondsSinceLastRegen = 0F;
        Health++;
        return true;
    }

    public bool TryHurt(int damage)
    {
        if (!IsAlive || IsCreative || IsHurt || damage <= 0)
        {
            return false;
        }

        Health = Math.Max(Health - damage, 0);
        _hurtSecondsRemaining = Constants.PLAYER_HURT_SECONDS;
        _secondsSinceLastHurt = 0F;
        _secondsSinceLastRegen = 0F;
        return true;
    }

    public void Respawn(Vector3 spawnPosition)
    {
        Health = Constants.PLAYER_MAX_HEALTH;
        _hurtSecondsRemaining = 0F;
        _secondsSinceLastHurt = Constants.PLAYER_REGEN_DELAY_SECONDS;
        _secondsSinceLastRegen = 0F;

        Position = spawnPosition;
        Velocity = Vector3.Zero;
        UpdateAxisAlignedBox();
    }

    public override void SetGameMode(GameMode gameMode)
    {
        base.SetGameMode(gameMode);

        if (gameMode == GameMode.Creative)
        {
            Health = Constants.PLAYER_MAX_HEALTH;
            _hurtSecondsRemaining = 0F;
        }
    }
}

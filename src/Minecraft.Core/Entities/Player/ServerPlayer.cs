using Minecraft.Core.Games;
using Minecraft.Core.Inventories;
using Minecraft.Core.Inventories.Items;
using Minecraft.Core.Worlds;
using OpenTK.Mathematics;

namespace Minecraft.Core.Entities.Player;

public sealed class ServerPlayer : Player
{
    public const float WalkExhaustionPerBlock = 0.01F;
    public const float SprintExhaustionPerBlock = 0.1F;
    public const float HealExhaustion = 3F;
    public const float HurtExhaustion = 0.1F;
    public const float AttackExhaustion = 0.1F;
    public const float BreakExhaustion = 0.005F;

    private const float MaxStepCountedBlocks = 8F;

    public int Health { get; private set; } = Constants.PLAYER_MAX_HEALTH;

    public int Food { get; private set; } = Constants.PLAYER_MAX_FOOD;

    public float Exhaustion { get; private set; }

    public bool IsSprinting { get; set; }

    public ItemStack HeldItem { get; set; }

    public bool IsAlive => Health > 0;

    public bool IsHurt => _hurtSecondsRemaining > 0F;

    private float _hurtSecondsRemaining;
    private float _secondsSinceLastHurt;
    private float _secondsSinceLastRegen;
    private float _secondsSinceLastStarve;

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
            Food < Constants.PLAYER_REGEN_MIN_FOOD ||
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
        AddExhaustion(HealExhaustion);
        return true;
    }

    public bool TryStarve(float deltaTime)
    {
        if (!IsAlive || IsCreative || Food > 0)
        {
            _secondsSinceLastStarve = 0F;
            return false;
        }

        _secondsSinceLastStarve += deltaTime;
        if (_secondsSinceLastStarve < Constants.PLAYER_STARVE_SECONDS_PER_HEALTH)
        {
            return false;
        }

        _secondsSinceLastStarve = 0F;
        return Health > 1;
    }

    public void AddExhaustion(float amount)
    {
        if (IsCreative || !IsAlive || amount <= 0F)
        {
            return;
        }

        Exhaustion += amount;
    }

    public void OnMoved(Vector3 from, Vector3 to)
    {
        float distance = new Vector2(to.X - from.X, to.Z - from.Z).Length;
        if (!float.IsFinite(distance) || distance > MaxStepCountedBlocks || IsFlying)
        {
            return;
        }

        AddExhaustion(distance * (IsSprinting ? SprintExhaustionPerBlock : WalkExhaustionPerBlock));
    }

    public bool TryDrainFood()
    {
        bool drained = false;

        while (Exhaustion >= Constants.PLAYER_EXHAUSTION_PER_FOOD)
        {
            Exhaustion -= Constants.PLAYER_EXHAUSTION_PER_FOOD;

            if (Food > 0)
            {
                Food--;
                drained = true;
            }
        }

        return drained;
    }

    public bool TryEat(FoodItem food)
    {
        if (!IsAlive || IsCreative || Food >= Constants.PLAYER_MAX_FOOD)
        {
            return false;
        }

        Food = Math.Min(Food + food.Nourishment, Constants.PLAYER_MAX_FOOD);
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
        AddExhaustion(HurtExhaustion);
        return true;
    }

    public void Respawn(Vector3 spawnPosition)
    {
        Health = Constants.PLAYER_MAX_HEALTH;
        Food = Constants.PLAYER_MAX_FOOD;
        Exhaustion = 0F;
        _secondsSinceLastStarve = 0F;
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
            Food = Constants.PLAYER_MAX_FOOD;
            Exhaustion = 0F;
            _hurtSecondsRemaining = 0F;
        }
    }
}

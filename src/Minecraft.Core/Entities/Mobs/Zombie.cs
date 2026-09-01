using Minecraft.Core.Entities.Player;
using Minecraft.Core.Worlds;
using OpenTK.Mathematics;

namespace Minecraft.Core.Entities.Mobs;

public sealed class Zombie : Mob
{
    public const float BodyWidth = 0.6F;
    public const float BodyHeight = 1.8F;
    public const float BodyLength = 0.6F;

    public const int FullHealth = 20;

    private const float AggroRadius = 24F;

    private const int TicksHuntingAttacker = 200;

    private const int WanderRadius = 6;
    private const int TicksBetweenDecisions = 30;
    private const int OneInChanceOfMoving = 2;

    private const float AttackReach = 1.5F;

    private const int AttackDamage = 3;
    private const int TicksBetweenAttacks = 20;

    private int _attackerId = -1;
    private int _huntingTicksRemaining;
    private int _ticksUntilNextAttack;

    public Zombie(int id, World? world, Vector3 position)
        : base(id, world, position, EntityType.Zombie, FullHealth)
    {
    }

    public override bool IsHostile => true;

    protected override float MoveSpeed => 26F;

    protected override void SetInitialDimensions()
    {
        _width = BodyWidth;
        _height = BodyHeight;
        _length = BodyLength;
    }

    protected override void OnHurtBy(Vector3 from, Entity? attacker)
    {
        if (attacker is null)
        {
            return;
        }

        _attackerId = attacker.ID;
        _huntingTicksRemaining = TicksHuntingAttacker;
    }

    protected override void DecideWhatToDo(WorldServer world)
    {
        TryAttackSomebodyWithinReach(world);

        if (TryHuntAttacker(world))
        {
            return;
        }

        ServerPlayer? player = FindNearestPlayer(world, Position, AggroRadius);
        if (player is not null)
        {
            SetTarget(player.Position);
            return;
        }

        TickWandering(WanderRadius, TicksBetweenDecisions, OneInChanceOfMoving);
    }

    private void TryAttackSomebodyWithinReach(WorldServer world)
    {
        if (_ticksUntilNextAttack > 0)
        {
            _ticksUntilNextAttack--;
            return;
        }

        Vector3 chest = Position + new Vector3(0F, BodyHeight / 2F, 0F);
        ServerPlayer? victim = FindNearestPlayer(world, chest, AttackReach);
        if (victim is null)
        {
            return;
        }

        _ticksUntilNextAttack = TicksBetweenAttacks;
        world.HurtPlayer(victim, AttackDamage);
    }

    private bool TryHuntAttacker(WorldServer world)
    {
        if (_huntingTicksRemaining <= 0)
        {
            return false;
        }

        _huntingTicksRemaining--;

        if (!world.LoadedEntities.TryGetValue(_attackerId, out Entity? attacker) || attacker is not ServerPlayer)
        {
            _huntingTicksRemaining = 0;
            return false;
        }

        SetTarget(attacker.Position);
        return true;
    }
}

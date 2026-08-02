using Minecraft.Core.Entities.Player;
using Minecraft.Core.Worlds;
using OpenTK.Mathematics;

namespace Minecraft.Core.Entities.Mobs;

/// <summary>
/// A hostile mob that walks at the nearest player within reach, and wanders when there is nobody to follow.
/// It cannot hurt anyone yet; reaching the player is as far as it goes.
/// </summary>
public sealed class Zombie : Mob
{
    public const float BodyWidth = 0.6F;
    public const float BodyHeight = 1.8F;
    public const float BodyLength = 0.6F;

    /// <summary>How far away a zombie notices a player.</summary>
    private const float AggroRadius = 24F;

    private const int WanderRadius = 6;
    private const int TicksBetweenDecisions = 30;
    private const int OneInChanceOfMoving = 2;

    public Zombie(int id, World? world, Vector3 position) : base(id, world, position, EntityType.Zombie)
    {
    }

    protected override float MoveSpeed => 26F;

    protected override void SetInitialDimensions()
    {
        _width = BodyWidth;
        _height = BodyHeight;
        _length = BodyLength;
    }

    protected override void DecideWhatToDo(WorldServer world)
    {
        // Re-aimed every tick, so the zombie follows a player who is moving rather than heading for where
        // they were standing when it first noticed them.
        ServerPlayer? player = FindNearestPlayer(world, Position, AggroRadius);
        if (player is not null)
        {
            SetTarget(player.Position);
            return;
        }

        // Whatever it was chasing has gone. It finishes walking to where they were last seen, then wanders.
        TickWandering(WanderRadius, TicksBetweenDecisions, OneInChanceOfMoving);
    }
}

using Minecraft.Core.Games;
using Minecraft.Core.Inventories.Items;
using Minecraft.Core.Network.Packets;
using Minecraft.Core.Worlds;
using Minecraft.Core.Worlds.Blocks;
using OpenTK.Mathematics;

namespace Minecraft.Core.Entities.Player;

public sealed class BlockBreaker
{
    private const float SecondsBetweenMiningSwings = 0.3F;

    private const float SecondsBetweenBreaks = 0.25F;

    private readonly Game _game;
    private readonly ClientPlayer _player;
    private readonly Action _onSwing;

    private Vector3i? _breakingBlockPos;

    private float _secondsSpentBreaking;
    private float _secondsUntilNextMiningSwing;

    private float _secondsUntilNextBreak;

    private bool _hasAskedToBreakTarget;

    public float Progress { get; private set; }

    public BlockBreaker(Game game, ClientPlayer player, Action onSwing)
    {
        _game = game;
        _player = player;
        _onSwing = onSwing;
    }

    public void Update(float deltaTime, World world, Vector3i? target)
    {
        if (target is null)
        {
            Stop();
            return;
        }

        _secondsUntilNextBreak = MathF.Max(0F, _secondsUntilNextBreak - deltaTime);

        Vector3i blockPos = target.Value;
        Block block = world.GetBlockAt(blockPos).GetBlock();

        if (!block.IsBreakable && !_player.IsCreative)
        {
            Stop();
            return;
        }

        if (_breakingBlockPos != blockPos)
        {
            _breakingBlockPos = blockPos;
            _secondsSpentBreaking = 0F;
            _secondsUntilNextMiningSwing = 0F;
            _hasAskedToBreakTarget = false;
        }

        if (_hasAskedToBreakTarget)
        {
            return;
        }

        float required = _player.IsCreative ? 0F : Harvesting.SecondsToBreak(block, _player.Inventory.Selected);

        _secondsSpentBreaking += deltaTime;
        Progress = required <= 0F ? 1F : Math.Clamp(_secondsSpentBreaking / required, 0F, 1F);

        _secondsUntilNextMiningSwing -= deltaTime;
        if (_secondsUntilNextMiningSwing <= 0F)
        {
            _secondsUntilNextMiningSwing = SecondsBetweenMiningSwings;
            _onSwing();
        }

        if (Progress < 1F || _secondsUntilNextBreak > 0F)
        {
            if (required <= 0F)
            {
                Progress = 0F;
            }

            return;
        }

        _game.Client.WritePacket(new RemoveBlockPacket(blockPos));

        if (Harvesting.IsCorrectToolFor(block, _player.Inventory.Selected) && _player.Inventory.WearSelected())
        {
            _game.SoundDirector.OnToolBroke(_player.Position);
        }

        _secondsUntilNextBreak = SecondsBetweenBreaks;
        _hasAskedToBreakTarget = true;
        Progress = 0F;
    }

    public void Stop()
    {
        _breakingBlockPos = null;
        _secondsSpentBreaking = 0F;
        _secondsUntilNextMiningSwing = 0F;

        _secondsUntilNextBreak = 0F;
        _hasAskedToBreakTarget = false;
        Progress = 0F;
    }
}

using Minecraft.Core.Games;
using Minecraft.Core.Inventories;
using Minecraft.Core.Worlds;
using OpenTK.Mathematics;

namespace Minecraft.Core.Entities.Player;

/// <summary>
/// A player as the server sees them: a position a client reports, and the health behind it, which the client
/// only ever reads.
/// <para>
/// Health is here rather than on <see cref="Player"/> for the same reason a mob's is on the server side of
/// the world — the only thing that decides a blow lands is here, and a client that held its own copy of the
/// number would be a second opinion on it. What the client is sent is the bar to draw.
/// </para>
/// </summary>
public sealed class ServerPlayer : Player
{
    /// <summary>What the player has left, in half hearts.</summary>
    public int Health { get; private set; } = Constants.PLAYER_MAX_HEALTH;

    /// <summary>
    /// What the client last said it was holding. The one piece of a client's inventory this side keeps, and
    /// it is kept because a break has to be answered here and the answer now depends on the tool: see
    /// <see cref="Network.Packets.PlayerHeldItemPacket"/>.
    /// <para>
    /// A count of one, whatever the client is really carrying. Nothing on this side asks how many; what it
    /// asks is what kind and how worn, which is all a break is decided on.
    /// </para>
    /// </summary>
    public ItemStack HeldItem { get; set; }

    public bool IsAlive => Health > 0;

    /// <summary>
    /// Whether a blow landed recently enough that the next one is held off. The same window a mob gets, and
    /// what stops a crowd of zombies from emptying the bar within a single second.
    /// </summary>
    public bool IsHurt => _hurtSecondsRemaining > 0F;

    private float _hurtSecondsRemaining;
    private float _secondsSinceLastHurt;
    private float _secondsSinceLastRegen;

    public ServerPlayer(int id, string playerName, World? world, Vector3 position)
        : base(id, playerName, world, position)
    {
    }

    /// <summary>
    /// The client simulates its own body and only reports where it ended up, so nothing here integrates a
    /// position. What this side does own is the clock the invulnerability and the mending run on.
    /// </summary>
    public override void Update(float deltaTime, World world)
    {
        _hurtSecondsRemaining = MathF.Max(_hurtSecondsRemaining - deltaTime, 0F);
        _secondsSinceLastHurt += deltaTime;

        base.Update(deltaTime, world);
    }

    /// <summary>
    /// Mends a half heart once the player has been left alone long enough, and reports whether one went on.
    /// There is nothing to eat yet, so without this every scrape a world ever deals is permanent.
    /// </summary>
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

    /// <summary>
    /// Takes a blow. Refused outright in creative, where nothing in the world can touch the player, and
    /// inside the window an earlier blow opened, which is what a crowd of zombies runs into. Reports whether
    /// any of it landed, since a blow that did not is not something to tell anybody about.
    /// </summary>
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

    /// <summary>Puts the player back on their feet at the given spawn, whole again.</summary>
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

    /// <summary>
    /// Switching into creative also heals, which is what stops somebody from stepping out of a fight to
    /// build and stepping back in on half a heart they can no longer lose.
    /// </summary>
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

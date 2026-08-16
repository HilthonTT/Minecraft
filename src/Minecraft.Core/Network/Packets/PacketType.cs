namespace Minecraft.Core.Network.Packets;

public enum PacketType
{
    Chat,
    PlaceBlock,
    RemoveBlock,
    ChunkData,
    ChunkUnload,
    EntityPosition,
    EntitySpawn,
    EntityDespawn,
    PlayerJoinRequest,
    PlayerJoinAccept,
    PlayerJoin,
    PlayerLeave,
    PlayerBlockInteraction,
    PlayerKeepAlive,
    Explosion,
    PlayerSettings,
    PlayerAttackEntity,
    EntityHurt,
    PlayerGameMode,
    PlayerHealth,
    PlayerRespawn,
    PlayerFell,
    ItemSpawn,
    ItemPickup
}

using Minecraft.Core.Network.Packets;

namespace Minecraft.Core.Network.NetHandler;

public interface INetHandler
{
    void ProcessChatPacket(ChatPacket chatPacker);

    void ProcessPlaceBlockPacket(PlaceBlockPacket blockPacket);

    void ProcessRemoveBlockPacket(RemoveBlockPacket removeBlockPacket);

    void ProcessChunkDataPacket(ChunkDataPacket chunkDataPacket);

    void ProcessChunkUnloadPacket(ChunkUnloadPacket unloadChunkPacket);

    void ProcessEntityDataPacket(EntityDataPacket entityDataPacket);

    void ProcessEntitySpawnPacket(EntitySpawnPacket entitySpawnPacket);

    void ProcessEntityDespawnPacket(EntityDespawnPacket entityDespawnPacket);

    void ProcessEntityHurtPacket(EntityHurtPacket entityHurtPacket);

    void ProcessPlayerAttackEntityPacket(PlayerAttackEntityPacket playerAttackEntityPacket);

    void ProcessJoinRequestPacket(PlayerJoinRequestPacket playerJoinRequestPacket);

    void ProcessJoinAcceptPacket(PlayerJoinAcceptPacket playerJoinAcceptPacket);

    void ProcessPlayerJoinPacket(PlayerJoinPacket playerJoinPacket);

    void ProcessPlayerLeavePacket(PlayerLeavePacket playerKickPacket);

    void ProcessPlayerKeepAlivePacket(PlayerKeepAlivePacket keepAlivePacket);

    void ProcessPlayerBlockInteractionpacket(PlayerBlockInteractionPacket playerInteractionPacket);

    void ProcessExplosionPacket(ExplosionPacket explosionPacket);

    void ProcessPlayerSettingsPacket(PlayerSettingsPacket playerSettingsPacket);
}

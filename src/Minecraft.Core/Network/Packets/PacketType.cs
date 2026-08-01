namespace Minecraft.Core.Network.Packets;

public enum PacketType
{
    Chat,
    PlaceBlock,
    RemoveBlock,
    ChunkData,
    ChunkUnload,
    EntityPosition,
    PlayerJoinRequest,
    PlayerJoinAccept,
    PlayerJoin,
    PlayerLeave,
    PlayerBlockInteraction,
    PlayerKeepAlive
}
using Minecraft.Core.Entities.Player;
using Minecraft.Core.Logging;
using Minecraft.Core.Network.NetHandler;
using Minecraft.Core.Network.Packets;
using Minecraft.Core.Worlds;
using OpenTK.Mathematics;

namespace Minecraft.Core.Network.Session;

/// <summary>
/// One end of a connection between a client and a server, holding the player it belongs to and the handler
/// that interprets its packets.
/// </summary>
public abstract class Session
{
    private static readonly PlayerSettings _defaultPlayerSettings = new()
    {
        ViewDistance = Constants.VIEW_DISTANCE_CHUNKS,
    };

    /// <summary>
    /// The furthest a client may ask to see. A view distance is honoured because it is the client's own
    /// business how much of the world it wants, but it costs the server generation and streaming work, so it
    /// is not left for a client to name any number it likes.
    /// </summary>
    private const int MaxAcceptedViewDistance = 16;

    private SessionState _state;

    /// <summary>Null until the server has accepted the connection and assigned a player to it.</summary>
    public Player? Player { get; private set; }

    public INetHandler NetHandler { get; }

    public PlayerSettings PlayerSettings { get; private set; }

    public Connection Connection { get; }

    public SessionState State
    {
        get => _state;
        set
        {
            if (_state == value)
            {
                return;
            }

            _state = value;
            OnStateChangedHandler?.Invoke(this);
        }
    }

    public delegate void OnStateChanged(Session session);
    public event OnStateChanged? OnStateChangedHandler;

    public delegate void OnPlayerAssigned();
    public event OnPlayerAssigned? OnPlayerAssignedHandler;

    protected Session(Connection connection, INetHandler netHandler)
    {
        Connection = connection;
        NetHandler = netHandler;

        PlayerSettings = _defaultPlayerSettings;
        State = SessionState.AwaitingAcceptance;
    }

    /// <summary>
    /// Takes the view distance the client asked for, held to what this server is willing to serve. The chunk
    /// provider reads it every update, so this takes effect on the next one rather than at the next session.
    /// </summary>
    public void SetViewDistance(int viewDistance)
    {
        PlayerSettings = PlayerSettings with
        {
            ViewDistance = Math.Clamp(viewDistance, 1, MaxAcceptedViewDistance),
        };
    }

    public void AssignPlayer(Player player)
    {
        Player = player;
        OnPlayerAssignedHandler?.Invoke();
    }

    /// <summary>
    /// Whether the chunk falls inside the player's view distance. Always false before a player has been
    /// assigned, since there is nowhere to measure from yet.
    /// </summary>
    public bool IsChunkVisible(Vector2 chunkPosition)
    {
        if (Player is null)
        {
            return false;
        }

        Vector2 playerChunkPos = World.GetChunkPosition(Player.Position.X, Player.Position.Z);
        int dx = (int)Math.Abs(chunkPosition.X - playerChunkPos.X);
        int dz = (int)Math.Abs(chunkPosition.Y - playerChunkPos.Y);
        return dx <= PlayerSettings.ViewDistance && dz <= PlayerSettings.ViewDistance;
    }

    public bool IsBlockPositionInViewRange(Vector3i blockPos)
    {
        return IsChunkVisible(World.GetChunkPosition(blockPos.X, blockPos.Z));
    }

    public bool NetDataAvailable() => Connection.NetStream.DataAvailable;

    public bool WritePacket(Packet packet)
    {
        if (State == SessionState.Closed)
        {
            Logger.Error("Trying to send packet " + packet.GetType() + " while the connection is closed.");
            return false;
        }

        if (!Connection.WritePacket(packet))
        {
            State = SessionState.Closed;
            return false;
        }

        return true;
    }

    public Packet ReadPacket()
    {
        return Connection.ReadPacket(this);
    }

    public void Close()
    {
        Connection.Close();
    }
}

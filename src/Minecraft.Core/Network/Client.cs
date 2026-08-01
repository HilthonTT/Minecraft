using Minecraft.Core.Entities.Player;
using Minecraft.Core.Games;
using Minecraft.Core.IO;
using Minecraft.Core.Logging;
using Minecraft.Core.Network.NetHandler;
using Minecraft.Core.Network.Packets;
using Minecraft.Core.Network.Session;
using System.Net.Sockets;

namespace Minecraft.Core.Network;

/// <summary>
/// The client half of the connection. Socket reads and writes happen on their own thread so that a slow
/// network never stalls a frame; the packets they produce are handed to the main thread to process.
/// </summary>
public sealed class Client
{
    /// <summary>How long the server waits before considering a silent client gone.</summary>
    public const float KeepAliveTimeoutSeconds = 35;

    /// <summary>How often a keep alive is sent. Has to stay comfortably below the timeout.</summary>
    private const float KeepAliveIntervalSeconds = 30;

    private readonly Game _game;

    private readonly Lock _writePacketLock = new();
    private readonly Lock _readPacketLock = new();
    private readonly Queue<Packet> _toSendPackets = new();
    private readonly Queue<Packet> _toProcessPackets = new();
    private readonly Thread _packetTransferThread;

    private ClientSession? _session;
    private float _elapsedSecondsSinceKeepAlive;

    public Client(Game game)
    {
        _game = game;

        _packetTransferThread = new Thread(HandlePacketCommunication)
        {
            IsBackground = true,
            Name = "Client packet transfer",
        };
        _packetTransferThread.Start();
    }

    public PlayerSettings GetPlayerSettings() => _session!.PlayerSettings;

    public bool ConnectWith(string host, int port)
    {
        TcpClient tcpClient;
        try
        {
            tcpClient = new TcpClient(host, port);
        }
        catch (Exception e)
        {
            Logger.Error("Failed to connect to " + host + ":" + port + " -> " + e.Message);
            return false;
        }

        NetworkStream netStream = tcpClient.GetStream();
        var serverConnection = new Connection
        {
            Client = tcpClient,
            NetStream = netStream,
            Reader = new BinaryReader(netStream),
            Writer = new BufferedDataStream(new BufferedStream(netStream)),
        };

        var netHandler = new ClientNetHandler(_game);
        var session = new ClientSession(serverConnection, netHandler);
        session.AssignPlayer(_game.ClientPlayer);
        session.OnStateChangedHandler += OnStateChanged;
        netHandler.AssignSession(session);

        // Published last: the transfer thread starts using the session the moment this is visible to it.
        _session = session;

        Logger.Info("Connected to server IP: " + host + " Port: " + port);
        WritePacket(new PlayerJoinRequestPacket("Player" + new Random().Next(100)));
        return true;
    }

    private void HandlePacketCommunication()
    {
        while (_session is null || _session.State == SessionState.Started)
        {
            Thread.Sleep(5);
        }

        while (_session.State != SessionState.Closed)
        {
            Thread.Sleep(5);

            lock (_readPacketLock)
            {
                try
                {
                    while (_session.NetDataAvailable())
                    {
                        Packet packet = _session.ReadPacket();
                        Logger.Packet("Client received packet " + packet);
                        _toProcessPackets.Enqueue(packet);
                    }
                }
                catch (Exception e)
                {
                    // Once the session is closing, the socket being torn down underneath this thread is
                    // how shutdown is meant to look rather than a failure worth reporting.
                    if (_session.State != SessionState.Closed)
                    {
                        Logger.Error("Failed reading packet: " + e.Message);
                    }

                    Stop();
                    break;
                }
            }

            lock (_writePacketLock)
            {
                while (_toSendPackets.Count > 0)
                {
                    Packet toSendPacket = _toSendPackets.Dequeue();
                    Logger.Packet("Client wrote packet " + toSendPacket);
                    _session.WritePacket(toSendPacket);
                }
            }
        }

        Logger.Info("Client packet communication thread terminated.");
    }

    public void Update(float deltaTime)
    {
        if (_session is null || _session.State == SessionState.Closed)
        {
            return;
        }

        CheckForKeepAlive(deltaTime);

        lock (_readPacketLock)
        {
            while (_toProcessPackets.Count > 0)
            {
                _toProcessPackets.Dequeue().Process(_session.NetHandler);
            }
        }
    }

    private void CheckForKeepAlive(float deltaTime)
    {
        _elapsedSecondsSinceKeepAlive += deltaTime;
        if (_elapsedSecondsSinceKeepAlive <= KeepAliveIntervalSeconds)
        {
            return;
        }

        _elapsedSecondsSinceKeepAlive = 0;
        Logger.Packet("Keep alive sent.");
        WritePacket(new PlayerKeepAlivePacket());
    }

    public void WritePacket(Packet packet)
    {
        lock (_writePacketLock)
        {
            _toSendPackets.Enqueue(packet);
        }
    }

    public void Stop()
    {
        if (_session is not null)
        {
            _session.State = SessionState.Closed;
        }
    }

    private static void OnStateChanged(Session.Session session)
    {
        if (session.State == SessionState.Accepted)
        {
            Logger.Info("Client: server accepted my connection.");
        }
        else if (session.State == SessionState.Closed)
        {
            session.Close();
            Logger.Info("Client: my connection with the server closed.");
        }
    }
}

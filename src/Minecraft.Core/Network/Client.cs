using System.Net.Sockets;
using Minecraft.Core.Games;
using Minecraft.Core.IO;
using Minecraft.Core.Logging;
using Minecraft.Core.Network.NetHandler;
using Minecraft.Core.Network.Packets;
using Minecraft.Core.Network.Session;

namespace Minecraft.Core.Network;

public sealed class Client
{
    public const float KeepAliveTimeoutSeconds = 35;

    private const float KeepAliveIntervalSeconds = 30;

    private readonly Game _game;

    private readonly Lock _writePacketLock = new();
    private readonly Lock _readPacketLock = new();
    private readonly Queue<Packet> _toSendPackets = new();
    private readonly Queue<Packet> _toProcessPackets = new();
    private readonly Thread _packetTransferThread;

    private ClientSession? _session;
    private float _elapsedSecondsSinceKeepAlive;
    private volatile bool _isRunning = true;

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

        _session = session;

        Logger.Info("Connected to server IP: " + host + " Port: " + port);
        WritePacket(new PlayerJoinRequestPacket("Player" + new Random().Next(100)));
        return true;
    }

    private void HandlePacketCommunication()
    {
        while (_isRunning && (_session is null || _session.State == SessionState.Started))
        {
            Thread.Sleep(5);
        }

        if (!_isRunning || _session is null)
        {
            Logger.Info("Client packet communication thread terminated before connecting.");
            return;
        }

        while (_isRunning && _session.State != SessionState.Closed)
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
        _isRunning = false;
        _session?.State = SessionState.Closed;
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

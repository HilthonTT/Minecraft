using System.Net.Sockets;
using System.Collections.Concurrent;
using System.Diagnostics;
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

    // Well inside the timeout, so that a keep-alive delayed by a busy link still lands in time.
    private const float KeepAliveIntervalSeconds = 10;

    private readonly Game _game;

    private readonly Lock _writePacketLock = new();
    private readonly Queue<Packet> _toSendPackets = new();
    // Filled by the network thread and drained by the main one. A queue that needs no lock means a large packet
    // still arriving never holds up a frame that is waiting to process the ones already here.
    private readonly ConcurrentQueue<Packet> _toProcessPackets = new();
    private readonly Thread _packetTransferThread;

    private ClientSession? _session;
    // Timed on the network thread with a real clock. Frame time is capped per frame, so a game that freezes for
    // a while would count only a fraction of it and the server would drop a player who had done nothing wrong.
    private readonly Stopwatch _sinceKeepAlive = new();
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

            if (!_sinceKeepAlive.IsRunning || _sinceKeepAlive.Elapsed.TotalSeconds >= KeepAliveIntervalSeconds)
            {
                _sinceKeepAlive.Restart();
                Logger.Packet("Keep alive sent.");
                WritePacket(new PlayerKeepAlivePacket());
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

        while (_toProcessPackets.TryDequeue(out Packet? packet))
        {
            packet.Process(_session.NetHandler);
        }
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

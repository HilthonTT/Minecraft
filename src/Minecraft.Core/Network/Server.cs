using Minecraft.Core.Games;
using Minecraft.Core.IO;
using Minecraft.Core.Logging;
using Minecraft.Core.Network.NetHandler;
using Minecraft.Core.Network.Packets;
using Minecraft.Core.Network.Session;
using Minecraft.Core.Utilities;
using Minecraft.Core.Worlds;
using Minecraft.Core.Worlds.Storage;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace Minecraft.Core.Network;

public sealed class Server
{
    private const string SavesDirectoryName = "saves";

    public static string SavesDirectory => Assets.Path(SavesDirectoryName);

    private readonly Game _game;

    private readonly Lock _newJoinsLock = new();
    private readonly Queue<TcpClient> _joinQueue = new();
    private readonly Queue<ServerSession> _toRemoveClients = new();
    private readonly Dictionary<ServerSession, Stopwatch> _keepAlives = [];

    private Thread? _connectionsThread;
    private TcpListener? _tcpServer;
    private volatile bool _isRunning;
    private int _port;
    private WorldStorage? _storage;

    public List<ServerSession> ConnectedClients { get; } = [];

    public WorldServer World { get; private set; } = null!;

    public bool IsOpenToPublic { get; }

    public int Port => _port;

    public Server(Game game, bool isOpenToPublic)
    {
        _game = game;
        IsOpenToPublic = isOpenToPublic;
    }

    public bool Start(int port)
    {
        _port = port;

        try
        {
            _tcpServer = new TcpListener(IPAddress.Any, _port);
            _tcpServer.Start();
        }
        catch (SocketException e)
        {
            Logger.Error("Failed to listen on port " + _port + " -> " + e.Message);
            _tcpServer = null;
            return false;
        }

        _storage = new WorldStorage(SavesDirectory, _game.WorldName);

        if (_game.FreshWorld)
        {
            _storage.DeleteExistingWorld();
        }

        World = new WorldServer(_game, _storage, _game.WorldSeed, _game.WorldGameMode);

        _isRunning = true;
        _connectionsThread = new Thread(ListenForConnections)
        {
            IsBackground = true,
            Name = "Server connection listener",
        };
        _connectionsThread.Start();

        return true;
    }

    public void Stop()
    {
        _isRunning = false;
        _tcpServer?.Stop();
        _tcpServer = null;

        World?.SaveAndFlush();
        _storage?.Dispose();
        _storage = null;
    }

    private void ListenForConnections()
    {
        Logger.Info("Started listening for connections on port " + _port);

        TcpListener tcpServer = _tcpServer!;

        while (_isRunning)
        {
            TcpClient client;
            try
            {
                client = tcpServer.AcceptTcpClient();
            }
            catch (SocketException)
            {
                break;
            }

            lock (_newJoinsLock)
            {
                _joinQueue.Enqueue(client);
            }

            Logger.Info("Server accepted new client.");
        }

        Logger.Warn("Server is closing down. Closing connections to all clients.");
        ConnectedClients.ForEach(client => client.Close());
        tcpServer.Stop();
        Logger.Info("Server closed.");
    }

    public void Update(float deltaTimeSeconds)
    {
        HandleClientJoin();
        HandleClientLeave();
        CheckForKeepAlive();

        foreach (ServerSession client in ConnectedClients)
        {
            if (client.State == SessionState.Closed)
            {
                continue;
            }

            client.Update(deltaTimeSeconds);

            try
            {
                while (client.NetDataAvailable())
                {
                    Packet packet = client.ReadPacket();
                    Logger.Packet("Server received packet " + packet);
                    packet.Process(client.NetHandler);
                }
            }
            catch (Exception e)
            {
                Logger.Error("Failed handling packet from client: " + e.Message);
                client.State = SessionState.Closed;
            }
        }
    }

    private void HandleClientJoin()
    {
        TcpClient newClient;
        lock (_newJoinsLock)
        {
            if (_joinQueue.Count == 0)
            {
                return;
            }

            newClient = _joinQueue.Dequeue();
        }

        NetworkStream stream = newClient.GetStream();
        var clientConnection = new Connection
        {
            Client = newClient,
            NetStream = stream,
            Reader = new BinaryReader(stream),
            Writer = new BufferedDataStream(new BufferedStream(stream)),
        };

        var netHandler = new ServerNetHandler(_game);
        var session = new ServerSession(clientConnection, netHandler);
        netHandler.AssignSession(session);
        session.OnStateChangedHandler += OnSessionStateChanged;

        ConnectedClients.Add(session);

        var timeoutWatch = new Stopwatch();
        timeoutWatch.Start();
        _keepAlives.Add(session, timeoutWatch);
    }

    private void HandleClientLeave()
    {
        while (_toRemoveClients.Count > 0)
        {
            ServerSession session = _toRemoveClients.Dequeue();
            ConnectedClients.Remove(session);

            try
            {
                session.Close();
            }
            catch (Exception e)
            {
                Logger.Error("Closing client connection failed: " + e.Message);
            }

            _keepAlives.Remove(session);

            if (session.Player is not null)
            {
                World.DespawnEntity(session.Player.ID);
            }
        }
    }

    public void UpdateKeepAliveFor(ServerSession session)
    {
        if (!_keepAlives.TryGetValue(session, out Stopwatch? keepAliveWatch))
        {
            Logger.Warn("Connection had no keep alive stopwatch assigned to it.");
            return;
        }

        Logger.Info("Reset keep alive for " + session.Player?.ID);
        keepAliveWatch.Restart();
    }

    private void CheckForKeepAlive()
    {
        foreach (KeyValuePair<ServerSession, Stopwatch> client in _keepAlives)
        {
            if (client.Value.ElapsedMilliseconds >= Client.KeepAliveTimeoutSeconds * 1000)
            {
                Logger.Warn("Failed to keep connection with " + client.Key.Player?.ID);
                client.Key.State = SessionState.Closed;
            }
        }
    }

    private void OnSessionStateChanged(Session.Session changedSession)
    {
        var session = (ServerSession)changedSession;
        if (session.State == SessionState.Closed)
        {
            Logger.Info("Connection closed with " + session.Player?.ID);
            _toRemoveClients.Enqueue(session);
        }
    }

    public void BroadcastPacket(Packet packet)
    {
        Logger.Packet("Server broadcasting packet [" + packet.GetType() + "]");
        ConnectedClients.ForEach(client => client.WritePacket(packet));
    }

    public void BroadcastPacketExceptTo(Session.Session session, Packet packet)
    {
        Logger.Packet("Server broadcasting packet [" + packet.GetType() + "]");
        foreach (Session.Session client in ConnectedClients)
        {
            if (client != session)
            {
                client.WritePacket(packet);
            }
        }
    }
}

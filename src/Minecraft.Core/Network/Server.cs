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

/// <summary>
/// The authoritative side of the game. Accepting connections blocks, so it runs on its own thread, while
/// everything that touches the world happens on the main thread through <see cref="Update"/>.
/// </summary>
public sealed class Server
{
    /// <summary>Worlds live under this directory, next to the executable.</summary>
    private const string SavesDirectoryName = "saves";

    private readonly Game _game;

    private readonly Lock _newJoinsLock = new();
    private readonly Queue<TcpClient> _joinQueue = new();
    private readonly Queue<ServerSession> _toRemoveClients = new();
    private readonly Dictionary<ServerSession, Stopwatch> _keepAlives = [];

    private Thread? _connectionsThread;
    private TcpListener? _tcpServer;
    private volatile bool _isRunning;
    private int _port;
    private ServerSession? _host;
    private WorldStorage? _storage;

    public List<ServerSession> ConnectedClients { get; } = [];

    public WorldServer World { get; private set; } = null!;

    /// <summary>Whether the server accepts connections from anyone other than the host.</summary>
    public bool IsOpenToPublic { get; }

    public Server(Game game, bool isOpenToPublic)
    {
        _game = game;
        IsOpenToPublic = isOpenToPublic;
    }

    public bool IsHost(Session.Session session) => session == _host;

    public void Start(string address, int port)
    {
        _port = port;

        _storage = new WorldStorage(Assets.Path(SavesDirectoryName), _game.WorldName);
        World = new WorldServer(_game, _storage, _game.WorldSeed);

        _connectionsThread = new Thread(StartServerAndListenForConnections)
        {
            IsBackground = true,
            Name = "Server connection listener",
        };
        _connectionsThread.Start();
    }

    public void Stop()
    {
        _isRunning = false;
        _tcpServer?.Stop();

        // Saved before the storage is torn down, so the last minute of play is not lost.
        World?.SaveAndFlush();
        _storage?.Dispose();
        _storage = null;
    }

    private void StartServerAndListenForConnections()
    {
        _tcpServer = new TcpListener(IPAddress.Any, _port);
        _tcpServer.Start();
        Logger.Info("Started listening for connections on port " + _port);

        _isRunning = true;
        while (_isRunning)
        {
            TcpClient client;
            try
            {
                client = _tcpServer.AcceptTcpClient();
            }
            catch (SocketException)
            {
                // Thrown when the listener is stopped from another thread, which is how shutdown works.
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
        _tcpServer.Stop();
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

        // In a combined client/server run the first connection is the local player, who hosts the world.
        if (_game.RunMode == RunMode.ClientServer && ConnectedClients.Count == 0)
        {
            _host = session;
        }

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

            if (session.Player != null)
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

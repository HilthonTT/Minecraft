using System.Collections.Concurrent;
using System.Net;
using Minecraft.Core.IO;
using Minecraft.Core.Logging;
using Minecraft.Core.Network.NetHandler;
using Minecraft.Core.Network.Packets;
using Minecraft.Core.Worlds.Chunks;

namespace Minecraft.Core.Network.Session;

public sealed class ServerSession : Session
{
    private const int MaxQueuedOutgoingBytes = 64 * 1024 * 1024;

    private const int MaxQueuedIncomingPackets = 4096;

    private readonly ChunkProvider _chunkProvider;
    private readonly EntityTracker _entityTracker;

    private readonly ConcurrentQueue<Packet> _receivedPackets = new();
    private readonly BlockingCollection<byte[]> _outgoing = new();
    private readonly Thread _readerThread;
    private readonly Thread _writerThread;

    private long _queuedOutgoingBytes;
    private volatile bool _connectionFailed;

    public ServerSession(Connection connection, INetHandler netHandler)
        : base(connection, netHandler)
    {
        _chunkProvider = new ChunkProvider(this);
        _entityTracker = new EntityTracker(this);

        IsLocal = connection.Client.Client.RemoteEndPoint is IPEndPoint endPoint && IPAddress.IsLoopback(endPoint.Address);

        _readerThread = new Thread(ReadPackets)
        {
            IsBackground = true,
            Name = "Server session reader",
        };
        _writerThread = new Thread(WritePackets)
        {
            IsBackground = true,
            Name = "Server session writer",
        };
    }

    public int LastContainerSequence { get; set; }

    public bool IsLocal { get; }

    public bool HasConnectionFailed => _connectionFailed;

    public void StartTransfer()
    {
        _readerThread.Start();
        _writerThread.Start();
    }

    public void Update(float deltaTimeSeconds)
    {
        _chunkProvider.Update();
        _entityTracker.Update(deltaTimeSeconds);
    }

    public bool TryTakeReceivedPacket(out Packet packet) => _receivedPackets.TryDequeue(out packet!);

    public void ReleaseWorldPresence()
    {
        if (Player?.World is { } world)
        {
            _chunkProvider.ReleaseAll(world);
        }
    }

    protected override bool SendPacket(Packet packet)
    {
        if (_connectionFailed)
        {
            return false;
        }

        byte[] bytes;
        using (var memory = new MemoryStream())
        {
            var stream = new BufferedDataStream(new BufferedStream(memory));
            packet.WriteToStream(stream);
            if (!stream.Flush())
            {
                return false;
            }

            bytes = memory.ToArray();
        }

        if (Interlocked.Add(ref _queuedOutgoingBytes, bytes.Length) > MaxQueuedOutgoingBytes)
        {
            Logger.Warn("Client " + Player?.ID + " is not keeping up with what is sent to it.");
            _connectionFailed = true;
            return false;
        }

        try
        {
            _outgoing.Add(bytes);
        }
        catch (InvalidOperationException)
        {
            return false;
        }

        return true;
    }

    public override void Close()
    {
        _connectionFailed = true;
        _outgoing.CompleteAdding();
        base.Close();
    }

    private void ReadPackets()
    {
        try
        {
            while (!_connectionFailed)
            {
                Packet packet = ReadPacket();

                if (_receivedPackets.Count >= MaxQueuedIncomingPackets)
                {
                    Logger.Warn("Client " + Player?.ID + " sent more packets than the server can take.");
                    break;
                }

                _receivedPackets.Enqueue(packet);
            }
        }
        catch (Exception e)
        {
            if (!_connectionFailed)
            {
                Logger.Error("Failed reading packet from client: " + e.Message);
            }
        }

        _connectionFailed = true;
    }

    private void WritePackets()
    {
        try
        {
            foreach (byte[] bytes in _outgoing.GetConsumingEnumerable())
            {
                Connection.NetStream.Write(bytes, 0, bytes.Length);
                Interlocked.Add(ref _queuedOutgoingBytes, -bytes.Length);
            }
        }
        catch (Exception e)
        {
            if (!_connectionFailed)
            {
                Logger.Error("Failed writing packet to client: " + e.Message);
            }
        }

        _connectionFailed = true;
    }
}

using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Threading;
using Shard.Bloons;

namespace Shard;


internal enum MessageType : byte
{
    GameState   = 1,   // host → clients
    TowerPlace  = 2,   // client → host
    PlayerMoney = 3,   // host → clients (money update after tower buy)
    GameStart = 4,
    PlayerJoin = 5,
    GameOver = 6,
    PlayerAim = 7,
}


internal struct BloonSnapshot : INetSerializable
{
    public int   Id;
    public float X;
    public float Y;
    public int   Layer;
    public float Progress;   // 0-1 along path
    public bool  IsCamo;
    public bool  IsRegrow;
    public bool isActive;
    public bool isTargetable;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Id);
        writer.Put(X);
        writer.Put(Y);
        writer.Put(Layer);
        writer.Put(Progress);
        writer.Put(IsCamo);
        writer.Put(IsRegrow);
        writer.Put(isActive);
        writer.Put(isTargetable);
    }

    public void Deserialize(NetDataReader reader)
    {
        Id       = reader.GetInt();
        X        = reader.GetFloat();
        Y        = reader.GetFloat();
        Layer    = reader.GetInt();
        Progress = reader.GetFloat();
        IsCamo   = reader.GetBool();
        IsRegrow = reader.GetBool();
        isActive = reader.GetBool();
        isTargetable = reader.GetBool();
    }
}

internal struct TowerSnapshot : INetSerializable
{
    public string TowerType;   // "Monkey", "Dartling", etc.
    public int    X;
    public int    Y;
    public int    OwnerId;
    public float  AimDirectionX;
    public float  AimDirectionY;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(TowerType);
        writer.Put(X);
        writer.Put(Y);
        writer.Put(OwnerId);
        writer.Put(AimDirectionX);
        writer.Put(AimDirectionY);
    }

    public void Deserialize(NetDataReader reader)
    {
        TowerType = reader.GetString();
        X         = reader.GetInt();
        Y         = reader.GetInt();
        OwnerId   = reader.GetInt();
        AimDirectionX = reader.GetFloat();
        AimDirectionY = reader.GetFloat();
    }
}

internal enum ProjectileRenderType : byte
{
    FilledCircle = 1,
    Cross = 2
}

internal struct ProjectileSnapshot : INetSerializable
{
    public float X;
    public float Y;
    public ProjectileRenderType RenderType;
    public int Size;
    public byte R;
    public byte G;
    public byte B;
    public byte A;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(X);
        writer.Put(Y);
        writer.Put((byte)RenderType);
        writer.Put(Size);
        writer.Put(R);
        writer.Put(G);
        writer.Put(B);
        writer.Put(A);
    }

    public void Deserialize(NetDataReader reader)
    {
        X = reader.GetFloat();
        Y = reader.GetFloat();
        RenderType = (ProjectileRenderType)reader.GetByte();
        Size = reader.GetInt();
        R = reader.GetByte();
        G = reader.GetByte();
        B = reader.GetByte();
        A = reader.GetByte();
    }
}

internal struct GameStateMessage : INetSerializable
{
    public int                    Lives;
    public int                    WaveNumber;
    public double                 WaveElapsedTimeMs;
    public List<BloonSnapshot>    Bloons;
    public List<TowerSnapshot>    Towers;
    public List<ProjectileSnapshot> Projectiles;
    public List<(int id, int money)> PlayerMoney;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Lives);
        writer.Put(WaveNumber);
        writer.Put((float)WaveElapsedTimeMs);

        writer.Put(Bloons?.Count ?? 0);
        foreach (var b in Bloons ?? new())
            writer.Put(b);

        writer.Put(Towers?.Count ?? 0);
        foreach (var t in Towers ?? new())
            writer.Put(t);

        writer.Put(Projectiles?.Count ?? 0);
        foreach (var projectile in Projectiles ?? new())
            writer.Put(projectile);

        writer.Put(PlayerMoney?.Count ?? 0);
        foreach (var (id, money) in PlayerMoney ?? new())
        {
            writer.Put(id);
            writer.Put(money);
        }
    }

    public void Deserialize(NetDataReader reader)
    {
        Lives             = reader.GetInt();
        WaveNumber        = reader.GetInt();
        WaveElapsedTimeMs = reader.GetFloat();

        int bloonCount = reader.GetInt();
        Bloons = new(bloonCount);
        for (int i = 0; i < bloonCount; i++)
        {
            var b = new BloonSnapshot();
            b.Deserialize(reader);
            Bloons.Add(b);
        }

        int towerCount = reader.GetInt();
        Towers = new(towerCount);
        for (int i = 0; i < towerCount; i++)
        {
            var t = new TowerSnapshot();
            t.Deserialize(reader);
            Towers.Add(t);
        }

        int projectileCount = reader.GetInt();
        Projectiles = new(projectileCount);
        for (int i = 0; i < projectileCount; i++)
        {
            var projectile = new ProjectileSnapshot();
            projectile.Deserialize(reader);
            Projectiles.Add(projectile);
        }

        int moneyCount = reader.GetInt();
        PlayerMoney = new(moneyCount);
        for (int i = 0; i < moneyCount; i++)
            PlayerMoney.Add((reader.GetInt(), reader.GetInt()));
    }
}

internal struct TowerPlaceMessage : INetSerializable
{
    public string TowerType;
    public int    X;
    public int    Y;
    public int    PlayerId;
    public int    ControllerId;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(TowerType);
        writer.Put(X);
        writer.Put(Y);
        writer.Put(PlayerId);
        writer.Put(ControllerId);
    }

    public void Deserialize(NetDataReader reader)
    {
        TowerType = reader.GetString();
        X         = reader.GetInt();
        Y         = reader.GetInt();
        PlayerId  = reader.GetInt();
        ControllerId = reader.GetInt();
    }
}

internal struct PlayerAimMessage : INetSerializable
{
    public int ControllerId;
    public int X;
    public int Y;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(ControllerId);
        writer.Put(X);
        writer.Put(Y);
    }

    public void Deserialize(NetDataReader reader)
    {
        ControllerId = reader.GetInt();
        X = reader.GetInt();
        Y = reader.GetInt();
    }
}


internal static class Network
{
    private const int StateBroadcastIntervalMs = 16; //tick rate
    private static Player host;
    private static NetManager         serverManager;
    private static NetManager         clientManager;
    private static NetPeer            serverPeer;   // client's connection to host
    private static double             timeSinceLastBroadcastMs;
    public static volatile bool ClientConnected = false;
    public static string ConnectedClientName = string.Empty;
    public static event Action OnGameStarted;
    public static event Action<bool> OnGameOver;
    private static volatile bool running = false;
    private static readonly System.Collections.Concurrent.ConcurrentQueue<Action> mainThreadActions 
        = new System.Collections.Concurrent.ConcurrentQueue<Action>();
    
    // ── Callbacks raised on the game thread via PollEvents ────────────────────
    /// <summary>Raised on the CLIENT when the host sends a state update.</summary>
    public static event Action<GameStateMessage> OnStateReceived;

    /// <summary>Raised on the HOST when a client requests a tower placement.</summary>
    public static event Action<TowerPlaceMessage> OnTowerPlaceRequested;
    public static event Action<PlayerAimMessage> OnPlayerAimUpdated;
    
    public static void sendGameStart()
    {
        if (serverManager == null) return;
        var writer = new NetDataWriter();
        writer.Put((byte)MessageType.GameStart);
        serverManager.SendToAll(writer, DeliveryMethod.ReliableOrdered);
    }
    
    public static void sendGameOver(bool isWin)
    {
        if (serverManager == null) return;
        var writer = new NetDataWriter();
        writer.Put((byte)MessageType.GameOver);
        writer.Put(isWin);
        serverManager.SendToAll(writer, DeliveryMethod.ReliableOrdered);
    }

    // ── Host API ──────────────────────────────────────────────────────────────
    public static void setHost(Player player) => host = player;

    /// <summary>
    /// Called every game frame by the HOST.
    /// Builds a snapshot of the current game state and broadcasts it
    /// to all connected clients every <see cref="StateBroadcastIntervalMs"/> ms.
    /// </summary>
    public static void broadcastState(
        GameStateMessage state,
        double deltaTimeMs)
    {
        if (serverManager == null) return;

        timeSinceLastBroadcastMs += deltaTimeMs;
        if (timeSinceLastBroadcastMs < StateBroadcastIntervalMs) return;
        timeSinceLastBroadcastMs = 0;

        var writer = new NetDataWriter();
        writer.Put((byte)MessageType.GameState);
        writer.Put(state);

        serverManager.SendToAll(writer, DeliveryMethod.ReliableOrdered);
    }

    /// <summary>Polls server events; call once per game frame on the HOST.</summary>
    public static void pollServer()
    {
        serverManager?.PollEvents();
        while (mainThreadActions.TryDequeue(out var action))
            action();
    }


    // ── Client API ────────────────────────────────────────────────────────────
    /// <summary>
    /// Called by a CLIENT when the local player places a tower.
    /// Sends a placement request to the host for validation.
    /// </summary>
    public static void requestTowerPlace(TowerPlaceMessage msg)
    {
        if (serverPeer == null) return;

        var writer = new NetDataWriter();
        writer.Put((byte)MessageType.TowerPlace);
        writer.Put(msg);
        serverPeer.Send(writer, DeliveryMethod.ReliableOrdered);
    }

    public static void sendPlayerAim(PlayerAimMessage msg)
    {
        if (serverPeer == null) return;

        var writer = new NetDataWriter();
        writer.Put((byte)MessageType.PlayerAim);
        writer.Put(msg);
        serverPeer.Send(writer, DeliveryMethod.Unreliable);
    }

    /// <summary>Polls client events; call once per game frame on the CLIENT.</summary>
    public static void pollClient()
    {
        clientManager?.PollEvents();
        while (mainThreadActions.TryDequeue(out var action))
            action();
    }
    // Host a server and listen for clients, also send a message to clients when they connect
    
    
    public static void startServer(int port = 9050)
    {
        running = true;
        var listener = new EventBasedNetListener();
        serverManager = new NetManager(listener);
        serverManager.Start(port);

        listener.ConnectionRequestEvent += request =>
        {
            if (serverManager.ConnectedPeersCount < 8)
                request.AcceptIfKey("SomeConnectionKey");
            else
                request.Reject();
        };

        listener.PeerConnectedEvent += peer =>
        {
            Console.WriteLine($"[Server] Client connected: {peer}");
            ClientConnected = true;
            // Send host info immediately so the client knows who the host is.
            if (host != null)
            {
                var writer = new NetDataWriter();
                writer.Put(host);
                peer.Send(writer, DeliveryMethod.ReliableOrdered);
            }
        };

        listener.NetworkReceiveEvent += (fromPeer, dataReader, deliveryMethod, channel) =>
        {
            var msgType = (MessageType)dataReader.GetByte();

            if (msgType == MessageType.TowerPlace)
            {
                var msg = new TowerPlaceMessage();
                msg.Deserialize(dataReader);
                Console.WriteLine($"[Server] Tower place request: {msg.TowerType} at ({msg.X},{msg.Y}) by player {msg.PlayerId}");

                // Raise on the game thread next time pollServer() is called —
                // LiteNetLib fires these callbacks inside PollEvents(), so this
                // is already synchronised with the game loop.
                //OnTowerPlaceRequested?.Invoke(msg);
                mainThreadActions.Enqueue(() => OnTowerPlaceRequested?.Invoke(msg));
            }else if (msgType == MessageType.PlayerAim)
            {
                var msg = new PlayerAimMessage();
                msg.Deserialize(dataReader);
                mainThreadActions.Enqueue(() => OnPlayerAimUpdated?.Invoke(msg));
            }else if (msgType == MessageType.PlayerJoin)
            {
                ConnectedClientName = dataReader.GetString();
                mainThreadActions.Enqueue(() => ClientConnected = true);
                Console.WriteLine($"[Server] Player joined: {ConnectedClientName}");
            } 

            dataReader.Recycle();
        };

        listener.PeerDisconnectedEvent += (peer, info) =>
            Console.WriteLine($"[Server] Client disconnected: {peer}, reason: {info.Reason}");

        // Keep-alive loop — state broadcasts happen via broadcastState() called
        // from the game loop, so we only need to poll here.
        while (running)
        {
            serverManager.PollEvents();
            Thread.Sleep(15);
        }
        serverManager?.Stop();
        
    }

    public static void connectToServer(string serverIp, int serverPort, string playerName)
    {
        running = true;
        var listener = new EventBasedNetListener();
        clientManager = new NetManager(listener);
        clientManager.Start();
        clientManager.Connect(serverIp, serverPort, "SomeConnectionKey");

        listener.PeerConnectedEvent += peer =>
        {
            Console.WriteLine($"[Client] Connected to host at {serverIp}:{serverPort}");
            serverPeer = peer;

            // Introduce ourselves.
            var writer = new NetDataWriter();
            writer.Put((byte)MessageType.PlayerJoin);   // reuse slot for intro
            writer.Put(playerName);
            peer.Send(writer, DeliveryMethod.ReliableOrdered);
        };

        listener.NetworkReceiveEvent += (fromPeer, dataReader, deliveryMethod, channel) =>
        {
            var msgType = (MessageType)dataReader.GetByte();

            if (msgType == MessageType.GameState)
            {
                var state = new GameStateMessage();
                state.Deserialize(dataReader);
                //OnStateReceived?.Invoke(state);
                mainThreadActions.Enqueue(() => OnStateReceived?.Invoke(state));
            }else if (msgType == MessageType.GameStart)
            {
                //OnGameStarted?.Invoke();
                mainThreadActions.Enqueue(() => OnGameStarted?.Invoke());
            }else if (msgType == MessageType.GameOver)
            {
                bool isWin = dataReader.GetBool();
                mainThreadActions.Enqueue(() => OnGameOver?.Invoke(isWin));
            }

            dataReader.Recycle();
        };

        listener.PeerDisconnectedEvent += (peer, info) =>
        {
            Console.WriteLine($"[Client] Disconnected from host: {info.Reason}");
            serverPeer = null;
        };

        while (running)
        {
            clientManager.PollEvents();
            Thread.Sleep(15);
        }
        clientManager?.Stop();
    }
    
    public static void reset()
    {
        running = false;
        OnGameOver = null;
        Thread.Sleep(20);
        ClientConnected = false;
        ConnectedClientName = string.Empty;

        serverManager?.Stop();
        serverManager = null;

        clientManager?.Stop();
        clientManager = null;

        serverPeer = null;
        timeSinceLastBroadcastMs = 0;

        // Clear all event subscribers so old GameBloons instances don't linger
        OnStateReceived = null;
        OnTowerPlaceRequested = null;
        OnPlayerAimUpdated = null;
        OnGameStarted = null;
    }
    
    
}

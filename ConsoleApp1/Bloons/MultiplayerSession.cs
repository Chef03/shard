namespace Shard;

internal enum MultiplayerRole
{
    Offline,
    Host,
    Join
}

internal readonly struct MultiplayerSession
{
    public MultiplayerRole Role { get; }
    public string PlayerName { get; }
    public string ServerIp { get; }
    public int ServerPort { get; }

    private MultiplayerSession(MultiplayerRole role, string playerName, string serverIp, int serverPort)
    {
        Role = role;
        PlayerName = playerName;
        ServerIp = serverIp;
        ServerPort = serverPort;
    }

    public static MultiplayerSession Offline()
    {
        return new MultiplayerSession(MultiplayerRole.Offline, "Player", "127.0.0.1", 9050);
    }

    public static MultiplayerSession Host(string playerName, int serverPort)
    {
        return new MultiplayerSession(MultiplayerRole.Host, playerName, "0.0.0.0", serverPort);
    }

    public static MultiplayerSession Join(string playerName, string serverIp, int serverPort)
    {
        return new MultiplayerSession(MultiplayerRole.Join, playerName, serverIp, serverPort);
    }
}

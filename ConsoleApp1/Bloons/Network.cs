using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Threading;
using Shard.Bloons;

namespace Shard;

internal static class Network
{
    private static Player host;

    // Host a server and listen for clients, also send a message to clients when they connect
    public static void startServer(int port = 9050)
    {
        var listener = new EventBasedNetListener();
        var server = new NetManager(listener);
        server.Start(port);

        listener.NetworkReceiveEvent += (fromPeer, dataReader, deliveryMethod, channel) =>
        {
            Console.WriteLine("Got a message");
            var data = dataReader.Get(() => new Player());
            Console.WriteLine(data.getName());
            dataReader.Recycle();
        };

        listener.ConnectionRequestEvent += request =>
        {
            if (server.ConnectedPeersCount < 2)
                request.AcceptIfKey("SomeConnectionKey");
            else
                request.Reject();
        };

        listener.PeerConnectedEvent += peer =>
        {
            Console.WriteLine("We got connection: {0}", peer); // Show peer IP
            var writer = new NetDataWriter(); // Create writer class
            if (host != null)
            {
                writer.Put(host);
            }
            peer.Send(writer, DeliveryMethod.ReliableOrdered); // Send with reliability
        };

        while (true)
        {
            server.PollEvents();
            Thread.Sleep(15);
        }
    }

    public static void connectToServer(string serverIp, int serverPort, string playerName)
    {
        var listener = new EventBasedNetListener();
        var client = new NetManager(listener);
        client.Start();
        client.Connect(serverIp, serverPort, "SomeConnectionKey");

        listener.NetworkReceiveEvent += (fromPeer, dataReader, deliveryMethod, channel) =>
        {
            var data = dataReader.Get(() => new Player());
            Console.WriteLine("We got: {0}", data.getName());
            dataReader.Recycle();
        };

        listener.PeerConnectedEvent += peer =>
        {
            Console.WriteLine("Connected to server!, sending message...");
            var server = peer;
            var writer = new NetDataWriter();
            writer.Put(new Player(3424, playerName, false, serverIp + ":" + serverPort));
            server.Send(writer, DeliveryMethod.ReliableOrdered);
        };

        while (true)
        {
            client.PollEvents();
            Thread.Sleep(15);
        }
    }

    public static void client()
    {
        connectToServer("127.0.0.1", 9050, "Client");
    }

    public static void setHost(Player host)
    {
        Network.host = host;
    }
}

using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Threading;
using Shard.Bloons;

namespace Shard;

internal class Network
{

    public string ip;
    private static Player host;
    private Player peer;
    private Map map;

    public Network()
    {
        
    }
    // Host a server and listen for clients, also send a message to clients when they connect
    public static void startServer()
    {
        var listener = new EventBasedNetListener();
        var server = new NetManager(listener);
        server.Start(9050 /* port */);

        listener.NetworkReceiveEvent += (fromPeer, dataReader, deliveryMethod, channel) =>
        {
            //Console.WriteLine(dataReader.GetString(1000));
            Console.WriteLine("Got a message");
            Player data = dataReader.Get(() => new Player());
            Console.WriteLine(data.getName());
            dataReader.Recycle();
        };

        listener.ConnectionRequestEvent += request =>
        {
            if (server.ConnectedPeersCount < 2 /* max connections */)
                request.AcceptIfKey("SomeConnectionKey");
            else
                request.Reject();
        };

        listener.PeerConnectedEvent += peer =>
        {
            Console.WriteLine("We got connection: {0}", peer); // Show peer IP
            var writer = new NetDataWriter(); // Create writer class
            //writer.Put("Hello client"); // Put some string
            if (host != null)
            {
                writer.Put(host);
            }
            peer.Send(writer, DeliveryMethod.ReliableOrdered); // Send with reliability
        };
        
        while (!Console.KeyAvailable)
        {
            server.PollEvents();
            Thread.Sleep(15);
        }

        server.Stop();
    }

    public static void client()
    {
        var listener = new EventBasedNetListener();
        var client = new NetManager(listener);
        client.Start();
        client.Connect("172.20.10.3" /* host IP or name */, 9050 /* port */, "SomeConnectionKey" /* text key or NetDataWriter */);
        listener.NetworkReceiveEvent += (fromPeer, dataReader, deliveryMethod, channel) =>
        {
            Player data = dataReader.Get(() => new Player());
            
            Console.WriteLine("We got: {0}",  data.getName());
            dataReader.Recycle();
        };

        listener.PeerConnectedEvent += (peer) =>
        {
            Console.WriteLine("Connected to server!, sending message...");
            var server = peer;;
            var writer = new NetDataWriter();
            writer.Put(new Player(3424, "jeffrey", true, "432872"));
            
            server.Send(writer, DeliveryMethod.ReliableOrdered);
        };
                
        while (!Console.KeyAvailable)
        {
            client.PollEvents();
            Thread.Sleep(15);
        }

        client.Stop();
    }

    public static void setHost(Player host)
    {
        Network.host = host;
    }
        
}
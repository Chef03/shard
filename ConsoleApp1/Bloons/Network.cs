using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Threading;

namespace Shard;

public class Network
{

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
            Console.WriteLine(dataReader.GetString(1000));
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
            writer.Put("Hello client!"); // Put some string
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
        client.Connect("127.0.0.1" /* host IP or name */, 9050 /* port */, "SomeConnectionKey" /* text key or NetDataWriter */);
        listener.NetworkReceiveEvent += (fromPeer, dataReader, deliveryMethod, channel) =>
        {
            Console.WriteLine("We got: {0}", dataReader.GetString(100 /* max length of string */));
            dataReader.Recycle();
        };

        listener.PeerConnectedEvent += (peer) =>
        {
            Console.WriteLine("Connected to server!, sending message...");
            var server = peer;;
            var writer = new NetDataWriter();


            string test = "hello world";
            writer.Put(test);
            server.Send(writer, DeliveryMethod.ReliableOrdered);
        };
                
        while (!Console.KeyAvailable)
        {
            client.PollEvents();
            Thread.Sleep(15);
        }

        client.Stop();
    }
        
}
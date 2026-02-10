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

}
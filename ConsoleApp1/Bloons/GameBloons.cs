using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Threading;

using static Shard.Network;

namespace Shard;

class GameBloons : Game, InputListener
{
    public override bool isRunning()
    {
        return true;
    }

    public override void update()
    {
        Bootstrap.getDisplay().showText("FPS: " + Bootstrap.getFPS(), 10, 10, 12, 255, 255, 255);
    }

    public override void initialize()
    {
        Bootstrap.getInput().addListener(this);

        Debug.Log("Bing!");
        new Thread(startServer).Start();
            
    }

    public void handleInput(InputEvent inp, string eventType)
    {


    }

    
    

}
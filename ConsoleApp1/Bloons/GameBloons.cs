using LiteNetLib;
using LiteNetLib.Utils;
using SDL;
using System;
using System.Threading;

using static Shard.Network;

namespace Shard;

class GameBloons : Game, InputListener
{

    GameObject background;
    private int mouseX, mouseY;
    private bool mouseLeft, mouseRight;
    private Circle circle = new Circle();
    private bool hold = false;

    private SoundManager soundManager;

    public override bool isRunning()
    {
        return true;
    }

    public override void update()
    {
        Bootstrap.getDisplay().showText("FPS: " + Bootstrap.getFPS(), 10, 10, 12, 255, 255, 255);
        Bootstrap.getDisplay().drawFilledCircle(circle);
        Bootstrap.getDisplay().showText($"Mouse: {mouseX}, {mouseY}", 10, 30, 12, 255, 255, 255);

        string bstate = (mouseLeft ? "L" : "-")  + (mouseRight ? "R" : "-");
        Bootstrap.getDisplay().showText($"Buttons: {bstate}", 10, 50, 12, 255, 255, 255);

        // draw a small cursor at the mouse position
        Bootstrap.getDisplay().drawFilledCircle(mouseX, mouseY, 4, System.Drawing.Color.FromArgb(255, 255, 0));

        if (mouseLeft == true)
        {
            if (mouseX >= circle.X - circle.Radius && mouseX <= circle.X + circle.Radius)
            {
                if (mouseY >= circle.Y - circle.Radius && mouseY <= circle.Y + circle.Radius) hold = true;
            }
        }
        if (mouseLeft == false) hold = false;
        if (hold)
        {
            circle.R = 100;
            circle.X = mouseX;
            circle.Y = mouseY;
        }
        else
        {
            circle.R = 255;
        }

        this.soundManager.drawVolumeSlider();
    }

    public override void initialize()
    {
        Bootstrap.getInput().addListener(this);

        Debug.Log("Bing!");
        //new Thread(startServer).Start();
        circle.X = 300;
        circle.Y = 300;
        circle.Radius = 50;
        circle.R = 255;
        circle.G = 255;
        circle.B = 255;
        circle.A = 255;

        this.soundManager = new SoundManager();
         var volumePercent = Bootstrap.getSound().getVolumePercent();
        Bootstrap.getSound().setVolumePercent(volumePercent);
        
        Bootstrap.getSound().playSound ("Sunshine Serenade.mp3");

    }

    public void handleInput(InputEvent input, string eventType)
    {
        if (Bootstrap.getRunningGame().isRunning() == false)
        {
            return;
        }

        if (eventType == "MouseMotion" || eventType == "MouseDown" || eventType == "MouseUp")
        {
            mouseX = input.X;
            mouseY = input.Y;
        }

        this.soundManager.handleVolumeInput(input, eventType);

        //Debug.Log("eventType = " + eventType);


        //left click = 1
        //right click = 2
        //middle click = 3

        switch (eventType)
        {
            case "MouseMotion":
                Debug.Log("mouseX: " + mouseX + " mouseY: " + mouseY);
                break;
            case "MouseDown":
                Debug.Log(input.ToString());

                if (input.Button == 1) mouseLeft = true;
                else if (input.Button == 3) mouseRight = true;



                break;
            case "MouseUp":
                if (input.Button == 1) mouseLeft = false;
                else if (input.Button == 3) mouseRight = false;
                break;
        }

    }

    
    

}

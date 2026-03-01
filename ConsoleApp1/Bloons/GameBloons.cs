using LiteNetLib;
using LiteNetLib.Utils;
using SDL;
using System;
//using System.Drawing;
using System.IO;
using System.Threading;
//using static System.Net.Mime.MediaTypeNames;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Shard.Bloons;
using System.Collections.Generic; // instead of System.Drawing, Crossplatform 2D graphics API
namespace Shard;


class GameBloons : Game, InputListener
{

    GameObject background;
    private int mouseX, mouseY;
    private bool mouseLeft, mouseRight;
    private bool mouseMiddlePressed;
    private bool prevMouseMiddlePressed;
    private Circle circle = new Circle();
    private bool hold = false;
    private bool isFullscreen = false;
    private int screenWidth = 1920, screenHeight = 1080;
    private Image image;
    private Map monkeyLane;


    private SoundManager soundManager;

    public override bool isRunning()
    {
        return true;
    }

    public override void update()
    {
        Bootstrap.getDisplay().showText("FPS: " + Bootstrap.getFPS(), 10, 10, 12, 255, 255, 255);

        Bootstrap.getDisplay().showText($"Mouse: {mouseX}, {mouseY}", 10, 30, 12, 255, 255, 255);

        string bstate = (mouseLeft ? "L" : "-") + (mouseRight ? "R" : "-");
        Bootstrap.getDisplay().showText($"Buttons: {bstate}", 10, 50, 12, 255, 255, 255);

        Bootstrap.getDisplay().addToDraw(background);

        Bootstrap.getDisplay().drawFilledCircle(circle);
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

        //press and release the middle mouse button to toggle fullscreen
        if (mouseMiddlePressed == false && prevMouseMiddlePressed == true)
        {
            if (isFullscreen)
            {
                Bootstrap.getDisplay().setWindowed(screenWidth, screenHeight);
                isFullscreen = false;
                background.Transform.Scaley = (float)Bootstrap.getDisplay().getHeight() / (float)image.Height;
                background.Transform.Scalex = background.Transform.Scaley;

                Debug.Log(background.Transform.Scalex.ToString());
                Debug.Log(background.Transform.Scaley.ToString());
            }
            else
            {
                Bootstrap.getDisplay().setFullscreen();
                isFullscreen = true;
                background.Transform.Scaley = (float)Bootstrap.getDisplay().getHeight() / (float)image.Height;
                background.Transform.Scalex = background.Transform.Scaley;

                Debug.Log(background.Transform.Scalex.ToString());
                Debug.Log(background.Transform.Scaley.ToString());
            }

        }
        prevMouseMiddlePressed = mouseMiddlePressed;

        this.soundManager.drawVolumeSlider();
    }

    public override void initialize()
    {
        Bootstrap.getInput().addListener(this);
        Bootstrap.getDisplay().setSDLSize(screenWidth, screenHeight);

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

        Bootstrap.getSound().playSound("Sunshine Serenade.mp3");

        background = new GameObject();
        background.Transform.SpritePath = getAssetManager().getAssetPath("Monkey_Lane_1390x1036.png");
        try
        {
            using var stream = File.OpenRead(getAssetManager().getAssetPath("Monkey_Lane_1390x1036.png"));
            image = Image.Load<Rgba32>(stream);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        background.Transform.Scaley = (float)Bootstrap.getDisplay().getHeight() / (float)image.Height;
        background.Transform.Scalex = background.Transform.Scaley;

        Debug.Log(background.Transform.Scalex.ToString());
        Debug.Log(background.Transform.Scaley.ToString());

        // TODO: Only works on 1920x1080 displays for now
        // Not sure why scaling does not match resolution on macbooks

        //Initialize Menu

        //initialze monkeys
        //Monkey dartMonkey = new Monkey();

        //Initialize map 1: Monkey lane
        monkeyLane = initializeMonkeyLane();

    }

    public void handleInput(InputEvent input, string eventType)
    {
        Debug.Log(eventType);
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
                mouseX = input.X;
                mouseY = input.Y;
                Debug.Log("mouseX: " + mouseX + " mouseY: " + mouseY);
                break;
            case "MouseDown":
                mouseX = input.X;
                mouseY = input.Y;
                Debug.Log(input.ToString());
                if (input.Button == 1) mouseLeft = true;
                else if (input.Button == 3) mouseRight = true;
                else if (input.Button == 2) mouseMiddlePressed = true;
                break;
            case "MouseUp":
                mouseX = input.X;
                mouseY = input.Y;
                if (input.Button == 1) mouseLeft = false;
                else if (input.Button == 3) mouseRight = false;
                else if (input.Button == 2) mouseMiddlePressed = false;
                break;
            case "WindowResize":

                break;

        }

    }

    public void handleWindowEvent(WindowEvent windowEvent, string eventType)
    {
        Debug.Log(eventType);
        if (Bootstrap.getRunningGame().isRunning() == false)
        {
            return;
        }

        //if (windowEvent.CloseRequested == true) System.Environment.Exit(0);

        switch (eventType)
        {
            case "WindowResize":

                break;
            case "WindowCloseRequested":
                System.Environment.Exit(0);
                break;
        }
    }

    private Map initializeMonkeyLane()
    {
        List<LPoint> path = new List<LPoint>()
        {
            new LPoint() { x = 0,    y = 635  },
            new LPoint() { x = 345,  y = 622  },
            new LPoint() { x = 361,  y = 475  },
            new LPoint() { x = 588,  y = 453  },
            new LPoint() { x = 600,  y = 1018 },
            new LPoint() { x = 940,  y = 1015 },
            new LPoint() { x = 936,  y = 319  },
            new LPoint() { x = 352,  y = 288  },
            new LPoint() { x = 356,  y = 141  },
            new LPoint() { x = 1125, y = 146  },
            new LPoint() { x = 1137, y = 286  },
            new LPoint() { x = 1334, y = 307  },
            new LPoint() { x = 1334, y = 473  },
            new LPoint() { x = 1141, y = 499  },
            new LPoint() { x = 1117, y = 785  },
            new LPoint() { x = 354,  y = 821  },
            new LPoint() { x = 354,  y = 1080 }
        };

        // Initialize lane
        Lane lane = new Lane(path);

        // Wave 1 - red bloons (layer 1, base speed, no camo, no regrow)
        Map.Wave wave1 = new Map.Wave()
        {
            n = 1,
            Bloons = new List<Bloon>()
            {
                new Bloon("red", 1, 1, false, false),
                new Bloon("red", 1, 1, false, false),
                new Bloon("red", 1, 1, false, false),
                new Bloon("red", 1, 1, false, false),
                new Bloon("red", 1, 1, false, false),
            }
        };

        List<Map.Wave> waves = new List<Map.Wave>();
        waves.Add(wave1);
        // Initialize map
        Map map = new Map(lane, waves);
        return monkeyLane;
    }
}
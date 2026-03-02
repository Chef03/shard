using Shard.Bloons;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;

namespace Shard;

class GameBloons : Game, InputListener
{
    private GameObject background;
    private int mouseX;
    private int mouseY;
    private bool mouseLeft;
    private bool mouseRight;
    private bool mouseMiddlePressed;
    private bool prevMouseMiddlePressed;
    private bool isFullscreen = false;
    private readonly int screenWidth = 1920;
    private readonly int screenHeight = 1080;
    private Image image;
    private Map monkeyLane;
    private Monkey placedTower;
    private readonly List<Bloon> cachedBloons = new List<Bloon>();

    private SoundManager soundManager;

    public override bool isRunning()
    {
        return true;
    }

    public override void update()
    {
        var display = Bootstrap.getDisplay();
        display.showText("FPS: " + Bootstrap.getFPS(), 10, 10, 12, 255, 255, 255);
        display.showText($"Mouse: {mouseX}, {mouseY}", 10, 30, 12, 255, 255, 255);
        display.showText("Left Click: Place Tower", 10, 50, 12, 255, 255, 255);

        string bstate = (mouseLeft ? "L" : "-") + (mouseRight ? "R" : "-");
        display.showText($"Buttons: {bstate}", 10, 70, 12, 255, 255, 255);

        display.addToDraw(background);
        display.drawFilledCircle(mouseX, mouseY, 4, System.Drawing.Color.FromArgb(255, 255, 0));

        updateFullscreenState();

        soundManager.drawVolumeSlider();

        double deltaTimeMs = Bootstrap.getDeltaTime() * 1000;
        updateBloons(monkeyLane, deltaTimeMs);

        if (placedTower != null)
        {
            placedTower.update(cachedBloons, deltaTimeMs);
        }

        renderBloons();
        renderPathPoints(monkeyLane);

        if (placedTower != null)
        {
            placedTower.draw(display);
        }
    }

    public override void initialize()
    {
        Bootstrap.getInput().addListener(this);
        Bootstrap.getDisplay().setSDLSize(screenWidth, screenHeight);

        soundManager = new SoundManager();
        var volumePercent = Bootstrap.getSound().getVolumePercent();
        Bootstrap.getSound().setVolumePercent(volumePercent);
        Bootstrap.getSound().playSound("Sunshine Serenade.mp3");

        background = new GameObject();
        background.Transform.SpritePath = getAssetManager().getAssetPath("Monkey_Lane_1390x1036.png");

        using (var stream = File.OpenRead(getAssetManager().getAssetPath("Monkey_Lane_1390x1036.png")))
        {
            image = Image.Load<Rgba32>(stream);
        }

        updateBackgroundScale();

        // Initialize map 1: Monkey lane
        monkeyLane = initializeMonkeyLane();
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

        soundManager.handleVolumeInput(input, eventType);

        // left click = 1
        // right click = 3
        // middle click = 2
        switch (eventType)
        {
            case "MouseMotion":
                mouseX = input.X;
                mouseY = input.Y;
                break;
            case "MouseDown":
                mouseX = input.X;
                mouseY = input.Y;
                if (input.Button == 1)
                {
                    mouseLeft = true;
                    if (input.Y > 80)
                    {
                        placedTower = new Monkey(new LPoint() { x = input.X, y = input.Y });
                    }
                }
                else if (input.Button == 3)
                {
                    mouseRight = true;
                }
                else if (input.Button == 2)
                {
                    mouseMiddlePressed = true;
                }
                break;
            case "MouseUp":
                mouseX = input.X;
                mouseY = input.Y;
                if (input.Button == 1)
                {
                    mouseLeft = false;
                }
                else if (input.Button == 3)
                {
                    mouseRight = false;
                }
                else if (input.Button == 2)
                {
                    mouseMiddlePressed = false;
                }
                break;
            case "WindowResize":
                break;
        }
    }

    public void handleWindowEvent(WindowEvent windowEvent, string eventType)
    {
        if (Bootstrap.getRunningGame().isRunning() == false)
        {
            return;
        }

        switch (eventType)
        {
            case "WindowResize":
                updateBackgroundScale();
                break;
            case "WindowCloseRequested":
                Environment.Exit(0);
                break;
        }
    }

    private void updateFullscreenState()
    {
        // press and release the middle mouse button to toggle fullscreen
        if (mouseMiddlePressed == false && prevMouseMiddlePressed == true)
        {
            if (isFullscreen)
            {
                Bootstrap.getDisplay().setWindowed(screenWidth, screenHeight);
                isFullscreen = false;
            }
            else
            {
                Bootstrap.getDisplay().setFullscreen();
                isFullscreen = true;
            }

            updateBackgroundScale();
        }

        prevMouseMiddlePressed = mouseMiddlePressed;
    }

    private void updateBackgroundScale()
    {
        background.Transform.Scaley = (float)Bootstrap.getDisplay().getHeight() / image.Height;
        background.Transform.Scalex = background.Transform.Scaley;
    }

    private void updateBloons(Map map, double deltaTimeMs)
    {
        cachedBloons.Clear();

        foreach (Map.Wave wave in map.Waves)
        {
            foreach (Bloon bloon in wave.Bloons)
            {
                bloon.updateBloon(map.Lane.getPath(), deltaTimeMs);
                cachedBloons.Add(bloon);
            }
        }
    }

    // for testing
    public void renderPathPoints(Map map)
    {
        foreach (LPoint point in map.Lane.getPath())
        {
            Bootstrap.getDisplay().drawFilledCircle(point.x, point.y, 5, System.Drawing.Color.FromArgb(255, 0, 255));
        }
    }

    public void renderBloons()
    {
        Display display = Bootstrap.getDisplay();
        foreach (Bloon bloon in cachedBloons)
        {
            if (!bloon.isTargetable())
            {
                continue;
            }

            var position = bloon.getPosition();
            display.drawFilledCircle(position.x, position.y, bloon.getRenderRadius(), bloon.getRenderColor());
        }
    }

    private Map initializeMonkeyLane()
    {
        List<LPoint> path = new List<LPoint>()
        {
            new LPoint() { x = 0,    y = 635  },
            new LPoint() { x = 345,  y = 622  },
            new LPoint() { x = 361,  y = 475  },
            new LPoint() { x = 588,  y = 479  },
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
            new LPoint() { x = 347,  y = 1080 }
        };

        // Initialize lane
        Lane lane = new Lane(path);
        int startX = lane.getPath()[0].x;
        int startY = lane.getPath()[0].y;

        // Wave 1 - red bloons (layer 1, base speed, no camo, no regrow)
        Map.Wave wave1 = new Map.Wave()
        {
            spawnIntervalMs = 1000,
            Bloons = new List<Bloon>()
        };

        for (int i = 1; i < 6; i++)
        {
            wave1.Bloons.Add(new Bloon(BloonColor.Red, 1, 0.5, false, false, startX, startY, spawnDelayMs: i * wave1.spawnIntervalMs));
        }

        List<Map.Wave> waves = new List<Map.Wave>();
        waves.Add(wave1);

        return new Map(lane, waves);
    }

    public override int getTargetFrameRate()
    {
        return 120; // cap at 120 fps
    }
}

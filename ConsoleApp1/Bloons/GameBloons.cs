using Shard.Bloons;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;
using SDL;
using System.Drawing;

namespace Shard;

class GameBloons : Game, InputListener
{
    private const int AspectRatioWidth = 16;
    private const int AspectRatioHeight = 9;
    private const int HudSlotHeight = 92;
    private const int HudSlotGap = 12;
    private const int HudSlotTop = 90;
    private readonly Color hudColor = Color.FromArgb(255, 110, 74, 42);
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
    private SixLabors.ImageSharp.Image image;
    private Map monkeyLane;
    private readonly List<Bloon> cachedBloons = new List<Bloon>();
    private readonly List<Tower> placedTowers = new List<Tower>();
    private readonly List<TowerOption> placeableTowers = new List<TowerOption>();
    private int selectedTowerIndex;

    private SoundManager soundManager;
    private unsafe MIX_Track* track;

    private sealed class TowerOption
    {
        public string Name { get; }
        public Func<LPoint, Tower> CreateTower { get; }

        public TowerOption(string name, Func<LPoint, Tower> createTower)
        {
            Name = name;
            CreateTower = createTower;
        }
    }

    public override bool isRunning()
    {
        return true;
    }

    public override void update()
    {
        var display = Bootstrap.getDisplay();
        var worldScale = getWorldScale();
        var selectedTowerName = getSelectedTowerName();
        display.showText("FPS: " + Bootstrap.getFPS(), 10, 10, 12, 255, 255, 255);
        display.showText($"Mouse: {mouseX}, {mouseY}", 10, 30, 12, 255, 255, 255);
        display.showText($"Selected Tower: {selectedTowerName}", 10, 50, 12, 255, 255, 255);
        display.showText("Left Click HUD: Select | Left Click Map: Place", 10, 70, 12, 255, 255, 255);

        string bstate = (mouseLeft ? "L" : "-") + (mouseRight ? "R" : "-");
        display.showText($"Buttons: {bstate}", 10, 90, 12, 255, 255, 255);

        display.addToDraw(background);
        drawRightSection(display);
        display.drawFilledCircle(mouseX, mouseY, 4, System.Drawing.Color.FromArgb(255, 255, 0));

        updateFullscreenState();

        soundManager.drawVolumeSlider();

        double deltaTimeMs = Bootstrap.getDeltaTime() * 1000;
        updateBloons(monkeyLane, deltaTimeMs);
        var pointerWorldPosition = toWorldPoint(mouseX, mouseY, worldScale);

        foreach (var tower in placedTowers)
        {
            tower.update(cachedBloons, deltaTimeMs, pointerWorldPosition);
        }

        renderBloons(worldScale);
        renderPathPoints(monkeyLane, worldScale);

        foreach (var tower in placedTowers)
        {
            tower.draw(display, worldScale, background.Transform.X, background.Transform.Y);
        }
    }

    public override void initialize()
    {
        Bootstrap.getInput().addListener(this);
        Bootstrap.getDisplay().setSDLSize(screenWidth, screenHeight);

        soundManager = new SoundManager();
        
        unsafe
        {
             var track = Bootstrap.getSound().playSound ("Sunshine Serenade.mp3", true, 10, 10);
             Bootstrap.getSound().setVolumePercent(track, 1);
             Console.WriteLine("Track: " + track->ToString());
             this.track = track;
        }
        
        background = new GameObject();
        background.Transform.SpritePath = getAssetManager().getAssetPath("Monkey_Lane_1390x1036.png");

        using (var stream = File.OpenRead(getAssetManager().getAssetPath("Monkey_Lane_1390x1036.png")))
        {
            image = SixLabors.ImageSharp.Image.Load<Rgba32>(stream);
        }

        updateBackgroundScale();

        // Only works on 1920x1080 displays for now

        placeableTowers.Add(new TowerOption("Monkey", position => new Monkey(position)));
        placeableTowers.Add(new TowerOption("Dartling", position => new Dartling(position)));
        placeableTowers.Add(new TowerOption("Bomb Shooter", position => new BombShooter(position)));
        placeableTowers.Add(new TowerOption("Tack Shooter", position => new TackShooter(position)));
        selectedTowerIndex = 0;

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

        var width = (float) Bootstrap.getDisplay().getWidth();
        var val = mouseX / width;
        
        unsafe
        {
            Bootstrap.getSound().pan(this.track ,100 - (val * 100),  val * 100);
            this.soundManager.handleVolumeInput(this.track, input, eventType);
        }

        //Debug.Log("eventType = " + eventType);


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
                    if (trySelectTowerFromHud(input.X, input.Y))
                    {
                        break;
                    }

                    if (isScreenPointOnMap(input.X, input.Y))
                    {
                        var selectedTower = getSelectedTowerOption();
                        if (selectedTower != null)
                        {
                            var worldPosition = toWorldPoint(input.X, input.Y, getWorldScale());
                            placedTowers.Add(selectedTower.CreateTower(worldPosition));
                        }
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
                applyAspectRatio(windowEvent.Width, windowEvent.Height);
                updateBackgroundScale();
                break;
            case "WindowEnterFullscreen":
                isFullscreen = true;
                break;
            case "WindowLeaveFullscreen":
                isFullscreen = false;
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
        var display = Bootstrap.getDisplay();
        var widthScale = (float)display.getWidth() / image.Width;
        var heightScale = (float)display.getHeight() / image.Height;
        var uniformScale = MathF.Min(widthScale, heightScale);
        background.Transform.Scalex = uniformScale;
        background.Transform.Scaley = uniformScale;
    }

    private void applyAspectRatio(int requestedWidth, int requestedHeight)
    {
        if (requestedWidth <= 0 || requestedHeight <= 0)
        {
            return;
        }

        var display = Bootstrap.getDisplay();

        if (isFullscreen)
        {
            display.setSize(requestedWidth, requestedHeight);
            return;
        }

        var adjustedWidth = requestedWidth;
        var adjustedHeight = requestedHeight;

        if (requestedWidth * AspectRatioHeight > requestedHeight * AspectRatioWidth)
        {
            adjustedWidth = (requestedHeight * AspectRatioWidth) / AspectRatioHeight;
        }
        else
        {
            adjustedHeight = (requestedWidth * AspectRatioHeight) / AspectRatioWidth;
        }

        if (adjustedWidth != requestedWidth || adjustedHeight != requestedHeight)
        {
            display.setWindowed(adjustedWidth, adjustedHeight);
            return;
        }

        display.setSize(adjustedWidth, adjustedHeight);
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

    private void drawRightSection(Display display)
    {
        var sectionStartX = getHudStartX(display);
        var sectionEndX = display.getWidth();
        var sectionHeight = display.getHeight();

        if (sectionStartX >= sectionEndX)
        {
            return;
        }

        for (var y = 0; y < sectionHeight; y += 1)
        {
            display.drawLine(
                sectionStartX,
                y,
                sectionEndX,
                y,
                this.hudColor.R,
                this.hudColor.G,
                this.hudColor.B,
                this.hudColor.A);
        }

        drawTowerPalette(display, sectionStartX, sectionEndX);
    }

    private void drawTowerPalette(Display display, int sectionStartX, int sectionEndX)
    {
        if (placeableTowers.Count == 0)
        {
            return;
        }

        var sectionWidth = sectionEndX - sectionStartX;
        if (sectionWidth <= 0)
        {
            return;
        }

        var slotPadding = 14;
        var slotX = sectionStartX + slotPadding;
        var slotWidth = Math.Max(24, sectionWidth - (slotPadding * 2));

        display.showText("TOWERS", slotX, 26, 26, 245, 245, 245);
        display.showText("Select one, then click map", slotX, 56, 12, 235, 235, 235);

        for (var i = 0; i < placeableTowers.Count; i++)
        {
            var slotY = HudSlotTop + (i * (HudSlotHeight + HudSlotGap));
            var isSelected = i == selectedTowerIndex;
            var slotColor = isSelected
                ? Color.FromArgb(255, 220, 176, 120)
                : Color.FromArgb(255, 155, 103, 65);

            drawFilledRect(display, slotX, slotY, slotWidth, HudSlotHeight, slotColor);
            drawRectOutline(display, slotX, slotY, slotWidth, HudSlotHeight, Color.FromArgb(255, 245, 236, 223));

            display.showText(placeableTowers[i].Name, slotX + 14, slotY + 16, 17, 255, 255, 255);
            display.showText(isSelected ? "Selected" : "Click to select", slotX + 14, slotY + 44, 12, 255, 255, 255);
        }
    }

    private void drawFilledRect(Display display, int x, int y, int width, int height, Color color)
    {
        if (width <= 0 || height <= 0)
        {
            return;
        }

        for (var row = 0; row < height; row++)
        {
            display.drawLine(x, y + row, x + width, y + row, color.R, color.G, color.B, color.A);
        }
    }

    private void drawRectOutline(Display display, int x, int y, int width, int height, Color color)
    {
        if (width <= 1 || height <= 1)
        {
            return;
        }

        display.drawLine(x, y, x + width, y, color.R, color.G, color.B, color.A);
        display.drawLine(x, y + height, x + width, y + height, color.R, color.G, color.B, color.A);
        display.drawLine(x, y, x, y + height, color.R, color.G, color.B, color.A);
        display.drawLine(x + width, y, x + width, y + height, color.R, color.G, color.B, color.A);
    }

    private int getHudStartX(Display display)
    {
        if (image == null)
        {
            return display.getWidth();
        }

        return (int)MathF.Ceiling(background.Transform.X + (image.Width * background.Transform.Scalex));
    }

    private bool isScreenPointOnMap(int screenX, int screenY)
    {
        var display = Bootstrap.getDisplay();
        var hudStartX = getHudStartX(display);
        return screenX >= 0 && screenY >= 0 && screenY < display.getHeight() && screenX < hudStartX;
    }

    private bool trySelectTowerFromHud(int screenX, int screenY)
    {
        var display = Bootstrap.getDisplay();
        var hudStartX = getHudStartX(display);
        if (screenX < hudStartX || placeableTowers.Count == 0)
        {
            return false;
        }

        var sectionWidth = display.getWidth() - hudStartX;
        var slotPadding = 14;
        var slotX = hudStartX + slotPadding;
        var slotWidth = Math.Max(24, sectionWidth - (slotPadding * 2));

        if (screenX < slotX || screenX > slotX + slotWidth)
        {
            return true;
        }

        for (var i = 0; i < placeableTowers.Count; i++)
        {
            var slotY = HudSlotTop + (i * (HudSlotHeight + HudSlotGap));
            if (screenY >= slotY && screenY <= slotY + HudSlotHeight)
            {
                selectedTowerIndex = i;
                return true;
            }
        }

        return true;
    }

    private TowerOption getSelectedTowerOption()
    {
        if (placeableTowers.Count == 0)
        {
            return null;
        }

        if (selectedTowerIndex < 0 || selectedTowerIndex >= placeableTowers.Count)
        {
            selectedTowerIndex = 0;
        }

        return placeableTowers[selectedTowerIndex];
    }

    private string getSelectedTowerName()
    {
        var selectedTower = getSelectedTowerOption();
        if (selectedTower == null)
        {
            return "None";
        }

        return selectedTower.Name;
    }

    // for testing
    public void renderPathPoints(Map map, float worldScale)
    {
        foreach (LPoint point in map.Lane.getPath())
        {
            var screenPoint = toScreenPoint(point, worldScale);
            var radius = Math.Max(1, (int)MathF.Round(5 * worldScale));
            Bootstrap.getDisplay().drawFilledCircle(screenPoint.x, screenPoint.y, radius, System.Drawing.Color.FromArgb(255, 0, 255));
        }
    }

    public void renderBloons(float worldScale)
    {
        Display display = Bootstrap.getDisplay();
        foreach (Bloon bloon in cachedBloons)
        {
            if (!bloon.isTargetable())
            {
                continue;
            }

            var position = bloon.getPosition();
            var screenPoint = toScreenPoint(position, worldScale);
            var radius = Math.Max(1, (int)MathF.Round(bloon.getRenderRadius() * worldScale));
            display.drawFilledCircle(screenPoint.x, screenPoint.y, radius, bloon.getRenderColor());
        }
    }

    private float getWorldScale()
    {
        if (image == null || image.Height == 0)
        {
            return 1.0f;
        }

        var baselineBackgroundScale = (float)screenHeight / image.Height;
        if (baselineBackgroundScale <= 0)
        {
            return 1.0f;
        }

        return background.Transform.Scaley / baselineBackgroundScale;
    }

    private LPoint toScreenPoint(LPoint worldPoint, float worldScale)
    {
        var xOffset = background.Transform.X;
        var yOffset = background.Transform.Y;
        return new LPoint()
        {
            x = (int)MathF.Round(xOffset + (worldPoint.x * worldScale)),
            y = (int)MathF.Round(yOffset + (worldPoint.y * worldScale))
        };
    }

    private LPoint toWorldPoint(int screenX, int screenY, float worldScale)
    {
        if (worldScale <= 0)
        {
            return new LPoint() { x = screenX, y = screenY };
        }

        var xOffset = background.Transform.X;
        var yOffset = background.Transform.Y;
        return new LPoint()
        {
            x = (int)MathF.Round((screenX - xOffset) / worldScale),
            y = (int)MathF.Round((screenY - yOffset) / worldScale)
        };
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
            wave1.Bloons.Add(new Bloon( 3, 0.5, false, false, startX, startY, spawnDelayMs: i * wave1.spawnIntervalMs));
        }

        // Wave 2 - yellow bloons (layer 4)
        Map.Wave wave2 = new Map.Wave()
        {
            spawnIntervalMs = 500,
            Bloons = new List<Bloon>()
        };

        var wave1DurationMs = 5 * wave1.spawnIntervalMs;
        var wave2StartDelayMs = wave1DurationMs + 1000;
        for (int i = 1; i <= 20; i++)
        {
            wave2.Bloons.Add(new Bloon(4, 0.5, false, false, startX, startY, spawnDelayMs: wave2StartDelayMs + (i * wave2.spawnIntervalMs)));
        }

        List<Map.Wave> waves = new List<Map.Wave>();
        waves.Add(wave1);
        waves.Add(wave2);

        return new Map(lane, waves);
    }

    public override int getTargetFrameRate()
    {
        return 120; // cap at 120 fps
    }
}

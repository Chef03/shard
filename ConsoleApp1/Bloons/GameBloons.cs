using SDL;
using Shard.Bloons;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using static Shard.Bloons.Map;

namespace Shard;

class GameBloons : Game, InputListener
{
    private const int HostControllerId = 0;
    private const int ClientControllerId = 1;
    private const int AspectRatioWidth = 16;
    private const int AspectRatioHeight = 9;
    private const int DesignWidth = 1920;
    private const int DesignHeight = 1080;
    private const int HudBaseSlotHeight = 92;
    private const int HudBaseSlotGap = 12;
    private const int HudBaseSlotTop = 90;
    private readonly Color hudColor = Color.FromArgb(255, 110, 74, 42);
    private readonly MultiplayerSession multiplayerSession;
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
    private readonly List<PlacedTower> placedTowers = new List<PlacedTower>();
    private readonly List<TowerOption> placeableTowers = new List<TowerOption>();
    private int selectedTowerIndex;
    private List<Player> players = new List<Player>();
    private int currentPlayerID = 0; 
    private bool gameover = false;
    private int currentWaveNumber = 0;
    private Map.Wave currentWave;
    private bool gameWin = false;
    private double waveElapsedTimeMs = 0;
    private int lives = 100;
    private Dictionary<string, TowerOption> towerOptionByTypeName;
    private List<BloonSnapshot> receivedBloonSnapshots = new();
    private List<ProjectileSnapshot> receivedProjectileSnapshots = new();
    private bool waitingForPlayer = false;
    private bool showEndScreen = false;
    private bool gameStarted = false;
    private bool startButtonHovered = false;
    private readonly ScoreBoardKey winningTimeBoard = new("bloons", "monkey-lane");
    private DateTimeOffset matchStartedAtUtc;
    private bool winningTimeStored;
    private readonly Dictionary<int, LPoint> latestPointerByControllerId = new Dictionary<int, LPoint>();
    
    private SoundManager soundManager;
    private unsafe MIX_Track* track;

    public GameBloons() : this(MultiplayerSession.Offline())
    {
    }

    public GameBloons(MultiplayerSession multiplayerSession)
    {
        this.multiplayerSession = multiplayerSession;
    }

    private sealed class TowerOption
    {
        public string Name { get; }
        public Func<LPoint,Tower> CreateTower { get; }

        public int getCost()
        {
            var tempTower = CreateTower(new LPoint() { x = 0, y = 0 });
            return tempTower.getCost();

        }

        public TowerOption(string name, Func<LPoint, Tower> createTower)
        {
            Name = name;
            CreateTower = createTower;
        }
    }

    private sealed class PlacedTower
    {
        public Tower Tower { get; }
        public int ControllerId { get; }

        public PlacedTower(Tower tower, int controllerId)
        {
            Tower = tower;
            ControllerId = controllerId;
        }
    }

    private readonly struct TowerPaletteLayout
    {
        public int SlotX { get; }
        public int SlotWidth { get; }
        public int SlotHeight { get; }
        public int SlotGap { get; }
        public int SlotTop { get; }
        public int TitleY { get; }
        public int SubtitleY { get; }
        public int TitleSize { get; }
        public int SubtitleSize { get; }
        public int NameTextSize { get; }
        public int StateTextSize { get; }
        public int NameOffsetX { get; }
        public int NameOffsetY { get; }
        public int StateOffsetY { get; }

        public TowerPaletteLayout(
            int slotX,
            int slotWidth,
            int slotHeight,
            int slotGap,
            int slotTop,
            int titleY,
            int subtitleY,
            int titleSize,
            int subtitleSize,
            int nameTextSize,
            int stateTextSize,
            int nameOffsetX,
            int nameOffsetY,
            int stateOffsetY)
        {
            SlotX = slotX;
            SlotWidth = slotWidth;
            SlotHeight = slotHeight;
            SlotGap = slotGap;
            SlotTop = slotTop;
            TitleY = titleY;
            SubtitleY = subtitleY;
            TitleSize = titleSize;
            SubtitleSize = subtitleSize;
            NameTextSize = nameTextSize;
            StateTextSize = stateTextSize;
            NameOffsetX = nameOffsetX;
            NameOffsetY = nameOffsetY;
            StateOffsetY = stateOffsetY;
        }
    }

    public override bool isRunning()
    {
        return true;
    }

    public override void update()
    {
        if (showEndScreen)
        {
            stopBackgroundMusic();
            Network.pollClient();
            drawEndScreen();
            return;
        }
        var display = Bootstrap.getDisplay();
        // Show waiting screen until a client connects
        if (waitingForPlayer || (multiplayerSession.Role == MultiplayerRole.Join && !gameStarted))
        {
            drawWaitingScreen();
            return;
        }
        var worldScale = getWorldScale();
        var selectedTowerName = getSelectedTowerName();
        display.showText("FPS: " + Bootstrap.getFPS(), 10, 10, 12, 255, 255, 255);
        display.showText($"Mouse: {mouseX}, {mouseY}", 10, 30, 12, 255, 255, 255);
        display.showText($"Selected Tower: {selectedTowerName}", 10, 50, 12, 255, 255, 255);
        display.showText("Left Click HUD: Select | Left Click Map: Place", 10, 70, 12, 255, 255, 255);

        string moneyText = $"$ {players[currentPlayerID].getMoney()}";
        //string livesText = $"<3 {players[currentPlayerID].getLives()}";
        string livesText = $"Lives: {lives}";
        string waveText = $"Wave: {currentWaveNumber + 1}/{monkeyLane.Waves.Count}";
        int centerX = display.getWidth() / 2;
        display.showText(moneyText, centerX - 60, 10, 16, 255, 215, 0);   // gold
        display.showText(livesText, centerX + 20, 10, 16, 255, 50, 50);  // red
        display.showText(waveText, centerX + 150, 10, 16, 255, 255, 255);  // red

        string bstate = (mouseLeft ? "L" : "-") + (mouseRight ? "R" : "-");
        display.showText($"Buttons: {bstate}", 10, 90, 12, 255, 255, 255);

        display.addToDraw(background);
        drawRightSection(display);
        display.drawFilledCircle(mouseX, mouseY, 4, System.Drawing.Color.FromArgb(255, 255, 0));

        updateFullscreenState();

        soundManager.drawVolumeSlider();

        double deltaTimeMs = Bootstrap.getDeltaTime() * 1000;
        if (multiplayerSession.Role is MultiplayerRole.Host or MultiplayerRole.Offline)
        {
            
            spawnBloons(monkeyLane, deltaTimeMs, currentWaveNumber);
        }

        var pointerWorldPosition = toWorldPoint(mouseX, mouseY, worldScale);
        latestPointerByControllerId[getLocalControllerId()] = pointerWorldPosition;

        if (multiplayerSession.Role == MultiplayerRole.Join)
        {
            Network.sendPlayerAim(new PlayerAimMessage
            {
                ControllerId = getLocalControllerId(),
                X = pointerWorldPosition.x,
                Y = pointerWorldPosition.y,
            });
        }

        if (multiplayerSession.Role is MultiplayerRole.Host or MultiplayerRole.Offline)
        {
            var localPlayer = players[currentPlayerID];
            foreach (var placedTower in placedTowers)
            {
                var towerPointer = getPointerForController(placedTower.ControllerId, pointerWorldPosition);
                placedTower.Tower.update(cachedBloons, deltaTimeMs, towerPointer, localPlayer);
            }
        }

        if (multiplayerSession.Role is MultiplayerRole.Host or MultiplayerRole.Offline)
        {
            renderBloons(worldScale);
        }
        renderPathPoints(monkeyLane, worldScale);

        foreach (var placedTower in placedTowers)
        {
            placedTower.Tower.draw(display, worldScale, background.Transform.X, background.Transform.Y);
        }

        if (multiplayerSession.Role == MultiplayerRole.Join)
        {
            renderProjectileSnapshots(worldScale);
        }

        foreach (Player player in players)
        {
            if(lives <= 0)
            {
                gameover = true;
                showEndScreen = true;
                //Debug.Log("You lost all your lives! Game over!");
                if (multiplayerSession.Role == MultiplayerRole.Host)
                    Network.sendGameOver(false);
            }
        }

        if (gameWin)
        {
            if (!showEndScreen)
            {
                if (multiplayerSession.Role is MultiplayerRole.Host or MultiplayerRole.Offline)
                {
                    storeWinningTime();
                }

                showEndScreen = true;
                if (multiplayerSession.Role == MultiplayerRole.Host)
                    Network.sendGameOver(true);
                Debug.Log("You survived all the waves! You win!");
            }
        }
        
        if (multiplayerSession.Role == MultiplayerRole.Host)
        {
            Network.pollServer();

            var bloonSnaps = cachedBloons.ConvertAll(b => new BloonSnapshot
            {
                X        = b.getPosition().x,
                Y        = b.getPosition().y,
                Layer    = b.getLayer(),
                Progress = b.getProgress(),
                IsCamo   = b.getIsCamo(),
                IsRegrow = b.getIsRegrow(),
                isActive = b.getActive(),
                isTargetable = b.getIsTargetable(),
            });

            var towerSnaps = placedTowers.ConvertAll(t => t.Tower.createSnapshot(t.ControllerId));
            var projectileSnaps = new List<ProjectileSnapshot>();
            foreach (var placedTower in placedTowers)
            {
                projectileSnaps.AddRange(placedTower.Tower.getProjectileSnapshots());
            }

            var moneySnaps = players.ConvertAll(p => (p.getId(), p.getMoney()));

            Network.broadcastState(new GameStateMessage
            {
                Lives             = lives,
                WaveNumber        = currentWaveNumber,
                WaveElapsedTimeMs = waveElapsedTimeMs,
                Bloons            = bloonSnaps,
                Towers            = towerSnaps,
                Projectiles       = projectileSnaps,
                PlayerMoney       = moneySnaps,
            }, Bootstrap.getDeltaTime() * 1000);
        }
        else if (multiplayerSession.Role == MultiplayerRole.Join)
        {
            Network.pollClient();   // fires OnStateReceived callbacks
            renderBloonSnapshots(worldScale);
        }
    }
    private void renderBloonSnapshots(float worldScale)
    {
        
        var display = Bootstrap.getDisplay();
        foreach (var snap in receivedBloonSnapshots)
        {
            if (!snap.isActive || !snap.isTargetable)
            {
                continue;
            }
            var screenPoint = toScreenPoint(new LPoint { x = (int)snap.X, y = (int)snap.Y }, worldScale);
            // Reuse the same radius/color logic as Bloon, keyed on layer
            var radius = Math.Max(1, (int)MathF.Round((30 + snap.Layer * 1) * worldScale));
            var color = snap.Layer switch
            {
                
                1 => Color.FromArgb(255, 0,   0),
                2 => Color.FromArgb(0,   0,   255),
                3 => Color.FromArgb(0,   200, 0),
                4 => Color.FromArgb(255, 220, 0),
                5 => Color.FromArgb(255, 105, 180),
                6 => Color.FromArgb(255, 255, 255),
                7 => Color.FromArgb(0,0,0),
                _ => Color.FromArgb(100,100,100)
            };
            if(snap.Layer > 0) display.drawFilledCircle(screenPoint.x, screenPoint.y, radius, color);
        }
    }

    private void renderProjectileSnapshots(float worldScale)
    {
        var display = Bootstrap.getDisplay();
        foreach (var snapshot in receivedProjectileSnapshots)
        {
            var screenPoint = toScreenPoint(new LPoint { x = (int)snapshot.X, y = (int)snapshot.Y }, worldScale);
            if (snapshot.RenderType == ProjectileRenderType.FilledCircle)
            {
                var radius = Math.Max(1, (int)MathF.Round(snapshot.Size * worldScale));
                display.drawFilledCircle(screenPoint.x, screenPoint.y, radius, Color.FromArgb(snapshot.A, snapshot.R, snapshot.G, snapshot.B));
                continue;
            }

            var halfLength = Math.Max(1, (int)MathF.Round(snapshot.Size * worldScale));
            display.drawLine(
                screenPoint.x - halfLength,
                screenPoint.y,
                screenPoint.x + halfLength,
                screenPoint.y,
                snapshot.R,
                snapshot.G,
                snapshot.B,
                snapshot.A);
            display.drawLine(
                screenPoint.x,
                screenPoint.y - halfLength,
                screenPoint.x,
                screenPoint.y + halfLength,
                snapshot.R,
                snapshot.G,
                snapshot.B,
                snapshot.A);
        }
    }

    public override void initialize()
    {
        Bootstrap.getInput().addListener(this);
        Bootstrap.getDisplay().setSDLSize(screenWidth, screenHeight);

        soundManager = new SoundManager();
        initializeMultiplayerSession();
        // HOST: when a client requests a tower, validate and place it server-side.
        Network.OnTowerPlaceRequested += OnClientTowerPlaceRequested;
        Network.OnPlayerAimUpdated += OnClientAimUpdated;

        // CLIENT: when the host sends a state update, apply it locally.
        Network.OnStateReceived += ApplyStateFromHost;
        
        unsafe
        {
             track = Bootstrap.getSound().playSound("Sunshine Serenade.mp3", true, 10, 10);
             Bootstrap.getSound().setVolumePercent(track, soundManager.getVolumePercent());
        }
        
        background = new GameObject();
        background.Transform.SpritePath = getAssetManager().getAssetPath("Monkey_Lane_1390x1036.png");

        using (var stream = File.OpenRead(getAssetManager().getAssetPath("Monkey_Lane_1390x1036.png")))
        {
            image = SixLabors.ImageSharp.Image.Load<Rgba32>(stream);
        }

        updateBackgroundScale();

        // Only works on 1920x1080 displays for now
        placeableTowers.Add(new TowerOption("Dart Monkey", position => new Monkey(position)));
        placeableTowers.Add(new TowerOption("Dartling", position => new Dartling(position)));
        placeableTowers.Add(new TowerOption("Bomb Shooter", position => new BombShooter(position)));
        placeableTowers.Add(new TowerOption("Tack Shooter", position => new TackShooter(position)));
        placeableTowers.Add(new TowerOption("Super Monkey", position => new SuperMonkey(position)));
        selectedTowerIndex = -1;
        
        towerOptionByTypeName = new Dictionary<string, TowerOption>
        {
            { "Monkey",       placeableTowers[0] },
            { "Dartling",     placeableTowers[1] },
            { "BombShooter",  placeableTowers[2] },
            { "TackShooter",  placeableTowers[3] },
            { "SuperMonkey",  placeableTowers[4] },
        };

        // Initialize map 1: Monkey lane
        monkeyLane = initializeMonkeyLane();

        players.Add(new Player(0, multiplayerSession.PlayerName, multiplayerSession.Role == MultiplayerRole.Host, multiplayerSession.ServerIp));

        if (multiplayerSession.Role == MultiplayerRole.Offline)
        {
            startMatchTimer();
        }
    }

    private void initializeMultiplayerSession()
    {
        if (multiplayerSession.Role == MultiplayerRole.Host)
        {
            Network.setHost(new Player(0, multiplayerSession.PlayerName, true, multiplayerSession.ServerIp));
            startBackgroundThread(() => Network.startServer(multiplayerSession.ServerPort));
            waitingForPlayer = true;
            return;
        }

        if (multiplayerSession.Role == MultiplayerRole.Join)
        {
            Network.OnGameStarted += () =>
            {
                gameStarted = true;
                startMatchTimer();
            };
            Network.OnGameOver += (isWin) =>
            {
                gameWin = isWin;
                gameover = !isWin;
                showEndScreen = true;
            };
            startBackgroundThread(() => Network.connectToServer(multiplayerSession.ServerIp, multiplayerSession.ServerPort, multiplayerSession.PlayerName));
        }
    }

    private static void startBackgroundThread(ThreadStart threadStart)
    {
        var thread = new Thread(threadStart)
        {
            IsBackground = true
        };
        thread.Start();
    }

    private void startMatchTimer()
    {
        if (matchStartedAtUtc != default)
        {
            return;
        }

        matchStartedAtUtc = DateTimeOffset.UtcNow;
        winningTimeStored = false;
    }

    private void storeWinningTime()
    {
        if (winningTimeStored)
        {
            return;
        }

        if (matchStartedAtUtc == default)
        {
            startMatchTimer();
        }

        var elapsed = DateTimeOffset.UtcNow - matchStartedAtUtc;
        var scoreManager = Bootstrap.getScoreManager();
        scoreManager.RecordWinningTime(winningTimeBoard, elapsed);
        scoreManager.Save();
        winningTimeStored = true;
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
                            var cost = selectedTower.getCost();
                            if (players[currentPlayerID].getMoney() >= cost)
                            {
                                if (multiplayerSession.Role is MultiplayerRole.Host or MultiplayerRole.Offline)
                                {
                                    // Host places locally; it will broadcast to clients on next state tick.
                                    players[currentPlayerID].removeMoney(cost);
                                    placedTowers.Add(new PlacedTower(selectedTower.CreateTower(worldPosition), getLocalControllerId()));
                                    selectedTowerIndex = -1;
                                }
                                else
                                {
                                    // Client asks the host to validate and place.
                                    Network.requestTowerPlace(new TowerPlaceMessage
                                    {
                                        TowerType = selectedTower.Name,
                                        X         = worldPosition.x,
                                        Y         = worldPosition.y,
                                        PlayerId  = players[currentPlayerID].getId(),
                                        ControllerId = getLocalControllerId(),
                                    });
                                    selectedTowerIndex = -1;
                                }
                            }
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
            case "KeyDown":
                if (input.Key == (int)SDL_Scancode.SDL_SCANCODE_1)
                {
                    selectedTowerIndex = (selectedTowerIndex == 0 ? -1 : 0);
                }
                if (input.Key == (int)SDL_Scancode.SDL_SCANCODE_2)
                {
                    selectedTowerIndex = (selectedTowerIndex == 1 ? -1 : 1);
                }
                if (input.Key == (int)SDL_Scancode.SDL_SCANCODE_3)
                {
                    selectedTowerIndex = (selectedTowerIndex == 2 ? -1 : 2);
                }
                if (input.Key == (int)SDL_Scancode.SDL_SCANCODE_4)
                {
                    selectedTowerIndex = (selectedTowerIndex == 3 ? -1 : 3);
                }

                if (input.Key == (int)SDL_Scancode.SDL_SCANCODE_5)
                {
                    selectedTowerIndex = (selectedTowerIndex == 4 ? -1 : 4);
                }

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
    private void spawnBloons(Map map, double deltaTimeMs, int waveIndex)
    {
        currentWave = map.Waves[waveIndex];
        updateBloons(currentWave, deltaTimeMs);
        if (map.Waves[map.Waves.Count - 1].Bloons.Count == 0 && lives > 0)
        {
            gameWin = true;
        }


    }
    private void updateBloons(Map.Wave wave, double deltaTimeMs)
    {
        cachedBloons.Clear();
        waveElapsedTimeMs += deltaTimeMs;
        // Debug.Log(wave.Bloons.Count.ToString());
        for (int i = wave.Bloons.Count - 1; i >= 0; i--)
        {
            Bloon bloon = wave.Bloons[i];
            bloon.updateBloon(monkeyLane.Lane.getPath(), deltaTimeMs, waveElapsedTimeMs);

            if (bloon.getEnd())
            {
                lives -= bloon.getLayer(); //maybe change live to shared
            }

            if (bloon.getPopped() || bloon.getEnd())
            {
                wave.Bloons.RemoveAt(i);
            }
            else
            {
                cachedBloons.Add(bloon);
            }
        }

        if (wave.Bloons.Count == 0 && currentWaveNumber < monkeyLane.Waves.Count - 1)
        {
            currentWaveNumber++;
            waveElapsedTimeMs = 0;
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

        var layout = getTowerPaletteLayout(display, sectionStartX, sectionEndX);

        var slotPadding = 14;
        var slotX = sectionStartX + slotPadding;

        display.showText("TOWERS", layout.SlotX, layout.TitleY, layout.TitleSize, 245, 245, 245);
        display.showText("Select one, then click map", layout.SlotX, layout.SubtitleY, layout.SubtitleSize, 235, 235, 235);

        for (var i = 0; i < placeableTowers.Count; i++)
        {
            var slotY = layout.SlotTop + (i * (layout.SlotHeight + layout.SlotGap));
            var isSelected = i == selectedTowerIndex;
            var slotColor = isSelected
                ? Color.FromArgb(255, 220, 176, 120)
                : Color.FromArgb(255, 155, 103, 65);

            drawFilledRect(display, layout.SlotX, slotY, layout.SlotWidth, layout.SlotHeight, slotColor);
            drawRectOutline(display, layout.SlotX, slotY, layout.SlotWidth, layout.SlotHeight, Color.FromArgb(255, 245, 236, 223));

            Tower temp = placeableTowers[i].CreateTower(new LPoint() { x = 0, y = 0 });
            var cost = placeableTowers[i].getCost();
            display.showText(placeableTowers[i].Name, slotX + 14, slotY + 16, 17, 255, 255, 255);
            if(cost > players[currentPlayerID].getMoney())
            {
                display.showText(cost.ToString(), slotX + 150, slotY + 16, 17, 255, 0, 0);
            }
            else
            {
                display.showText(cost.ToString(), slotX + 150, slotY + 16, 17, 255, 255, 255);
            }

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

        var layout = getTowerPaletteLayout(display, hudStartX, display.getWidth());
        if (screenX < layout.SlotX || screenX > layout.SlotX + layout.SlotWidth)
        {
            return true;
        }

        for (var i = 0; i < placeableTowers.Count; i++)
        {
            var slotY = layout.SlotTop + (i * (layout.SlotHeight + layout.SlotGap));
            if (screenY >= slotY && screenY <= slotY + layout.SlotHeight)
            {
                selectedTowerIndex = i;
                return true;
            }
        }

        return true;
    }

    private TowerOption getSelectedTowerOption()
    {
        if (placeableTowers.Count == 0 || selectedTowerIndex < 0 || selectedTowerIndex >= placeableTowers.Count)
        {
            return null;
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

    private TowerPaletteLayout getTowerPaletteLayout(Display display, int sectionStartX, int sectionEndX)
    {
        var sectionWidth = Math.Max(0, sectionEndX - sectionStartX);
        var uiScale = getUiScale(display.getWidth(), display.getHeight());
        var slotPadding = scaleValue(14, uiScale, minimum: 6);
        var slotWidth = Math.Max(24, sectionWidth - (slotPadding * 2));
        var slotHeight = scaleValue(HudBaseSlotHeight, uiScale, minimum: 48);
        var slotGap = scaleValue(HudBaseSlotGap, uiScale, minimum: 6);
        var slotTop = scaleValue(HudBaseSlotTop, uiScale, minimum: 56);
        var titleSize = scaleValue(26, uiScale, minimum: 14);
        var subtitleSize = scaleValue(12, uiScale, minimum: 10);
        var nameTextSize = scaleValue(17, uiScale, minimum: 10);
        var stateTextSize = scaleValue(12, uiScale, minimum: 9);
        var nameOffsetX = scaleValue(14, uiScale, minimum: 8);
        var nameOffsetY = Math.Max(6, slotHeight / 5);
        var stateOffsetY = Math.Max(nameOffsetY + scaleValue(20, uiScale, minimum: 12), (slotHeight * 45) / 100);

        return new TowerPaletteLayout(
            sectionStartX + slotPadding,
            slotWidth,
            slotHeight,
            slotGap,
            slotTop,
            scaleValue(26, uiScale, minimum: 14),
            scaleValue(56, uiScale, minimum: 30),
            titleSize,
            subtitleSize,
            nameTextSize,
            stateTextSize,
            nameOffsetX,
            nameOffsetY,
            stateOffsetY);
    }

    private static float getUiScale(int screenW, int screenH)
    {
        var safeWidth = Math.Max(1, screenW);
        var safeHeight = Math.Max(1, screenH);
        var scaleX = safeWidth / (float)DesignWidth;
        var scaleY = safeHeight / (float)DesignHeight;
        return Math.Max(0.45f, MathF.Min(scaleX, scaleY));
    }

    private static int scaleValue(int baseValue, float scale, int minimum)
    {
        return Math.Max(minimum, (int)MathF.Round(baseValue * scale));
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
            if (!bloon.getIsTargetable() || !bloon.getActive())
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
            new LPoint() { x = 350,  y = 630  },
            new LPoint() { x = 350,  y = 475  },
            new LPoint() { x = 590,  y = 475  },
            new LPoint() { x = 590,  y = 1015 },
            new LPoint() { x = 940,  y = 1015 },
            new LPoint() { x = 940,  y = 950, tunnelStart = true}, //tunnel 
            new LPoint() { x = 940,  y = 648, tunnelEnd = true}, //tunnel 
            new LPoint() { x = 940,  y = 305  },
            new LPoint() { x = 352,  y = 291  },
            new LPoint() { x = 352,  y = 136  },
            new LPoint() { x = 1128, y = 136  },
            new LPoint() { x = 1128, y = 286  },
            new LPoint() { x = 1334, y = 286  },
            new LPoint() { x = 1334, y = 473  },
            new LPoint() { x = 1135, y = 473  },
            new LPoint() { x = 1135, y = 795  },
            new LPoint() { x = 754,  y = 795, tunnelStart = true }, //tunnel 
            new LPoint() { x = 427,  y = 814, tunnelEnd = true }, //tunnel 
            new LPoint() { x = 347,  y = 829  },
            new LPoint() { x = 347,  y = 1080 }
        };

        // Initialize lane
        Lane lane = new Lane(path);
        int startX = lane.getPath()[0].x;
        int startY = lane.getPath()[0].y;

        // Wave 1 - 20 Reds
        var wave1 = new Map.Wave { spawnIntervalMs = 400, Bloons = new List<Bloon>() };
        for (int i = 1; i <= 20; i++)
            wave1.Bloons.Add(new Bloon(1, false, false, startX, startY, i * wave1.spawnIntervalMs));
        for (int i = 1; i <= 20; i++)
            wave1.Bloons.Add(new Bloon(7, false, false, startX, startY, i * wave1.spawnIntervalMs));

        // Wave 2 - 30 Reds, 10 Blues
        var wave2 = new Map.Wave { spawnIntervalMs = 350, Bloons = new List<Bloon>() };
        for (int i = 1; i <= 30; i++)
            wave2.Bloons.Add(new Bloon(1, false, false, startX, startY, i * wave2.spawnIntervalMs));
        for (int i = 1; i <= 10; i++)
            wave2.Bloons.Add(new Bloon(2, false, false, startX, startY, (30 + i) * wave2.spawnIntervalMs));

        // Wave 3 - 20 Blues, 10 Greens
        var wave3 = new Map.Wave { spawnIntervalMs = 300, Bloons = new List<Bloon>() };
        for (int i = 1; i <= 20; i++)
            wave3.Bloons.Add(new Bloon(2, false, false, startX, startY, i * wave3.spawnIntervalMs));
        for (int i = 1; i <= 10; i++)
            wave3.Bloons.Add(new Bloon(3, false, false, startX, startY, (20 + i) * wave3.spawnIntervalMs));

        // Wave 4 - 30 Blues, 20 Greens, 5 Yellows
        var wave4 = new Map.Wave { spawnIntervalMs = 280, Bloons = new List<Bloon>() };
        for (int i = 1; i <= 30; i++)
            wave4.Bloons.Add(new Bloon(2, false, false, startX, startY, i * wave4.spawnIntervalMs));
        for (int i = 1; i <= 20; i++)
            wave4.Bloons.Add(new Bloon(3, false, false, startX, startY, (30 + i) * wave4.spawnIntervalMs));
        for (int i = 1; i <= 5; i++)
            wave4.Bloons.Add(new Bloon(4, false, false, startX, startY, (50 + i) * wave4.spawnIntervalMs));

        // Wave 5 - 40 Greens, 15 Yellows
        var wave5 = new Map.Wave { spawnIntervalMs = 250, Bloons = new List<Bloon>() };
        for (int i = 1; i <= 40; i++)
            wave5.Bloons.Add(new Bloon(3, false, false, startX, startY, i * wave5.spawnIntervalMs));
        for (int i = 1; i <= 15; i++)
            wave5.Bloons.Add(new Bloon(4, false, false, startX, startY, (40 + i) * wave5.spawnIntervalMs));

        // Wave 6 - 25 Greens, 20 Yellows, 5 Pinks
        var wave6 = new Map.Wave { spawnIntervalMs = 220, Bloons = new List<Bloon>() };
        for (int i = 1; i <= 25; i++)
            wave6.Bloons.Add(new Bloon(3, false, false, startX, startY, i * wave6.spawnIntervalMs));
        for (int i = 1; i <= 20; i++)
            wave6.Bloons.Add(new Bloon(4, false, false, startX, startY, (25 + i) * wave6.spawnIntervalMs));
        for (int i = 1; i <= 5; i++)
            wave6.Bloons.Add(new Bloon(5, false, false, startX, startY, (45 + i) * wave6.spawnIntervalMs));

        // Wave 7 - 30 Yellows, 20 Pinks interleaved
        var wave7 = new Map.Wave { spawnIntervalMs = 180, Bloons = new List<Bloon>() };
        for (int i = 1; i <= 30; i++)
            wave7.Bloons.Add(new Bloon(4, false, false, startX, startY, i * 2 * wave7.spawnIntervalMs));
        for (int i = 1; i <= 20; i++)
            wave7.Bloons.Add(new Bloon(5, false, false, startX, startY, ((i * 2) - 1) * wave7.spawnIntervalMs));

        // Wave 8 - 50 Pinks, 10 Yellows mixed in front
        var wave8 = new Map.Wave { spawnIntervalMs = 160, Bloons = new List<Bloon>() };
        for (int i = 1; i <= 10; i++)
            wave8.Bloons.Add(new Bloon(4, false, false, startX, startY, i * wave8.spawnIntervalMs));
        for (int i = 1; i <= 50; i++)
            wave8.Bloons.Add(new Bloon(5, false, false, startX, startY, (10 + i) * wave8.spawnIntervalMs));

        // Wave 9 - 40 Pinks, 20 Yellows, 10 Greens all at once rapid-fire
        var wave9 = new Map.Wave { spawnIntervalMs = 120, Bloons = new List<Bloon>() };
        for (int i = 1; i <= 20; i++)
            wave9.Bloons.Add(new Bloon(3, false, false, startX, startY, i * wave9.spawnIntervalMs));
        for (int i = 1; i <= 20; i++)
            wave9.Bloons.Add(new Bloon(4, false, false, startX, startY, (20 + i) * wave9.spawnIntervalMs));
        for (int i = 1; i <= 40; i++)
            wave9.Bloons.Add(new Bloon(5, false, false, startX, startY, (40 + i) * wave9.spawnIntervalMs));

        // Wave 10 - All Pinks, massive swarm
        var wave10 = new Map.Wave { spawnIntervalMs = 80, Bloons = new List<Bloon>() };
        for (int i = 1; i <= 80; i++)
            wave10.Bloons.Add(new Bloon(5, false, false, startX, startY, i * wave10.spawnIntervalMs));

        // Wave 11 - white + pink
        var wave11 = new Map.Wave { spawnIntervalMs = 100, Bloons = new List<Bloon>() };
        for (int i = 1; i <= 40; i++)
            wave10.Bloons.Add(new Bloon(5, false, false, startX, startY, i * wave11.spawnIntervalMs));
        for (int i = 1; i <= 40; i++)
            wave11.Bloons.Add(new Bloon(6, false, false, startX, startY, i * wave11.spawnIntervalMs));
        for (int i = 1; i <= 40; i++)
            wave11.Bloons.Add(new Bloon(7, false, false, startX, startY, i * wave11.spawnIntervalMs));
        
        List<Map.Wave> waves = new List<Map.Wave> { wave1, wave2, wave3, wave4, wave5, wave6, wave7, wave8, wave9, wave10, wave11 };
        return new Map(lane, waves);
    }

    public override int getTargetFrameRate()
    {
        return 120; // cap at 120 fps
    }

    private int getLocalControllerId()
    {
        return multiplayerSession.Role == MultiplayerRole.Join
            ? ClientControllerId
            : HostControllerId;
    }

    private LPoint getPointerForController(int controllerId, LPoint fallbackPointer)
    {
        if (latestPointerByControllerId.TryGetValue(controllerId, out var pointer))
        {
            return pointer;
        }

        return fallbackPointer;
    }

    private void OnClientAimUpdated(PlayerAimMessage msg)
    {
        latestPointerByControllerId[msg.ControllerId] = new LPoint
        {
            x = msg.X,
            y = msg.Y,
        };
    }
    
    // ── New method: HOST validates and places a tower for a client ────────────────
    private void OnClientTowerPlaceRequested(TowerPlaceMessage msg)
    {
        // Only the host runs this.
        var option = placeableTowers.Find(t => t.Name == msg.TowerType);
        if (option == null) return;

        var player = players.Find(p => p.getId() == msg.PlayerId);
        if (player == null || player.getMoney() < option.getCost()) return;

        player.removeMoney(option.getCost());
        placedTowers.Add(new PlacedTower(option.CreateTower(new LPoint { x = msg.X, y = msg.Y }), msg.ControllerId));
    }

// ── New method: CLIENT applies authoritative state from host ──────────────────
    private void ApplyStateFromHost(GameStateMessage state)
    {
        // Authoritative values come from the host.
        lives             = state.Lives;
        currentWaveNumber = state.WaveNumber;
        waveElapsedTimeMs = state.WaveElapsedTimeMs;

        // Sync player money.
        foreach (var (id, money) in state.PlayerMoney)
        {
            var player = players.Find(p => p.getId() == id);
            if (player != null)
                player.setMoney(money);   // add setMoney() to Player if not present
        }

        // Rebuild tower list from host snapshot (simple replace strategy).
        // A production implementation would diff to avoid re-creating unchanged towers.
        placedTowers.Clear();
        foreach (var snap in state.Towers)
        {
            if (towerOptionByTypeName.TryGetValue(snap.TowerType, out var option))
            {
                var tower = option.CreateTower(new LPoint { x = snap.X, y = snap.Y });
                tower.applySnapshot(snap);
                placedTowers.Add(new PlacedTower(tower, snap.OwnerId));
            }
        }

        receivedBloonSnapshots = state.Bloons ?? new();
        receivedProjectileSnapshots = state.Projectiles ?? new();
    }
    private void drawEndScreen()
    {
        var display = Bootstrap.getDisplay();
        var screenW = display.getWidth();
        var screenH = display.getHeight();
        var centerX = screenW / 2;
        var centerY = screenH / 2;

        // Dim overlay
        for (var y = 0; y < screenH; y++)
            display.drawLine(0, y, screenW, y, 0, 0, 0, 180);

        // Title
        var titleText = gameWin ? "YOU WIN!" : "GAME OVER";
        var titleColor = gameWin
            ? (r: 255, g: 230, b: 100)
            : (r: 255, g: 60,  b: 60);

        display.showText(titleText, centerX - 120, centerY - 100, 52, titleColor.r, titleColor.g, titleColor.b);

        var statsText = gameWin
            ? $"You survived all {monkeyLane.Waves.Count} waves!"
            : $"You ran out of lives on wave {currentWaveNumber + 1}.";
        display.showText(statsText, centerX - 200, centerY - 20, 22, 255, 255, 255);

        // Back to menu button
        var btnW = 320;
        var btnH = 72;
        var btnX = centerX - btnW / 2;
        var btnY = centerY + 60;

        var isHovered = mouseX >= btnX && mouseX <= btnX + btnW && mouseY >= btnY && mouseY <= btnY + btnH;
        var btnColor = isHovered ? Color.FromArgb(255, 190, 126, 75) : Color.FromArgb(255, 110, 74, 42);

        for (var row = 0; row < btnH; row++)
            display.drawLine(btnX, btnY + row, btnX + btnW, btnY + row, btnColor.R, btnColor.G, btnColor.B, btnColor.A);
        display.drawLine(btnX, btnY, btnX + btnW, btnY, 246, 225, 180, 255);
        display.drawLine(btnX, btnY + btnH, btnX + btnW, btnY + btnH, 246, 225, 180, 255);
        display.drawLine(btnX, btnY, btnX, btnY + btnH, 246, 225, 180, 255);
        display.drawLine(btnX + btnW, btnY, btnX + btnW, btnY + btnH, 246, 225, 180, 255);

        display.showText("Back to Main Menu", btnX + 34, btnY + 20, 24, 255, 255, 255);
        display.drawFilledCircle(mouseX, mouseY, 4, Color.FromArgb(255, 255, 245, 120));

        // Click handling
        if (mouseLeft && isHovered)
        {
            mouseLeft = false;
            returnToMainMenu();
        }
    }

    private void returnToMainMenu()
    {
        stopBackgroundMusic();
        Network.reset();
        Network.OnTowerPlaceRequested -= OnClientTowerPlaceRequested;
        Network.OnPlayerAimUpdated -= OnClientAimUpdated;
        Network.OnStateReceived -= ApplyStateFromHost;
        Bootstrap.getInput().removeListener(this);
        Bootstrap.setRunningGame(new GameMainMenu());
    }

    private unsafe void stopBackgroundMusic()
    {
        if (track == null)
        {
            return;
        }

        Bootstrap.getSound().stopSound(track);
        track = null;
    }

    private void drawWaitingScreen()
    {
        
        // Poll network events even while waiting so OnGameStarted can fire
        if (multiplayerSession.Role == MultiplayerRole.Join)
            Network.pollClient();
        else if (multiplayerSession.Role == MultiplayerRole.Host)
            Network.pollServer();
        
        var display = Bootstrap.getDisplay();
        var screenW = display.getWidth();
        var screenH = display.getHeight();
        var centerX = screenW / 2;
        var centerY = screenH / 2;

        // Dim background
        for (var y = 0; y < screenH; y++)
            display.drawLine(0, y, screenW, y, 20, 15, 10, 255);

        if (multiplayerSession.Role == MultiplayerRole.Host)
        {
            if (!Network.ClientConnected)
            {
                display.showText("Waiting for player to join...", centerX - 200, centerY - 20, 28, 255, 255, 255);
            }
            else
            {
                // Client has connected — show their name and a Start button
                display.showText($"{Network.ConnectedClientName} has joined!", centerX - 180, centerY - 60, 26, 180, 255, 150);

                var btnW = 280;
                var btnH = 72;
                var btnX = centerX - btnW / 2;
                var btnY = centerY + 10;

                startButtonHovered = mouseX >= btnX && mouseX <= btnX + btnW
                                   && mouseY >= btnY && mouseY <= btnY + btnH;

                var btnColor = startButtonHovered
                    ? Color.FromArgb(255, 100, 180, 80)
                    : Color.FromArgb(255, 60, 130, 50);

                for (var row = 0; row < btnH; row++)
                    display.drawLine(btnX, btnY + row, btnX + btnW, btnY + row, btnColor.R, btnColor.G, btnColor.B, btnColor.A);
                display.drawLine(btnX,        btnY,        btnX + btnW, btnY,        200, 255, 180, 255);
                display.drawLine(btnX,        btnY + btnH, btnX + btnW, btnY + btnH, 200, 255, 180, 255);
                display.drawLine(btnX,        btnY,        btnX,        btnY + btnH, 200, 255, 180, 255);
                display.drawLine(btnX + btnW, btnY,        btnX + btnW, btnY + btnH, 200, 255, 180, 255);

                display.showText("Start Game", btnX + 54, btnY + 20, 26, 255, 255, 255);
                display.drawFilledCircle(mouseX, mouseY, 4, Color.FromArgb(255, 255, 245, 120));

                if (mouseLeft && startButtonHovered)
                {
                    mouseLeft = false;
                    waitingForPlayer = false;
                    gameStarted = true;
                    startMatchTimer();
                    Network.sendGameStart();
                }
            }
        }
        else
        {
            // Client waiting for host to press Start
            display.showText("Waiting for host to start the game...", centerX - 240, centerY - 20, 26, 255, 255, 255);
            display.showText($"Connected as: {multiplayerSession.PlayerName}", centerX - 130, centerY + 30, 18, 180, 220, 255);
        }
        //cancel button
        var cancelW = 200;
        var cancelH = 58;
        var cancelX = centerX - cancelW / 2;
        var cancelY = centerY + 100;

        var cancelHovered = mouseX >= cancelX && mouseX <= cancelX + cancelW
                                              && mouseY >= cancelY && mouseY <= cancelY + cancelH;
        var cancelColor = cancelHovered
            ? Color.FromArgb(255, 180, 60, 50)
            : Color.FromArgb(255, 120, 40, 35);

        for (var row = 0; row < cancelH; row++)
            display.drawLine(cancelX, cancelY + row, cancelX + cancelW, cancelY + row, cancelColor.R, cancelColor.G, cancelColor.B, cancelColor.A);
        display.drawLine(cancelX,          cancelY,          cancelX + cancelW, cancelY,          255, 180, 170, 255);
        display.drawLine(cancelX,          cancelY + cancelH, cancelX + cancelW, cancelY + cancelH, 255, 180, 170, 255);
        display.drawLine(cancelX,          cancelY,          cancelX,           cancelY + cancelH, 255, 180, 170, 255);
        display.drawLine(cancelX + cancelW, cancelY,         cancelX + cancelW, cancelY + cancelH, 255, 180, 170, 255);
        display.showText("Cancel", cancelX + 54, cancelY + 14, 22, 255, 255, 255);

        display.drawFilledCircle(mouseX, mouseY, 4, Color.FromArgb(255, 255, 245, 120));

        if (mouseLeft && cancelHovered)
        {
            mouseLeft = false;
            returnToMainMenu();
        }
    }
}

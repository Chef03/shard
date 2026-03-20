using SDL;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace Shard
{
    internal class GameMainMenu : Game, InputListener
    {
        private const int AspectRatioWidth = 16;
        private const int AspectRatioHeight = 9;
        private const int DesignWidth = 1920;
        private const int DesignHeight = 1080;
        private const int DefaultServerPort = 9050;
        private static readonly ScoreBoardKey ScoreboardKey = new ScoreBoardKey("bloons", "monkey-lane");

        private readonly List<MenuButton> menuButtons = new List<MenuButton>();
        private int mouseX;
        private int mouseY;
        private bool isFullscreen;
        private bool isShiftPressed;
        private string multiplayerStatus = string.Empty;
        private string joinPlayerName = "Player";
        private string joinServerIp = "127.0.0.1";
        private string joinServerPort = DefaultServerPort.ToString();
        private MenuScreen menuScreen = MenuScreen.Main;
        private JoinField activeJoinField = JoinField.None;
        private enum MenuScreen
        {
            Main,
            Multiplayer,
            Join,
            Scoreboard
        }

        private enum JoinField
        {
            None,
            PlayerName,
            ServerIp,
            ServerPort
        }

        private sealed class MenuButton
        {
            public string Label { get; }
            public Action OnClick { get; }
            public int X { get; set; }
            public int Y { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }

            public MenuButton(string label, Action onClick)
            {
                Label = label;
                OnClick = onClick;
            }
        }

        private readonly struct JoinLayout
        {
            public int FieldX { get; }
            public int FieldWidth { get; }
            public int FieldHeight { get; }
            public int PlayerFieldY { get; }
            public int ServerFieldY { get; }
            public int PortFieldY { get; }
            public int ButtonTop { get; }

            public JoinLayout(int fieldX, int fieldWidth, int fieldHeight, int playerFieldY, int serverFieldY, int portFieldY, int buttonTop)
            {
                FieldX = fieldX;
                FieldWidth = fieldWidth;
                FieldHeight = fieldHeight;
                PlayerFieldY = playerFieldY;
                ServerFieldY = serverFieldY;
                PortFieldY = portFieldY;
                ButtonTop = buttonTop;
            }
        }

        public override void initialize()
        {
            var display = Bootstrap.getDisplay();
            display.setSDLSize(DesignWidth, DesignHeight);
            isFullscreen = false;
            isShiftPressed = false;

            Bootstrap.getInput().addListener(this);
            setMenuScreen(MenuScreen.Main);
        }

        public override void update()
        {
            var display = Bootstrap.getDisplay();
            var screenW = Math.Max(1, display.getWidth());
            var screenH = Math.Max(1, display.getHeight());
            var uiScale = getUiScale(screenW, screenH);

            var titleX = scaledValue(60, uiScale, minimum: 16);
            var titleY = scaledValue(60, uiScale, minimum: 16);
            var subtitleY = titleY + scaledValue(65, uiScale, minimum: 24);
            var statusY = subtitleY + scaledValue(60, uiScale, minimum: 30);

            if (menuScreen == MenuScreen.Join)
            {
                updateJoinButtonLayout(screenW, screenH, uiScale);
            }
            else
            {
                updateButtonLayout(screenW, screenH, uiScale);
            }

            display.showText("BLOONS", titleX, titleY, scaledValue(56, uiScale, minimum: 20), 255, 230, 170);
            display.showText(getScreenSubtitle(), titleX + scaledValue(6, uiScale, minimum: 2), subtitleY, scaledValue(24, uiScale, minimum: 13), 255, 255, 255);

            if (menuScreen == MenuScreen.Join)
            {
                drawJoinInputs(display, screenW, screenH, uiScale, statusY);
            }
            else if (menuScreen == MenuScreen.Scoreboard)
            {
                drawScoreboard(display, screenW, screenH, uiScale, statusY);
            }

            foreach (var button in menuButtons)
            {
                drawButton(display, button, uiScale);
            }

            if (!string.IsNullOrWhiteSpace(multiplayerStatus))
            {
                display.showText(multiplayerStatus, titleX + scaledValue(6, uiScale, minimum: 2), statusY, scaledValue(18, uiScale, minimum: 11), 240, 220, 170);
            }

            display.drawFilledCircle(mouseX, mouseY, scaledValue(4, uiScale, minimum: 2), Color.FromArgb(255, 255, 245, 120));
        }

        public override int getTargetFrameRate()
        {
            return 120;
        }

        public void handleInput(InputEvent input, string eventType)
        {
            if (eventType == "MouseMotion" || eventType == "MouseDown" || eventType == "MouseUp")
            {
                mouseX = input.X;
                mouseY = input.Y;
            }

            if (eventType == "KeyDown")
            {
                handleKeyDown(input.Key);
                return;
            }

            if (eventType == "KeyUp")
            {
                handleKeyUp(input.Key);
                return;
            }

            if (eventType != "MouseDown" || input.Button != 1)
            {
                return;
            }

            if (menuScreen == MenuScreen.Join)
            {
                var display = Bootstrap.getDisplay();
                var layout = getJoinLayout(display.getWidth(), display.getHeight(), getUiScale(display.getWidth(), display.getHeight()));
                if (tryActivateJoinField(mouseX, mouseY, layout))
                {
                    return;
                }
            }

            foreach (var button in menuButtons)
            {
                if (!isPointInsideButton(mouseX, mouseY, button))
                {
                    continue;
                }

                button.OnClick();
                return;
            }
        }

        public void handleWindowEvent(WindowEvent windowEvent, string eventType)
        {
            switch (eventType)
            {
                case "WindowResize":
                    applyAspectRatio(windowEvent.Width, windowEvent.Height);
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

        private void startSinglePlayer()
        {
            startGame(MultiplayerSession.Offline());
        }

        private void openMultiplayer()
        {
            multiplayerStatus = "Select host or join.";
            setMenuScreen(MenuScreen.Multiplayer);
        }

        private void openScoreboard()
        {
            multiplayerStatus = string.Empty;
            setMenuScreen(MenuScreen.Scoreboard);
        }

        private void openJoinMenu()
        {
            activeJoinField = JoinField.PlayerName;
            multiplayerStatus = "Enter player and server details.";
            setMenuScreen(MenuScreen.Join);
        }

        private void hostMultiplayer()
        {
            var hostName = string.IsNullOrWhiteSpace(joinPlayerName) ? "Host" : joinPlayerName.Trim();
            multiplayerStatus = "Starting host...";
            startGame(MultiplayerSession.Host(hostName, DefaultServerPort));
        }

        private void joinMultiplayer()
        {
            var playerName = string.IsNullOrWhiteSpace(joinPlayerName) ? "Player" : joinPlayerName.Trim();
            var serverIp = joinServerIp.Trim();

            if (string.IsNullOrWhiteSpace(serverIp))
            {
                multiplayerStatus = "Server IP cannot be empty.";
                return;
            }

            if (!int.TryParse(joinServerPort.Trim(), out var port) || port <= 0 || port > 65535)
            {
                multiplayerStatus = "Port must be a number between 1 and 65535.";
                return;
            }

            multiplayerStatus = "Connecting...";
            startGame(MultiplayerSession.Join(playerName, serverIp, port));
        }

        private void exitGame()
        {
            Environment.Exit(0);
        }

        private void backToMainMenu()
        {
            multiplayerStatus = string.Empty;
            setMenuScreen(MenuScreen.Main);
        }

        private void backToMultiplayerMenu()
        {
            multiplayerStatus = "Select host or join.";
            setMenuScreen(MenuScreen.Multiplayer);
        }

        private void startGame(MultiplayerSession multiplayerSession)
        {
            Bootstrap.getInput().removeListener(this);
            Bootstrap.setRunningGame(new GameBloons(multiplayerSession));
        }

        private void setMenuScreen(MenuScreen newScreen)
        {
            menuScreen = newScreen;
            rebuildButtons();
        }

        private void rebuildButtons()
        {
            menuButtons.Clear();

            if (menuScreen == MenuScreen.Main)
            {
                menuButtons.Add(new MenuButton("Single player", startSinglePlayer));
                menuButtons.Add(new MenuButton("Multiplayer", openMultiplayer));
                menuButtons.Add(new MenuButton("Scoreboard", openScoreboard));
                menuButtons.Add(new MenuButton("Exit", exitGame));
                return;
            }

            if (menuScreen == MenuScreen.Multiplayer)
            {
                menuButtons.Add(new MenuButton("Host game", hostMultiplayer));
                menuButtons.Add(new MenuButton("Join game", openJoinMenu));
                menuButtons.Add(new MenuButton("Back", backToMainMenu));
                return;
            }

            if (menuScreen == MenuScreen.Scoreboard)
            {
                menuButtons.Add(new MenuButton("Back", backToMainMenu));
                return;
            }

            menuButtons.Add(new MenuButton("Connect", joinMultiplayer));
            menuButtons.Add(new MenuButton("Back", backToMultiplayerMenu));
        }

        private string getScreenSubtitle()
        {
            if (menuScreen == MenuScreen.Multiplayer)
            {
                return "MULTIPLAYER";
            }

            if (menuScreen == MenuScreen.Join)
            {
                return "JOIN SERVER";
            }

            if (menuScreen == MenuScreen.Scoreboard)
            {
                return "WIN HISTORY";
            }

            return "MAIN MENU";
        }

        private void drawJoinInputs(Display display, int screenW, int screenH, float uiScale, int statusY)
        {
            var layout = getJoinLayout(screenW, screenH, uiScale);
            var labelSize = scaledValue(18, uiScale, minimum: 10);
            var helperTextY = statusY + scaledValue(34, uiScale, minimum: 20);

            display.showText("Type values and press Connect (Tab to switch field)", layout.FieldX, helperTextY, labelSize, 225, 225, 225);
            drawInputField(display, layout.FieldX, layout.PlayerFieldY, layout.FieldWidth, layout.FieldHeight, "Player name", joinPlayerName, JoinField.PlayerName, uiScale);
            drawInputField(display, layout.FieldX, layout.ServerFieldY, layout.FieldWidth, layout.FieldHeight, "Server IP or host", joinServerIp, JoinField.ServerIp, uiScale);
            drawInputField(display, layout.FieldX, layout.PortFieldY, layout.FieldWidth, layout.FieldHeight, "Port", joinServerPort, JoinField.ServerPort, uiScale);
        }

        private void drawScoreboard(Display display, int screenW, int screenH, float uiScale, int statusY)
        {
            var safeWidth = Math.Max(1, screenW);
            var fieldWidth = Math.Min((int)MathF.Round(safeWidth * 0.8f), scaledValue(820, uiScale, minimum: 280));
            var fieldX = (safeWidth - fieldWidth) / 2;
            var top = statusY + scaledValue(48, uiScale, minimum: 24);
            var titleSize = scaledValue(18, uiScale, minimum: 10);
            var lineSize = scaledValue(22, uiScale, minimum: 12);
            var lineHeight = scaledValue(34, uiScale, minimum: 18);
            var winningTimes = Bootstrap.getScoreManager().GetWinningTimes(
                ScoreboardKey,
                static entry => entry.DurationMs,
                12);

            display.showText("Fastest Bloons wins", fieldX, top, titleSize, 225, 225, 225);

            if (winningTimes.Count == 0)
            {
                display.showText("No winning times recorded yet.", fieldX, top + lineHeight, lineSize, 255, 255, 255);
                return;
            }

            for (var index = 0; index < winningTimes.Count; index++)
            {
                var time = TimeSpan.FromMilliseconds(winningTimes[index].DurationMs);
                display.showText($"{index + 1}. {formatWinningTime(time)}", fieldX, top + ((index + 1) * lineHeight), lineSize, 255, 255, 255);
            }
        }

        private void drawInputField(Display display, int x, int y, int width, int height, string label, string value, JoinField field, float uiScale)
        {
            var isActive = activeJoinField == field;
            var background = isActive ? Color.FromArgb(255, 158, 112, 72) : Color.FromArgb(255, 102, 74, 48);
            var border = isActive ? Color.FromArgb(255, 255, 240, 190) : Color.FromArgb(255, 208, 179, 138);
            var labelSize = scaledValue(14, uiScale, minimum: 10);
            var valueSize = scaledValue(20, uiScale, minimum: 11);

            drawFilledRect(display, x, y, width, height, background);
            drawRectOutline(display, x, y, width, height, border);
            display.showText(label, x + scaledValue(14, uiScale, minimum: 6), y + scaledValue(8, uiScale, minimum: 4), labelSize, 245, 245, 245);
            display.showText(value, x + scaledValue(14, uiScale, minimum: 6), y + scaledValue(31, uiScale, minimum: 16), valueSize, 255, 255, 255);
        }

        private bool tryActivateJoinField(int x, int y, JoinLayout layout)
        {
            if (isPointInsideRect(x, y, layout.FieldX, layout.PlayerFieldY, layout.FieldWidth, layout.FieldHeight))
            {
                activeJoinField = JoinField.PlayerName;
                return true;
            }

            if (isPointInsideRect(x, y, layout.FieldX, layout.ServerFieldY, layout.FieldWidth, layout.FieldHeight))
            {
                activeJoinField = JoinField.ServerIp;
                return true;
            }

            if (isPointInsideRect(x, y, layout.FieldX, layout.PortFieldY, layout.FieldWidth, layout.FieldHeight))
            {
                activeJoinField = JoinField.ServerPort;
                return true;
            }

            return false;
        }

        private void handleKeyDown(int key)
        {
            if (key == (int)SDL_Scancode.SDL_SCANCODE_LSHIFT || key == (int)SDL_Scancode.SDL_SCANCODE_RSHIFT)
            {
                isShiftPressed = true;
                return;
            }

            if (menuScreen != MenuScreen.Join)
            {
                if (menuScreen == MenuScreen.Scoreboard && key == (int)SDL_Scancode.SDL_SCANCODE_ESCAPE)
                {
                    backToMainMenu();
                }
                return;
            }

            if (key == (int)SDL_Scancode.SDL_SCANCODE_TAB)
            {
                selectNextJoinField();
                return;
            }

            if (key == (int)SDL_Scancode.SDL_SCANCODE_RETURN || key == (int)SDL_Scancode.SDL_SCANCODE_KP_ENTER)
            {
                joinMultiplayer();
                return;
            }

            if (key == (int)SDL_Scancode.SDL_SCANCODE_ESCAPE)
            {
                backToMultiplayerMenu();
                return;
            }

            if (key == (int)SDL_Scancode.SDL_SCANCODE_BACKSPACE)
            {
                removeLastCharacter();
                return;
            }

            if (tryGetTextCharacter(key, isShiftPressed, out var character))
            {
                appendJoinCharacter(character);
            }
        }

        private void handleKeyUp(int key)
        {
            if (key == (int)SDL_Scancode.SDL_SCANCODE_LSHIFT || key == (int)SDL_Scancode.SDL_SCANCODE_RSHIFT)
            {
                isShiftPressed = false;
            }
        }

        private void appendJoinCharacter(char character)
        {
            if (activeJoinField == JoinField.None)
            {
                activeJoinField = JoinField.PlayerName;
            }

            if (activeJoinField == JoinField.PlayerName)
            {
                if ((char.IsLetterOrDigit(character) || character == '-' || character == '_' || character == ' ') && joinPlayerName.Length < 20)
                {
                    joinPlayerName += character;
                }
                return;
            }

            if (activeJoinField == JoinField.ServerIp)
            {
                if ((char.IsLetterOrDigit(character) || character == '.' || character == '-') && joinServerIp.Length < 60)
                {
                    joinServerIp += character;
                }
                return;
            }

            if (activeJoinField == JoinField.ServerPort)
            {
                if (char.IsDigit(character) && joinServerPort.Length < 5)
                {
                    joinServerPort += character;
                }
            }
        }

        private void removeLastCharacter()
        {
            if (activeJoinField == JoinField.PlayerName)
            {
                joinPlayerName = removeLastCharacter(joinPlayerName);
                return;
            }

            if (activeJoinField == JoinField.ServerIp)
            {
                joinServerIp = removeLastCharacter(joinServerIp);
                return;
            }

            if (activeJoinField == JoinField.ServerPort)
            {
                joinServerPort = removeLastCharacter(joinServerPort);
            }
        }

        private void selectNextJoinField()
        {
            if (activeJoinField == JoinField.PlayerName)
            {
                activeJoinField = JoinField.ServerIp;
                return;
            }

            if (activeJoinField == JoinField.ServerIp)
            {
                activeJoinField = JoinField.ServerPort;
                return;
            }

            activeJoinField = JoinField.PlayerName;
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

        private void updateButtonLayout(int screenW, int screenH, float uiScale)
        {
            var buttonWidth = Math.Min((int)MathF.Round(screenW * 0.72f), scaledValue(460, uiScale, minimum: 220));
            var buttonHeight = scaledValue(90, uiScale, minimum: 52);
            var buttonGap = scaledValue(22, uiScale, minimum: 10);

            var totalHeight = (menuButtons.Count * buttonHeight) + ((menuButtons.Count - 1) * buttonGap);
            var startY = (screenH - totalHeight) / 2;
            var x = (screenW - buttonWidth) / 2;

            for (var i = 0; i < menuButtons.Count; i++)
            {
                menuButtons[i].X = x;
                menuButtons[i].Y = startY + (i * (buttonHeight + buttonGap));
                menuButtons[i].Width = buttonWidth;
                menuButtons[i].Height = buttonHeight;
            }
        }

        private void updateJoinButtonLayout(int screenW, int screenH, float uiScale)
        {
            var layout = getJoinLayout(screenW, screenH, uiScale);
            var buttonWidth = layout.FieldWidth;
            var buttonHeight = scaledValue(80, uiScale, minimum: 48);
            var buttonGap = scaledValue(18, uiScale, minimum: 9);

            for (var i = 0; i < menuButtons.Count; i++)
            {
                menuButtons[i].X = layout.FieldX;
                menuButtons[i].Y = layout.ButtonTop + (i * (buttonHeight + buttonGap));
                menuButtons[i].Width = buttonWidth;
                menuButtons[i].Height = buttonHeight;
            }
        }

        private JoinLayout getJoinLayout(int screenW, int screenH, float uiScale)
        {
            var safeWidth = Math.Max(1, screenW);
            var safeHeight = Math.Max(1, screenH);
            var fieldWidth = Math.Min((int)MathF.Round(safeWidth * 0.8f), scaledValue(820, uiScale, minimum: 280));
            var fieldX = (safeWidth - fieldWidth) / 2;
            var fieldHeight = scaledValue(74, uiScale, minimum: 46);
            var fieldGap = scaledValue(18, uiScale, minimum: 8);
            var fieldsTop = scaledValue(300, uiScale, minimum: 140);
            var playerFieldY = fieldsTop;
            var serverFieldY = playerFieldY + fieldHeight + fieldGap;
            var portFieldY = serverFieldY + fieldHeight + fieldGap;
            var buttonTop = portFieldY + fieldHeight + scaledValue(28, uiScale, minimum: 16);

            return new JoinLayout(fieldX, fieldWidth, fieldHeight, playerFieldY, serverFieldY, portFieldY, Math.Min(buttonTop, safeHeight - (fieldHeight * 2) - (fieldGap * 2)));
        }

        private void drawButton(Display display, MenuButton button, float uiScale)
        {
            var isHovered = isPointInsideButton(mouseX, mouseY, button);
            var buttonColor = isHovered ? Color.FromArgb(255, 190, 126, 75) : Color.FromArgb(255, 128, 84, 51);
            var outlineColor = Color.FromArgb(255, 246, 225, 180);
            var textSize = scaledValue(24, uiScale, minimum: 13);
            var textOffsetX = scaledValue(34, uiScale, minimum: 12);
            var textOffsetY = Math.Max(6, (button.Height - textSize) / 2);

            drawFilledRect(display, button.X, button.Y, button.Width, button.Height, buttonColor);
            drawRectOutline(display, button.X, button.Y, button.Width, button.Height, outlineColor);

            display.showText(button.Label, button.X + textOffsetX, button.Y + textOffsetY, textSize, 255, 255, 255);
        }

        private static float getUiScale(int screenW, int screenH)
        {
            var scaleX = screenW / (float)DesignWidth;
            var scaleY = screenH / (float)DesignHeight;
            return Math.Max(0.45f, MathF.Min(scaleX, scaleY));
        }

        private static int scaledValue(int baseValue, float scale, int minimum)
        {
            return Math.Max(minimum, (int)MathF.Round(baseValue * scale));
        }

        private static bool isPointInsideButton(int x, int y, MenuButton button)
        {
            return isPointInsideRect(x, y, button.X, button.Y, button.Width, button.Height);
        }

        private static bool isPointInsideRect(int x, int y, int rectX, int rectY, int width, int height)
        {
            return x >= rectX && x <= rectX + width && y >= rectY && y <= rectY + height;
        }

        private static bool tryGetTextCharacter(int key, bool shiftPressed, out char character)
        {
            character = '\0';

            if (key >= (int)SDL_Scancode.SDL_SCANCODE_A && key <= (int)SDL_Scancode.SDL_SCANCODE_Z)
            {
                var offset = key - (int)SDL_Scancode.SDL_SCANCODE_A;
                character = (char)((shiftPressed ? 'A' : 'a') + offset);
                return true;
            }

            if (key >= (int)SDL_Scancode.SDL_SCANCODE_1 && key <= (int)SDL_Scancode.SDL_SCANCODE_9)
            {
                var offset = key - (int)SDL_Scancode.SDL_SCANCODE_1;
                character = (char)('1' + offset);
                return true;
            }

            if (key == (int)SDL_Scancode.SDL_SCANCODE_0)
            {
                character = '0';
                return true;
            }

            if (key == (int)SDL_Scancode.SDL_SCANCODE_PERIOD)
            {
                character = '.';
                return true;
            }

            if (key == (int)SDL_Scancode.SDL_SCANCODE_MINUS)
            {
                character = shiftPressed ? '_' : '-';
                return true;
            }

            if (key == (int)SDL_Scancode.SDL_SCANCODE_SPACE)
            {
                character = ' ';
                return true;
            }

            return false;
        }

        private static string removeLastCharacter(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            return value.Substring(0, value.Length - 1);
        }

        private static string formatWinningTime(TimeSpan duration)
        {
            return $"{(int)duration.TotalMinutes:00}:{duration.Seconds:00}.{duration.Milliseconds / 10:00}";
        }

        private static void drawFilledRect(Display display, int x, int y, int width, int height, Color color)
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

        private static void drawRectOutline(Display display, int x, int y, int width, int height, Color color)
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
    }
}

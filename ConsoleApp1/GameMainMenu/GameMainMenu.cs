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

        private readonly List<MenuButton> menuButtons = new List<MenuButton>();
        private int mouseX;
        private int mouseY;
        private bool isFullscreen;
        private string multiplayerStatus = string.Empty;

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

        public override void initialize()
        {
            var display = Bootstrap.getDisplay();
            display.setSDLSize(DesignWidth, DesignHeight);
            isFullscreen = false;

            Bootstrap.getInput().addListener(this);

            menuButtons.Clear();
            menuButtons.Add(new MenuButton("Single player", startSinglePlayer));
            menuButtons.Add(new MenuButton("Multiplayer", openMultiplayer));
            menuButtons.Add(new MenuButton("Exit", exitGame));
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

            updateButtonLayout(screenW, screenH, uiScale);

            display.showText("BLOONS", titleX, titleY, scaledValue(56, uiScale, minimum: 20), 255, 230, 170);
            display.showText("MAIN MENU", titleX + scaledValue(6, uiScale, minimum: 2), subtitleY, scaledValue(24, uiScale, minimum: 13), 255, 255, 255);

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

            if (eventType != "MouseDown" || input.Button != 1)
            {
                return;
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
            Bootstrap.getInput().removeListener(this);
            Bootstrap.setRunningGame(new GameBloons());
        }

        private void openMultiplayer()
        {
            multiplayerStatus = "Multiplayer is not wired yet.";
        }

        private void exitGame()
        {
            Environment.Exit(0);
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
            return x >= button.X && x <= button.X + button.Width && y >= button.Y && y <= button.Y + button.Height;
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

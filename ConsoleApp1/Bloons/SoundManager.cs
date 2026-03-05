using SDL;
using System;

namespace Shard;

class SoundManager
{
    private const int DesignWidth = 1920;
    private const int DesignHeight = 1080;
    private const int BaseTopMargin = 24;
    private const int BaseRightMargin = 24;
    private const int BaseTrackWidth = 170;
    private const int BaseTrackThickness = 6;
    private const int BaseKnobRadius = 9;
    private const int BaseHitPadding = 8;
    private const int BaseTextSize = 16;
    private const int BaseTextOffsetY = 12;

    private int volumePercent = 1;
    private bool volumeDragging;

    private readonly struct SliderLayout
    {
        public int XStart { get; }
        public int XEnd { get; }
        public int Y { get; }
        public int TrackWidth { get; }
        public int TrackThickness { get; }
        public int KnobRadius { get; }
        public int HitPadding { get; }
        public int TextSize { get; }
        public int TextOffsetY { get; }

        public SliderLayout(
            int xStart,
            int xEnd,
            int y,
            int trackWidth,
            int trackThickness,
            int knobRadius,
            int hitPadding,
            int textSize,
            int textOffsetY)
        {
            XStart = xStart;
            XEnd = xEnd;
            Y = y;
            TrackWidth = trackWidth;
            TrackThickness = trackThickness;
            KnobRadius = knobRadius;
            HitPadding = hitPadding;
            TextSize = textSize;
            TextOffsetY = textOffsetY;
        }
    }

    public void drawVolumeSlider()
    {
        var display = Bootstrap.getDisplay();
        var layout = getVolumeSliderLayout(display.getWidth(), display.getHeight());
        var knobX = volumeToX(volumePercent, layout);

        drawVolumeTrack(layout, layout.XStart, layout.XEnd, 65, 65, 65, 220);
        drawVolumeTrack(layout, layout.XStart, knobX, 90, 180, 255, 255);
        display.drawFilledCircle(knobX, layout.Y, layout.KnobRadius, 235, 245, 255, 255);
        display.showText("VOL " + volumePercent + "%", layout.XStart, layout.Y + layout.TextOffsetY, layout.TextSize, 235, 235, 235);
    }

    private static SliderLayout getVolumeSliderLayout(int displayWidth, int displayHeight)
    {
        var safeWidth = Math.Max(1, displayWidth);
        var safeHeight = Math.Max(1, displayHeight);
        var scale = getUiScale(safeWidth, safeHeight);

        var rightMargin = scaledValue(BaseRightMargin, scale, minimum: 8);
        var topMargin = scaledValue(BaseTopMargin, scale, minimum: 8);
        var trackWidth = Math.Min((int)MathF.Round(safeWidth * 0.22f), scaledValue(BaseTrackWidth, scale, minimum: 90));
        var trackThickness = scaledValue(BaseTrackThickness, scale, minimum: 3);
        var knobRadius = scaledValue(BaseKnobRadius, scale, minimum: 5);
        var hitPadding = scaledValue(BaseHitPadding, scale, minimum: 4);
        var textSize = scaledValue(BaseTextSize, scale, minimum: 10);
        var textOffsetY = scaledValue(BaseTextOffsetY, scale, minimum: 8);

        var xEnd = Math.Max(rightMargin, safeWidth - rightMargin);
        var xStart = Math.Max(0, xEnd - trackWidth);
        if (xStart >= xEnd)
        {
            xStart = Math.Max(0, xEnd - 1);
            trackWidth = Math.Max(1, xEnd - xStart);
        }

        var y = topMargin + knobRadius + 2;
        return new SliderLayout(xStart, xEnd, y, trackWidth, trackThickness, knobRadius, hitPadding, textSize, textOffsetY);
    }

    private static int volumeToX(int volume, SliderLayout layout)
    {
        var clamped = Math.Clamp(volume, 0, 100);
        return layout.XStart + (int)Math.Round((layout.TrackWidth * clamped) / 100.0);
    }

    private static int xToVolume(int x, SliderLayout layout)
    {
        var clampedX = Math.Clamp(x, layout.XStart, layout.XStart + layout.TrackWidth);
        var offset = clampedX - layout.XStart;
        return (int)Math.Round((offset * 100.0) / layout.TrackWidth);
    }

    private static void drawVolumeTrack(SliderLayout layout, int xStart, int xEnd, int r, int g, int b, int a)
    {
        var display = Bootstrap.getDisplay();
        var halfThickness = layout.TrackThickness / 2;
        for (var row = -halfThickness; row <= halfThickness; row += 1)
        {
            display.drawLine(xStart, layout.Y + row, xEnd, layout.Y + row, r, g, b, a);
        }
    }

    public unsafe void handleVolumeInput(MIX_Track* track, InputEvent input, string eventType)
    {
        if (eventType == "MouseDown" && input.Button == 1)
        {
            if (!isInsideVolumeSlider(input.X, input.Y))
            {
                return;
            }

            volumeDragging = true;
            setVolumeFromMouse(track, input.X);
        }

        if (eventType == "MouseMotion" && volumeDragging)
        {
            setVolumeFromMouse(track, input.X);
        }

        if (eventType == "MouseUp" && input.Button == 1 && volumeDragging)
        {
            setVolumeFromMouse(track, input.X);
            volumeDragging = false;
        }

        if (eventType == "MouseUp" && input.Button == 1)
        {
            volumeDragging = false;
        }
    }

    private bool isInsideVolumeSlider(int x, int y)
    {
        var display = Bootstrap.getDisplay();
        var layout = getVolumeSliderLayout(display.getWidth(), display.getHeight());

        var minY = layout.Y - layout.KnobRadius - layout.HitPadding;
        var maxY = layout.Y + layout.KnobRadius + layout.HitPadding;
        var minX = layout.XStart - layout.HitPadding;
        var maxX = layout.XEnd + layout.HitPadding;

        return x >= minX && x <= maxX && y >= minY && y <= maxY;
    }

    private unsafe void setVolumeFromMouse(MIX_Track* track, int mouseXPos)
    {
        var display = Bootstrap.getDisplay();
        var layout = getVolumeSliderLayout(display.getWidth(), display.getHeight());
        var nextVolume = xToVolume(mouseXPos, layout);

        if (nextVolume == volumePercent)
        {
            return;
        }

        volumePercent = nextVolume;
        Bootstrap.getSound().setVolumePercent(track, volumePercent);
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

    public int getVolumePercent()
    {
        return volumePercent;
    }
}

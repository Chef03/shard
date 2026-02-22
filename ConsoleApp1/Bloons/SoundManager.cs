using SDL;
using System;

namespace Shard;

class SoundManager
{
    
    private int volumePercent = 60;
    private const int VolumeTopMargin = 24;
    private const int VolumeRightMargin = 24;
    private const int VolumeTrackWidth = 170;
    private const int VolumeTrackThickness = 6;
    private const int VolumeKnobRadius = 9;
    private const int VolumeHitPadding = 8;
    private bool volumeDragging = false;
    
    public void drawVolumeSlider()
    {
        var display = Bootstrap.getDisplay();
        getVolumeSliderLayout(display.getWidth(), out var xStart, out var xEnd, out var y);
        var knobX = volumeToX(volumePercent, xStart);

        drawVolumeTrack(xStart, xEnd, y, 65, 65, 65, 220);
        drawVolumeTrack(xStart, knobX, y, 90, 180, 255, 255);
        display.drawFilledCircle(knobX, y, VolumeKnobRadius, 235, 245, 255, 255);
        display.showText("VOL " + volumePercent + "%", xStart, y + 12, 16, 235, 235, 235);
    }

    private static void getVolumeSliderLayout(int displayWidth, out int xStart, out int xEnd, out int y)
    {
        xEnd = displayWidth - VolumeRightMargin;
        xStart = xEnd - VolumeTrackWidth;
        y = VolumeTopMargin;
    }

    private static int volumeToX(int volume, int xStart)
    {
        var clamped = Math.Clamp(volume, 0, 100);
        return xStart + (int)Math.Round((VolumeTrackWidth * clamped) / 100.0);
    }

    private static int xToVolume(int x, int xStart)
    {
        var clampedX = Math.Clamp(x, xStart, xStart + VolumeTrackWidth);
        var offset = clampedX - xStart;
        return (int)Math.Round((offset * 100.0) / VolumeTrackWidth);
    }

    private void drawVolumeTrack(int xStart, int xEnd, int y, int r, int g, int b, int a)
    {
        var display = Bootstrap.getDisplay();
        var halfThickness = VolumeTrackThickness / 2;
        for (var row = -halfThickness; row <= halfThickness; row += 1)
        {
            display.drawLine(xStart, y + row, xEnd, y + row, r, g, b, a);
        }
    }
    
    public void handleVolumeInput(InputEvent input, string eventType)
    {
        if (eventType == "MouseDown" && input.Button == 1)
        {
            if (!isInsideVolumeSlider(input.X, input.Y))
            {
                return;
            }

            volumeDragging = true;
            setVolumeFromMouse(input.X);
        }

        if (eventType == "MouseMotion" && volumeDragging)
        {
            setVolumeFromMouse(input.X);
        }

        if (eventType == "MouseUp" && input.Button == 1 && volumeDragging)
        {
            setVolumeFromMouse(input.X);
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
        getVolumeSliderLayout(display.getWidth(), out var xStart, out var xEnd, out var sliderY);

        var minY = sliderY - VolumeKnobRadius - VolumeHitPadding;
        var maxY = sliderY + VolumeKnobRadius + VolumeHitPadding;
        var minX = xStart - VolumeHitPadding;
        var maxX = xEnd + VolumeHitPadding;

        return x >= minX && x <= maxX && y >= minY && y <= maxY;
    }

    private void setVolumeFromMouse(int mouseXPos)
    {
        var display = Bootstrap.getDisplay();
        getVolumeSliderLayout(display.getWidth(), out var xStart, out _, out _);
        var nextVolume = xToVolume(mouseXPos, xStart);

        if (nextVolume == volumePercent)
        {
            return;
        }

        volumePercent = nextVolume;
        Bootstrap.getSound().setVolumePercent(volumePercent);
    }

}
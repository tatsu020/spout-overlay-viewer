using System.Drawing;

namespace SpoutOverlay.App.Helpers;

public static class OverlayBoundsHelper
{
    private const int DefaultWidth = 640;
    private const int DefaultHeight = 360;
    private const int MinimumSize = 64;

    public static Rectangle Normalize(Rectangle bounds)
    {
        if (bounds.Width < MinimumSize || bounds.Height < MinimumSize)
        {
            return Rectangle.Empty;
        }

        return bounds;
    }

    public static Rectangle CreateDefault(Rectangle workingArea, int sourceWidth, int sourceHeight)
    {
        if (sourceWidth <= 0 || sourceHeight <= 0)
        {
            sourceWidth = DefaultWidth;
            sourceHeight = DefaultHeight;
        }

        var aspect = (double)sourceWidth / sourceHeight;
        var width = Math.Min(workingArea.Width / 2, sourceWidth);
        if (width < MinimumSize)
        {
            width = DefaultWidth;
        }

        var height = (int)Math.Round(width / aspect);
        if (height > workingArea.Height / 2)
        {
            height = workingArea.Height / 2;
            width = (int)Math.Round(height * aspect);
        }

        width = Math.Max(width, MinimumSize);
        height = Math.Max(height, MinimumSize);

        var x = workingArea.Left + ((workingArea.Width - width) / 2);
        var y = workingArea.Top + ((workingArea.Height - height) / 2);
        return new Rectangle(x, y, width, height);
    }
}

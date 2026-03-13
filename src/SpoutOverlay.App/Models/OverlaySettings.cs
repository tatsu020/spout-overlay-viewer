using System.Drawing;

namespace SpoutOverlay.App.Models;

public sealed class OverlaySettings
{
    public const float DefaultOverlayOpacity = 1.0f;
    public const string DefaultVisibilityToggleHotKey = "Ctrl+Shift+H";

    public string? SelectedSenderName { get; set; }

    public Rectangle WindowBounds { get; set; } = Rectangle.Empty;

    public OverlayMode Mode { get; set; } = OverlayMode.Adjust;

    public bool AlwaysOnTop { get; set; } = true;

    public float OverlayOpacity { get; set; } = DefaultOverlayOpacity;

    public string VisibilityToggleHotKey { get; set; } = DefaultVisibilityToggleHotKey;

    public int LastKnownSourceWidth { get; set; }

    public int LastKnownSourceHeight { get; set; }

    public static float ClampOverlayOpacity(float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
        {
            return DefaultOverlayOpacity;
        }

        return Math.Clamp(value, 0f, 1f);
    }
}

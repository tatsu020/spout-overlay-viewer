using System.Drawing;
using SpoutOverlay.App.Models;

namespace SpoutOverlay.App.Helpers;

internal static class OverlayOpacitySliderLayout
{
    private const float MinTrackWidth = 140f;
    private const float MaxTrackWidth = 260f;
    private const float HorizontalPadding = 28f;
    private const float TrackHeight = 6f;
    private const float ThumbRadius = 9f;
    private const float BottomMargin = 26f;
    private const float HitPadding = 12f;

    public static RectangleF GetTrackBounds(Size clientSize)
    {
        var availableWidth = Math.Max(0f, clientSize.Width - (HorizontalPadding * 2f));
        var width = availableWidth <= 0f
            ? 0f
            : availableWidth < MinTrackWidth
                ? availableWidth
                : Math.Min(availableWidth, MaxTrackWidth);
        var left = Math.Max(HorizontalPadding, (clientSize.Width - width) / 2f);
        var top = Math.Max(ThumbRadius + HitPadding, clientSize.Height - BottomMargin - ThumbRadius - TrackHeight);
        return new RectangleF(left, top, width, TrackHeight);
    }

    public static RectangleF GetInteractiveBounds(Size clientSize)
    {
        var track = GetTrackBounds(clientSize);
        return RectangleF.Inflate(track, ThumbRadius + HitPadding, ThumbRadius + HitPadding);
    }

    public static PointF GetThumbCenter(Size clientSize, float opacity)
    {
        var track = GetTrackBounds(clientSize);
        var normalizedOpacity = OverlaySettings.ClampOverlayOpacity(opacity);
        return new PointF(track.Left + (track.Width * normalizedOpacity), track.Top + (track.Height / 2f));
    }

    public static float GetOpacityFromPoint(Size clientSize, Point point)
    {
        var track = GetTrackBounds(clientSize);
        if (track.Width <= 0f)
        {
            return OverlaySettings.DefaultOverlayOpacity;
        }

        var x = Math.Clamp(point.X, track.Left, track.Right);
        var value = (x - track.Left) / track.Width;
        return OverlaySettings.ClampOverlayOpacity(value);
    }
}

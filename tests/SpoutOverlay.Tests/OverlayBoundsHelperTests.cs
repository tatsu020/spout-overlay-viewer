using System.Drawing;
using SpoutOverlay.App.Helpers;

namespace SpoutOverlay.Tests;

public sealed class OverlayBoundsHelperTests
{
    [Fact]
    public void NormalizeRejectsTooSmallBounds()
    {
        Assert.Equal(Rectangle.Empty, OverlayBoundsHelper.Normalize(new Rectangle(0, 0, 10, 10)));
    }

    [Fact]
    public void CreateDefaultCentersInWorkingArea()
    {
        var workingArea = new Rectangle(100, 200, 1200, 800);

        var bounds = OverlayBoundsHelper.CreateDefault(workingArea, 1920, 1080);

        Assert.True(bounds.Width >= 64);
        Assert.True(bounds.Height >= 64);
        Assert.True(workingArea.Contains(bounds));
    }
}

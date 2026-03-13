using System.Windows.Forms;
using SpoutOverlay.App.Helpers;
using SpoutOverlay.App.Models;

namespace SpoutOverlay.Tests;

public sealed class GlobalHotKeyGestureTests
{
    [Fact]
    public void ParseOrDefaultNormalizesShortcutText()
    {
        var gesture = GlobalHotKeyGesture.ParseOrDefault("control+alt+f10");

        Assert.Equal(Keys.F10, gesture.Key);
        Assert.Equal(0x4003u, gesture.Modifiers);
        Assert.Equal("Ctrl+Alt+F10", gesture.DisplayText);
    }

    [Fact]
    public void ParseOrDefaultFallsBackForModifierOnlyShortcut()
    {
        var gesture = GlobalHotKeyGesture.ParseOrDefault("Shift");

        Assert.Equal(OverlaySettings.DefaultVisibilityToggleHotKey, gesture.DisplayText);
    }

    [Fact]
    public void TryCreateFromKeyDataBuildsGestureFromPressedKeys()
    {
        var success = GlobalHotKeyGesture.TryCreateFromKeyData(Keys.Control | Keys.Shift | Keys.K, out var gesture);

        Assert.True(success);
        Assert.Equal("Ctrl+Shift+K", gesture.DisplayText);
    }

    [Fact]
    public void TryCreateFromKeyDataAllowsSingleNonModifierKey()
    {
        var success = GlobalHotKeyGesture.TryCreateFromKeyData(Keys.F8, out var gesture);

        Assert.True(success);
        Assert.Equal("F8", gesture.DisplayText);
    }
}

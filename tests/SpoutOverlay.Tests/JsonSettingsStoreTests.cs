using System.Drawing;
using SpoutOverlay.App.Helpers;
using SpoutOverlay.App.Models;
using SpoutOverlay.App.Services;

namespace SpoutOverlay.Tests;

public sealed class JsonSettingsStoreTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), "SpoutOverlayTests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void SaveAndLoadRoundTripsSettings()
    {
        Directory.CreateDirectory(_tempDirectory);
        var path = Path.Combine(_tempDirectory, "settings.json");
        var store = new JsonSettingsStore(path);
        var settings = new OverlaySettings
        {
            SelectedSenderName = "SenderA",
            WindowBounds = new Rectangle(10, 20, 300, 200),
            Mode = OverlayMode.Passthrough,
            AlwaysOnTop = true,
            OverlayOpacity = 0.35f,
            VisibilityToggleHotKey = "Ctrl+Alt+F10",
            LastKnownSourceWidth = 1920,
            LastKnownSourceHeight = 1080
        };

        store.Save(settings);
        var loaded = store.Load();

        Assert.Equal("SenderA", loaded.SelectedSenderName);
        Assert.Equal(new Rectangle(10, 20, 300, 200), loaded.WindowBounds);
        Assert.Equal(OverlayMode.Passthrough, loaded.Mode);
        Assert.Equal(0.35f, loaded.OverlayOpacity);
        Assert.Equal("Ctrl+Alt+F10", loaded.VisibilityToggleHotKey);
        Assert.Equal(1920, loaded.LastKnownSourceWidth);
        Assert.Equal(1080, loaded.LastKnownSourceHeight);
    }

    [Fact]
    public void LoadFallsBackToDefaultsOnInvalidJson()
    {
        Directory.CreateDirectory(_tempDirectory);
        var path = Path.Combine(_tempDirectory, "settings.json");
        File.WriteAllText(path, "{ not json");
        var store = new JsonSettingsStore(path);

        var loaded = store.Load();

        Assert.Equal(OverlayMode.Adjust, loaded.Mode);
        Assert.Equal(Rectangle.Empty, loaded.WindowBounds);
        Assert.Null(loaded.SelectedSenderName);
        Assert.Equal(OverlaySettings.DefaultOverlayOpacity, loaded.OverlayOpacity);
        Assert.Equal(OverlaySettings.DefaultVisibilityToggleHotKey, loaded.VisibilityToggleHotKey);
    }

    [Fact]
    public void LoadFallsBackToDefaultHotKeyOnInvalidShortcut()
    {
        Directory.CreateDirectory(_tempDirectory);
        var path = Path.Combine(_tempDirectory, "settings.json");
        File.WriteAllText(path, """
        {
          "visibilityToggleHotKey": "Shift"
        }
        """);

        var store = new JsonSettingsStore(path);
        var loaded = store.Load();

        Assert.Equal(OverlaySettings.DefaultVisibilityToggleHotKey, loaded.VisibilityToggleHotKey);
        Assert.Equal(OverlaySettings.DefaultVisibilityToggleHotKey, GlobalHotKeyGesture.ParseOrDefault(loaded.VisibilityToggleHotKey).DisplayText);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }
}

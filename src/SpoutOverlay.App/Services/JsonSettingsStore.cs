using System.Text.Json;
using SpoutOverlay.App.Contracts;
using SpoutOverlay.App.Helpers;
using SpoutOverlay.App.Models;

namespace SpoutOverlay.App.Services;

public sealed class JsonSettingsStore : ISettingsStore
{
    private readonly string _settingsPath;
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        WriteIndented = true
    };

    public JsonSettingsStore()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _settingsPath = Path.Combine(appData, "SpoutOverlay", "settings.json");
    }

    public JsonSettingsStore(string settingsPath)
    {
        _settingsPath = settingsPath;
    }

    public OverlaySettings Load()
    {
        if (!File.Exists(_settingsPath))
        {
            return new OverlaySettings();
        }

        try
        {
            var json = File.ReadAllText(_settingsPath);
            var settings = JsonSerializer.Deserialize<OverlaySettings>(json, _serializerOptions) ?? new OverlaySettings();
            settings.WindowBounds = OverlayBoundsHelper.Normalize(settings.WindowBounds);
            settings.OverlayOpacity = OverlaySettings.ClampOverlayOpacity(settings.OverlayOpacity);
            settings.VisibilityToggleHotKey = GlobalHotKeyGesture.ParseOrDefault(settings.VisibilityToggleHotKey).DisplayText;
            return settings;
        }
        catch (Exception)
        {
            return new OverlaySettings();
        }
    }

    public void Save(OverlaySettings settings)
    {
        var directory = Path.GetDirectoryName(_settingsPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        settings.WindowBounds = OverlayBoundsHelper.Normalize(settings.WindowBounds);
        settings.OverlayOpacity = OverlaySettings.ClampOverlayOpacity(settings.OverlayOpacity);
        settings.VisibilityToggleHotKey = GlobalHotKeyGesture.ParseOrDefault(settings.VisibilityToggleHotKey).DisplayText;
        var json = JsonSerializer.Serialize(settings, _serializerOptions);
        File.WriteAllText(_settingsPath, json);
    }
}

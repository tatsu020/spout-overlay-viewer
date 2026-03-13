using SpoutOverlay.App.Models;

namespace SpoutOverlay.App.Contracts;

public interface ISettingsStore
{
    OverlaySettings Load();

    void Save(OverlaySettings settings);
}

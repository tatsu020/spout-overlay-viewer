using SpoutOverlay.App.Models;

namespace SpoutOverlay.App.Services;

public sealed class OverlayStateCoordinator
{
    private readonly HashSet<string> _availableSenders = new(StringComparer.Ordinal);

    public OverlayMode Mode { get; private set; } = OverlayMode.Adjust;

    public string? SelectedSenderName { get; private set; }

    public bool WaitingForReconnect { get; private set; }

    public IReadOnlyCollection<string> AvailableSenders => _availableSenders;

    public void SetMode(OverlayMode mode)
    {
        Mode = mode;
    }

    public void SelectSender(string? senderName)
    {
        SelectedSenderName = string.IsNullOrWhiteSpace(senderName) ? null : senderName;
        WaitingForReconnect = false;
    }

    public void UpdateAvailableSenders(IEnumerable<string> senderNames)
    {
        _availableSenders.Clear();
        foreach (var senderName in senderNames.Where(static name => !string.IsNullOrWhiteSpace(name)))
        {
            _availableSenders.Add(senderName);
        }

        if (!string.IsNullOrWhiteSpace(SelectedSenderName))
        {
            WaitingForReconnect = !_availableSenders.Contains(SelectedSenderName);
        }
        else
        {
            WaitingForReconnect = false;
        }
    }
}

using SpoutOverlay.App.Models;

namespace SpoutOverlay.App.Contracts;

public interface ITrayMenuController : IDisposable
{
    event EventHandler? AdjustModeRequested;

    event EventHandler? PassthroughModeRequested;

    event EventHandler? ResetBoundsRequested;

    event EventHandler? ExitRequested;

    event EventHandler? VisibilityToggleRequested;

    event EventHandler? VisibilityHotKeyChangeRequested;

    event EventHandler<string?>? SenderSelected;

    void UpdateMode(OverlayMode mode);

    void UpdateVisibility(bool isVisible, string shortcutText);

    void UpdateSenders(IReadOnlyList<string> senders, string? selectedSenderName, bool waitingForReconnect);
}

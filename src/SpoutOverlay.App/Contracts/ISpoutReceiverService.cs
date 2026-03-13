using SpoutOverlay.App.Models;

namespace SpoutOverlay.App.Contracts;

public interface ISpoutReceiverService : IDisposable
{
    SenderInfo? CurrentSenderInfo { get; }

    IReadOnlyList<string> GetAvailableSenders();

    void SelectSender(string? name);

    SpoutFrameResult? TryReceiveFrame();
}

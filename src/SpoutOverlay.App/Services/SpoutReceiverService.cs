using SpoutDx.Net.Interop;
using SpoutOverlay.App.Contracts;
using SpoutOverlay.App.Models;

namespace SpoutOverlay.App.Services;

public sealed class SpoutReceiverService : ISpoutReceiverService
{
    private readonly SpoutReceiver _receiver;
    private nint _currentTexturePointer;

    public SpoutReceiverService(nint d3d11DevicePointer)
    {
        _receiver = new SpoutReceiver(d3d11DevicePointer);
    }

    public SenderInfo? CurrentSenderInfo
    {
        get
        {
            if (!_receiver.IsConnected || string.IsNullOrWhiteSpace(_receiver.SenderName))
            {
                return null;
            }

            return new SenderInfo(
                _receiver.SenderName,
                _receiver.SenderTextureWidth,
                _receiver.SenderTextureHeight,
                _receiver.SenderTextureFormat,
                _receiver.IsConnected);
        }
    }

    public IReadOnlyList<string> GetAvailableSenders()
    {
        return _receiver.GetSenderNames()
            .Where(static name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public void SelectSender(string? name)
    {
        _currentTexturePointer = nint.Zero;
        _receiver.SenderName = string.IsNullOrWhiteSpace(name) ? string.Empty : name;
    }

    public SpoutFrameResult? TryReceiveFrame()
    {
        if (_receiver.IsUpdated() || _currentTexturePointer == nint.Zero)
        {
            _currentTexturePointer = _receiver.GetSenderTexture();
        }

        var received = _receiver.ReceiveTexture();
        if (!received || !_receiver.IsConnected || _currentTexturePointer == nint.Zero)
        {
            return null;
        }

        var sender = CurrentSenderInfo;
        if (sender is null)
        {
            return null;
        }

        return new SpoutFrameResult(sender, _currentTexturePointer, _receiver.IsFrameNew());
    }

    public void Dispose()
    {
        _receiver.Dispose();
    }
}

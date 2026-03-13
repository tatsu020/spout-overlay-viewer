namespace SpoutOverlay.App.Models;

public sealed class SpoutFrameResult
{
    public SpoutFrameResult(SenderInfo sender, nint texturePointer, bool isFrameNew)
    {
        Sender = sender;
        TexturePointer = texturePointer;
        IsFrameNew = isFrameNew;
    }

    public SenderInfo Sender { get; }

    public nint TexturePointer { get; }

    public bool IsFrameNew { get; }
}

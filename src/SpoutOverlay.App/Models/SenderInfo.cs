namespace SpoutOverlay.App.Models;

public sealed class SenderInfo
{
    public SenderInfo(string name, uint width, uint height, uint format, bool isConnected)
    {
        Name = name;
        Width = width;
        Height = height;
        Format = format;
        IsConnected = isConnected;
    }

    public string Name { get; }

    public uint Width { get; }

    public uint Height { get; }

    public uint Format { get; }

    public bool IsConnected { get; }
}

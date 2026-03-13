using System.Drawing;
using SpoutOverlay.App.Models;

namespace SpoutOverlay.App.Contracts;

public interface IOverlayWindowController : IDisposable
{
    event EventHandler<Rectangle>? BoundsChanged;

    event EventHandler<float>? OverlayOpacityChanged;

    Rectangle Bounds { get; }

    nint WindowHandle { get; }

    OverlayMode Mode { get; }

    float OverlayOpacity { get; }

    bool IsVisible { get; }

    void Show();

    void Hide();

    void Close();

    void ShowAdjustFrame();

    void HideAdjustFrame();

    void SetClickThrough(bool enabled);

    void SetBounds(Rectangle bounds);

    void ResetBounds();

    void SetOverlayMode(OverlayMode mode);

    void SetOverlayOpacity(float opacity);

    void SetSourceSize(int width, int height);
}

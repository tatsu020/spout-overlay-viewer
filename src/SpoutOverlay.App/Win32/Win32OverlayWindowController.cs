using System.Drawing;
using System.Windows.Forms;
using SpoutOverlay.App.Contracts;
using SpoutOverlay.App.Helpers;
using SpoutOverlay.App.Models;

namespace SpoutOverlay.App.Win32;

public sealed class Win32OverlayWindowController : IOverlayWindowController
{
    private readonly OverlayRenderForm _form;

    public Win32OverlayWindowController(Rectangle initialBounds, OverlayMode mode, float overlayOpacity)
    {
        var normalized = OverlayBoundsHelper.Normalize(initialBounds);
        _form = new OverlayRenderForm(
            normalized == Rectangle.Empty
                ? OverlayBoundsHelper.CreateDefault(Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(100, 100, 1280, 720), 640, 360)
                : normalized,
            mode,
            overlayOpacity);
        _form.BoundsCommitted += (_, bounds) => BoundsChanged?.Invoke(this, bounds);
        _form.OverlayOpacityChanged += (_, opacity) => OverlayOpacityChanged?.Invoke(this, opacity);
    }

    public event EventHandler<Rectangle>? BoundsChanged;

    public event EventHandler<float>? OverlayOpacityChanged;

    public Rectangle Bounds => _form.Bounds;

    public nint WindowHandle => _form.Handle;

    public OverlayMode Mode => _form.Mode;

    public float OverlayOpacity => _form.OverlayOpacity;

    public bool IsVisible => _form.Visible;

    public void Show()
    {
        if (!_form.Visible)
        {
            _form.Show();
        }
    }

    public void Hide()
    {
        if (_form.Visible)
        {
            _form.Hide();
        }
    }

    public void Close()
    {
        _form.Close();
    }

    public void ShowAdjustFrame()
    {
        _form.SetOverlayMode(OverlayMode.Adjust);
    }

    public void HideAdjustFrame()
    {
        _form.SetOverlayMode(OverlayMode.Passthrough);
    }

    public void SetClickThrough(bool enabled)
    {
        _form.SetOverlayMode(enabled ? OverlayMode.Passthrough : OverlayMode.Adjust);
    }

    public void SetBounds(Rectangle bounds)
    {
        var normalized = OverlayBoundsHelper.Normalize(bounds);
        if (normalized != Rectangle.Empty)
        {
            _form.Bounds = normalized;
        }
    }

    public void ResetBounds()
    {
        _form.ResetBounds();
    }

    public void SetOverlayMode(OverlayMode mode)
    {
        _form.SetOverlayMode(mode);
    }

    public void SetOverlayOpacity(float opacity)
    {
        _form.SetOverlayOpacity(opacity);
    }

    public void SetSourceSize(int width, int height)
    {
        _form.SetSourceSize(width, height);
    }

    public void Dispose()
    {
        _form.Dispose();
    }
}

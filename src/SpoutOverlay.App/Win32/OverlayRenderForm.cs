using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SpoutOverlay.App.Helpers;
using SpoutOverlay.App.Models;

namespace SpoutOverlay.App.Win32;

internal sealed class OverlayRenderForm : Form
{
    private const int ResizeGrip = 12;
    private const int OverlayExStyle = NativeMethods.WsExToolwindow | NativeMethods.WsExTopmost | NativeMethods.WsExLayered;
    private const int ClickThroughExStyle = OverlayExStyle | NativeMethods.WsExTransparent;
    private double _aspectRatio = 16d / 9d;
    private bool _opacityDragActive;
    private float _overlayOpacity;

    public OverlayRenderForm(Rectangle initialBounds, OverlayMode mode, float overlayOpacity)
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        TopMost = true;
        Bounds = initialBounds;
        Mode = mode;
        _overlayOpacity = OverlaySettings.ClampOverlayOpacity(overlayOpacity);
        DoubleBuffered = false;
        SetStyle(ControlStyles.Opaque, true);
    }

    public event EventHandler<Rectangle>? BoundsCommitted;

    public event EventHandler<float>? OverlayOpacityChanged;

    public OverlayMode Mode { get; private set; }

    public float OverlayOpacity => _overlayOpacity;

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.Style = NativeMethods.WsPopup;
            cp.ExStyle |= OverlayExStyle;
            return cp;
        }
    }

    protected override bool ShowWithoutActivation => true;

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ApplyTopMost();
        ApplyClickThrough(Mode == OverlayMode.Passthrough);
    }

    public void SetSourceSize(int width, int height)
    {
        if (width > 0 && height > 0)
        {
            _aspectRatio = (double)width / height;
        }
    }

    public void SetOverlayMode(OverlayMode mode)
    {
        Mode = mode;
        ReleaseOpacityDrag();
        ApplyClickThrough(mode == OverlayMode.Passthrough);
    }

    public void SetOverlayOpacity(float opacity)
    {
        _overlayOpacity = OverlaySettings.ClampOverlayOpacity(opacity);
    }

    public void ResetBounds()
    {
        var workingArea = Screen.FromPoint(Location).WorkingArea;
        Bounds = OverlayBoundsHelper.CreateDefault(workingArea, (int)Math.Round(_aspectRatio * 360), 360);
        BoundsCommitted?.Invoke(this, Bounds);
    }

    private void ApplyTopMost()
    {
        NativeMethods.SetWindowPos(
            Handle,
            NativeMethods.HwndTopmost,
            0,
            0,
            0,
            0,
            NativeMethods.SwpNoActivate | NativeMethods.SwpNoMove | NativeMethods.SwpNoSize | NativeMethods.SwpNoOwnerZOrder);
    }

    private void ApplyClickThrough(bool enabled)
    {
        if (!IsHandleCreated)
        {
            return;
        }

        var currentStyle = NativeMethods.GetWindowLongPtr(Handle, NativeMethods.GwlExstyle).ToInt64();
        var preservedStyle = currentStyle & ~(long)ClickThroughExStyle;
        var desiredStyle = preservedStyle | (long)(enabled ? ClickThroughExStyle : OverlayExStyle);

        NativeMethods.SetWindowLongPtr(Handle, NativeMethods.GwlExstyle, new nint(desiredStyle));
        NativeMethods.SetWindowPos(
            Handle,
            NativeMethods.HwndTopmost,
            0,
            0,
            0,
            0,
            NativeMethods.SwpFrameChanged | NativeMethods.SwpNoActivate | NativeMethods.SwpNoMove | NativeMethods.SwpNoSize | NativeMethods.SwpNoOwnerZOrder);
    }

    protected override void WndProc(ref Message m)
    {
        switch (m.Msg)
        {
            case NativeMethods.WmNchittest:
                if (Mode == OverlayMode.Adjust)
                {
                    m.Result = new nint(GetHitTest(PointToClient(GetScreenPoint(m.LParam))));
                    return;
                }

                // Keep passthrough mode non-interactive even if a stale hit test occurs.
                m.Result = new nint(NativeMethods.HtTransparent);
                return;

            case NativeMethods.WmSizing:
                if (Mode == OverlayMode.Adjust)
                {
                    MaintainAspectRatio(m.LParam, m.WParam.ToInt32());
                }

                break;
            case NativeMethods.WmExitsizemove:
                BoundsCommitted?.Invoke(this, Bounds);
                break;
        }

        base.WndProc(ref m);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (Mode == OverlayMode.Adjust &&
            e.Button == MouseButtons.Left &&
            OverlayOpacitySliderLayout.GetInteractiveBounds(ClientSize).Contains(e.Location))
        {
            _opacityDragActive = true;
            Capture = true;
            UpdateOverlayOpacity(e.Location);
        }

        base.OnMouseDown(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (Mode == OverlayMode.Adjust)
        {
            if (_opacityDragActive)
            {
                UpdateOverlayOpacity(e.Location);
            }

            Cursor = OverlayOpacitySliderLayout.GetInteractiveBounds(ClientSize).Contains(e.Location) || _opacityDragActive
                ? Cursors.Hand
                : Cursors.Default;
        }
        else
        {
            Cursor = Cursors.Default;
        }

        base.OnMouseMove(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            ReleaseOpacityDrag();
        }

        base.OnMouseUp(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        if (!_opacityDragActive)
        {
            Cursor = Cursors.Default;
        }

        base.OnMouseLeave(e);
    }

    protected override void OnMouseCaptureChanged(EventArgs e)
    {
        ReleaseOpacityDrag();
        base.OnMouseCaptureChanged(e);
    }

    private int GetHitTest(Point point)
    {
        if (point.X < 0 || point.Y < 0 || point.X >= Width || point.Y >= Height)
        {
            return NativeMethods.HtNowhere;
        }

        if (OverlayOpacitySliderLayout.GetInteractiveBounds(ClientSize).Contains(point))
        {
            return NativeMethods.HtClient;
        }

        var left = point.X <= ResizeGrip;
        var right = point.X >= Width - ResizeGrip;
        var top = point.Y <= ResizeGrip;
        var bottom = point.Y >= Height - ResizeGrip;

        if (left && top)
        {
            return NativeMethods.HtTopLeft;
        }

        if (right && top)
        {
            return NativeMethods.HtTopRight;
        }

        if (left && bottom)
        {
            return NativeMethods.HtBottomLeft;
        }

        if (right && bottom)
        {
            return NativeMethods.HtBottomRight;
        }

        if (left)
        {
            return NativeMethods.HtLeft;
        }

        if (right)
        {
            return NativeMethods.HtRight;
        }

        if (top)
        {
            return NativeMethods.HtTop;
        }

        if (bottom)
        {
            return NativeMethods.HtBottom;
        }

        return NativeMethods.HtCaption;
    }

    private static Point GetScreenPoint(nint lParam)
    {
        var value = lParam.ToInt64();
        var x = unchecked((short)(value & 0xFFFF));
        var y = unchecked((short)((value >> 16) & 0xFFFF));
        return new Point(x, y);
    }

    private void UpdateOverlayOpacity(Point point)
    {
        var opacity = OverlayOpacitySliderLayout.GetOpacityFromPoint(ClientSize, point);
        if (Math.Abs(_overlayOpacity - opacity) < 0.001f)
        {
            return;
        }

        _overlayOpacity = opacity;
        OverlayOpacityChanged?.Invoke(this, _overlayOpacity);
    }

    private void ReleaseOpacityDrag()
    {
        if (!_opacityDragActive)
        {
            return;
        }

        _opacityDragActive = false;
        if (Capture)
        {
            Capture = false;
        }
    }

    private void MaintainAspectRatio(nint rectPointer, int edge)
    {
        if (rectPointer == nint.Zero || _aspectRatio <= 0d)
        {
            return;
        }

        var rect = Marshal.PtrToStructure<NativeMethods.Rect>(rectPointer);
        var width = rect.Right - rect.Left;
        var height = rect.Bottom - rect.Top;

        if (width <= 0 || height <= 0)
        {
            return;
        }

        switch (edge)
        {
            case 1:
            case 2:
            case 4:
            case 5:
                height = (int)Math.Round(width / _aspectRatio);
                rect.Bottom = rect.Top + height;
                break;
            default:
                width = (int)Math.Round(height * _aspectRatio);
                rect.Right = rect.Left + width;
                break;
        }

        Marshal.StructureToPtr(rect, rectPointer, false);
    }
}

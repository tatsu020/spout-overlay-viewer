using System.Windows.Forms;

namespace SpoutOverlay.App.Win32;

internal sealed class GlobalHotKeyWindow : NativeWindow, IDisposable
{
    private readonly int _hotKeyId;
    private bool _registered;

    private GlobalHotKeyWindow(int hotKeyId)
    {
        _hotKeyId = hotKeyId;
        CreateHandle(new CreateParams());
    }

    public event EventHandler? HotKeyPressed;

    public static GlobalHotKeyWindow? TryCreate(int hotKeyId, uint modifiers, Keys key)
    {
        var window = new GlobalHotKeyWindow(hotKeyId);
        if (!NativeMethods.RegisterHotKey(window.Handle, hotKeyId, modifiers, (uint)key))
        {
            window.Dispose();
            return null;
        }

        window._registered = true;
        return window;
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.WmHotkey && m.WParam.ToInt32() == _hotKeyId)
        {
            HotKeyPressed?.Invoke(this, EventArgs.Empty);
            return;
        }

        base.WndProc(ref m);
    }

    public void Dispose()
    {
        if (Handle != nint.Zero)
        {
            if (_registered)
            {
                NativeMethods.UnregisterHotKey(Handle, _hotKeyId);
                _registered = false;
            }

            DestroyHandle();
        }
    }
}

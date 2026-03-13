using System.Drawing;
using System.Windows.Forms;
using SpoutOverlay.App.Contracts;
using SpoutOverlay.App.Models;

namespace SpoutOverlay.App.Services;

public sealed class TrayMenuController : ITrayMenuController
{
    private static readonly Icon NotifyIconGraphic = LoadNotifyIcon();

    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _menu;
    private readonly ToolStripMenuItem _adjustModeItem;
    private readonly ToolStripMenuItem _passthroughModeItem;
    private readonly ToolStripMenuItem _toggleVisibilityItem;
    private readonly ToolStripMenuItem _configureVisibilityHotKeyItem;
    private readonly ToolStripMenuItem _sendersRootItem;
    private readonly ToolStripMenuItem _resetBoundsItem;
    private readonly ToolStripMenuItem _exitItem;
    private SenderMenuState? _senderMenuState;
    private SenderMenuState? _pendingSenderMenuState;

    public TrayMenuController()
    {
        _menu = new ContextMenuStrip();
        _adjustModeItem = new ToolStripMenuItem("調整モード");
        _passthroughModeItem = new ToolStripMenuItem("クリック透過モード");
        _toggleVisibilityItem = new ToolStripMenuItem();
        _configureVisibilityHotKeyItem = new ToolStripMenuItem();
        _sendersRootItem = new ToolStripMenuItem("送信元選択");
        _resetBoundsItem = new ToolStripMenuItem("位置/サイズリセット");
        _exitItem = new ToolStripMenuItem("終了");

        _adjustModeItem.Click += (_, _) => AdjustModeRequested?.Invoke(this, EventArgs.Empty);
        _passthroughModeItem.Click += (_, _) => PassthroughModeRequested?.Invoke(this, EventArgs.Empty);
        _toggleVisibilityItem.Click += (_, _) => VisibilityToggleRequested?.Invoke(this, EventArgs.Empty);
        _configureVisibilityHotKeyItem.Click += (_, _) => VisibilityHotKeyChangeRequested?.Invoke(this, EventArgs.Empty);
        _resetBoundsItem.Click += (_, _) => ResetBoundsRequested?.Invoke(this, EventArgs.Empty);
        _exitItem.Click += (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty);
        _sendersRootItem.DropDown.Closed += OnSendersDropDownClosed;

        _menu.Items.AddRange(
        [
            _adjustModeItem,
            _passthroughModeItem,
            _toggleVisibilityItem,
            _configureVisibilityHotKeyItem,
            new ToolStripSeparator(),
            _sendersRootItem,
            new ToolStripSeparator(),
            _resetBoundsItem,
            _exitItem
        ]);
        UpdateVisibility(true, OverlaySettings.DefaultVisibilityToggleHotKey);

        _notifyIcon = new NotifyIcon
        {
            Text = "Spout Overlay",
            Icon = NotifyIconGraphic,
            Visible = true,
            ContextMenuStrip = _menu
        };
    }

    public event EventHandler? AdjustModeRequested;

    public event EventHandler? PassthroughModeRequested;

    public event EventHandler? ResetBoundsRequested;

    public event EventHandler? ExitRequested;

    public event EventHandler? VisibilityToggleRequested;

    public event EventHandler? VisibilityHotKeyChangeRequested;

    public event EventHandler<string?>? SenderSelected;

    public void UpdateMode(OverlayMode mode)
    {
        _adjustModeItem.Checked = mode == OverlayMode.Adjust;
        _passthroughModeItem.Checked = mode == OverlayMode.Passthrough;
    }

    public void UpdateVisibility(bool isVisible, string shortcutText)
    {
        _toggleVisibilityItem.Text = isVisible
            ? $"オーバーレイを隠す ({shortcutText})"
            : $"オーバーレイを表示する ({shortcutText})";
        _configureVisibilityHotKeyItem.Text = "表示切替ショートカット設定...";
    }

    public void UpdateSenders(IReadOnlyList<string> senders, string? selectedSenderName, bool waitingForReconnect)
    {
        var nextState = new SenderMenuState([.. senders], selectedSenderName, waitingForReconnect);
        if (_sendersRootItem.DropDown.Visible)
        {
            _pendingSenderMenuState = nextState;
            return;
        }

        if (SenderMenuStateEquals(_senderMenuState, nextState))
        {
            return;
        }

        ApplySenderMenuState(nextState);
    }

    private void ApplySenderMenuState(SenderMenuState state)
    {
        _sendersRootItem.DropDownItems.Clear();

        if (state.Senders.Count == 0)
        {
            var emptyItem = new ToolStripMenuItem(state.WaitingForReconnect ? "送信元待機中..." : "送信元が見つかりません")
            {
                Enabled = false
            };
            _sendersRootItem.DropDownItems.Add(emptyItem);
        }
        else
        {
            foreach (var sender in state.Senders)
            {
                var item = new ToolStripMenuItem(sender)
                {
                    Checked = string.Equals(sender, state.SelectedSenderName, StringComparison.Ordinal)
                };
                item.Click += (_, _) => SenderSelected?.Invoke(this, sender);
                _sendersRootItem.DropDownItems.Add(item);
            }
        }

        _senderMenuState = state;
    }

    private void OnSendersDropDownClosed(object? sender, ToolStripDropDownClosedEventArgs e)
    {
        if (_pendingSenderMenuState is null)
        {
            return;
        }

        var pendingState = _pendingSenderMenuState;
        _pendingSenderMenuState = null;
        if (!SenderMenuStateEquals(_senderMenuState, pendingState))
        {
            ApplySenderMenuState(pendingState);
        }
    }

    private static bool SenderMenuStateEquals(SenderMenuState? left, SenderMenuState? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null)
        {
            return false;
        }

        return left.WaitingForReconnect == right.WaitingForReconnect &&
            string.Equals(left.SelectedSenderName, right.SelectedSenderName, StringComparison.Ordinal) &&
            left.Senders.SequenceEqual(right.Senders, StringComparer.Ordinal);
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _menu.Dispose();
    }

    private static Icon LoadNotifyIcon()
    {
        var assetPath = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
        if (File.Exists(assetPath))
        {
            return new Icon(assetPath);
        }

        var executablePath = Application.ExecutablePath;
        if (!string.IsNullOrWhiteSpace(executablePath) && File.Exists(executablePath))
        {
            var extracted = Icon.ExtractAssociatedIcon(executablePath);
            if (extracted is not null)
            {
                return extracted;
            }
        }

        return SystemIcons.Application;
    }

    private sealed record SenderMenuState(IReadOnlyList<string> Senders, string? SelectedSenderName, bool WaitingForReconnect);
}

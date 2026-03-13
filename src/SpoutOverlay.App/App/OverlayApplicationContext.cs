using System.Drawing;
using System.Windows.Forms;
using SpoutOverlay.App.Contracts;
using SpoutOverlay.App.Helpers;
using SpoutOverlay.App.Models;
using SpoutOverlay.App.Rendering;
using SpoutOverlay.App.Services;
using SpoutOverlay.App.Win32;

namespace SpoutOverlay.App.App;

public sealed class OverlayApplicationContext : ApplicationContext
{
    private const int ToggleVisibilityHotKeyId = 1;

    private readonly ISettingsStore _settingsStore;
    private readonly OverlayStateCoordinator _stateCoordinator;
    private readonly IOverlayWindowController _overlayWindowController;
    private readonly OverlayRenderer _renderer;
    private readonly ISpoutReceiverService _spoutReceiverService;
    private readonly ITrayMenuController _trayMenuController;
    private GlobalHotKeyGesture _visibilityToggleHotKey;
    private GlobalHotKeyWindow? _hotKeyWindow;
    private readonly System.Windows.Forms.Timer _renderTimer;
    private readonly System.Windows.Forms.Timer _senderRefreshTimer;
    private readonly OverlaySettings _settings;

    public OverlayApplicationContext()
    {
        _settingsStore = new JsonSettingsStore();
        _settings = _settingsStore.Load();
        _visibilityToggleHotKey = GlobalHotKeyGesture.ParseOrDefault(_settings.VisibilityToggleHotKey);
        _settings.VisibilityToggleHotKey = _visibilityToggleHotKey.DisplayText;
        _stateCoordinator = new OverlayStateCoordinator();
        _stateCoordinator.SetMode(_settings.Mode);
        _stateCoordinator.SelectSender(_settings.SelectedSenderName);

        var initialBounds = _settings.WindowBounds == Rectangle.Empty
            ? OverlayBoundsHelper.CreateDefault(Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(100, 100, 1280, 720), _settings.LastKnownSourceWidth, _settings.LastKnownSourceHeight)
            : _settings.WindowBounds;

        _overlayWindowController = new Win32OverlayWindowController(initialBounds, _settings.Mode, _settings.OverlayOpacity);
        _overlayWindowController.BoundsChanged += OnBoundsChanged;
        _overlayWindowController.OverlayOpacityChanged += OnOverlayOpacityChanged;
        _overlayWindowController.Show();

        _renderer = new OverlayRenderer(_overlayWindowController.WindowHandle, _overlayWindowController.Bounds.Size);
        _spoutReceiverService = new SpoutReceiverService(_renderer.D3D11DevicePointer);
        _trayMenuController = new TrayMenuController();
        _trayMenuController.AdjustModeRequested += (_, _) => SetMode(OverlayMode.Adjust);
        _trayMenuController.PassthroughModeRequested += (_, _) => SetMode(OverlayMode.Passthrough);
        _trayMenuController.VisibilityToggleRequested += (_, _) => ToggleOverlayVisibility();
        _trayMenuController.VisibilityHotKeyChangeRequested += (_, _) => ConfigureVisibilityHotKey();
        _trayMenuController.ResetBoundsRequested += (_, _) => ResetBounds();
        _trayMenuController.ExitRequested += (_, _) => ExitThread();
        _trayMenuController.SenderSelected += (_, name) => SelectSender(name);
        UpdateTrayVisibilityLabel();
        TryRegisterVisibilityHotKey(_visibilityToggleHotKey, showFailureMessage: false);

        _renderTimer = new System.Windows.Forms.Timer { Interval = 16 };
        _renderTimer.Tick += (_, _) => RenderFrame();

        _senderRefreshTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        _senderRefreshTimer.Tick += (_, _) => RefreshSenderMenu();

        if (!string.IsNullOrWhiteSpace(_settings.SelectedSenderName))
        {
            _spoutReceiverService.SelectSender(_settings.SelectedSenderName);
        }

        RefreshSenderMenu();
        SetMode(_settings.Mode);
        _renderTimer.Start();
        _senderRefreshTimer.Start();
    }

    private void ResetBounds()
    {
        _overlayWindowController.ResetBounds();
        PersistSettings();
    }

    private void OnBoundsChanged(object? sender, Rectangle bounds)
    {
        _renderer.Resize(bounds.Size);
        _settings.WindowBounds = OverlayBoundsHelper.Normalize(bounds);
        PersistSettings();
    }

    private void OnOverlayOpacityChanged(object? sender, float opacity)
    {
        var normalizedOpacity = OverlaySettings.ClampOverlayOpacity(opacity);
        if (Math.Abs(_settings.OverlayOpacity - normalizedOpacity) < 0.001f)
        {
            return;
        }

        _settings.OverlayOpacity = normalizedOpacity;
        PersistSettings();
    }

    private void SelectSender(string? senderName)
    {
        _stateCoordinator.SelectSender(senderName);
        _spoutReceiverService.SelectSender(senderName);
        _settings.SelectedSenderName = senderName;
        PersistSettings();
        RefreshSenderMenu();
    }

    private void SetMode(OverlayMode mode)
    {
        _stateCoordinator.SetMode(mode);
        _settings.Mode = mode;
        _overlayWindowController.SetOverlayMode(mode);
        _trayMenuController.UpdateMode(mode);
        PersistSettings();
    }

    private void ToggleOverlayVisibility()
    {
        if (_overlayWindowController.IsVisible)
        {
            _overlayWindowController.Hide();
        }
        else
        {
            _overlayWindowController.Show();
            _overlayWindowController.SetOverlayMode(_stateCoordinator.Mode);
        }

        UpdateTrayVisibilityLabel();
    }

    private void ConfigureVisibilityHotKey()
    {
        if (!HotKeySettingsDialog.TryShow(_visibilityToggleHotKey, out var selectedGesture) || selectedGesture == _visibilityToggleHotKey)
        {
            return;
        }

        if (!TryRegisterVisibilityHotKey(selectedGesture, showFailureMessage: true))
        {
            return;
        }

        _visibilityToggleHotKey = selectedGesture;
        _settings.VisibilityToggleHotKey = selectedGesture.DisplayText;
        UpdateTrayVisibilityLabel();
        PersistSettings();
    }

    private void RefreshSenderMenu()
    {
        var senders = _spoutReceiverService.GetAvailableSenders();
        _stateCoordinator.UpdateAvailableSenders(senders);
        _trayMenuController.UpdateSenders(senders, _stateCoordinator.SelectedSenderName, _stateCoordinator.WaitingForReconnect);
    }

    private void RenderFrame()
    {
        if (!_overlayWindowController.IsVisible)
        {
            return;
        }

        _renderer.Resize(_overlayWindowController.Bounds.Size);

        var frame = _spoutReceiverService.TryReceiveFrame();
        if (frame is not null)
        {
            _overlayWindowController.SetSourceSize((int)frame.Sender.Width, (int)frame.Sender.Height);
            var width = (int)frame.Sender.Width;
            var height = (int)frame.Sender.Height;
            if (_settings.LastKnownSourceWidth != width || _settings.LastKnownSourceHeight != height)
            {
                _settings.LastKnownSourceWidth = width;
                _settings.LastKnownSourceHeight = height;
                PersistSettings();
            }
        }

        _renderer.Render(frame, _stateCoordinator.Mode, _stateCoordinator.WaitingForReconnect, _settings.OverlayOpacity);
    }

    private void PersistSettings()
    {
        _settings.WindowBounds = _overlayWindowController.Bounds;
        _settings.VisibilityToggleHotKey = _visibilityToggleHotKey.DisplayText;
        _settingsStore.Save(_settings);
    }

    private bool TryRegisterVisibilityHotKey(GlobalHotKeyGesture gesture, bool showFailureMessage)
    {
        var newWindow = GlobalHotKeyWindow.TryCreate(ToggleVisibilityHotKeyId, gesture.Modifiers, gesture.Key);
        if (newWindow is null)
        {
            if (showFailureMessage)
            {
                MessageBox.Show(
                    $"ショートカット {gesture.DisplayText} は登録できませんでした。ほかのアプリで使われている可能性があります。",
                    "ショートカット登録失敗",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }

            return false;
        }

        newWindow.HotKeyPressed += (_, _) => ToggleOverlayVisibility();
        var previousWindow = _hotKeyWindow;
        _hotKeyWindow = newWindow;
        previousWindow?.Dispose();
        return true;
    }

    private void UpdateTrayVisibilityLabel()
    {
        _trayMenuController.UpdateVisibility(_overlayWindowController.IsVisible, _visibilityToggleHotKey.DisplayText);
    }

    protected override void ExitThreadCore()
    {
        PersistSettings();
        _renderTimer.Stop();
        _senderRefreshTimer.Stop();
        _renderTimer.Dispose();
        _senderRefreshTimer.Dispose();
        _trayMenuController.Dispose();
        _hotKeyWindow?.Dispose();
        _spoutReceiverService.Dispose();
        _renderer.Dispose();
        _overlayWindowController.Dispose();
        base.ExitThreadCore();
    }
}

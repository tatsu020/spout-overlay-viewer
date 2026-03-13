using System.Drawing;
using System.Windows.Forms;
using SpoutOverlay.App.Helpers;

namespace SpoutOverlay.App.Win32;

internal sealed class HotKeySettingsDialog : Form
{
    private readonly Label _descriptionLabel;
    private readonly TextBox _shortcutTextBox;
    private readonly Button _okButton;
    private readonly Button _cancelButton;
    private readonly Button _resetButton;

    private HotKeySettingsDialog(GlobalHotKeyGesture currentGesture)
    {
        Text = "表示切替ショートカット";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(360, 150);
        KeyPreview = true;

        _descriptionLabel = new Label
        {
            AutoSize = false,
            Bounds = new Rectangle(16, 14, 328, 40),
            Text = "使いたいキーまたはキーの組み合わせを押してください。\r\n修飾キーなしの単独キーも設定できます。"
        };

        _shortcutTextBox = new TextBox
        {
            Bounds = new Rectangle(16, 64, 328, 28),
            ReadOnly = true,
            TabStop = false
        };

        _resetButton = new Button
        {
            Bounds = new Rectangle(16, 106, 90, 28),
            Text = "既定に戻す"
        };

        _cancelButton = new Button
        {
            Bounds = new Rectangle(174, 106, 80, 28),
            Text = "キャンセル",
            DialogResult = DialogResult.Cancel
        };

        _okButton = new Button
        {
            Bounds = new Rectangle(264, 106, 80, 28),
            Text = "OK",
            DialogResult = DialogResult.OK
        };

        _resetButton.Click += (_, _) => SetGesture(GlobalHotKeyGesture.Default);

        Controls.AddRange([_descriptionLabel, _shortcutTextBox, _resetButton, _cancelButton, _okButton]);
        AcceptButton = _okButton;
        CancelButton = _cancelButton;

        SetGesture(currentGesture);
    }

    public GlobalHotKeyGesture SelectedGesture { get; private set; } = GlobalHotKeyGesture.Default;

    public static bool TryShow(GlobalHotKeyGesture currentGesture, out GlobalHotKeyGesture selectedGesture)
    {
        using var dialog = new HotKeySettingsDialog(currentGesture);
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            selectedGesture = dialog.SelectedGesture;
            return true;
        }

        selectedGesture = currentGesture;
        return false;
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (GlobalHotKeyGesture.TryCreateFromKeyData(keyData, out var gesture))
        {
            SetGesture(gesture);
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void SetGesture(GlobalHotKeyGesture gesture)
    {
        SelectedGesture = gesture;
        _shortcutTextBox.Text = gesture.DisplayText;
    }
}

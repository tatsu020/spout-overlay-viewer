using System.Globalization;
using System.Windows.Forms;
using SpoutOverlay.App.Models;
using SpoutOverlay.App.Win32;

namespace SpoutOverlay.App.Helpers;

public sealed record GlobalHotKeyGesture(uint Modifiers, Keys Key, string DisplayText)
{
    public static GlobalHotKeyGesture Default { get; } =
        new(NativeMethods.ModControl | NativeMethods.ModShift | NativeMethods.ModNoRepeat, Keys.H, OverlaySettings.DefaultVisibilityToggleHotKey);

    public static GlobalHotKeyGesture ParseOrDefault(string? value)
    {
        return TryParse(value, out var gesture) ? gesture : Default;
    }

    public static bool TryParse(string? value, out GlobalHotKeyGesture gesture)
    {
        gesture = Default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var tokens = value.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length == 0)
        {
            return false;
        }

        var modifiers = NativeMethods.ModNoRepeat;
        Keys key = Keys.None;

        foreach (var token in tokens)
        {
            if (token.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) || token.Equals("Control", StringComparison.OrdinalIgnoreCase))
            {
                modifiers |= NativeMethods.ModControl;
                continue;
            }

            if (token.Equals("Shift", StringComparison.OrdinalIgnoreCase))
            {
                modifiers |= NativeMethods.ModShift;
                continue;
            }

            if (token.Equals("Alt", StringComparison.OrdinalIgnoreCase))
            {
                modifiers |= NativeMethods.ModAlt;
                continue;
            }

            if (key != Keys.None)
            {
                return false;
            }

            if (!TryParseKeyToken(token, out key))
            {
                return false;
            }
        }

        return TryCreate(modifiers, key, out gesture);
    }

    public static bool TryCreateFromKeyData(Keys keyData, out GlobalHotKeyGesture gesture)
    {
        var modifiers = NativeMethods.ModNoRepeat;
        if ((keyData & Keys.Control) == Keys.Control)
        {
            modifiers |= NativeMethods.ModControl;
        }

        if ((keyData & Keys.Shift) == Keys.Shift)
        {
            modifiers |= NativeMethods.ModShift;
        }

        if ((keyData & Keys.Alt) == Keys.Alt)
        {
            modifiers |= NativeMethods.ModAlt;
        }

        var key = keyData & Keys.KeyCode;
        return TryCreate(modifiers, key, out gesture);
    }

    private static bool TryCreate(uint modifiers, Keys key, out GlobalHotKeyGesture gesture)
    {
        gesture = Default;
        if (IsModifierKey(key) || key == Keys.None)
        {
            return false;
        }

        var converter = new KeysConverter();
        var keyText = converter.ConvertToInvariantString(key);
        if (string.IsNullOrWhiteSpace(keyText))
        {
            return false;
        }

        gesture = new GlobalHotKeyGesture(modifiers, key, FormatDisplayText(modifiers, keyText));
        return true;
    }

    private static bool TryParseKeyToken(string token, out Keys key)
    {
        if (token.Length == 1 && char.IsDigit(token[0]))
        {
            key = Keys.D0 + (token[0] - '0');
            return true;
        }

        return Enum.TryParse(token, ignoreCase: true, out key);
    }

    private static bool IsModifierKey(Keys key)
    {
        return key is Keys.ControlKey or Keys.ShiftKey or Keys.Menu;
    }

    private static string FormatDisplayText(uint modifiers, string keyText)
    {
        var parts = new List<string>(4);
        if ((modifiers & NativeMethods.ModControl) != 0)
        {
            parts.Add("Ctrl");
        }

        if ((modifiers & NativeMethods.ModShift) != 0)
        {
            parts.Add("Shift");
        }

        if ((modifiers & NativeMethods.ModAlt) != 0)
        {
            parts.Add("Alt");
        }

        parts.Add(NormalizeKeyText(keyText));
        return string.Join("+", parts);
    }

    private static string NormalizeKeyText(string keyText)
    {
        return keyText.StartsWith("D", StringComparison.Ordinal) &&
            keyText.Length == 2 &&
            char.IsDigit(keyText[1])
            ? keyText[1].ToString(CultureInfo.InvariantCulture)
            : keyText.ToUpperInvariant();
    }
}

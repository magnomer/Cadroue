using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Cadroue.UIShell.PMainWindow;
using Cadroue.UIShell.PSShared;

namespace Cadroue.UIShell;

internal sealed class PSKeymapChord : Button
{
    internal const double PSKeymapChordWidth = 168;

    private readonly Action<PSKeymapChord, string> psKeymapChordCallback;

    private string psKeymapChordGesture;
    private string psKeymapChordPending = string.Empty;
    private bool psKeymapChordActive;

    internal PSKeymapChord(string psKeymapChordStart, Action<PSKeymapChord, string> psKeymapChordAction)
    {
        psKeymapChordGesture = psKeymapChordStart;
        psKeymapChordCallback = psKeymapChordAction;

        Width = PSKeymapChordWidth;
        Height = PSField.PSFieldControlHeight;
        HorizontalAlignment = HorizontalAlignment.Right;
        VerticalAlignment = VerticalAlignment.Center;
        Focusable = true;
        Style = PButton.PButtonWhiteCreate();
        PSKeymapTextUpdate();
    }

    internal string PSKeymapChordGesture => psKeymapChordGesture;

    internal void PSKeymapChordSet(string psKeymapChordValue)
    {
        psKeymapChordGesture = psKeymapChordValue;
        psKeymapChordPending = string.Empty;
        psKeymapChordActive = false;
        PSKeymapTextUpdate();
    }

    protected override void OnClick()
    {
        base.OnClick();
        PSKeymapChordStart();
    }

    protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
    {
        base.OnLostKeyboardFocus(e);
        if (psKeymapChordActive)
        {
            PSKeymapChordCancel();
        }
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        if (!psKeymapChordActive)
        {
            base.OnPreviewKeyDown(e);
            return;
        }

        e.Handled = true;
        Key psKeymapChordKey = e.Key == Key.System ? e.SystemKey : e.Key;

        if (psKeymapChordKey == Key.Escape)
        {
            PSKeymapChordCancel();
            return;
        }

        if (psKeymapChordKey == Key.Enter)
        {
            PSKeymapChordCommit();
            return;
        }

        string psKeymapChordCaught = PShortcut.PShortcutGestureFormat(psKeymapChordKey, Keyboard.Modifiers);
        if (psKeymapChordCaught.Length == 0)
        {
            return;
        }

        psKeymapChordPending = psKeymapChordCaught;
        PSKeymapTextUpdate();
    }

    private void PSKeymapChordStart()
    {
        psKeymapChordActive = true;
        psKeymapChordPending = string.Empty;
        Keyboard.Focus(this);
        PSKeymapTextUpdate();
    }

    private void PSKeymapChordCommit()
    {
        psKeymapChordActive = false;
        if (psKeymapChordPending.Length > 0)
        {
            psKeymapChordGesture = psKeymapChordPending;
            psKeymapChordPending = string.Empty;
            PSKeymapTextUpdate();
            psKeymapChordCallback(this, psKeymapChordGesture);
            return;
        }

        PSKeymapTextUpdate();
    }

    private void PSKeymapChordCancel()
    {
        psKeymapChordActive = false;
        psKeymapChordPending = string.Empty;
        PSKeymapTextUpdate();
    }

    private void PSKeymapTextUpdate()
    {
        if (psKeymapChordActive)
        {
            Content = psKeymapChordPending.Length > 0
                ? psKeymapChordPending
                : LLocalization.LLocalizationTextRead("Chrome.Shortcuts.Capture");
            return;
        }

        Content = psKeymapChordGesture.Length > 0
            ? psKeymapChordGesture
            : LLocalization.LLocalizationTextRead("Chrome.Shortcuts.Unassigned");
    }
}

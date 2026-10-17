using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer;

internal sealed class PSKeymapChord : Button
{
    internal const double PSKeymapChordWidth = 168;

    private readonly QSKeymap lsKeymap;
    private readonly QSKeymapChord lsKeymapChord;

    internal PSKeymapChord(QSKeymap psKeymapOwner, QSKeymapChord psKeymapChordState)
    {
        lsKeymap = psKeymapOwner;
        lsKeymapChord = psKeymapChordState;
        PSChordLookApply();
        PSKeymapChordAttach();
        PSKeymapTextUpdate();
    }

    private void PSChordLookApply()
    {
        Width = PSKeymapChordWidth;
        Height = PSField.PSFieldControlHeight;
        HorizontalAlignment = HorizontalAlignment.Right;
        VerticalAlignment = VerticalAlignment.Center;
        Focusable = true;
        QButton.QButtonAttach(this, QButtonLook.QButtonLookWhite);
    }

    private void PSKeymapChordAttach() => lsKeymap.QSKeymapChordChanged += PSKeymapChordHandle;

    private void PSKeymapChordHandle(QSKeymapChord psKeymapChanged)
    {
        if (ReferenceEquals(psKeymapChanged, lsKeymapChord))
        {
            PSKeymapTextUpdate();
        }
    }

    protected override void OnClick()
    {
        base.OnClick();
        Keyboard.Focus(this);
        lsKeymap.QSKeymapChordStart(lsKeymapChord);
    }

    protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
    {
        base.OnLostKeyboardFocus(e);
        if (lsKeymapChord.QSKeymapChordActive)
        {
            lsKeymap.QSKeymapChordCancel(lsKeymapChord);
        }
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        if (!lsKeymapChord.QSKeymapChordActive)
        {
            base.OnPreviewKeyDown(e);
            return;
        }

        Key psKeymapChordKey = PSChordKeyClaim(e);
        PSChordKeyHandle(psKeymapChordKey);
    }

    private static Key PSChordKeyClaim(KeyEventArgs e)
    {
        e.Handled = true;
        return e.Key == Key.System ? e.SystemKey : e.Key;
    }

    private void PSChordKeyHandle(Key psKeymapChordKey)
    {
        if (psKeymapChordKey == Key.Escape)
        {
            lsKeymap.QSKeymapChordCancel(lsKeymapChord);
            return;
        }

        if (psKeymapChordKey == Key.Enter)
        {
            lsKeymap.QSKeymapChordCommit(lsKeymapChord);
            return;
        }

        lsKeymap.QSKeymapPendingSet(
            lsKeymapChord,
            QShortcut.QShortcutGestureFormat(psKeymapChordKey, Keyboard.Modifiers));
    }

    private void PSKeymapTextUpdate() => PSKeymapTextShow(PSKeymapTextFormat());

    private string PSKeymapTextFormat()
    {
        if (lsKeymapChord.QSKeymapChordActive)
        {
            return lsKeymapChord.QSKeymapChordPending.Length > 0
                ? lsKeymapChord.QSKeymapChordPending
                : QLocalizationCatalog.QLocalizationTextRead("Chrome.Shortcuts.Capture");
        }

        return lsKeymapChord.QSKeymapChordGesture.Length > 0
            ? lsKeymapChord.QSKeymapChordGesture
            : QLocalizationCatalog.QLocalizationTextRead("Chrome.Shortcuts.Unassigned");
    }

    private void PSKeymapTextShow(string pText) => Content = pText;
}

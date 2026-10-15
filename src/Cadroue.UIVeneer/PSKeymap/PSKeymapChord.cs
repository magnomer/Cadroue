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

        Width = PSKeymapChordWidth;
        Height = PSField.PSFieldControlHeight;
        HorizontalAlignment = HorizontalAlignment.Right;
        VerticalAlignment = VerticalAlignment.Center;
        Focusable = true;
        Style = PButton.PButtonWhiteCreate();
        lsKeymap.QSKeymapChordChanged += PSKeymapChordHandle;
        PSKeymapTextUpdate();
    }

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

        e.Handled = true;
        Key psKeymapChordKey = e.Key == Key.System ? e.SystemKey : e.Key;

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
            PShortcut.PShortcutGestureFormat(psKeymapChordKey, Keyboard.Modifiers));
    }

    private void PSKeymapTextUpdate()
    {
        if (lsKeymapChord.QSKeymapChordActive)
        {
            Content = lsKeymapChord.QSKeymapChordPending.Length > 0
                ? lsKeymapChord.QSKeymapChordPending
                : QLocalizationCatalog.QLocalizationTextRead("Chrome.Shortcuts.Capture");
            return;
        }

        Content = lsKeymapChord.QSKeymapChordGesture.Length > 0
            ? lsKeymapChord.QSKeymapChordGesture
            : QLocalizationCatalog.QLocalizationTextRead("Chrome.Shortcuts.Unassigned");
    }
}

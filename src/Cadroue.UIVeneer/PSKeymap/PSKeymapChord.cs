using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Cadroue.Application;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PHouse;
using Cadroue.UIVeneer.PSCasement;

namespace Cadroue.UIVeneer;

internal sealed class PSKeymapChord : Button
{
    internal const double PSKeymapChordWidth = 168;

    private readonly LSKeymap lsKeymap;
    private readonly LSKeymapChord lsKeymapChord;

    internal PSKeymapChord(LSKeymap psKeymapOwner, LSKeymapChord psKeymapChordState)
    {
        lsKeymap = psKeymapOwner;
        lsKeymapChord = psKeymapChordState;

        Width = PSKeymapChordWidth;
        Height = PSField.PSFieldControlHeight;
        HorizontalAlignment = HorizontalAlignment.Right;
        VerticalAlignment = VerticalAlignment.Center;
        Focusable = true;
        Style = PButton.PButtonWhiteCreate();
        lsKeymap.LSKeymapChordChange += PSKeymapChordHandle;
        PSKeymapTextUpdate();
    }

    private void PSKeymapChordHandle(LSKeymapChord psKeymapChanged)
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
        lsKeymap.LSKeymapChordStart(lsKeymapChord);
    }

    protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
    {
        base.OnLostKeyboardFocus(e);
        if (lsKeymapChord.LSKeymapChordActive)
        {
            lsKeymap.LSKeymapChordCancel(lsKeymapChord);
        }
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        if (!lsKeymapChord.LSKeymapChordActive)
        {
            base.OnPreviewKeyDown(e);
            return;
        }

        e.Handled = true;
        Key psKeymapChordKey = e.Key == Key.System ? e.SystemKey : e.Key;

        if (psKeymapChordKey == Key.Escape)
        {
            lsKeymap.LSKeymapChordCancel(lsKeymapChord);
            return;
        }

        if (psKeymapChordKey == Key.Enter)
        {
            lsKeymap.LSKeymapChordCommit(lsKeymapChord);
            return;
        }

        lsKeymap.LSKeymapPendingSet(
            lsKeymapChord,
            PShortcut.PShortcutGestureFormat(psKeymapChordKey, Keyboard.Modifiers));
    }

    private void PSKeymapTextUpdate()
    {
        if (lsKeymapChord.LSKeymapChordActive)
        {
            Content = lsKeymapChord.LSKeymapChordPending.Length > 0
                ? lsKeymapChord.LSKeymapChordPending
                : LLocalization.LLocalizationTextRead("Chrome.Shortcuts.Capture");
            return;
        }

        Content = lsKeymapChord.LSKeymapChordGesture.Length > 0
            ? lsKeymapChord.LSKeymapChordGesture
            : LLocalization.LLocalizationTextRead("Chrome.Shortcuts.Unassigned");
    }
}

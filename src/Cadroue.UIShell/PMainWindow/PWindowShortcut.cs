using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIShell.PControlBar;
using Cadroue.ShellEngine;

namespace Cadroue.UIShell.PMainWindow;

public partial class PWindow
{
    private void PShortcutKeyHandle(object sender, KeyEventArgs e)
    {
        PShortcutKeyProcess(e);
    }

    private void PShortcutViewerHandle(KeyEventArgs e)
    {
        PShortcutKeyProcess(e);
    }

    private void PShortcutKeyProcess(KeyEventArgs e)
    {
        if (PWindowInputFind(e.OriginalSource as DependencyObject))
        {
            return;
        }

        bool pHandled = PShortcutDispatch(e.Key == Key.System ? e.SystemKey : e.Key, Keyboard.Modifiers);
        if (pHandled)
        {
            e.Handled = true;
        }
    }

    private bool PShortcutDispatch(Key pKey, ModifierKeys pModifiers)
    {
        string pShortcutGesture = PShortcut.PShortcutGestureFormat(pKey, pModifiers);
        string? pShortcutToken = Cadroue.Infrastructure.LBinding.LBindingTokenFind(
            Cadroue.Infrastructure.LBinding.LBindingCurrent, pShortcutGesture);
        return pShortcutToken is not null && PShortcutRun(pShortcutToken);
    }

    private bool PShortcutRun(string pShortcutToken)
    {
        switch (pShortcutToken)
        {
            case "Show":
                pControlBar.PToolbarShortcutShow();
                return true;
            case "Undo":
                return PShortcutHistoryRun(false);
            case "Redo":
                return PShortcutHistoryRun(true);
            case "UnloadAll":
                return pStrip.PStripContentClear();
            case "PlayPause":
                return PShortcutPlayToggle();
            case "Unload":
                return PShortcutMediaClose();
            default:
                return pFlowActive?.PFlowShortcutDispatch(PShortcutCodeRead(pShortcutToken)) == true;
        }
    }

    private static string PShortcutCodeRead(string pShortcutToken) => pShortcutToken switch
    {
        "ZoomIn" => "zoomIn",
        "ZoomOut" => "zoomOut",
        "SectionAdd" => "addSection",
        "SectionStart" => "setStart",
        "SectionSplit" => "splitSection",
        "SectionEnd" => "setEnd",
        "SectionDelete" => "deleteSection",
        "SectionRename" => "nameSection",
        "KeyframePrevious" => "previousKey",
        "KeyframeNearest" => "nearestKey",
        "KeyframeNext" => "nextKey",
        _ => string.Empty
    };

    private bool PShortcutHistoryRun(bool pShortcutRedo)
    {
        PWorkspace? pWorkspace = pStrip.PStripSelected?.PTabWorkspace;
        if (pWorkspace is null)
        {
            return false;
        }

        return pShortcutRedo ? pWorkspace.PWorkspaceRedo() : pWorkspace.PWorkspaceUndo();
    }

    private bool PShortcutMediaClose()
    {
        PWorkspace? pWorkspace = pStrip.PStripSelected?.PTabWorkspace;
        if (pWorkspace is not null)
        {
            return pWorkspace.PWorkspaceMediaClear(LBastion.LBastionCohortsRead());
        }

        return false;
    }

    private bool PShortcutPlayToggle()
    {
        if (pViewerActive is null)
        {
            return false;
        }

        if (pViewerActive.LPreviewStateCurrent.LPlaybackState.LPlaybackStatePlaying)
        {
            pViewerActive.PViewerPause();
        }
        else
        {
            pViewerActive.PViewerPlay();
        }

        return true;
    }

    private static bool PWindowInputFind(DependencyObject? pSource)
    {
        while (pSource is not null)
        {
            if (pSource is TextBoxBase || pSource is PasswordBox)
            {
                return true;
            }

            pSource = VisualTreeHelper.GetParent(pSource);
        }

        return false;
    }
}

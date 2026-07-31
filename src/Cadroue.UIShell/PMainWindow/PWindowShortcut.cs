using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIShell.PControlBar;

namespace Cadroue.UIShell.PMainWindow;

public partial class PWindow
{
    private void PShortcutKeyHandle(object sender, KeyEventArgs e)
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
        string pShortcutGesture = LBinding.LBindingFormat(pKey, pModifiers);
        string? pShortcutToken = LBinding.LBindingTokenFind(PProgram.LPreferenceStateCurrent.LPreferenceShortcuts, pShortcutGesture);
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
                return lTabset.LTabsetContentClear();
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
        PWorkspace? pWorkspace = lTabset.PTabsetCurrent?.PTabWorkspace;
        if (pWorkspace is null)
        {
            return false;
        }

        return pShortcutRedo ? pWorkspace.PWorkspaceRedo() : pWorkspace.PWorkspaceUndo();
    }

    private bool PShortcutMediaClose()
    {
        if (pViewerActive is null && pListActive is null)
        {
            return false;
        }

        pViewerActive?.PViewerMediaClose();
        pListActive?.PListClear();
        pWindowSurfaceActive?.PTabGroup?.PGroupClear();
        return true;
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

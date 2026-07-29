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
        if (pModifiers == ModifierKeys.Control && (pKey == Key.OemQuestion || pKey == Key.Divide))
        {
            pControlBar.PToolbarShortcutShow();
            return true;
        }

        if (pModifiers == ModifierKeys.Control && pKey == Key.Z)
        {
            return PShortcutHistoryRun(false);
        }

        if ((pModifiers == ModifierKeys.Control && pKey == Key.Y)
            || (pModifiers == (ModifierKeys.Control | ModifierKeys.Shift) && pKey == Key.Z))
        {
            return PShortcutHistoryRun(true);
        }

        if (pModifiers != ModifierKeys.None)
        {
            return false;
        }

        return pKey switch
        {
            Key.Space => PShortcutPlayToggle(),
            Key.F4 => PShortcutMediaClose(),
            Key.C => pFlowActive?.PFlowShortcutDispatch("zoomIn") == true,
            Key.V => pFlowActive?.PFlowShortcutDispatch("zoomOut") == true,
            Key.Q => pFlowActive?.PFlowShortcutDispatch("addSection") == true,
            Key.D => pFlowActive?.PFlowShortcutDispatch("setStart") == true,
            Key.S => pFlowActive?.PFlowShortcutDispatch("splitSection") == true,
            Key.F => pFlowActive?.PFlowShortcutDispatch("setEnd") == true,
            Key.Delete => pFlowActive?.PFlowShortcutDispatch("deleteSection") == true,
            Key.A => pFlowActive?.PFlowShortcutDispatch("nameSection") == true,
            Key.E => pFlowActive?.PFlowShortcutDispatch("previousKey") == true,
            Key.W => pFlowActive?.PFlowShortcutDispatch("nearestKey") == true,
            Key.R => pFlowActive?.PFlowShortcutDispatch("nextKey") == true,
            _ => false
        };
    }

    private bool PShortcutHistoryRun(bool pShortcutRedo)
    {
        PWorkspace? pWorkspace = lTabset.PTabsetSelectRecord?.PTabWorkspace;
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

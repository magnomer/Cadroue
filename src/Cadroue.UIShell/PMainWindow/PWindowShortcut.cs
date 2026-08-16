using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Cadroue.UIShell.PControlBar;
using Cadroue.ShellEngine;

namespace Cadroue.UIShell.PMainWindow;

public partial class PWindow
{
    private const int PShortcutKeydownMessage = 0x0100;
    private const int PShortcutSyskeydownMessage = 0x0104;
    private const int PShortcutVirtualShift = 0x10;
    private const int PShortcutVirtualControl = 0x11;
    private const int PShortcutVirtualAlt = 0x12;
    private const int PShortcutVirtualLwin = 0x5B;
    private const int PShortcutVirtualRwin = 0x5C;

    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern short GetKeyState(int virtualKey);

    private void PShortcutMessageHandle(ref MSG pShortcutMessage, ref bool pShortcutHandled)
    {
        if (pShortcutHandled)
        {
            return;
        }

        if (pShortcutMessage.message != PShortcutKeydownMessage
            && pShortcutMessage.message != PShortcutSyskeydownMessage)
        {
            return;
        }

        if (ComponentDispatcher.IsThreadModal || !PShortcutSurfaceFind())
        {
            return;
        }

        if (PWindowInputFind(Keyboard.FocusedElement as DependencyObject))
        {
            return;
        }

        Key pShortcutKey = KeyInterop.KeyFromVirtualKey((int)pShortcutMessage.wParam);
        if (PShortcutDispatch(pShortcutKey, PShortcutModifierRead()))
        {
            pShortcutHandled = true;
        }
    }

    private bool PShortcutSurfaceFind()
    {
        nint pShortcutForeground = GetForegroundWindow();
        if (pShortcutForeground == nint.Zero)
        {
            return false;
        }

        if (pShortcutForeground == new WindowInteropHelper(this).Handle)
        {
            return true;
        }

        return pViewerActive?.PViewerSurfaceMatch(pShortcutForeground) == true;
    }

    private static ModifierKeys PShortcutModifierRead()
    {
        ModifierKeys pShortcutModifiers = ModifierKeys.None;
        if (PShortcutVirtualCheck(PShortcutVirtualControl))
        {
            pShortcutModifiers |= ModifierKeys.Control;
        }

        if (PShortcutVirtualCheck(PShortcutVirtualAlt))
        {
            pShortcutModifiers |= ModifierKeys.Alt;
        }

        if (PShortcutVirtualCheck(PShortcutVirtualShift))
        {
            pShortcutModifiers |= ModifierKeys.Shift;
        }

        if (PShortcutVirtualCheck(PShortcutVirtualLwin) || PShortcutVirtualCheck(PShortcutVirtualRwin))
        {
            pShortcutModifiers |= ModifierKeys.Windows;
        }

        return pShortcutModifiers;
    }

    private static bool PShortcutVirtualCheck(int pShortcutVirtualKey) =>
        (GetKeyState(pShortcutVirtualKey) & 0x8000) != 0;

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
                pToolbar.PToolbarShortcutShow();
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

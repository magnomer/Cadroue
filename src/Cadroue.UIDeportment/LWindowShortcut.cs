using Cadroue.Infrastructure;
using Cadroue.ShellEngine;

namespace Cadroue.UIDeportment;

public sealed record LWindowPress(
    bool LWindowPressHandled,
    int LWindowPressMessage,
    int LWindowPressVirtual,
    bool LWindowPressModal,
    nint LWindowPressOwn,
    Func<nint> LWindowPressForeground,
    Func<nint, bool?> LWindowPressViewer,
    Func<bool> LWindowPressInput,
    Func<int, string> LWindowPressKey,
    Func<int, short> LWindowPressState);

public sealed class LWindowShortcut
{
    private const int LWindowShortcutKeydown = 0x0100;
    private const int LWindowShortcutSyskeydown = 0x0104;
    private const int LWindowDownMask = 0x8000;
    private const int LWindowShortcutShift = 0x10;
    private const int LWindowShortcutControl = 0x11;
    private const int LWindowShortcutAlt = 0x12;
    private const int LWindowLeftWin = 0x5B;
    private const int LWindowRightWin = 0x5C;

    private readonly LWindow lWindow;
    private Action? lWindowShowSeam;
    private Func<bool?>? lWindowUndoSeam;
    private Func<bool?>? lWindowRedoSeam;
    private Func<bool>? lWindowUnloadSeam;
    private Func<IReadOnlySet<Guid>, bool?>? lWindowClearSeam;

    public LWindowShortcut(LWindow lOwner)
    {
        lWindow = lOwner;
    }

    public event Action? LWindowShortcutPlay;
    public event Action? LWindowShortcutPause;

    public void LWindowShortcutAttach(
        Action lShow,
        Func<bool?> lUndo,
        Func<bool?> lRedo,
        Func<bool> lUnload,
        Func<IReadOnlySet<Guid>, bool?> lClear)
    {
        lWindowShowSeam = lShow;
        lWindowUndoSeam = lUndo;
        lWindowRedoSeam = lRedo;
        lWindowUnloadSeam = lUnload;
        lWindowClearSeam = lClear;
    }

    public bool LWindowShortcutHandle(LWindowPress lPress)
    {
        if (lPress.LWindowPressHandled)
        {
            return true;
        }

        if (lPress.LWindowPressMessage != LWindowShortcutKeydown
            && lPress.LWindowPressMessage != LWindowShortcutSyskeydown)
        {
            return false;
        }

        if (lPress.LWindowPressModal || !LWindowSurfaceCheck(lPress) || lPress.LWindowPressInput())
        {
            return false;
        }

        string lGesture = LBinding.LBindingGestureFormat(
            lPress.LWindowPressKey(lPress.LWindowPressVirtual),
            LWindowDownCheck(lPress.LWindowPressState(LWindowShortcutControl)),
            LWindowDownCheck(lPress.LWindowPressState(LWindowShortcutAlt)),
            LWindowDownCheck(lPress.LWindowPressState(LWindowShortcutShift)),
            LWindowDownCheck(lPress.LWindowPressState(LWindowLeftWin))
                || LWindowDownCheck(lPress.LWindowPressState(LWindowRightWin)));
        string? lToken = LBinding.LBindingTokenFind(LBinding.LBindingCurrent, lGesture);
        return lToken is not null && LWindowShortcutRun(lToken);
    }

    public bool LWindowShortcutRun(string lToken)
    {
        switch (lToken)
        {
            case "Show":
                lWindowShowSeam?.Invoke();
                return true;
            case "Undo":
                return lWindowUndoSeam?.Invoke() ?? false;
            case "Redo":
                return lWindowRedoSeam?.Invoke() ?? false;
            case "UnloadAll":
                return lWindowUnloadSeam?.Invoke() ?? false;
            case "PlayPause":
                return LWindowPlayToggle();
            case "Unload":
                return lWindowClearSeam?.Invoke(LBastion.LBastionCohortsRead()) ?? false;
            default:
                return lWindow.LWindowTab?.LStripTabWorkspace?.LWorkspaceFlow?.LFlowShortcutRun(lToken) ?? false;
        }
    }

    private bool LWindowPlayToggle()
    {
        if (lWindow.LWindowViewer is not { } lViewer)
        {
            return false;
        }

        if (lViewer.LViewerPlaying)
        {
            LWindowShortcutPause?.Invoke();
        }
        else
        {
            LWindowShortcutPlay?.Invoke();
        }

        return true;
    }

    private static bool LWindowSurfaceCheck(LWindowPress lPress)
    {
        nint lForeground = lPress.LWindowPressForeground();
        if (lForeground == nint.Zero)
        {
            return false;
        }

        return lForeground == lPress.LWindowPressOwn || lPress.LWindowPressViewer(lForeground) == true;
    }

    private static bool LWindowDownCheck(short lState) => (lState & LWindowDownMask) != 0;
}

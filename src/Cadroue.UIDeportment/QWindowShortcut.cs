using System.Windows.Input;
using Cadroue.ShellEngine;

namespace Cadroue.UIDeportment;

public sealed record QWindowPress(
    bool QWindowPressHandled,
    int QWindowPressMessage,
    int QWindowPressVirtual,
    bool QWindowPressModal,
    nint QWindowPressOwn,
    Func<nint> QWindowPressForeground,
    Func<nint, bool?> QWindowPressViewer,
    Func<bool> QWindowPressInput,
    Func<int, string> QWindowPressKey,
    Func<int, short> QWindowPressState);

public sealed class QWindowShortcut
{
    private const int QWindowShortcutKeydown = 0x0100;
    private const int QWindowShortcutSyskeydown = 0x0104;
    private const int QWindowDownMask = 0x8000;
    private const int QWindowShortcutShift = 0x10;
    private const int QWindowShortcutControl = 0x11;
    private const int QWindowShortcutAlt = 0x12;
    private const int QWindowLeftWin = 0x5B;
    private const int QWindowRightWin = 0x5C;

    private readonly QWindow qWindow;
    private Action? qWindowShowSeam;
    private Func<bool?>? qWindowUndoSeam;
    private Func<bool?>? qWindowRedoSeam;
    private Func<bool>? qWindowUnloadSeam;
    private Func<IReadOnlySet<Guid>, bool?>? qWindowClearSeam;

    public QWindowShortcut(QWindow lOwner)
    {
        qWindow = lOwner;
    }

    public event Action? QWindowShortcutPlayed;
    public event Action? QWindowShortcutPaused;

    public void QWindowShortcutAttach(
        Action lShow,
        Func<bool?> lUndo,
        Func<bool?> lRedo,
        Func<bool> lUnload,
        Func<IReadOnlySet<Guid>, bool?> lClear)
    {
        qWindowShowSeam = lShow;
        qWindowUndoSeam = lUndo;
        qWindowRedoSeam = lRedo;
        qWindowUnloadSeam = lUnload;
        qWindowClearSeam = lClear;
    }

    public bool QWindowShortcutHandle(QWindowPress lPress)
    {
        if (lPress.QWindowPressHandled)
        {
            return true;
        }

        if (lPress.QWindowPressMessage != QWindowShortcutKeydown
            && lPress.QWindowPressMessage != QWindowShortcutSyskeydown)
        {
            return false;
        }

        if (lPress.QWindowPressModal || !QWindowSurfaceCheck(lPress) || lPress.QWindowPressInput())
        {
            return false;
        }

        string lGesture = QShortcut.QShortcutGestureFormat(
            lPress.QWindowPressKey(lPress.QWindowPressVirtual),
            QWindowModifierSelect(
                    QWindowDownCheck(lPress.QWindowPressState(QWindowShortcutControl)), ModifierKeys.Control)
                | QWindowModifierSelect(
                    QWindowDownCheck(lPress.QWindowPressState(QWindowShortcutAlt)), ModifierKeys.Alt)
                | QWindowModifierSelect(
                    QWindowDownCheck(lPress.QWindowPressState(QWindowShortcutShift)), ModifierKeys.Shift)
                | QWindowModifierSelect(
                    QWindowDownCheck(lPress.QWindowPressState(QWindowLeftWin))
                        || QWindowDownCheck(lPress.QWindowPressState(QWindowRightWin)),
                    ModifierKeys.Windows));
        string? lToken = QShortcut.QShortcutTokenFind(lGesture);
        return lToken is not null && QWindowShortcutRun(lToken);
    }

    public bool QWindowShortcutRun(string lToken)
    {
        switch (lToken)
        {
            case "Show":
                qWindowShowSeam?.Invoke();
                return true;
            case "Undo":
                return qWindowUndoSeam?.Invoke() ?? false;
            case "Redo":
                return qWindowRedoSeam?.Invoke() ?? false;
            case "UnloadAll":
                return qWindowUnloadSeam?.Invoke() ?? false;
            case "PlayPause":
                return QWindowPlayToggle();
            case "Unload":
                return qWindowClearSeam?.Invoke(LBastion.LBastionCohortsRead()) ?? false;
            default:
                return qWindow.QWindowTab?.QStripTabWorkspace.QWorkspaceFlow?.QFlowShortcutRun(lToken) ?? false;
        }
    }

    private bool QWindowPlayToggle()
    {
        if (qWindow.QWindowViewer is not { } lViewer)
        {
            return false;
        }

        if (lViewer.QViewerPlaying)
        {
            QWindowShortcutPaused?.Invoke();
        }
        else
        {
            QWindowShortcutPlayed?.Invoke();
        }

        return true;
    }

    private static bool QWindowSurfaceCheck(QWindowPress lPress)
    {
        nint lForeground = lPress.QWindowPressForeground();
        if (lForeground == nint.Zero)
        {
            return false;
        }

        return lForeground == lPress.QWindowPressOwn || lPress.QWindowPressViewer(lForeground) == true;
    }

    private static bool QWindowDownCheck(short lState) => (lState & QWindowDownMask) != 0;

    private static ModifierKeys QWindowModifierSelect(bool lDown, ModifierKeys lModifier) =>
        lDown ? lModifier : ModifierKeys.None;
}

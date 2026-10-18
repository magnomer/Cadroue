using Cadroue.Conduct;
using Cadroue.Media;

namespace Cadroue.UIDeportment;

public enum QWindowDropEffect
{
    QWindowDropNone,
    QWindowDropFile,
}

public sealed record QWindowDrag(
    string[]? QWindowDragPaths,
    bool QWindowDragGroup,
    string? QWindowDragSource,
    bool QWindowDragCopyable,
    object? QWindowDragData);

// The window's file drop: decides each drag's effect on the active list or viewer, hands an accepted drop to it and
// tells its trace what it decided.
public sealed class QWindowDrop
{
    private readonly QWindow qWindow;
    private readonly CUsher qWindowDropUsher;
    private readonly QWindowDropTrace qWindowDropTrace = new();
    private QWindowDropEffect qWindowDropEffect;
    private bool qWindowDropHandled;

    public QWindowDrop(QWindow lOwner)
    {
        qWindow = lOwner;
        qWindowDropUsher = lOwner.QWindowUsher;
    }

    public event Action<IReadOnlyList<string>>? QWindowListAdded;
    public event Action<string>? QWindowViewerOpened;

    public QWindowDropEffect QWindowDropEffect => qWindowDropEffect;

    public bool QWindowDropHandled => qWindowDropHandled;

    public void QWindowEnterHandle(QWindowDrag lDrag)
    {
        if (!lDrag.QWindowDragGroup)
        {
            QWindowEnterAppend(lDrag);
        }

        QWindowOverHandle(lDrag);
    }

    private void QWindowEnterAppend(QWindowDrag lDrag)
    {
        QWindowDropEffect lEffect = QWindowDropResolve(lDrag, out _, out _);
        qWindowDropTrace.QWindowDropAppend(
            lDrag,
            "Drag entered window: "
            + (lEffect == QWindowDropEffect.QWindowDropNone
                ? "will REFUSE (forbidden cursor)"
                : $"will accept ({lEffect})"),
            $"originalSource={lDrag.QWindowDragSource ?? "null"}, "
            + $"list={(qWindow.QWindowListPresent ? "present" : "NULL")}, "
            + $"viewer={(qWindow.QWindowViewerPresent ? "present" : "NULL")}, "
            + $"audioTab={qWindow.QWindowAudioAllowed}, groupAncestor={lDrag.QWindowDragGroup}");
    }

    public void QWindowOverHandle(QWindowDrag lDrag)
    {
        if (lDrag.QWindowDragGroup)
        {
            qWindowDropTrace.QWindowLastClear();
            QWindowDropSet(QWindowDropEffect.QWindowDropNone, false);
            return;
        }

        qWindowDropTrace.QWindowDropStart(lDrag);
        QWindowDropEffect lEffect = QWindowDropResolve(lDrag, out string lReason, out _);
        qWindowDropTrace.QWindowOverAppend(lDrag, lEffect, lReason);
        QWindowDropSet(lEffect, true);
    }

    public void QWindowDropHandle(QWindowDrag lDrag)
    {
        if (lDrag.QWindowDragGroup)
        {
            QWindowDropSet(QWindowDropEffect.QWindowDropNone, false);
            return;
        }

        qWindowDropTrace.QWindowLastClear();
        QWindowDropEffect lEffect = QWindowDropResolve(lDrag, out string lReason, out string lName);
        QWindowDropSet(lEffect, true);

        string lTarget = QWindowTargetRead();
        qWindowDropTrace.QWindowReleaseAppend(lDrag, lEffect, lTarget, lReason);

        if (lEffect == QWindowDropEffect.QWindowDropNone)
        {
            qWindowDropTrace.QWindowDropRecord($"File drag refused onto {lTarget}");
            return;
        }

        IReadOnlyList<string> lPaths = QWindowPathsRead(lDrag);
        if (qWindow.QWindowListPresent)
        {
            QWindowListAdded?.Invoke(lPaths);
            qWindowDropTrace.QWindowDropAppend(
                lDrag, $"Drop into list: {lPaths.Count} path(s) handed to the list scan");
            qWindowDropTrace.QWindowDropRecord($"File drag accepted onto list ({lEffect})");
            return;
        }

        if (!qWindow.QWindowViewerPresent)
        {
            qWindowDropTrace.QWindowDropRecord($"File drag accepted onto {lTarget} ({lEffect})");
            return;
        }

        string? lSourcePath = QWindowPathRead(lDrag);
        if (lSourcePath is null)
        {
            QWindowDropSet(QWindowDropEffect.QWindowDropNone, true);
            qWindowDropTrace.QWindowDropAppend(lDrag, "Drop into viewer refused: no existing file in payload");
            qWindowDropTrace.QWindowDropRecord("File drag refused onto viewer", true);
            return;
        }

        QWindowViewerOpened?.Invoke(lSourcePath);
        qWindowDropTrace.QWindowDropAppend(lDrag, $"Drop into viewer: opened {lName}");
        qWindowDropTrace.QWindowDropRecord($"File drag accepted onto viewer ({lEffect})");
    }

    private string QWindowTargetRead() =>
        qWindow.QWindowListPresent ? "list" : qWindow.QWindowViewerPresent ? "viewer" : "none";

    private void QWindowDropSet(QWindowDropEffect lEffect, bool lHandled)
    {
        qWindowDropEffect = lEffect;
        qWindowDropHandled = lHandled;
    }

    private QWindowDropEffect QWindowDropResolve(QWindowDrag lDrag, out string lReason, out string lName)
    {
        lName = string.Empty;
        IReadOnlyList<string> lPaths = QWindowPathsRead(lDrag);
        string lPayload = lPaths.Count == 0
            ? "no FileDrop payload"
            : $"{lPaths.Count} path(s): {string.Join(", ", lPaths.Select(qWindowDropUsher.CUsherNameRead))}";

        if (qWindow.QWindowListPresent)
        {
            bool lListMatch = QWindowMediaCheck(qWindowDropUsher, lPaths);
            lReason = lListMatch
                ? $"target=list, {lPayload}"
                : $"target=list, none are media/folders — {lPayload}";
            return lListMatch ? QWindowAllowedRead(lDrag) : QWindowDropEffect.QWindowDropNone;
        }

        if (!qWindow.QWindowViewerPresent)
        {
            lReason = $"no active list or viewer (active tab has no drop target); {lPayload}";
            return QWindowDropEffect.QWindowDropNone;
        }

        string? lSourcePath = QWindowPathRead(lDrag);
        if (lSourcePath is null)
        {
            lReason = $"target=viewer, no existing file in payload — {lPayload}";
            return QWindowDropEffect.QWindowDropNone;
        }

        lName = qWindowDropUsher.CUsherNameRead(lSourcePath);
        if (LMedia.LMediaAudioCheck(lSourcePath) && !qWindow.QWindowAudioAllowed)
        {
            lReason = "target=viewer, audio-only file on a video-only tab — "
                + $"{lName}";
            return QWindowDropEffect.QWindowDropNone;
        }

        lReason = $"target=viewer, {lName}";
        return QWindowAllowedRead(lDrag);
    }

    private static QWindowDropEffect QWindowAllowedRead(QWindowDrag lDrag) =>
        lDrag.QWindowDragCopyable ? QWindowDropEffect.QWindowDropFile : QWindowDropEffect.QWindowDropNone;

    private static IReadOnlyList<string> QWindowPathsRead(QWindowDrag lDrag) =>
        lDrag.QWindowDragPaths ?? [];

    private string? QWindowPathRead(QWindowDrag lDrag) => QWindowFileFind(qWindowDropUsher, QWindowPathsRead(lDrag));

    internal static string? QWindowFileFind(CUsher lUsher, IReadOnlyList<string> lPaths) =>
        lPaths.FirstOrDefault(lUsher.CUsherFileExist);

    internal static bool QWindowMediaCheck(CUsher lUsher, IReadOnlyList<string> lPaths) =>
        lPaths.Any(lPath => lUsher.CUsherFolderExist(lPath) || LMedia.LMediaCheck(lPath));
}

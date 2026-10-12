using System.Globalization;
using Cadroue.Conduct;
using Cadroue.Infrastructure;
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

public sealed class QWindowDrop
{
    private const int QWindowDropIndent = 14;
    private readonly QWindow qWindow;
    private readonly CUsher qWindowDropUsher;
    private readonly List<string> qWindowDropTrace = [];
    private object? qWindowDropData;
    private QWindowDropEffect? qWindowDropLast;
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
            QWindowDropEffect lEffect = QWindowDropResolve(lDrag, out _, out _);
            QWindowDropAppend(
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

        QWindowOverHandle(lDrag);
    }

    public void QWindowOverHandle(QWindowDrag lDrag)
    {
        if (lDrag.QWindowDragGroup)
        {
            qWindowDropLast = null;
            QWindowDropSet(QWindowDropEffect.QWindowDropNone, false);
            return;
        }

        QWindowDropStart(lDrag);
        QWindowDropEffect lEffect = QWindowDropResolve(lDrag, out string lReason, out _);
        if (lEffect != qWindowDropLast)
        {
            qWindowDropLast = lEffect;
            QWindowDropAppend(
                lDrag,
                "Drag over: "
                + (lEffect == QWindowDropEffect.QWindowDropNone ? "REFUSED (forbidden cursor)" : lEffect.ToString()),
                lReason);
        }

        QWindowDropSet(lEffect, true);
    }

    public void QWindowDropHandle(QWindowDrag lDrag)
    {
        if (lDrag.QWindowDragGroup)
        {
            QWindowDropSet(QWindowDropEffect.QWindowDropNone, false);
            return;
        }

        qWindowDropLast = null;
        QWindowDropEffect lEffect = QWindowDropResolve(lDrag, out string lReason, out string lName);
        QWindowDropSet(lEffect, true);

        string lTarget = qWindow.QWindowListPresent ? "list" : qWindow.QWindowViewerPresent ? "viewer" : "none";
        QWindowDropAppend(
            lDrag,
            $"Drop released: {(lEffect == QWindowDropEffect.QWindowDropNone ? "REFUSED" : "accepted")} onto {lTarget}",
            lReason);

        if (lEffect == QWindowDropEffect.QWindowDropNone)
        {
            QWindowDropRecord($"File drag refused onto {lTarget}");
            return;
        }

        IReadOnlyList<string> lPaths = QWindowPathsRead(lDrag);
        if (qWindow.QWindowListPresent)
        {
            QWindowListAdded?.Invoke(lPaths);
            QWindowDropAppend(lDrag, $"Drop into list: {lPaths.Count} path(s) handed to the list scan");
            QWindowDropRecord($"File drag accepted onto list ({lEffect})");
            return;
        }

        if (!qWindow.QWindowViewerPresent)
        {
            QWindowDropRecord($"File drag accepted onto {lTarget} ({lEffect})");
            return;
        }

        string? lSourcePath = QWindowPathRead(lDrag);
        if (lSourcePath is null)
        {
            QWindowDropSet(QWindowDropEffect.QWindowDropNone, true);
            QWindowDropAppend(lDrag, "Drop into viewer refused: no existing file in payload");
            QWindowDropRecord("File drag refused onto viewer", true);
            return;
        }

        QWindowViewerOpened?.Invoke(lSourcePath);
        QWindowDropAppend(lDrag, $"Drop into viewer: opened {lName}");
        QWindowDropRecord($"File drag accepted onto viewer ({lEffect})");
    }

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

    private void QWindowDropAppend(QWindowDrag lDrag, string lSummary, string? lDetail = null)
    {
        QWindowDropStart(lDrag);
        string lTime = DateTimeOffset.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
        qWindowDropTrace.Add($"{lTime}  {lSummary}");
        if (!string.IsNullOrWhiteSpace(lDetail))
        {
            qWindowDropTrace.Add($"{new string(' ', QWindowDropIndent)}{lDetail}");
        }
    }

    private void QWindowDropStart(QWindowDrag lDrag)
    {
        if (ReferenceEquals(qWindowDropData, lDrag.QWindowDragData))
        {
            return;
        }

        qWindowDropData = lDrag.QWindowDragData;
        qWindowDropTrace.Clear();
        qWindowDropLast = null;
    }

    private void QWindowDropRecord(string lSummary, bool lWarning = false)
    {
        string? lDetail = qWindowDropTrace.Count == 0
            ? null
            : string.Join(Environment.NewLine, qWindowDropTrace);
        if (lWarning)
        {
            LTraceLog.LTraceWarningRecord(lSummary, lDetail);
        }
        else
        {
            LTraceLog.LTraceInfoRecord(lSummary, lDetail);
        }

        qWindowDropData = null;
        qWindowDropTrace.Clear();
        qWindowDropLast = null;
    }
}

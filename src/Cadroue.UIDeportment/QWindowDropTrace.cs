using System.Globalization;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

// One file drag's trace: the timed lines a drag gathers, logged as one record when its drop ends. A new drag payload
// starts a fresh trace, and an over line is added only when the effect changes.
public sealed class QWindowDropTrace
{
    private const int QWindowDropIndent = 14;
    private readonly List<string> qWindowDropTrace = [];
    private object? qWindowDropData;
    private QWindowDropEffect? qWindowDropLast;

    internal void QWindowOverAppend(QWindowDrag lDrag, QWindowDropEffect lEffect, string lReason)
    {
        if (lEffect == qWindowDropLast)
        {
            return;
        }

        qWindowDropLast = lEffect;
        QWindowDropAppend(
            lDrag,
            "Drag over: "
            + (lEffect == QWindowDropEffect.QWindowDropNone ? "REFUSED (forbidden cursor)" : lEffect.ToString()),
            lReason);
    }

    internal void QWindowLastClear() => qWindowDropLast = null;

    internal void QWindowReleaseAppend(QWindowDrag lDrag, QWindowDropEffect lEffect, string lTarget, string lReason) =>
        QWindowDropAppend(
            lDrag,
            $"Drop released: {(lEffect == QWindowDropEffect.QWindowDropNone ? "REFUSED" : "accepted")} onto {lTarget}",
            lReason);

    internal void QWindowDropAppend(QWindowDrag lDrag, string lSummary, string? lDetail = null)
    {
        QWindowDropStart(lDrag);
        string lTime = DateTimeOffset.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
        qWindowDropTrace.Add($"{lTime}  {lSummary}");
        if (!string.IsNullOrWhiteSpace(lDetail))
        {
            qWindowDropTrace.Add($"{new string(' ', QWindowDropIndent)}{lDetail}");
        }
    }

    internal void QWindowDropStart(QWindowDrag lDrag)
    {
        if (ReferenceEquals(qWindowDropData, lDrag.QWindowDragData))
        {
            return;
        }

        qWindowDropData = lDrag.QWindowDragData;
        qWindowDropTrace.Clear();
        qWindowDropLast = null;
    }

    internal void QWindowDropRecord(string lSummary, bool lWarning = false)
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

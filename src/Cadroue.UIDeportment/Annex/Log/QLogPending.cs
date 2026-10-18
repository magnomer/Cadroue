using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

// Committed trace entries handed over from the trace thread, held under a lock until the log takes them.
public sealed class QLogPending
{
    private readonly List<(long QLogSequence, LTraceEntry QLogEntry)> qLogPendingEntries = [];
    private readonly object qLogPendingLock = new();

    public void QLogPendingAttach() => LTrace.LTraceCommittedAppended += QLogPendingAdd;

    public void QLogPendingDetach() => LTrace.LTraceCommittedAppended -= QLogPendingAdd;

    // Clears the entries the snapshot already settled.
    public void QLogPendingClear(Func<long, bool> lSettled)
    {
        lock (qLogPendingLock)
        {
            qLogPendingEntries.RemoveAll(lItem => lSettled(lItem.QLogSequence));
        }
    }

    public List<(long QLogSequence, LTraceEntry QLogEntry)>? QLogPendingRemove()
    {
        lock (qLogPendingLock)
        {
            if (qLogPendingEntries.Count == 0)
            {
                return null;
            }

            var lBatch = new List<(long QLogSequence, LTraceEntry QLogEntry)>(qLogPendingEntries);
            qLogPendingEntries.Clear();
            return lBatch;
        }
    }

    private void QLogPendingAdd(long lSequence, LTraceEntry lEntry)
    {
        lock (qLogPendingLock)
        {
            qLogPendingEntries.Add((lSequence, lEntry));
        }
    }
}

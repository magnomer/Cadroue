using System.Collections.Generic;
using System.Threading;

namespace Cadroue.Infrastructure;

public static partial class LTrace
{
    private const int LTraceDrawPeriod = 1000;

    private static readonly object lTraceDrawLock = new();
    private static readonly Dictionary<string, LTraceDrawTally> lTraceDrawTable = new(StringComparer.Ordinal);

    private static Timer? lTraceDrawTimer;
    private static LTraceTimelineTally? lTraceTimelineTally;

    public static void LTraceDrawAdd(
        string lTraceSurface,
        string lTraceTrigger,
        double lTraceMilliseconds,
        int lTraceGlyphCount = 0)
    {
        if (!Volatile.Read(ref lTraceVerbose))
        {
            return;
        }

        lock (lTraceDrawLock)
        {
            if (!Volatile.Read(ref lTraceVerbose))
            {
                return;
            }

            if (!lTraceDrawTable.TryGetValue(lTraceSurface, out LTraceDrawTally? lTraceTally))
            {
                lTraceTally = new LTraceDrawTally();
                lTraceDrawTable[lTraceSurface] = lTraceTally;
            }

            lTraceTally.LTraceDrawAdd(lTraceTrigger, lTraceMilliseconds, lTraceGlyphCount);
        }
    }

    public static void LTraceTimelineAdd(
        string lTraceSurface,
        TimeSpan lTraceCursor,
        string? lTraceSourcePath,
        string lTraceTrigger,
        double lTraceMilliseconds,
        int lTraceGlyphCount = 0)
    {
        if (!Volatile.Read(ref lTraceVerbose))
        {
            return;
        }

        lock (lTraceDrawLock)
        {
            if (!Volatile.Read(ref lTraceVerbose))
            {
                return;
            }

            lTraceTimelineTally ??= new LTraceTimelineTally();
            lTraceTimelineTally.LTraceDrawAdd(
                lTraceSurface,
                lTraceCursor,
                lTraceSourcePath,
                lTraceTrigger,
                lTraceMilliseconds,
                lTraceGlyphCount);
        }
    }

    public static void LTraceDrawTick()
    {
        List<(string Surface, LTraceDrawTally Tally)> lTraceReady;
        LTraceTimelineTally? lTraceTimelineReady;
        lock (lTraceDrawLock)
        {
            if (lTraceDrawTable.Count == 0 && lTraceTimelineTally is null)
            {
                return;
            }

            lTraceReady = new List<(string, LTraceDrawTally)>(lTraceDrawTable.Count);
            foreach (KeyValuePair<string, LTraceDrawTally> lTraceEntry in lTraceDrawTable)
            {
                lTraceReady.Add((lTraceEntry.Key, lTraceEntry.Value));
            }

            lTraceDrawTable.Clear();
            lTraceTimelineReady = lTraceTimelineTally;
            lTraceTimelineTally = null;
        }

        foreach ((string lTraceSurface, LTraceDrawTally lTraceTally) in lTraceReady)
        {
            LTraceRecord(
                LTraceKind.LTraceUi,
                lTraceTally.LTraceSummaryRead(lTraceSurface),
                lTraceTally.LTraceDetailRead(),
                null,
                false,
                1,
                true);
        }

        if (lTraceTimelineReady is not null)
        {
            LTraceRecord(
                LTraceKind.LTraceUi,
                lTraceTimelineReady.LTraceSummaryRead(),
                lTraceTimelineReady.LTraceDetailRead(),
                null,
                false,
                1,
                true);
        }
    }

    public static void LTraceReset()
    {
        lock (lTraceDrawLock)
        {
            lTraceDrawTable.Clear();
            lTraceTimelineTally = null;
        }

        lock (lTraceStampLock)
        {
            lTracePreviousStamp = -1;
        }
    }

    private static void LTraceTimerSet(bool lTraceRunning)
    {
        if (!lTraceRunning)
        {
            Timer? lTraceStopping = Interlocked.Exchange(ref lTraceDrawTimer, null);
            lTraceStopping?.Dispose();
            LTraceDrawTick();
            return;
        }

        var lTraceStarting = new Timer(
            _ => LTraceDrawTick(),
            null,
            LTraceDrawPeriod,
            LTraceDrawPeriod);
        Timer? lTracePrevious = Interlocked.Exchange(ref lTraceDrawTimer, lTraceStarting);
        lTracePrevious?.Dispose();
    }
}

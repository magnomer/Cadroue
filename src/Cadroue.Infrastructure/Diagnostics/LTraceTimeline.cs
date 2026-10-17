using System.Threading;

namespace Cadroue.Infrastructure;

public static class LTraceTimeline
{
    private const int LTraceDrawPeriod = 1000;

    private static readonly object lTraceDrawLock = new();

    private static Timer? lTraceDrawTimer;
    private static LTraceTimelineTally? lTraceTimelineTally;

    public static void LTraceTimelineAdd(
        string lTraceSurface,
        TimeSpan lTraceCursor,
        string? lTraceSourcePath,
        string lTraceTrigger,
        double lTraceMilliseconds,
        int lTraceGlyphCount = 0)
    {
        if (!LTrace.LTraceVerbose)
        {
            return;
        }

        lock (lTraceDrawLock)
        {
            if (!LTrace.LTraceVerbose)
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
        LTraceTimelineTally? lTraceTimelineReady;
        lock (lTraceDrawLock)
        {
            lTraceTimelineReady = lTraceTimelineTally;
            lTraceTimelineTally = null;
        }

        if (lTraceTimelineReady is null)
        {
            return;
        }

        LTrace.LTraceRecord(
            LTraceKind.LTraceUi,
            lTraceTimelineReady.LTraceSummaryRead(),
            lTraceTimelineReady.LTraceDetailRead(),
            null,
            false,
            1,
            true);
    }

    internal static void LTraceDrawReset()
    {
        lock (lTraceDrawLock)
        {
            lTraceTimelineTally = null;
        }
    }

    internal static void LTraceDrawStart() => LTraceTimerSet(true);

    internal static void LTraceDrawStop(Action lTraceVerboseClear)
    {
        lock (lTraceDrawLock)
        {
            LTraceTimerSet(false);
            lTraceVerboseClear();
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

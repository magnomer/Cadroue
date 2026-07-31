using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;

namespace Cadroue.UIShell;

public enum LTraceKind
{
    LTraceInfo,
    LTraceError,
    LTraceDraw,
    LTraceView,
    LTraceWork,
    LTraceFfmpeg
}

public static class LTrace
{
    private const int LTraceDrawPeriod = 1000;

    private static readonly object lTraceStampLock = new();
    private static readonly object lTraceDrawLock = new();
    private static readonly Dictionary<string, LTraceDrawTally> lTraceDrawTable = new(StringComparer.Ordinal);

    private static long lTracePreviousStamp = -1;
    private static Timer? lTraceDrawTimer;
    private static bool lTraceVerbose;

    public static event Action<LTraceEntry>? LTraceAppend;

    public static Action<bool>? LTraceVerboseCallback;

    public static bool LTraceVerbose
    {
        get => Volatile.Read(ref lTraceVerbose);
        set
        {
            if (Volatile.Read(ref lTraceVerbose) == value)
            {
                return;
            }

            Volatile.Write(ref lTraceVerbose, value);
            LTraceTimerSet(value);
            LTraceVerboseCallback?.Invoke(value);
            LTraceRecord(
                LTraceKind.LTraceInfo,
                value ? "Verbose logging on" : "Verbose logging off",
                value
                    ? "Draw, View, Work and Ffmpeg entries are now recorded.\nDraw entries are aggregated once per second per surface."
                    : null);
        }
    }

    public static bool LTraceCheck(LTraceKind lTraceKind) =>
        lTraceKind is LTraceKind.LTraceInfo or LTraceKind.LTraceError || Volatile.Read(ref lTraceVerbose);

    public static void LTraceRecord(
        LTraceKind lTraceKind,
        string lTraceSummary,
        string? lTraceDetail = null,
        double? lTraceMilliseconds = null)
    {
        if (!LTraceCheck(lTraceKind))
        {
            return;
        }

        DateTimeOffset lTraceNow = DateTimeOffset.Now;
        var lTraceEntry = new LTraceEntry(
            lTraceNow.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture),
            LTraceDeltaFormat(lTraceNow),
            lTraceKind,
            lTraceSummary,
            lTraceDetail,
            lTraceMilliseconds);

        LTraceWriter.LTraceWriterRecord(LTraceEntry.LTraceEntryFormat(lTraceEntry));
        LTraceAppend?.Invoke(lTraceEntry);
    }

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
            if (!lTraceDrawTable.TryGetValue(lTraceSurface, out LTraceDrawTally? lTraceTally))
            {
                lTraceTally = new LTraceDrawTally();
                lTraceDrawTable[lTraceSurface] = lTraceTally;
            }

            lTraceTally.LTraceDrawAdd(lTraceTrigger, lTraceMilliseconds, lTraceGlyphCount);
        }
    }

    public static void LTraceDrawTick()
    {
        List<(string Surface, LTraceDrawTally Tally)> lTraceReady;
        lock (lTraceDrawLock)
        {
            if (lTraceDrawTable.Count == 0)
            {
                return;
            }

            lTraceReady = new List<(string, LTraceDrawTally)>(lTraceDrawTable.Count);
            foreach (KeyValuePair<string, LTraceDrawTally> lTraceEntry in lTraceDrawTable)
            {
                lTraceReady.Add((lTraceEntry.Key, lTraceEntry.Value));
            }

            lTraceDrawTable.Clear();
        }

        foreach ((string lTraceSurface, LTraceDrawTally lTraceTally) in lTraceReady)
        {
            LTraceRecord(
                LTraceKind.LTraceDraw,
                lTraceTally.LTraceSummaryRead(lTraceSurface),
                lTraceTally.LTraceDetailRead());
        }
    }

    public static void LTraceReset()
    {
        lock (lTraceDrawLock)
        {
            lTraceDrawTable.Clear();
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

    private static string LTraceDeltaFormat(DateTimeOffset lTraceNow)
    {
        long lTraceStamp = lTraceNow.UtcTicks;
        long lTracePrevious;
        lock (lTraceStampLock)
        {
            lTracePrevious = lTracePreviousStamp;
            lTracePreviousStamp = lTraceStamp;
        }

        if (lTracePrevious < 0)
        {
            return "Δ-";
        }

        double lTraceSeconds = TimeSpan.FromTicks(Math.Max(0, lTraceStamp - lTracePrevious)).TotalSeconds;
        return lTraceSeconds >= 999.999
            ? "Δ999.9+"
            : string.Create(CultureInfo.InvariantCulture, $"Δ{lTraceSeconds:0.000}");
    }

    private sealed class LTraceDrawTally
    {
        private readonly Dictionary<string, int> lTraceTriggerCounts = new(StringComparer.Ordinal);

        private int lTraceRenderCount;
        private double lTraceTotalMilliseconds;
        private double lTracePeakMilliseconds;
        private long lTraceGlyphTotal;

        internal void LTraceDrawAdd(string lTraceTrigger, double lTraceMilliseconds, int lTraceGlyphCount)
        {
            lTraceRenderCount++;
            lTraceTotalMilliseconds += lTraceMilliseconds;
            lTraceGlyphTotal += lTraceGlyphCount;
            if (lTraceMilliseconds > lTracePeakMilliseconds)
            {
                lTracePeakMilliseconds = lTraceMilliseconds;
            }

            lTraceTriggerCounts.TryGetValue(lTraceTrigger, out int lTraceSeen);
            lTraceTriggerCounts[lTraceTrigger] = lTraceSeen + 1;
        }

        internal string LTraceSummaryRead(string lTraceSurface) => string.Create(
            CultureInfo.InvariantCulture,
            $"{lTraceSurface} drew {lTraceRenderCount}x in the last second");

        internal string LTraceDetailRead()
        {
            var lTraceBuilder = new StringBuilder();
            double lTraceAverage = lTraceRenderCount == 0 ? 0 : lTraceTotalMilliseconds / lTraceRenderCount;
            lTraceBuilder.Append(CultureInfo.InvariantCulture,
                $"avg {lTraceAverage:0.00}ms, peak {lTracePeakMilliseconds:0.00}ms, total {lTraceTotalMilliseconds:0.0}ms");

            if (lTraceGlyphTotal > 0)
            {
                double lTracePerRender = lTraceRenderCount == 0 ? 0 : (double)lTraceGlyphTotal / lTraceRenderCount;
                lTraceBuilder.Append('\n');
                lTraceBuilder.Append(CultureInfo.InvariantCulture,
                    $"{lTraceGlyphTotal} FormattedText built ({lTracePerRender:0.#}/draw)");
            }

            if (lTraceTriggerCounts.Count > 0)
            {
                lTraceBuilder.Append('\n');
                lTraceBuilder.Append("triggers: ");
                bool lTraceFirst = true;
                foreach (KeyValuePair<string, int> lTraceTrigger in lTraceTriggerCounts)
                {
                    if (!lTraceFirst)
                    {
                        lTraceBuilder.Append(", ");
                    }

                    lTraceBuilder.Append(CultureInfo.InvariantCulture, $"{lTraceTrigger.Key} {lTraceTrigger.Value}");
                    lTraceFirst = false;
                }
            }

            return lTraceBuilder.ToString();
        }
    }
}

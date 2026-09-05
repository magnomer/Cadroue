using System.Collections.Generic;
using System.Globalization;
using System.Threading;

namespace Cadroue.Infrastructure;

public enum LTraceKind
{
    LTraceInfo,
    LTraceLoading,
    LTraceWarning,
    LTraceError,
    LTraceInteraction,
    LTraceUi,
    LTraceWork,
    LTraceFfmpeg
}


public static partial class LTrace
{
    private static readonly object lTraceStampLock = new();
    private static readonly object lTraceVerboseLock = new();

    private static long lTracePreviousStamp = -1;
    private static bool lTraceVerbose;
    private static bool lTraceLoading;

    public static event Action<LTraceEntry>? LTraceAppend;
    internal static event Action<long, LTraceEntry>? LTraceCommittedAppend;

    public static Action<bool>? LTraceVerboseCallback;

    public static bool LTraceVerbose
    {
        get => Volatile.Read(ref lTraceVerbose);
        set
        {
            lock (lTraceVerboseLock)
            {
                if (Volatile.Read(ref lTraceVerbose) == value)
                {
                    return;
                }

                if (value)
                {
                    Volatile.Write(ref lTraceVerbose, true);
                    LTraceTimerSet(true);
                }
                else
                {
                    lock (lTraceDrawLock)
                    {
                        LTraceTimerSet(false);
                        Volatile.Write(ref lTraceVerbose, false);
                    }
                }

                LTraceVerboseCallback?.Invoke(value);
                LTraceRecord(
                    LTraceKind.LTraceInfo,
                    value ? "Verbose logging on" : "Verbose logging off",
                    value
                        ? "UI, Work and Ffmpeg entries are now recorded.\nUI draw entries are aggregated once per second per surface."
                        : null);
            }
        }
    }

    public static bool LTraceLoading
    {
        get => Volatile.Read(ref lTraceLoading);
    }

    public static void LTraceLoadingSet(bool lTraceLoadingActive) =>
        Volatile.Write(ref lTraceLoading, lTraceLoadingActive);

    public static bool LTraceCheck(LTraceKind lTraceKind) =>
        lTraceKind is LTraceKind.LTraceInfo
            or LTraceKind.LTraceLoading
            or LTraceKind.LTraceWarning
            or LTraceKind.LTraceError
            or LTraceKind.LTraceInteraction
            || Volatile.Read(ref lTraceVerbose);

    public static void LTraceRecord(
        LTraceKind lTraceKind,
        string lTraceSummary,
        string? lTraceDetail = null,
        double? lTraceMilliseconds = null) =>
        LTraceRecord(lTraceKind, lTraceSummary, lTraceDetail, lTraceMilliseconds, false, 1);

    private static void LTraceRecord(
        LTraceKind lTraceKind,
        string lTraceSummary,
        string? lTraceDetail,
        double? lTraceMilliseconds,
        bool lTraceLossReport,
        int lTraceLossWeight,
        bool lTraceAlreadyAccepted = false)
    {
        if (Volatile.Read(ref lTraceLoading)
            && lTraceKind is not (LTraceKind.LTraceLoading or LTraceKind.LTraceWarning or LTraceKind.LTraceError))
        {
            return;
        }

        if (!lTraceLossReport && LTraceWriter.LTraceLossRead() is int lTraceLost && lTraceLost > 0)
        {
            LTraceRecord(
                LTraceKind.LTraceWarning,
                "Trace entries lost",
                $"{lTraceLost} accepted entries could not be saved after repeated storage failures.",
                null,
                true,
                lTraceLost);
        }

        if (!lTraceAlreadyAccepted && !LTraceCheck(lTraceKind))
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

        LTraceWriter.LTraceWriterRecord(
            LTraceEntry.LTraceEntryFormat(lTraceEntry),
            lTraceSequence =>
            {
                LTraceCommittedAppend?.Invoke(lTraceSequence, lTraceEntry);
                LTraceAppend?.Invoke(lTraceEntry);
            },
            lTraceLossWeight);
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
}

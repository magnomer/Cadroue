using Cadroue.Core;

namespace Cadroue.Tests;

internal static class TKeyframeData
{
    internal static TimeSpan TKeyframeAtCreate(double seconds) => TimeSpan.FromSeconds(seconds);
    internal static LKeyframeEntry TKeyframeEntryCreate(double seconds) => new(TKeyframeAtCreate(seconds));
    internal static LKeyframeEntry TKeyframeEntryCreate(double seconds, double decodeSeconds) =>
        new(TKeyframeAtCreate(seconds), TKeyframeAtCreate(decodeSeconds));

    internal static LSpool TKeyframeSpoolCreate(double origin, double limit)
    {
        LSpool spool = TInterface.TSpoolCreate(TKeyframeAtCreate(limit));
        TInterface.TSpoolStartSet(spool, TKeyframeAtCreate(origin));
        TInterface.TSpoolEndSet(spool, TKeyframeAtCreate(limit));
        return spool;
    }

    internal static LKeyframeScanRange TKeyframeScanCreate(double start, double end) => new(TKeyframeAtCreate(start), TKeyframeAtCreate(end));
}

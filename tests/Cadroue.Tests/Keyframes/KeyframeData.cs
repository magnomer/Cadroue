using Cadroue.Core;

namespace Cadroue.Tests;

internal static class KeyframeData
{
    internal static TimeSpan At(double seconds) => TimeSpan.FromSeconds(seconds);

    internal static LKeyframeEntry Entry(double seconds) => new(At(seconds));

    internal static LSpool Spool(double origin, double limit)
    {
        var spool = new LSpool(At(limit));
        spool.LSpoolStartSet(At(origin));
        spool.LSpoolEndSet(At(limit));
        return spool;
    }

    internal static LKeyframeScanRange Scan(double start, double end) => new(At(start), At(end));
}

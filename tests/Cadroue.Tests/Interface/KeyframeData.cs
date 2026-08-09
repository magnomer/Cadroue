using Cadroue.Core;

namespace Cadroue.Tests;

internal static class KeyframeData
{
    internal static TimeSpan At(double seconds) => TimeSpan.FromSeconds(seconds);
    internal static LKeyframeEntry Entry(double seconds) => new(At(seconds));

    internal static LSpool Spool(double origin, double limit)
    {
        LSpool spool = TInterface.SpoolCreate(At(limit));
        TInterface.SpoolStartSet(spool, At(origin));
        TInterface.SpoolEndSet(spool, At(limit));
        return spool;
    }

    internal static LKeyframeScanRange Scan(double start, double end) => new(At(start), At(end));
}

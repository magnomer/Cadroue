using System.Globalization;

namespace Cadroue.ShellEngine;

internal static class LSweepField
{
    internal static double? LSweepFieldRead(string lSweepLine, string lSweepKey)
    {
        int lSweepAt = lSweepLine.IndexOf(lSweepKey, StringComparison.Ordinal);
        if (lSweepAt < 0)
        {
            return null;
        }

        int lSweepFrom = lSweepAt + lSweepKey.Length;
        int lSweepTo = lSweepFrom;
        while (lSweepTo < lSweepLine.Length && !char.IsWhiteSpace(lSweepLine[lSweepTo]))
        {
            lSweepTo++;
        }

        return double.TryParse(
            lSweepLine.AsSpan(lSweepFrom, lSweepTo - lSweepFrom),
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out double lSweepValue)
            ? lSweepValue
            : null;
    }
}

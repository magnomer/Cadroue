namespace Cadroue.ShellEngine;

public delegate bool LAutopsyProseReader(string lAutopsyProseKey, out string lAutopsyProseValue);

public static class LAutopsy
{
    private const long LAutopsyDwordMask = 0xFFFFFFFFL;
    private const long LAutopsySignBoundary = 0x80000000L;
    private const long LAutopsyWraparound = 0x100000000L;

    private const string LAutopsyNegativeProse = "fallback.unknownNegative";
    private const string LAutopsyPositiveProse = "fallback.unexpectedPositive";

    public static LAutopsyProseReader? LAutopsyProse { get; set; }

    internal static LAutopsyResult LAutopsyResolve(int lAutopsyExitCode, string lAutopsyStderrTail)
    {
        _ = lAutopsyStderrTail;

        long lAutopsyNormalized = lAutopsyExitCode & LAutopsyDwordMask;
        if (lAutopsyNormalized >= LAutopsySignBoundary)
        {
            lAutopsyNormalized -= LAutopsyWraparound;
        }

        int lAutopsyCode = (int)lAutopsyNormalized;
        bool lAutopsyMatched = LAutopsySpine.LAutopsySpineRead(
            lAutopsyCode.ToString(System.Globalization.CultureInfo.InvariantCulture),
            out LAutopsySpine lAutopsyEntry);

        string lAutopsyProseRoot;
        if (lAutopsyMatched)
        {
            lAutopsyProseRoot = lAutopsyCode.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        else
        {
            bool lAutopsyNegative = lAutopsyCode < 0;
            lAutopsyEntry = LAutopsySpine.LAutopsySpineResolve(lAutopsyNegative);
            lAutopsyProseRoot = lAutopsyNegative
                ? LAutopsyNegativeProse
                : LAutopsyPositiveProse;
        }

        return new LAutopsyResult(
            lAutopsyCode,
            LAutopsyProseRead(lAutopsyProseRoot + ".simple") ?? string.Empty,
            LAutopsyProseRead(lAutopsyProseRoot + ".technical") ?? string.Empty,
            LAutopsyProseRead(lAutopsyProseRoot + ".action"),
            lAutopsyEntry.LAutopsySpineCategory,
            lAutopsyEntry.LAutopsySpineSeverity,
            lAutopsyEntry.LAutopsySpineVisible,
            lAutopsyEntry.LAutopsySpineRetryable,
            lAutopsyEntry.LAutopsySpineSymbol,
            lAutopsyMatched);
    }

    private static string? LAutopsyProseRead(string lAutopsyKey)
    {
        LAutopsyProseReader? lAutopsyReader = LAutopsyProse;
        if (lAutopsyReader is not null && lAutopsyReader(lAutopsyKey, out string lAutopsyValue))
        {
            return lAutopsyValue;
        }

        return null;
    }
}

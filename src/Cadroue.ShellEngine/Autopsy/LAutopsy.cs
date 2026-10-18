using Cadroue.Application;

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

    internal static LAutopsyResult LAutopsyResolve(int lAutopsyExitCode)
    {
        int lAutopsyCode = LAutopsyCodeNormalize(lAutopsyExitCode);
        bool lAutopsyMatched = LAutopsySpineTable.LAutopsySpineRead(
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
            lAutopsyEntry = LAutopsySpineTable.LAutopsySpineResolve(lAutopsyNegative);
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

    // The exit code read as a signed 32-bit value, the form the spine keys hold.
    private static int LAutopsyCodeNormalize(int lAutopsyExitCode)
    {
        long lAutopsyNormalized = lAutopsyExitCode & LAutopsyDwordMask;
        if (lAutopsyNormalized >= LAutopsySignBoundary)
        {
            lAutopsyNormalized -= LAutopsyWraparound;
        }

        return (int)lAutopsyNormalized;
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

    public static void LAutopsyProseAttach() => LAutopsyProse = LLocalizationAutopsy.LLocalizationAutopsyRead;
}

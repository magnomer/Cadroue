using Cadroue.ShellEngine;

namespace Cadroue.Tests.Interface;

internal readonly record struct TAutopsyResult(
    int TAutopsyResultCode,
    string TAutopsyResultSimple,
    string TAutopsyResultTechnical,
    string? TAutopsyResultAction,
    string? TAutopsyResultSymbol,
    bool TAutopsyResultMatched);

internal static class TAutopsy
{
    internal static TAutopsyResult TAutopsyResolve(int exitCode) =>
        TAutopsyResultCreate(LAutopsy.LAutopsyResolve(exitCode, string.Empty));

    internal static TAutopsyResult TAutopsyProseResolve(
        int exitCode, IReadOnlyDictionary<string, string> prose)
    {
        LAutopsyProseReader? previous = LAutopsy.LAutopsyProse;
        try
        {
            LAutopsy.LAutopsyProse = (string key, out string value) => prose.TryGetValue(key, out value!);
            return TAutopsyResultCreate(LAutopsy.LAutopsyResolve(exitCode, string.Empty));
        }
        finally
        {
            LAutopsy.LAutopsyProse = previous;
        }
    }

    internal static TAutopsyResult TAutopsyPlainResolve(int exitCode)
    {
        LAutopsyProseReader? previous = LAutopsy.LAutopsyProse;
        try
        {
            LAutopsy.LAutopsyProse = null;
            return TAutopsyResultCreate(LAutopsy.LAutopsyResolve(exitCode, string.Empty));
        }
        finally
        {
            LAutopsy.LAutopsyProse = previous;
        }
    }

    private static TAutopsyResult TAutopsyResultCreate(LAutopsyResult result) => new(
        result.LAutopsyResultCode,
        result.LAutopsyResultSimple,
        result.LAutopsyResultTechnical,
        result.LAutopsyResultAction,
        result.LAutopsyResultSymbol,
        result.LAutopsyResultMatched);
}

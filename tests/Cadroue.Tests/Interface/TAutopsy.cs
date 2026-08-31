using Cadroue.ShellEngine;

namespace Cadroue.Tests;

internal static class TAutopsy
{
    internal static (int Code, bool Matched, string? Symbol) TAutopsyResolve(int exitCode)
    {
        LAutopsyResult result = LAutopsy.LAutopsyResolve(exitCode, string.Empty);
        return (result.LAutopsyResultCode, result.LAutopsyResultMatched, result.LAutopsyResultSymbol);
    }

    internal static (string Simple, string Technical, string? Action) TAutopsyProseResolve(
        int exitCode, IReadOnlyDictionary<string, string> prose)
    {
        LAutopsyProseReader? previous = LAutopsy.LAutopsyProse;
        try
        {
            LAutopsy.LAutopsyProse = (string key, out string value) => prose.TryGetValue(key, out value!);
            LAutopsyResult result = LAutopsy.LAutopsyResolve(exitCode, string.Empty);
            return (result.LAutopsyResultSimple, result.LAutopsyResultTechnical, result.LAutopsyResultAction);
        }
        finally
        {
            LAutopsy.LAutopsyProse = previous;
        }
    }

    internal static (string Simple, string Technical, string? Action) TAutopsyPlainResolve(int exitCode)
    {
        LAutopsyProseReader? previous = LAutopsy.LAutopsyProse;
        try
        {
            LAutopsy.LAutopsyProse = null;
            LAutopsyResult result = LAutopsy.LAutopsyResolve(exitCode, string.Empty);
            return (result.LAutopsyResultSimple, result.LAutopsyResultTechnical, result.LAutopsyResultAction);
        }
        finally
        {
            LAutopsy.LAutopsyProse = previous;
        }
    }
}

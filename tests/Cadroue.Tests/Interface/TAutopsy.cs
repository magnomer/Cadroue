using Cadroue.ShellEngine;

namespace Cadroue.Tests;

internal static class TAutopsy
{
    internal static (int Code, bool Matched, string? Symbol) Resolve(int exitCode)
    {
        LAutopsyResult result = LAutopsy.LAutopsyResolve(exitCode, string.Empty);
        return (result.LAutopsyResultCode, result.LAutopsyResultMatched, result.LAutopsyResultSymbol);
    }
}

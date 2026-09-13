using Cadroue.ShellEngine;

namespace Cadroue.Tests;

internal static class TAutopsy
{
    internal static LAutopsyResult TAutopsyResolve(int exitCode) =>
        LAutopsy.LAutopsyResolve(exitCode, string.Empty);

    internal static LAutopsyResult TAutopsyProseResolve(
        int exitCode, IReadOnlyDictionary<string, string> prose)
    {
        LAutopsyProseReader? previous = LAutopsy.LAutopsyProse;
        try
        {
            LAutopsy.LAutopsyProse = (string key, out string value) => prose.TryGetValue(key, out value!);
            return LAutopsy.LAutopsyResolve(exitCode, string.Empty);
        }
        finally
        {
            LAutopsy.LAutopsyProse = previous;
        }
    }

    internal static LAutopsyResult TAutopsyPlainResolve(int exitCode)
    {
        LAutopsyProseReader? previous = LAutopsy.LAutopsyProse;
        try
        {
            LAutopsy.LAutopsyProse = null;
            return LAutopsy.LAutopsyResolve(exitCode, string.Empty);
        }
        finally
        {
            LAutopsy.LAutopsyProse = previous;
        }
    }
}

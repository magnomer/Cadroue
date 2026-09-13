using Cadroue.Core;

namespace Cadroue.Tests;

internal sealed class TClassifierFault : IDisposable
{
    private readonly Action<string>? tClassifierPrevious;

    internal TClassifierFault(Action<string> record)
    {
        tClassifierPrevious = LClassifier.LClassifierFaultSource;
        LClassifier.LClassifierFaultSource = record;
    }

    public void Dispose() => LClassifier.LClassifierFaultSource = tClassifierPrevious;
}

internal static class TClassifierData
{
    internal static LSceneFunnelMatch TClassifierConditionCreate(
        string text,
        bool caseSensitive = false,
        bool join = true) =>
        new() { LSceneFunnelText = text, LSceneFunnelCase = caseSensitive, LSceneFunnelJoin = join };

    internal static LSceneFunnelRule TClassifierRegexCreate(string pattern, bool whole) =>
        new()
        {
            LSceneFunnelType = (int)LSceneFunnelForm.LSceneFunnelRegex,
            LSceneFunnelRegex = pattern,
            LSceneFunnelWhole = whole
        };

    internal static LSceneFunnelRule TClassifierFilenameCreate() =>
        new() { LSceneFunnelType = (int)LSceneFunnelForm.LSceneFunnelFilename };

    internal static LSceneFunnelRule TClassifierRemainderCreate() =>
        new() { LSceneFunnelType = (int)LSceneFunnelForm.LSceneFunnelFilename, LSceneFunnelRemainder = true };
}

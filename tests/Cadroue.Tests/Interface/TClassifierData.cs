using Cadroue.Core;

namespace Cadroue.Tests;

internal static class TClassifierData
{
    internal static LSceneFunnelMatch TClassifierConditionCreate(string text, bool caseSensitive = false, bool join = true) =>
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

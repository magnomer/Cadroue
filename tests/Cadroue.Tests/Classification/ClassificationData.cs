using Cadroue.Core;

namespace Cadroue.Tests;

internal static class ClassificationData
{
    internal static LSceneFunnelMatch Cond(string text, bool caseSensitive = false, bool join = true) =>
        new() { LSceneFunnelText = text, LSceneFunnelCase = caseSensitive, LSceneFunnelJoin = join };

    internal static LSceneFunnelRule Regex(string pattern, bool whole) =>
        new()
        {
            LSceneFunnelType = (int)LSceneFunnelForm.LSceneFunnelRegex,
            LSceneFunnelRegex = pattern,
            LSceneFunnelWhole = whole
        };

    internal static LSceneFunnelRule Filename() =>
        new() { LSceneFunnelType = (int)LSceneFunnelForm.LSceneFunnelFilename };
}

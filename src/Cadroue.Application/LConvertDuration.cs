using Cadroue.Core;

namespace Cadroue.Application;

public static class LConvertDuration
{
    public static async Task<Func<string, TimeSpan>> LConvertDurationResolve(
        LEncoding lConvertOutput, IReadOnlyList<string> lConvertSourcePaths)
    {
        if (!LConvertDurationCheck(lConvertOutput.LEncodingNamePattern ?? string.Empty))
        {
            return LLibrarian.LLibrarianDurationRead;
        }

        Dictionary<string, TimeSpan> lConvertDurations = await Task.Run(async () =>
        {
            var lConvertMap = new Dictionary<string, TimeSpan>(StringComparer.OrdinalIgnoreCase);
            foreach (string lConvertSourcePath in lConvertSourcePaths)
            {
                lConvertMap[lConvertSourcePath] =
                    await LLibrarian.LLibrarianDurationResolve(lConvertSourcePath).ConfigureAwait(false);
            }

            return lConvertMap;
        }).ConfigureAwait(true);

        return lConvertSourcePath =>
            lConvertDurations.TryGetValue(lConvertSourcePath, out TimeSpan lConvertDuration)
                ? lConvertDuration
                : LLibrarian.LLibrarianDurationRead(lConvertSourcePath);
    }

    private static bool LConvertDurationCheck(string lConvertPattern) =>
        lConvertPattern.Contains("{SectionEnd}", StringComparison.OrdinalIgnoreCase)
        || lConvertPattern.Contains("{SectionDuration}", StringComparison.OrdinalIgnoreCase);
}

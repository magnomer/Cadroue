using Cadroue.Core;
using Cadroue.UIShell.PPanels;

namespace Cadroue.UIShell.PMainArea;

public sealed record LConvertWorkDescription(
    IReadOnlyList<string> LConvertSourcePaths,
    LWorkOutput LConvertOutput,
    IReadOnlyDictionary<string, LWorkMedia>? LConvertMedia = null);

public static partial class LConvert
{
    public static async Task<int> LConvertDescribe(
        LWorkPriority lWorkPriority,
        IReadOnlyList<string> lConvertSourcePaths,
        LExportSpecificState lExportSpecificState)
    {
        LWorkOutput lConvertOutput = lExportSpecificState.LPresetOutputCreate();
        IReadOnlyDictionary<string, LWorkMedia> lConvertMedia =
            await Task.Run(() => LConvertMediaCollect(lConvertSourcePaths)).ConfigureAwait(true);

        LConvertWorkDescription lConvertWorkDescription = new(
            lConvertSourcePaths,
            lConvertOutput,
            lConvertMedia);

        return LConvert.LConvertInterpret(lWorkPriority, lConvertWorkDescription);
    }

    private static IReadOnlyDictionary<string, LWorkMedia> LConvertMediaCollect(
        IReadOnlyList<string> lConvertSourcePaths)
    {
        var lConvertMedia = new Dictionary<string, LWorkMedia>(StringComparer.OrdinalIgnoreCase);
        foreach (string lConvertSourcePath in lConvertSourcePaths)
        {
            if (lConvertMedia.ContainsKey(lConvertSourcePath))
            {
                continue;
            }

            if (LConvertMediaRead(lConvertSourcePath) is { } lConvertProbed)
            {
                lConvertMedia[lConvertSourcePath] = lConvertProbed;
            }
        }

        return lConvertMedia;
    }

    private static LWorkMedia? LConvertMediaRead(string lConvertSourcePath)
    {
        try
        {
            Cadroue.Media.LMediaInfo lConvertInfo = Cadroue.Media.LMediaInfo.LMediaFfprobeRead(lConvertSourcePath);
            return new LWorkMedia(
                lConvertInfo.LMediaInfoVideoWidth,
                lConvertInfo.LMediaInfoVideoHeight,
                lConvertInfo.LMediaInfoVideoFrameRate,
                (long)Math.Round(lConvertInfo.LMediaInfoDuration.TotalMilliseconds),
                lConvertInfo.LMediaInfoVideoPresent);
        }
        catch (Exception lConvertError)
        {
            LAppLog.LError($"Convert could not read '{System.IO.Path.GetFileName(lConvertSourcePath)}': {lConvertError.Message}");
            return null;
        }
    }
}

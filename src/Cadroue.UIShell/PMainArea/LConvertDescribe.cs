using Cadroue.Core;
using Cadroue.UIShell.PPanels;

namespace Cadroue.UIShell.PMainArea;

public sealed record LConvertWorkDescription(
    IReadOnlyList<string> LConvertSourcePaths,
    LWorkOutput LConvertOutput);

public static partial class LConvert
{
    public static int LConvertDescribe(
        LWorkPriority lWorkPriority,
        IReadOnlyList<string> lConvertSourcePaths,
        LExportSpecificState lExportSpecificState)
    {
        LConvertWorkDescription lConvertWorkDescription = new(
            lConvertSourcePaths,
            lExportSpecificState.LPresetOutputCreate());

        return LConvert.LConvertInterpret(lWorkPriority, lConvertWorkDescription);
    }
}

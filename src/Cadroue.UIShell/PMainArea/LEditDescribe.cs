using Cadroue.Core;
using Cadroue.UIShell.PPanels;

namespace Cadroue.UIShell.PMainArea;

public sealed record LEditWorkDescription(
    string? LEditSourcePath,
    TimeSpan LEditDuration,
    LWorkCrop LEditCrop,
    LWorkOutput LEditOutput);

public static partial class LEdit
{
    public static int LEditDescribe(
        LWorkPriority lWorkPriority,
        string? lEditSourcePath,
        TimeSpan lEditDuration,
        LWorkCrop lEditCrop,
        LExportSpecificState lExportSpecificState)
    {
        LEditWorkDescription lEditWorkDescription = new(
            lEditSourcePath,
            lEditDuration,
            lEditCrop,
            lExportSpecificState.LPresetOutputCreate());

        return LEdit.LEditInterpret(lWorkPriority, lEditWorkDescription);
    }
}

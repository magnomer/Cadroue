using Cadroue.Core;
using Cadroue.UIShell.PPanels;

namespace Cadroue.UIShell.PMainArea;

public static partial class LMerge
{
    public static int LMergeDescribe(
        LWorkPriority lWorkPriority,
        IReadOnlyList<PGroup.PGroupSelection> lMergeGroups,
        LExportSpecificState lExportSpecificState)
    {
        return LMerge.LMergeInterpret(lWorkPriority, lMergeGroups, lExportSpecificState.LPresetOutputCreate());
    }
}

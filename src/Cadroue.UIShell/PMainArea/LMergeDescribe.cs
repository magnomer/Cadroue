using Cadroue.Core;
using Cadroue.UIShell.PPanels;

namespace Cadroue.UIShell.PMainArea;

public static partial class LMerge
{
    public static int LMergeDescribe(
        LWorkPriority lWorkPriority,
        IReadOnlyList<PGroup.PGroupSelection> lMergeGroups,
        LPreset lExportSpecificState,
        Guid lMergeRelayTarget = default,
        Guid lMergeRelaySource = default)
    {
        return LMerge.LMergeInterpret(
            lWorkPriority,
            lMergeGroups,
            lExportSpecificState.LPresetOutputCreate(),
            lMergeRelayTarget,
            lMergeRelaySource);
    }
}

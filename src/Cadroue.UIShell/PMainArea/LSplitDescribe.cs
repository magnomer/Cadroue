using Cadroue.Core;
using Cadroue.UIShell.PPanels;

namespace Cadroue.UIShell.PMainArea;

public static partial class LSplit
{
    public static int LSplitDescribe(
        LWorkPriority lWorkPriority,
        string? lSplitSourcePath,
        IReadOnlyList<LSplitSectionDescription> lSplitSections,
        LExportSpecificState lExportSpecificState)
    {
        LSplitWorkDescription lSplitWorkDescription = new(
            lSplitSourcePath,
            lSplitSections,
            lExportSpecificState.LPresetOutputCreate());

        return LSplit.LSplitInterpret(lWorkPriority, lSplitWorkDescription);
    }
}

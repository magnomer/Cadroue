using Cadroue.Core;
using Cadroue.UIShell.PPanels;

namespace Cadroue.UIShell.PMainArea;

public static partial class LSplit
{
    /// <summary>
    /// Gather the current split settings — source, sections and export settings — and
    /// hand them to the interpreter. Returns how many work items reached the schedule.
    /// </summary>
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

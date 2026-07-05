using Cadroue.UIShell.LWork;

namespace Cadroue.UIShell.PMainArea;

public static partial class LSplit
{
    public static void LSplitDescribe(
        LWorkPriority lWorkPriority,
        string? lSplitSourcePath,
        IReadOnlyList<LSplitSectionDescription> lSplitSections)
    {
        LSplitWorkDescription lSplitWorkDescription = new(lSplitSourcePath, lSplitSections);
        LSplit.LSplitInterpret(lWorkPriority, lSplitWorkDescription);
    }
}

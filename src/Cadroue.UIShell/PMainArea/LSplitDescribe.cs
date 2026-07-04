using Cadroue.UIShell.LWork;

namespace Cadroue.UIShell.PMainArea;

public static class LSplitDescribe
{
    public static void LSplitDescribeCall(
        LWorkListAddPriority lWorkListAddPriority,
        string? lSplitSourcePath,
        IReadOnlyList<LSplitSectionDescription> lSplitSections)
    {
        LSplitWorkDescription lSplitWorkDescription = new(lSplitSourcePath, lSplitSections);
        LSplitInterpret.LSplitInterpreterCall(lWorkListAddPriority, lSplitWorkDescription);
    }
}

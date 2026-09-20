using Cadroue.Application;
using Cadroue.Core;
using Cadroue.ShellEngine;

namespace Cadroue.UIDeportment;

public sealed class LFunnelTab
{
    private readonly LList lFunnelList;
    private readonly LDocket lFunnelDocket;

    public LFunnelTab(LList lList, LDocket lDocket)
    {
        lFunnelList = lList;
        lFunnelDocket = lDocket;
    }

    public LFunnel LFunnel { get; } = new();

    public void LFunnelClose() => LFunnel.LFunnelStripDetach();

    public IReadOnlyList<string> LFunnelEligibleRead() => lFunnelDocket.LDocketPathsRead();

    public void LFunnelLayoutApply(LSceneTabRecord? lLayout)
    {
        if (lLayout is { } lRecord)
        {
            LFunnel.LFunnelRulesRestore(lRecord.LSceneFunnelRules);
        }
    }

    public LSceneTabRecord LFunnelLayoutRead(LSceneTabRecord lLayout)
    {
        lLayout.LSceneFunnelRules = LFunnel.LFunnelRules.Select(LFunnelLayoutCreate).ToList();
        return lLayout;
    }

    public void LFunnelRun() =>
        LFunnelDispatch(
            lFunnelList.LListPathCurrent is { } lPath && lFunnelDocket.LDocketItemFind(lPath) is { } lItem
                ? [lItem]
                : []);

    public void LFunnelAllRun() => LFunnelDispatch(lFunnelDocket.LDocketItemsRead());

    public void LFunnelItemsRun(IReadOnlyList<string> lPaths) =>
        LFunnelDispatch(
            lFunnelDocket.LDocketItemsRead()
                .Where(lItem => lPaths.Contains(lItem.LDocketEntryPath, StringComparer.OrdinalIgnoreCase))
                .ToArray());

    private void LFunnelDispatch(IReadOnlyList<LDocketEntry> lItems)
    {
        if (lItems.Count == 0)
        {
            return;
        }

        var lLive = LFunnel.LFunnelTargetsRead().Select(lTarget => lTarget.LFunnelTargetId).ToHashSet();
        IReadOnlyList<LFunnelRule> lRules = LFunnel.LFunnelRules;
        LMessenger.LMessengerFunnelDescribe(
            LFunnel.LFunnelSelf,
            lRules.Select(LFunnel.LFunnelRecordCreate).ToList(),
            lRules
                .Select(lRule => lLive.Contains(lRule.LFunnelRuleTarget) ? lRule.LFunnelRuleTarget : Guid.Empty)
                .ToList(),
            lItems.Select(lItem => (lItem.LDocketEntryPath, lItem.LDocketEntryBatch)).ToList());
    }

    private LSceneFunnelRule LFunnelLayoutCreate(LFunnelRule lRule)
    {
        LSceneFunnelRule lRecord = LFunnel.LFunnelRecordCreate(lRule);
        lRecord.LSceneFunnelTarget = LFunnel.LFunnelIndexRead(lRule.LFunnelRuleTarget);
        return lRecord;
    }
}

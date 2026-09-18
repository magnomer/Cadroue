using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

public sealed class TFunnelRule
{
    [Fact]
    public void JoinToggle_FlipsOneCondition_NotifiesOnce()
    {
        LFunnel funnel = TInterface.TFunnelCreate();
        LFunnelRule rule = TInterface.TFunnelRuleAdd(funnel, LFunnelForm.LFunnelFormFilename);
        List<LFunnelRule> notices = [];
        TInterface.TFunnelRuleAttach(funnel, notices.Add);

        TInterface.TFunnelJoinSet(funnel, rule, LFunnelKind.LFunnelKindPrefix, false);
        TInterface.TFunnelJoinSet(funnel, rule, LFunnelKind.LFunnelKindPrefix, false);

        Assert.False(rule.LFunnelRulePrefix.LSceneFunnelJoin);
        Assert.True(rule.LFunnelRuleEnd.LSceneFunnelJoin);
        Assert.Single(notices);
    }

    [Fact]
    public void TextSet_Trims_RecordCarriesIt()
    {
        LFunnel funnel = TInterface.TFunnelCreate();
        LFunnelRule rule = TInterface.TFunnelRuleAdd(funnel, LFunnelForm.LFunnelFormFilename);

        TInterface.TFunnelTextSet(funnel, rule, LFunnelKind.LFunnelKindContains, "  cam  ");
        LSceneFunnelRule record = TInterface.TFunnelRecordCreate(funnel, rule);

        Assert.Equal("cam", record.LSceneFunnelContains.LSceneFunnelText);
        Assert.Equal((int)LFunnelForm.LFunnelFormFilename, record.LSceneFunnelType);
        Assert.False(record.LSceneFunnelRemainder);
    }

    [Fact]
    public void RestoredTarget_StaysPending_UntilTabsResolve()
    {
        LFunnel funnel = TInterface.TFunnelCreate();
        TInterface.TFunnelRulesRestore(
            funnel,
            TInterface.TFunnelRecordCreate((int)LFunnelForm.LFunnelFormRegex, false, 1, "^a"),
            TInterface.TFunnelRecordCreate((int)LFunnelForm.LFunnelFormFilename, true, 7));
        LFunnelRule regex = funnel.LFunnelRules[0];
        LFunnelRule remainder = funnel.LFunnelRules[1];

        Assert.Equal(1, regex.LFunnelRulePending);
        Assert.Equal(Guid.Empty, regex.LFunnelRuleTarget);
        Assert.Equal("^a", regex.LFunnelRuleRegex);
        Assert.Equal(LFunnelForm.LFunnelFormRemainder, remainder.LFunnelRuleForm);
        Assert.True(TInterface.TFunnelRemainderCheck(funnel));

        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();
        TInterface.TFunnelTargetsResolve(funnel, first, second);

        Assert.Equal(second, regex.LFunnelRuleTarget);
        Assert.Equal(-1, regex.LFunnelRulePending);
        Assert.Equal(Guid.Empty, remainder.LFunnelRuleTarget);
        Assert.Equal(-1, remainder.LFunnelRulePending);
    }

    [Fact]
    public void RestoreTwice_ReplacesRules_EmptyRestoreClears()
    {
        LFunnel funnel = TInterface.TFunnelCreate();
        TInterface.TFunnelRuleAdd(funnel, LFunnelForm.LFunnelFormRegex);
        LSceneFunnelRule[] records =
        [
            TInterface.TFunnelRecordCreate((int)LFunnelForm.LFunnelFormFilename, false, 0),
            TInterface.TFunnelRecordCreate((int)LFunnelForm.LFunnelFormFilename, true, 1),
        ];

        TInterface.TFunnelRulesRestore(funnel, records);
        TInterface.TFunnelRulesRestore(funnel, records);

        Assert.Equal(2, funnel.LFunnelRules.Count);
        Assert.Same(funnel.LFunnelRules[1], funnel.LFunnelSelected);

        TInterface.TFunnelRulesRestore(funnel);

        Assert.Empty(funnel.LFunnelRules);
        Assert.Null(funnel.LFunnelSelected);
    }

    [Fact]
    public void TargetChoice_ClearsPending_NotifiesRuleOnly()
    {
        LFunnel funnel = TInterface.TFunnelCreate();
        TInterface.TFunnelRulesRestore(
            funnel, TInterface.TFunnelRecordCreate((int)LFunnelForm.LFunnelFormFilename, false, 0));
        LFunnelRule rule = funnel.LFunnelRules[0];
        int listNotices = 0;
        TInterface.TFunnelAttach(funnel, () => listNotices++);
        Guid target = Guid.NewGuid();

        TInterface.TFunnelTargetSet(funnel, rule, target);
        TInterface.TFunnelTargetSet(funnel, rule, target);

        Assert.Equal(target, rule.LFunnelRuleTarget);
        Assert.Equal(-1, rule.LFunnelRulePending);
        Assert.Equal(0, listNotices);
    }

    [Fact]
    public void RemoveSelected_SelectsNeighbour_MoveReorders()
    {
        LFunnel funnel = TInterface.TFunnelCreate();
        LFunnelRule first = TInterface.TFunnelRuleAdd(funnel, LFunnelForm.LFunnelFormFilename);
        LFunnelRule second = TInterface.TFunnelRuleAdd(funnel, LFunnelForm.LFunnelFormRegex);
        LFunnelRule third = TInterface.TFunnelRuleAdd(funnel, LFunnelForm.LFunnelFormRemainder);

        Assert.Same(third, funnel.LFunnelSelected);
        Assert.True(TInterface.TFunnelRuleMove(funnel, third, 0));
        Assert.Equal([third, first, second], funnel.LFunnelRules);
        Assert.False(TInterface.TFunnelRuleMove(funnel, third, 1));

        TInterface.TFunnelRuleSelect(funnel, first);
        TInterface.TFunnelRuleRemove(funnel, first);

        Assert.Equal([third, second], funnel.LFunnelRules);
        Assert.Same(second, funnel.LFunnelSelected);
    }
}

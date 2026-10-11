using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests.Windows;

public sealed class TFunnelRule
{
    [Fact]
    public void JoinToggle_FlipsOneCondition_NotifiesOnce()
    {
        LFunnel funnel = TInterfaceFunnel.TFunnelCreate();
        LFunnelRule rule = TInterfaceFunnel.TFunnelRuleAdd(funnel, LFunnelForm.LFunnelFormFilename);
        List<LFunnelRule> notices = [];
        TInterfaceFunnel.TFunnelRuleAttach(funnel, notices.Add);

        TInterfaceFunnel.TFunnelJoinSet(funnel, rule, LFunnelKind.LFunnelKindPrefix, false);
        TInterfaceFunnel.TFunnelJoinSet(funnel, rule, LFunnelKind.LFunnelKindPrefix, false);

        Assert.False(rule.LFunnelRulePrefix.LSceneFunnelJoin);
        Assert.True(rule.LFunnelRuleEnd.LSceneFunnelJoin);
        Assert.Single(notices);
    }

    [Fact]
    public void TextSet_Trims_RecordCarriesIt()
    {
        LFunnel funnel = TInterfaceFunnel.TFunnelCreate();
        LFunnelRule rule = TInterfaceFunnel.TFunnelRuleAdd(funnel, LFunnelForm.LFunnelFormFilename);

        TInterfaceFunnel.TFunnelTextSet(funnel, rule, LFunnelKind.LFunnelKindContains, "  cam  ");
        LSceneFunnelRule record = TInterfaceFunnel.TFunnelRecordCreate(funnel, rule);

        Assert.Equal("cam", record.LSceneFunnelContains.LSceneFunnelText);
        Assert.Equal((int)LFunnelForm.LFunnelFormFilename, record.LSceneFunnelType);
        Assert.False(record.LSceneFunnelRemainder);
    }

    [Fact]
    public void RestoredTarget_StaysPending_UntilTabsResolve()
    {
        LFunnel funnel = TInterfaceFunnel.TFunnelCreate();
        TInterfaceFunnel.TFunnelRulesRestore(
            funnel,
            TInterfaceFunnel.TFunnelRecordCreate((int)LFunnelForm.LFunnelFormRegex, false, 1, "^a"),
            TInterfaceFunnel.TFunnelRecordCreate((int)LFunnelForm.LFunnelFormFilename, true, 7));
        LFunnelRule regex = funnel.LFunnelRules[0];
        LFunnelRule remainder = funnel.LFunnelRules[1];

        Assert.Equal(1, regex.LFunnelRulePending);
        Assert.Equal(Guid.Empty, regex.LFunnelRuleTarget);
        Assert.Equal("^a", regex.LFunnelRuleRegex);
        Assert.Equal(LFunnelForm.LFunnelFormRemainder, remainder.LFunnelRuleForm);
        Assert.True(TInterfaceFunnel.TFunnelRemainderCheck(funnel));

        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();
        TInterfaceFunnel.TFunnelTargetsResolve(funnel, first, second);

        Assert.Equal(second, regex.LFunnelRuleTarget);
        Assert.Equal(-1, regex.LFunnelRulePending);
        Assert.Equal(Guid.Empty, remainder.LFunnelRuleTarget);
        Assert.Equal(-1, remainder.LFunnelRulePending);
    }

    [Fact]
    public void RestoreTwice_ReplacesRules_EmptyRestoreClears()
    {
        LFunnel funnel = TInterfaceFunnel.TFunnelCreate();
        TInterfaceFunnel.TFunnelRuleAdd(funnel, LFunnelForm.LFunnelFormRegex);
        LSceneFunnelRule[] records =
        [
            TInterfaceFunnel.TFunnelRecordCreate((int)LFunnelForm.LFunnelFormFilename, false, 0),
            TInterfaceFunnel.TFunnelRecordCreate((int)LFunnelForm.LFunnelFormFilename, true, 1),
        ];

        TInterfaceFunnel.TFunnelRulesRestore(funnel, records);
        TInterfaceFunnel.TFunnelRulesRestore(funnel, records);

        Assert.Equal(2, funnel.LFunnelRules.Count);
        Assert.Same(funnel.LFunnelRules[1], funnel.LFunnelSelected);

        TInterfaceFunnel.TFunnelRulesRestore(funnel);

        Assert.Empty(funnel.LFunnelRules);
        Assert.Null(funnel.LFunnelSelected);
    }

    [Fact]
    public void TargetChoice_ClearsPending_NotifiesRuleOnly()
    {
        LFunnel funnel = TInterfaceFunnel.TFunnelCreate();
        TInterfaceFunnel.TFunnelRulesRestore(
            funnel, TInterfaceFunnel.TFunnelRecordCreate((int)LFunnelForm.LFunnelFormFilename, false, 0));
        LFunnelRule rule = funnel.LFunnelRules[0];
        int listNotices = 0;
        TInterfaceFunnel.TFunnelAttach(funnel, () => listNotices++);
        Guid target = Guid.NewGuid();

        TInterfaceFunnel.TFunnelTargetSet(funnel, rule, target);
        TInterfaceFunnel.TFunnelTargetSet(funnel, rule, target);

        Assert.Equal(target, rule.LFunnelRuleTarget);
        Assert.Equal(-1, rule.LFunnelRulePending);
        Assert.Equal(0, listNotices);
    }

    [Fact]
    public void RemoveSelected_SelectsNeighbour_MoveReorders()
    {
        LFunnel funnel = TInterfaceFunnel.TFunnelCreate();
        LFunnelRule first = TInterfaceFunnel.TFunnelRuleAdd(funnel, LFunnelForm.LFunnelFormFilename);
        LFunnelRule second = TInterfaceFunnel.TFunnelRuleAdd(funnel, LFunnelForm.LFunnelFormRegex);
        LFunnelRule third = TInterfaceFunnel.TFunnelRuleAdd(funnel, LFunnelForm.LFunnelFormRemainder);

        Assert.Same(third, funnel.LFunnelSelected);
        Assert.True(TInterfaceFunnel.TFunnelRuleMove(funnel, third, 0));
        Assert.Equal([third, first, second], funnel.LFunnelRules);
        Assert.False(TInterfaceFunnel.TFunnelRuleMove(funnel, third, 1));

        TInterfaceFunnel.TFunnelRuleSelect(funnel, first);
        TInterfaceFunnel.TFunnelRuleRemove(funnel, first);

        Assert.Equal([third, second], funnel.LFunnelRules);
        Assert.Same(second, funnel.LFunnelSelected);
    }

    private static (LStrip, LStripTab, LStripTab) TFunnelStripBuild()
    {
        LStrip strip = TInterface.TStripCreate(key => key, (name, ordinal) => $"{name} {ordinal}");
        LStripTab self = TInterface.TStripTabCreate("Funnel");
        LStripTab target = TInterface.TStripTabCreate("Convert");
        TInterface.TStripAdd(strip, self);
        TInterface.TStripAdd(strip, target);
        TInterface.TStripWorkspaceAttach(
            target, TInterface.TPresetInitialCreate("Convert"), TInterfaceList.TDocketCreate());
        return (strip, self, target);
    }

    [Fact]
    public void StripAttach_TargetsExcludeSelf_OptionsStartWithNone_IndexFollowsStrip()
    {
        LFunnel funnel = TInterfaceFunnel.TFunnelCreate();
        (LStrip strip, LStripTab self, LStripTab target) = TFunnelStripBuild();

        Assert.Empty(TInterfaceFunnel.TFunnelTargetsRead(funnel));
        TInterfaceFunnel.TFunnelStripAttach(funnel, strip, self.LStripTabId);
        IReadOnlyList<LFunnelTarget> targets = TInterfaceFunnel.TFunnelTargetsRead(funnel);
        IReadOnlyList<LFunnelTarget> options = TInterfaceFunnel.TFunnelOptionsRead(funnel);

        Assert.Equal([target.LStripTabId], targets.Select(entry => entry.LFunnelTargetId));
        Assert.Equal("Convert", targets[0].LFunnelTargetKey);
        Assert.Equal(Guid.Empty, options[0].LFunnelTargetId);
        Assert.Equal(string.Empty, options[0].LFunnelTargetKey);
        Assert.Equal(2, options.Count);
        Assert.Equal(1, TInterfaceFunnel.TFunnelIndexRead(funnel, target.LStripTabId));
        Assert.Equal(-1, TInterfaceFunnel.TFunnelIndexRead(funnel, Guid.NewGuid()));
        Assert.Equal(-1, TInterfaceFunnel.TFunnelIndexRead(funnel, Guid.Empty));
    }

    [Fact]
    public void TargetSelect_ByOptionIndex_StripChangeClearsGoneTarget()
    {
        LFunnel funnel = TInterfaceFunnel.TFunnelCreate();
        (LStrip strip, LStripTab self, LStripTab target) = TFunnelStripBuild();
        TInterfaceFunnel.TFunnelStripAttach(funnel, strip, self.LStripTabId);
        LFunnelRule rule = TInterfaceFunnel.TFunnelRuleAdd(funnel, LFunnelForm.LFunnelFormFilename);
        List<LFunnelRule> notices = [];
        TInterfaceFunnel.TFunnelRuleAttach(funnel, notices.Add);

        TInterfaceFunnel.TFunnelTargetSelect(funnel, rule, 1);
        Assert.Equal(target.LStripTabId, rule.LFunnelRuleTarget);
        TInterfaceFunnel.TFunnelTargetSelect(funnel, rule, -1);
        TInterfaceFunnel.TFunnelTargetSelect(funnel, rule, 5);
        Assert.Equal(target.LStripTabId, rule.LFunnelRuleTarget);
        Assert.Single(notices);

        TInterface.TStripRemove(strip, target);

        Assert.Equal(Guid.Empty, rule.LFunnelRuleTarget);
        TInterfaceFunnel.TFunnelStripDetach(funnel);
    }

    [Fact]
    public void LayoutRoundTrip_TargetIndexAndPending_ThroughTab()
    {
        LDocket docket = TInterfaceList.TDocketCreate();
        LFunnelTab source = TInterfaceFunnel.TFunnelTabCreate(TInterfaceList.TListCreate(docket), docket);
        (LStrip strip, LStripTab self, LStripTab target) = TFunnelStripBuild();
        TInterfaceFunnel.TFunnelStripAttach(source.LFunnel, strip, self.LStripTabId);
        LFunnelRule rule = TInterfaceFunnel.TFunnelRuleAdd(source.LFunnel, LFunnelForm.LFunnelFormRegex);
        TInterfaceFunnel.TFunnelRegexSet(source.LFunnel, rule, "^a");
        TInterfaceFunnel.TFunnelTargetSelect(source.LFunnel, rule, 1);

        LSceneTabEntry layout = TInterfaceFunnel.TFunnelLayoutRead(source);
        LFunnelTab restored = TInterfaceFunnel.TFunnelTabCreate(TInterfaceList.TListCreate(docket), docket);
        TInterfaceFunnel.TFunnelLayoutApply(restored, layout);
        LFunnelRule pending = restored.LFunnel.LFunnelRules[0];

        Assert.Equal(1, layout.LSceneFunnelRules[0].LSceneFunnelTarget);
        Assert.Equal(1, pending.LFunnelRulePending);
        TInterfaceFunnel.TFunnelStripAttach(restored.LFunnel, strip, self.LStripTabId);
        Assert.Equal(1, pending.LFunnelRulePending);
        TInterfaceFunnel.TFunnelTargetsResolve(restored.LFunnel);
        Assert.Equal(target.LStripTabId, pending.LFunnelRuleTarget);
        Assert.Equal("^a", pending.LFunnelRuleRegex);
        TInterfaceFunnel.TFunnelClose(source);
        TInterfaceFunnel.TFunnelClose(restored);
    }

    [Fact]
    public void Slots_FollowOrderAndSelection_EmptyFlag()
    {
        LFunnel funnel = TInterfaceFunnel.TFunnelCreate();
        Assert.True(funnel.LFunnelEmpty);
        LFunnelRule first = TInterfaceFunnel.TFunnelRuleAdd(funnel, LFunnelForm.LFunnelFormFilename);
        LFunnelRule second = TInterfaceFunnel.TFunnelRuleAdd(funnel, LFunnelForm.LFunnelFormRegex);
        TInterfaceFunnel.TFunnelRuleMove(funnel, second, 0);

        IReadOnlyList<LFunnelSlot> slots = TInterfaceFunnel.TFunnelSlotsRead(funnel);

        Assert.False(funnel.LFunnelEmpty);
        Assert.Same(second, slots[0].LFunnelSlotRule);
        Assert.Equal(
            (0, 1, true), (slots[0].LFunnelSlotIndex, slots[0].LFunnelSlotOrder, slots[0].LFunnelSlotSelected));
        Assert.Same(first, slots[1].LFunnelSlotRule);
        Assert.Equal(
            (1, 2, false), (slots[1].LFunnelSlotIndex, slots[1].LFunnelSlotOrder, slots[1].LFunnelSlotSelected));
    }

    [Fact]
    public void CreateAndDelete_NotifyPerRule_SelectedRemoveUsesSelection()
    {
        LFunnel funnel = TInterfaceFunnel.TFunnelCreate();
        List<LFunnelRule> created = [];
        List<LFunnelRule> deleted = [];
        TInterfaceFunnel.TFunnelCreateAttach(funnel, created.Add);
        TInterfaceFunnel.TFunnelDeleteAttach(funnel, deleted.Add);

        LFunnelRule first = TInterfaceFunnel.TFunnelRuleAdd(funnel, LFunnelForm.LFunnelFormFilename);
        TInterfaceFunnel.TFunnelSelectedRemove(funnel);
        TInterfaceFunnel.TFunnelSelectedRemove(funnel);
        TInterfaceFunnel.TFunnelRulesRestore(
            funnel, TInterfaceFunnel.TFunnelRecordCreate((int)LFunnelForm.LFunnelFormFilename, true, -1));

        Assert.Equal(2, created.Count);
        Assert.Equal([first], deleted);
        Assert.Equal("Inspector.Funnel.Remainder", created[1].LFunnelRuleTitle);
        Assert.False(created[1].LFunnelRuleFields);
        Assert.True(first.LFunnelRuleFields);
    }

    [Fact]
    public void TextResolve_KeepsShownWhenTrimEqual_ReplacesOtherwise()
    {
        LFunnel funnel = TInterfaceFunnel.TFunnelCreate();
        LFunnelRule rule = TInterfaceFunnel.TFunnelRuleAdd(funnel, LFunnelForm.LFunnelFormRegex);
        TInterfaceFunnel.TFunnelRegexSet(funnel, rule, "abc ");
        TInterfaceFunnel.TFunnelTextSet(funnel, rule, LFunnelKind.LFunnelKindPrefix, "cam");

        Assert.Equal("abc ", TInterfaceFunnel.TFunnelRegexResolve(funnel, rule, "abc "));
        Assert.Equal("abc", TInterfaceFunnel.TFunnelRegexResolve(funnel, rule, "zzz"));
        Assert.Equal(" cam", TInterfaceFunnel.TFunnelTextResolve(funnel, rule, LFunnelKind.LFunnelKindPrefix, " cam"));
        Assert.Equal("cam", TInterfaceFunnel.TFunnelTextResolve(funnel, rule, LFunnelKind.LFunnelKindPrefix, ""));
    }

    [Fact]
    public void Toggles_FlipCaseAndCollapsed()
    {
        LFunnel funnel = TInterfaceFunnel.TFunnelCreate();
        LFunnelRule rule = TInterfaceFunnel.TFunnelRuleAdd(funnel, LFunnelForm.LFunnelFormFilename);

        TInterfaceFunnel.TFunnelCaseToggle(funnel, rule, LFunnelKind.LFunnelKindEnd);
        TInterfaceFunnel.TFunnelCollapsedToggle(funnel, rule);

        Assert.True(rule.LFunnelRuleEnd.LSceneFunnelCase);
        Assert.False(rule.LFunnelRuleContains.LSceneFunnelCase);
        Assert.True(rule.LFunnelRuleCollapsed);
        Assert.Equal(4, TInterfaceFunnel.TFunnelConditionsRead().Count);
        Assert.False(TInterfaceFunnel.TFunnelConditionsRead()[0].LFunnelConditionJoin);
    }

    [Fact]
    public void Drag_ThresholdThenMoveByCenters_ReleaseClears()
    {
        LFunnel funnel = TInterfaceFunnel.TFunnelCreate();
        LFunnelRule first = TInterfaceFunnel.TFunnelRuleAdd(funnel, LFunnelForm.LFunnelFormFilename);
        LFunnelRule second = TInterfaceFunnel.TFunnelRuleAdd(funnel, LFunnelForm.LFunnelFormRegex);
        LFunnelRule third = TInterfaceFunnel.TFunnelRuleAdd(funnel, LFunnelForm.LFunnelFormRemainder);

        TInterfaceFunnel.TFunnelPressHandle(funnel, first, 10, 10);
        Assert.True(TInterfaceFunnel.TFunnelMoveCheck(funnel, first, true));
        Assert.False(TInterfaceFunnel.TFunnelMoveCheck(funnel, second, true));
        Assert.False(TInterfaceFunnel.TFunnelMoveCheck(funnel, first, false));
        Assert.False(TInterfaceFunnel.TFunnelDragResolve(funnel, 11, 11, 4, 4));
        Assert.False(TInterfaceFunnel.TFunnelDragCheck(funnel));
        TInterfaceFunnel.TFunnelDragMove(funnel, 200, 20, 60, 100);
        Assert.Equal([first, second, third], funnel.LFunnelRules);

        Assert.True(TInterfaceFunnel.TFunnelDragResolve(funnel, 30, 30, 4, 4));
        Assert.False(TInterfaceFunnel.TFunnelDragResolve(funnel, 40, 40, 4, 4));
        TInterfaceFunnel.TFunnelDragMove(funnel, 200, 20, 60, 100);
        Assert.Equal([second, third, first], funnel.LFunnelRules);

        Assert.True(TInterfaceFunnel.TFunnelReleaseCheck(funnel, first));
        TInterfaceFunnel.TFunnelDragClear(funnel);
        Assert.False(TInterfaceFunnel.TFunnelReleaseCheck(funnel, first));
        Assert.Equal(0, TInterfaceFunnel.TFunnelIndexResolve(5, 20, 60, 100));
        Assert.Equal(2, TInterfaceFunnel.TFunnelIndexResolve(70, 20, 60, 100));
        Assert.Equal(30, TInterfaceFunnel.TFunnelCenterResolve(20, 20));
    }
}

using Cadroue.Application;
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

    private static (LStrip, LStripTab, LStripTab) TFunnelStripBuild()
    {
        LStrip strip = TInterface.TStripCreate(key => key, (name, ordinal) => $"{name} {ordinal}");
        LStripTab self = TInterface.TStripTabCreate("Funnel");
        LStripTab target = TInterface.TStripTabCreate("Convert");
        TInterface.TStripAdd(strip, self);
        TInterface.TStripAdd(strip, target);
        TInterface.TStripWorkspaceAttach(
            target, TInterface.TPresetInitialCreate("Convert"), TInterface.TDocketCreate());
        return (strip, self, target);
    }

    [Fact]
    public void StripAttach_TargetsExcludeSelf_OptionsStartWithNone_IndexFollowsStrip()
    {
        LFunnel funnel = TInterface.TFunnelCreate();
        (LStrip strip, LStripTab self, LStripTab target) = TFunnelStripBuild();

        Assert.Empty(TInterface.TFunnelTargetsRead(funnel));
        TInterface.TFunnelStripAttach(funnel, strip, self.LStripTabId);
        IReadOnlyList<LFunnelTarget> targets = TInterface.TFunnelTargetsRead(funnel);
        IReadOnlyList<LFunnelTarget> options = TInterface.TFunnelOptionsRead(funnel);

        Assert.Equal([target.LStripTabId], targets.Select(entry => entry.LFunnelTargetId));
        Assert.Equal("Convert", targets[0].LFunnelTargetKey);
        Assert.Equal(Guid.Empty, options[0].LFunnelTargetId);
        Assert.Equal(string.Empty, options[0].LFunnelTargetKey);
        Assert.Equal(2, options.Count);
        Assert.Equal(1, TInterface.TFunnelIndexRead(funnel, target.LStripTabId));
        Assert.Equal(-1, TInterface.TFunnelIndexRead(funnel, Guid.NewGuid()));
        Assert.Equal(-1, TInterface.TFunnelIndexRead(funnel, Guid.Empty));
    }

    [Fact]
    public void TargetSelect_ByOptionIndex_StripChangeClearsGoneTarget()
    {
        LFunnel funnel = TInterface.TFunnelCreate();
        (LStrip strip, LStripTab self, LStripTab target) = TFunnelStripBuild();
        TInterface.TFunnelStripAttach(funnel, strip, self.LStripTabId);
        LFunnelRule rule = TInterface.TFunnelRuleAdd(funnel, LFunnelForm.LFunnelFormFilename);
        List<LFunnelRule> notices = [];
        TInterface.TFunnelRuleAttach(funnel, notices.Add);

        TInterface.TFunnelTargetSelect(funnel, rule, 1);
        Assert.Equal(target.LStripTabId, rule.LFunnelRuleTarget);
        TInterface.TFunnelTargetSelect(funnel, rule, -1);
        TInterface.TFunnelTargetSelect(funnel, rule, 5);
        Assert.Equal(target.LStripTabId, rule.LFunnelRuleTarget);
        Assert.Single(notices);

        TInterface.TStripRemove(strip, target);

        Assert.Equal(Guid.Empty, rule.LFunnelRuleTarget);
        TInterface.TFunnelStripDetach(funnel);
    }

    [Fact]
    public void LayoutRoundTrip_TargetIndexAndPending_ThroughTab()
    {
        LDocket docket = TInterface.TDocketCreate();
        LFunnelTab source = TInterface.TFunnelTabCreate(TInterface.TListCreate(docket), docket);
        (LStrip strip, LStripTab self, LStripTab target) = TFunnelStripBuild();
        TInterface.TFunnelStripAttach(source.LFunnel, strip, self.LStripTabId);
        LFunnelRule rule = TInterface.TFunnelRuleAdd(source.LFunnel, LFunnelForm.LFunnelFormRegex);
        TInterface.TFunnelRegexSet(source.LFunnel, rule, "^a");
        TInterface.TFunnelTargetSelect(source.LFunnel, rule, 1);

        LSceneTabRecord layout = TInterface.TFunnelLayoutRead(source);
        LFunnelTab restored = TInterface.TFunnelTabCreate(TInterface.TListCreate(docket), docket);
        TInterface.TFunnelLayoutApply(restored, layout);
        LFunnelRule pending = restored.LFunnel.LFunnelRules[0];

        Assert.Equal(1, layout.LSceneFunnelRules[0].LSceneFunnelTarget);
        Assert.Equal(1, pending.LFunnelRulePending);
        TInterface.TFunnelStripAttach(restored.LFunnel, strip, self.LStripTabId);
        Assert.Equal(1, pending.LFunnelRulePending);
        TInterface.TFunnelTargetsResolve(restored.LFunnel);
        Assert.Equal(target.LStripTabId, pending.LFunnelRuleTarget);
        Assert.Equal("^a", pending.LFunnelRuleRegex);
        TInterface.TFunnelClose(source);
        TInterface.TFunnelClose(restored);
    }

    [Fact]
    public void Slots_FollowOrderAndSelection_EmptyFlag()
    {
        LFunnel funnel = TInterface.TFunnelCreate();
        Assert.True(funnel.LFunnelEmpty);
        LFunnelRule first = TInterface.TFunnelRuleAdd(funnel, LFunnelForm.LFunnelFormFilename);
        LFunnelRule second = TInterface.TFunnelRuleAdd(funnel, LFunnelForm.LFunnelFormRegex);
        TInterface.TFunnelRuleMove(funnel, second, 0);

        IReadOnlyList<LFunnelSlot> slots = TInterface.TFunnelSlotsRead(funnel);

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
        LFunnel funnel = TInterface.TFunnelCreate();
        List<LFunnelRule> created = [];
        List<LFunnelRule> deleted = [];
        TInterface.TFunnelCreateAttach(funnel, created.Add);
        TInterface.TFunnelDeleteAttach(funnel, deleted.Add);

        LFunnelRule first = TInterface.TFunnelRuleAdd(funnel, LFunnelForm.LFunnelFormFilename);
        TInterface.TFunnelSelectedRemove(funnel);
        TInterface.TFunnelSelectedRemove(funnel);
        TInterface.TFunnelRulesRestore(
            funnel, TInterface.TFunnelRecordCreate((int)LFunnelForm.LFunnelFormFilename, true, -1));

        Assert.Equal(2, created.Count);
        Assert.Equal([first], deleted);
        Assert.Equal("Inspector.Funnel.Remainder", created[1].LFunnelRuleTitle);
        Assert.False(created[1].LFunnelRuleFields);
        Assert.True(first.LFunnelRuleFields);
    }

    [Fact]
    public void TextResolve_KeepsShownWhenTrimEqual_ReplacesOtherwise()
    {
        LFunnel funnel = TInterface.TFunnelCreate();
        LFunnelRule rule = TInterface.TFunnelRuleAdd(funnel, LFunnelForm.LFunnelFormRegex);
        TInterface.TFunnelRegexSet(funnel, rule, "abc ");
        TInterface.TFunnelTextSet(funnel, rule, LFunnelKind.LFunnelKindPrefix, "cam");

        Assert.Equal("abc ", TInterface.TFunnelRegexResolve(funnel, rule, "abc "));
        Assert.Equal("abc", TInterface.TFunnelRegexResolve(funnel, rule, "zzz"));
        Assert.Equal(" cam", TInterface.TFunnelTextResolve(funnel, rule, LFunnelKind.LFunnelKindPrefix, " cam"));
        Assert.Equal("cam", TInterface.TFunnelTextResolve(funnel, rule, LFunnelKind.LFunnelKindPrefix, ""));
    }

    [Fact]
    public void Toggles_FlipCaseAndCollapsed()
    {
        LFunnel funnel = TInterface.TFunnelCreate();
        LFunnelRule rule = TInterface.TFunnelRuleAdd(funnel, LFunnelForm.LFunnelFormFilename);

        TInterface.TFunnelCaseToggle(funnel, rule, LFunnelKind.LFunnelKindEnd);
        TInterface.TFunnelCollapsedToggle(funnel, rule);

        Assert.True(rule.LFunnelRuleEnd.LSceneFunnelCase);
        Assert.False(rule.LFunnelRuleContains.LSceneFunnelCase);
        Assert.True(rule.LFunnelRuleCollapsed);
        Assert.Equal(4, TInterface.TFunnelConditionsRead().Count);
        Assert.False(TInterface.TFunnelConditionsRead()[0].LFunnelConditionJoin);
    }

    [Fact]
    public void Drag_ThresholdThenMoveByCenters_ReleaseClears()
    {
        LFunnel funnel = TInterface.TFunnelCreate();
        LFunnelRule first = TInterface.TFunnelRuleAdd(funnel, LFunnelForm.LFunnelFormFilename);
        LFunnelRule second = TInterface.TFunnelRuleAdd(funnel, LFunnelForm.LFunnelFormRegex);
        LFunnelRule third = TInterface.TFunnelRuleAdd(funnel, LFunnelForm.LFunnelFormRemainder);

        TInterface.TFunnelPressHandle(funnel, first, 10, 10);
        Assert.True(TInterface.TFunnelMoveCheck(funnel, first, true));
        Assert.False(TInterface.TFunnelMoveCheck(funnel, second, true));
        Assert.False(TInterface.TFunnelMoveCheck(funnel, first, false));
        Assert.False(TInterface.TFunnelDragResolve(funnel, 11, 11, 4, 4));
        Assert.False(TInterface.TFunnelDragCheck(funnel));
        TInterface.TFunnelDragMove(funnel, 200, 20, 60, 100);
        Assert.Equal([first, second, third], funnel.LFunnelRules);

        Assert.True(TInterface.TFunnelDragResolve(funnel, 30, 30, 4, 4));
        Assert.False(TInterface.TFunnelDragResolve(funnel, 40, 40, 4, 4));
        TInterface.TFunnelDragMove(funnel, 200, 20, 60, 100);
        Assert.Equal([second, third, first], funnel.LFunnelRules);

        Assert.True(TInterface.TFunnelReleaseCheck(funnel, first));
        TInterface.TFunnelDragClear(funnel);
        Assert.False(TInterface.TFunnelReleaseCheck(funnel, first));
        Assert.Equal(0, TInterface.TFunnelIndexResolve(5, 20, 60, 100));
        Assert.Equal(2, TInterface.TFunnelIndexResolve(70, 20, 60, 100));
        Assert.Equal(30, TInterface.TFunnelCenterResolve(20, 20));
    }
}

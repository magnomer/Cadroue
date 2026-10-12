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
        QFunnel funnel = TInterfaceFunnel.TFunnelCreate();
        QFunnelRule rule = TInterfaceFunnel.TFunnelRuleAdd(funnel, QFunnelForm.QFunnelFormFilename);
        List<QFunnelRule> notices = [];
        TInterfaceFunnel.TFunnelRuleAttach(funnel, notices.Add);

        TInterfaceFunnel.TFunnelJoinSet(funnel, rule, QFunnelKind.QFunnelKindPrefix, false);
        TInterfaceFunnel.TFunnelJoinSet(funnel, rule, QFunnelKind.QFunnelKindPrefix, false);

        Assert.False(rule.QFunnelRulePrefix.LSceneFunnelJoin);
        Assert.True(rule.QFunnelRuleEnd.LSceneFunnelJoin);
        Assert.Single(notices);
    }

    [Fact]
    public void TextSet_Trims_RecordCarriesIt()
    {
        QFunnel funnel = TInterfaceFunnel.TFunnelCreate();
        QFunnelRule rule = TInterfaceFunnel.TFunnelRuleAdd(funnel, QFunnelForm.QFunnelFormFilename);

        TInterfaceFunnel.TFunnelTextSet(funnel, rule, QFunnelKind.QFunnelKindContains, "  cam  ");
        LSceneFunnelRule record = TInterfaceFunnel.TFunnelRecordCreate(funnel, rule);

        Assert.Equal("cam", record.LSceneFunnelContains.LSceneFunnelText);
        Assert.Equal((int)QFunnelForm.QFunnelFormFilename, record.LSceneFunnelType);
        Assert.False(record.LSceneFunnelRemainder);
    }

    [Fact]
    public void RestoredTarget_StaysPending_UntilTabsResolve()
    {
        QFunnel funnel = TInterfaceFunnel.TFunnelCreate();
        TInterfaceFunnel.TFunnelRulesRestore(
            funnel,
            TInterfaceFunnel.TFunnelRecordCreate((int)QFunnelForm.QFunnelFormRegex, false, 1, "^a"),
            TInterfaceFunnel.TFunnelRecordCreate((int)QFunnelForm.QFunnelFormFilename, true, 7));
        QFunnelRule regex = funnel.QFunnelRules[0];
        QFunnelRule remainder = funnel.QFunnelRules[1];

        Assert.Equal(1, regex.QFunnelRulePending);
        Assert.Equal(Guid.Empty, regex.QFunnelRuleTarget);
        Assert.Equal("^a", regex.QFunnelRuleRegex);
        Assert.Equal(QFunnelForm.QFunnelFormRemainder, remainder.QFunnelRuleForm);
        Assert.True(TInterfaceFunnel.TFunnelRemainderCheck(funnel));

        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();
        TInterfaceFunnel.TFunnelTargetsResolve(funnel, first, second);

        Assert.Equal(second, regex.QFunnelRuleTarget);
        Assert.Equal(-1, regex.QFunnelRulePending);
        Assert.Equal(Guid.Empty, remainder.QFunnelRuleTarget);
        Assert.Equal(-1, remainder.QFunnelRulePending);
    }

    [Fact]
    public void RestoreTwice_ReplacesRules_EmptyRestoreClears()
    {
        QFunnel funnel = TInterfaceFunnel.TFunnelCreate();
        TInterfaceFunnel.TFunnelRuleAdd(funnel, QFunnelForm.QFunnelFormRegex);
        LSceneFunnelRule[] records =
        [
            TInterfaceFunnel.TFunnelRecordCreate((int)QFunnelForm.QFunnelFormFilename, false, 0),
            TInterfaceFunnel.TFunnelRecordCreate((int)QFunnelForm.QFunnelFormFilename, true, 1),
        ];

        TInterfaceFunnel.TFunnelRulesRestore(funnel, records);
        TInterfaceFunnel.TFunnelRulesRestore(funnel, records);

        Assert.Equal(2, funnel.QFunnelRules.Count);
        Assert.Same(funnel.QFunnelRules[1], funnel.QFunnelSelected);

        TInterfaceFunnel.TFunnelRulesRestore(funnel);

        Assert.Empty(funnel.QFunnelRules);
        Assert.Null(funnel.QFunnelSelected);
    }

    [Fact]
    public void TargetChoice_ClearsPending_NotifiesRuleOnly()
    {
        QFunnel funnel = TInterfaceFunnel.TFunnelCreate();
        TInterfaceFunnel.TFunnelRulesRestore(
            funnel, TInterfaceFunnel.TFunnelRecordCreate((int)QFunnelForm.QFunnelFormFilename, false, 0));
        QFunnelRule rule = funnel.QFunnelRules[0];
        int listNotices = 0;
        TInterfaceFunnel.TFunnelAttach(funnel, () => listNotices++);
        Guid target = Guid.NewGuid();

        TInterfaceFunnel.TFunnelTargetSet(funnel, rule, target);
        TInterfaceFunnel.TFunnelTargetSet(funnel, rule, target);

        Assert.Equal(target, rule.QFunnelRuleTarget);
        Assert.Equal(-1, rule.QFunnelRulePending);
        Assert.Equal(0, listNotices);
    }

    [Fact]
    public void RemoveSelected_SelectsNeighbour_MoveReorders()
    {
        QFunnel funnel = TInterfaceFunnel.TFunnelCreate();
        QFunnelRule first = TInterfaceFunnel.TFunnelRuleAdd(funnel, QFunnelForm.QFunnelFormFilename);
        QFunnelRule second = TInterfaceFunnel.TFunnelRuleAdd(funnel, QFunnelForm.QFunnelFormRegex);
        QFunnelRule third = TInterfaceFunnel.TFunnelRuleAdd(funnel, QFunnelForm.QFunnelFormRemainder);

        Assert.Same(third, funnel.QFunnelSelected);
        Assert.True(TInterfaceFunnel.TFunnelRuleMove(funnel, third, 0));
        Assert.Equal([third, first, second], funnel.QFunnelRules);
        Assert.False(TInterfaceFunnel.TFunnelRuleMove(funnel, third, 1));

        TInterfaceFunnel.TFunnelRuleSelect(funnel, first);
        TInterfaceFunnel.TFunnelRuleRemove(funnel, first);

        Assert.Equal([third, second], funnel.QFunnelRules);
        Assert.Same(second, funnel.QFunnelSelected);
    }

    private static (QStrip, QStripTab, QStripTab) TFunnelStripBuild()
    {
        QStrip strip = TInterface.TStripCreate(key => key, (name, ordinal) => $"{name} {ordinal}");
        QStripTab self = TInterface.TStripTabCreate("Funnel");
        QStripTab target = TInterface.TStripTabCreate("Convert");
        TInterface.TStripAdd(strip, self);
        TInterface.TStripAdd(strip, target);
        TInterface.TStripWorkspaceAttach(target, TInterface.TPresetInitialCreate("Convert"));
        return (strip, self, target);
    }

    [Fact]
    public void StripAttach_TargetsExcludeSelf_OptionsStartWithNone_IndexFollowsStrip()
    {
        QFunnel funnel = TInterfaceFunnel.TFunnelCreate();
        (QStrip strip, QStripTab self, QStripTab target) = TFunnelStripBuild();

        Assert.Empty(TInterfaceFunnel.TFunnelTargetsRead(funnel));
        TInterfaceFunnel.TFunnelStripAttach(funnel, strip, self.QStripTabId);
        IReadOnlyList<QFunnelTarget> targets = TInterfaceFunnel.TFunnelTargetsRead(funnel);
        IReadOnlyList<QFunnelTarget> options = TInterfaceFunnel.TFunnelOptionsRead(funnel);

        Assert.Equal([target.QStripTabId], targets.Select(entry => entry.QFunnelTargetId));
        Assert.Equal("Convert", targets[0].QFunnelTargetKey);
        Assert.Equal(Guid.Empty, options[0].QFunnelTargetId);
        Assert.Equal(string.Empty, options[0].QFunnelTargetKey);
        Assert.Equal(2, options.Count);
        Assert.Equal(1, TInterfaceFunnel.TFunnelIndexRead(funnel, target.QStripTabId));
        Assert.Equal(-1, TInterfaceFunnel.TFunnelIndexRead(funnel, Guid.NewGuid()));
        Assert.Equal(-1, TInterfaceFunnel.TFunnelIndexRead(funnel, Guid.Empty));
    }

    [Fact]
    public void TargetSelect_ByOptionIndex_StripChangeClearsGoneTarget()
    {
        QFunnel funnel = TInterfaceFunnel.TFunnelCreate();
        (QStrip strip, QStripTab self, QStripTab target) = TFunnelStripBuild();
        TInterfaceFunnel.TFunnelStripAttach(funnel, strip, self.QStripTabId);
        QFunnelRule rule = TInterfaceFunnel.TFunnelRuleAdd(funnel, QFunnelForm.QFunnelFormFilename);
        List<QFunnelRule> notices = [];
        TInterfaceFunnel.TFunnelRuleAttach(funnel, notices.Add);

        TInterfaceFunnel.TFunnelTargetSelect(funnel, rule, 1);
        Assert.Equal(target.QStripTabId, rule.QFunnelRuleTarget);
        TInterfaceFunnel.TFunnelTargetSelect(funnel, rule, -1);
        TInterfaceFunnel.TFunnelTargetSelect(funnel, rule, 5);
        Assert.Equal(target.QStripTabId, rule.QFunnelRuleTarget);
        Assert.Single(notices);

        TInterface.TStripRemove(strip, target);

        Assert.Equal(Guid.Empty, rule.QFunnelRuleTarget);
        TInterfaceFunnel.TFunnelStripDetach(funnel);
    }

    [Fact]
    public void LayoutRoundTrip_TargetIndexAndPending_ThroughTab()
    {
        QList list = TInterfaceList.TListCreate();
        QFunnelTab source = TInterfaceFunnel.TFunnelTabCreate(TInterfaceViewer.TViewerCreate(), list);
        (QStrip strip, QStripTab self, QStripTab target) = TFunnelStripBuild();
        TInterfaceFunnel.TFunnelStripAttach(source.QFunnel, strip, self.QStripTabId);
        QFunnelRule rule = TInterfaceFunnel.TFunnelRuleAdd(source.QFunnel, QFunnelForm.QFunnelFormRegex);
        TInterfaceFunnel.TFunnelRegexSet(source.QFunnel, rule, "^a");
        TInterfaceFunnel.TFunnelTargetSelect(source.QFunnel, rule, 1);

        LSceneTabEntry layout = TInterfaceFunnel.TFunnelLayoutRead(source);
        QList restoredList = TInterfaceList.TListCreate();
        QFunnelTab restored = TInterfaceFunnel.TFunnelTabCreate(TInterfaceViewer.TViewerCreate(), restoredList);
        TInterfaceFunnel.TFunnelLayoutApply(restored, layout);
        QFunnelRule pending = restored.QFunnel.QFunnelRules[0];

        Assert.Equal(1, layout.LSceneFunnelRules[0].LSceneFunnelTarget);
        Assert.Equal(1, pending.QFunnelRulePending);
        TInterfaceFunnel.TFunnelStripAttach(restored.QFunnel, strip, self.QStripTabId);
        Assert.Equal(1, pending.QFunnelRulePending);
        TInterfaceFunnel.TFunnelTargetsResolve(restored.QFunnel);
        Assert.Equal(target.QStripTabId, pending.QFunnelRuleTarget);
        Assert.Equal("^a", pending.QFunnelRuleRegex);
        TInterfaceFunnel.TFunnelClose(source);
        TInterfaceFunnel.TFunnelClose(restored);
    }

    [Fact]
    public void Slots_FollowOrderAndSelection_EmptyFlag()
    {
        QFunnel funnel = TInterfaceFunnel.TFunnelCreate();
        Assert.True(funnel.QFunnelEmpty);
        QFunnelRule first = TInterfaceFunnel.TFunnelRuleAdd(funnel, QFunnelForm.QFunnelFormFilename);
        QFunnelRule second = TInterfaceFunnel.TFunnelRuleAdd(funnel, QFunnelForm.QFunnelFormRegex);
        TInterfaceFunnel.TFunnelRuleMove(funnel, second, 0);

        IReadOnlyList<QFunnelSlot> slots = TInterfaceFunnel.TFunnelSlotsRead(funnel);

        Assert.False(funnel.QFunnelEmpty);
        Assert.Same(second, slots[0].QFunnelSlotRule);
        Assert.Equal(
            (0, 1, true), (slots[0].QFunnelSlotIndex, slots[0].QFunnelSlotOrder, slots[0].QFunnelSlotSelected));
        Assert.Same(first, slots[1].QFunnelSlotRule);
        Assert.Equal(
            (1, 2, false), (slots[1].QFunnelSlotIndex, slots[1].QFunnelSlotOrder, slots[1].QFunnelSlotSelected));
    }

    [Fact]
    public void CreateAndDelete_NotifyPerRule_SelectedRemoveUsesSelection()
    {
        QFunnel funnel = TInterfaceFunnel.TFunnelCreate();
        List<QFunnelRule> created = [];
        List<QFunnelRule> deleted = [];
        TInterfaceFunnel.TFunnelCreateAttach(funnel, created.Add);
        TInterfaceFunnel.TFunnelDeleteAttach(funnel, deleted.Add);

        QFunnelRule first = TInterfaceFunnel.TFunnelRuleAdd(funnel, QFunnelForm.QFunnelFormFilename);
        TInterfaceFunnel.TFunnelSelectedRemove(funnel);
        TInterfaceFunnel.TFunnelSelectedRemove(funnel);
        TInterfaceFunnel.TFunnelRulesRestore(
            funnel, TInterfaceFunnel.TFunnelRecordCreate((int)QFunnelForm.QFunnelFormFilename, true, -1));

        Assert.Equal(2, created.Count);
        Assert.Equal([first], deleted);
        Assert.Equal("Inspector.Funnel.Remainder", created[1].QFunnelRuleTitle);
        Assert.False(created[1].QFunnelRuleFields);
        Assert.True(first.QFunnelRuleFields);
    }

    [Fact]
    public void TextResolve_KeepsShownWhenTrimEqual_ReplacesOtherwise()
    {
        QFunnel funnel = TInterfaceFunnel.TFunnelCreate();
        QFunnelRule rule = TInterfaceFunnel.TFunnelRuleAdd(funnel, QFunnelForm.QFunnelFormRegex);
        TInterfaceFunnel.TFunnelRegexSet(funnel, rule, "abc ");
        TInterfaceFunnel.TFunnelTextSet(funnel, rule, QFunnelKind.QFunnelKindPrefix, "cam");

        Assert.Equal("abc ", TInterfaceFunnel.TFunnelRegexResolve(funnel, rule, "abc "));
        Assert.Equal("abc", TInterfaceFunnel.TFunnelRegexResolve(funnel, rule, "zzz"));
        Assert.Equal(" cam", TInterfaceFunnel.TFunnelTextResolve(funnel, rule, QFunnelKind.QFunnelKindPrefix, " cam"));
        Assert.Equal("cam", TInterfaceFunnel.TFunnelTextResolve(funnel, rule, QFunnelKind.QFunnelKindPrefix, ""));
    }

    [Fact]
    public void Toggles_FlipCaseAndCollapsed()
    {
        QFunnel funnel = TInterfaceFunnel.TFunnelCreate();
        QFunnelRule rule = TInterfaceFunnel.TFunnelRuleAdd(funnel, QFunnelForm.QFunnelFormFilename);

        TInterfaceFunnel.TFunnelCaseToggle(funnel, rule, QFunnelKind.QFunnelKindEnd);
        TInterfaceFunnel.TFunnelCollapsedToggle(funnel, rule);

        Assert.True(rule.QFunnelRuleEnd.LSceneFunnelCase);
        Assert.False(rule.QFunnelRuleContains.LSceneFunnelCase);
        Assert.True(rule.QFunnelRuleCollapsed);
        Assert.Equal(4, TInterfaceFunnel.TFunnelConditionsRead().Count);
        Assert.False(TInterfaceFunnel.TFunnelConditionsRead()[0].QFunnelConditionJoin);
    }

    [Fact]
    public void Drag_ThresholdThenMoveByCenters_ReleaseClears()
    {
        QFunnel funnel = TInterfaceFunnel.TFunnelCreate();
        QFunnelRule first = TInterfaceFunnel.TFunnelRuleAdd(funnel, QFunnelForm.QFunnelFormFilename);
        QFunnelRule second = TInterfaceFunnel.TFunnelRuleAdd(funnel, QFunnelForm.QFunnelFormRegex);
        QFunnelRule third = TInterfaceFunnel.TFunnelRuleAdd(funnel, QFunnelForm.QFunnelFormRemainder);

        TInterfaceFunnel.TFunnelPressHandle(funnel, first, 10, 10);
        Assert.True(TInterfaceFunnel.TFunnelMoveCheck(funnel, first, true));
        Assert.False(TInterfaceFunnel.TFunnelMoveCheck(funnel, second, true));
        Assert.False(TInterfaceFunnel.TFunnelMoveCheck(funnel, first, false));
        Assert.False(TInterfaceFunnel.TFunnelDragResolve(funnel, 11, 11, 4, 4));
        Assert.False(TInterfaceFunnel.TFunnelDragCheck(funnel));
        TInterfaceFunnel.TFunnelDragMove(funnel, 200, 20, 60, 100);
        Assert.Equal([first, second, third], funnel.QFunnelRules);

        Assert.True(TInterfaceFunnel.TFunnelDragResolve(funnel, 30, 30, 4, 4));
        Assert.False(TInterfaceFunnel.TFunnelDragResolve(funnel, 40, 40, 4, 4));
        TInterfaceFunnel.TFunnelDragMove(funnel, 200, 20, 60, 100);
        Assert.Equal([second, third, first], funnel.QFunnelRules);

        Assert.True(TInterfaceFunnel.TFunnelReleaseCheck(funnel, first));
        TInterfaceFunnel.TFunnelDragClear(funnel);
        Assert.False(TInterfaceFunnel.TFunnelReleaseCheck(funnel, first));
        Assert.Equal(0, TInterfaceFunnel.TFunnelIndexResolve(5, 20, 60, 100));
        Assert.Equal(2, TInterfaceFunnel.TFunnelIndexResolve(70, 20, 60, 100));
        Assert.Equal(30, TInterfaceFunnel.TFunnelCenterResolve(20, 20));
    }
}

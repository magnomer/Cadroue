using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;

namespace Cadroue.Tests.Windows;

internal static class TInterfaceFunnel
{
    internal static LFunnel TFunnelCreate() => new();
    internal static void TFunnelAttach(LFunnel funnel, Action handler) => funnel.LFunnelChanged += handler;
    internal static void TFunnelRuleAttach(LFunnel funnel, Action<LFunnelRule> handler) =>
        funnel.LFunnelRuleChanged += handler;
    internal static LFunnelRule TFunnelRuleAdd(LFunnel funnel, LFunnelForm form) => funnel.LFunnelRuleAdd(form);
    internal static void TFunnelRuleRemove(LFunnel funnel, LFunnelRule rule) => funnel.LFunnelRuleRemove(rule);
    internal static bool TFunnelRuleMove(LFunnel funnel, LFunnelRule rule, int target) =>
        funnel.LFunnelRuleMove(rule, target);
    internal static void TFunnelRuleSelect(LFunnel funnel, LFunnelRule rule) => funnel.LFunnelRuleSelect(rule);
    internal static void TFunnelJoinSet(LFunnel funnel, LFunnelRule rule, LFunnelKind kind, bool and) =>
        funnel.LFunnelJoinSet(rule, kind, and);
    internal static void TFunnelTextSet(LFunnel funnel, LFunnelRule rule, LFunnelKind kind, string text) =>
        funnel.LFunnelTextSet(rule, kind, text);
    internal static void TFunnelTargetSet(LFunnel funnel, LFunnelRule rule, Guid target) =>
        funnel.LFunnelTargetSet(rule, target);
    internal static void TFunnelTargetsResolve(LFunnel funnel, params Guid[] tabs) =>
        funnel.LFunnelTargetsResolve(tabs);
    internal static void TFunnelRulesRestore(LFunnel funnel, params LSceneFunnelRule[] records) =>
        funnel.LFunnelRulesRestore(records);
    internal static LSceneFunnelRule TFunnelRecordCreate(LFunnel funnel, LFunnelRule rule) =>
        funnel.LFunnelRecordCreate(rule);
    internal static LSceneFunnelRule TFunnelRecordCreate(int type, bool remainder, int target, string regex = "") =>
        new()
        {
            LSceneFunnelType = type,
            LSceneFunnelRemainder = remainder,
            LSceneFunnelTarget = target,
            LSceneFunnelRegex = regex,
        };
    internal static bool TFunnelRemainderCheck(LFunnel funnel) => funnel.LFunnelRemainderCheck();

    internal static LFunnelTab TFunnelTabCreate(LList list, LDocket docket) => new(list, docket);
    internal static void TFunnelClose(LFunnelTab tab) => tab.LFunnelClose();
    internal static void TFunnelLayoutApply(LFunnelTab tab, LSceneTabEntry? layout) => tab.LFunnelLayoutApply(layout);
    internal static LSceneTabEntry TFunnelLayoutRead(LFunnelTab tab) => tab.LFunnelLayoutRead(new LSceneTabEntry());
    internal static IReadOnlyList<string> TFunnelEligibleRead(LFunnelTab tab) => tab.LFunnelEligibleRead();
    internal static void TFunnelRun(LFunnelTab tab) => tab.LFunnelRun();
    internal static void TFunnelAllRun(LFunnelTab tab) => tab.LFunnelAllRun();
    internal static void TFunnelItemsRun(LFunnelTab tab, params string[] paths) => tab.LFunnelItemsRun(paths);
    internal static void TFunnelStripAttach(LFunnel funnel, LStrip strip, Guid self) =>
        funnel.LFunnelStripAttach(strip, self);
    internal static void TFunnelStripDetach(LFunnel funnel) => funnel.LFunnelStripDetach();
    internal static void TFunnelTargetsResolve(LFunnel funnel) => funnel.LFunnelTargetsResolve();
    internal static IReadOnlyList<LFunnelTarget> TFunnelTargetsRead(LFunnel funnel) => funnel.LFunnelTargetsRead();
    internal static IReadOnlyList<LFunnelTarget> TFunnelOptionsRead(LFunnel funnel) => funnel.LFunnelOptionsRead();
    internal static IReadOnlyList<LFunnelSlot> TFunnelSlotsRead(LFunnel funnel) => funnel.LFunnelSlotsRead();
    internal static int TFunnelIndexRead(LFunnel funnel, Guid target) => funnel.LFunnelIndexRead(target);
    internal static void TFunnelSelectedRemove(LFunnel funnel) => funnel.LFunnelSelectedRemove();
    internal static void TFunnelTargetSelect(LFunnel funnel, LFunnelRule rule, int index) =>
        funnel.LFunnelTargetSelect(rule, index);
    internal static string TFunnelTextResolve(LFunnel funnel, LFunnelRule rule, LFunnelKind kind, string shown) =>
        funnel.LFunnelTextResolve(rule, kind, shown);
    internal static string TFunnelRegexResolve(LFunnel funnel, LFunnelRule rule, string shown) =>
        funnel.LFunnelRegexResolve(rule, shown);
    internal static void TFunnelRegexSet(LFunnel funnel, LFunnelRule rule, string regex) =>
        funnel.LFunnelRegexSet(rule, regex);
    internal static void TFunnelCaseToggle(LFunnel funnel, LFunnelRule rule, LFunnelKind kind) =>
        funnel.LFunnelCaseToggle(rule, kind);
    internal static void TFunnelCollapsedToggle(LFunnel funnel, LFunnelRule rule) =>
        funnel.LFunnelCollapsedToggle(rule);
    internal static void TFunnelCreateAttach(LFunnel funnel, Action<LFunnelRule> handler) =>
        funnel.LFunnelRuleCreated += handler;
    internal static void TFunnelDeleteAttach(LFunnel funnel, Action<LFunnelRule> handler) =>
        funnel.LFunnelRuleDeleted += handler;
    internal static IReadOnlyList<LFunnelCondition> TFunnelConditionsRead() => LFunnel.LFunnelConditions;
    internal static void TFunnelPressHandle(LFunnel funnel, LFunnelRule rule, double x, double y) =>
        funnel.LFunnelDrag.LFunnelPressHandle(rule, x, y, 0, 0);
    internal static bool TFunnelMoveCheck(LFunnel funnel, LFunnelRule rule, bool pressed) =>
        funnel.LFunnelDrag.LFunnelMoveCheck(rule, pressed);
    internal static bool TFunnelDragResolve(LFunnel funnel, double x, double y, double minimumX, double minimumY) =>
        funnel.LFunnelDrag.LFunnelDragResolve(x, y, minimumX, minimumY);
    internal static void TFunnelDragMove(LFunnel funnel, double pointer, params double[] centers) =>
        funnel.LFunnelDrag.LFunnelDragMove(pointer, centers);
    internal static bool TFunnelReleaseCheck(LFunnel funnel, LFunnelRule rule) =>
        funnel.LFunnelDrag.LFunnelReleaseCheck(rule);
    internal static void TFunnelDragClear(LFunnel funnel) => funnel.LFunnelDrag.LFunnelDragClear();
    internal static bool TFunnelDragCheck(LFunnel funnel) => funnel.LFunnelDrag.LFunnelDragActive;
    internal static int TFunnelIndexResolve(double pointer, params double[] centers) =>
        LFunnelDrag.LFunnelIndexResolve(pointer, centers);
    internal static double TFunnelCenterResolve(double top, double height) =>
        LFunnelDrag.LFunnelCenterResolve(top, height);
}

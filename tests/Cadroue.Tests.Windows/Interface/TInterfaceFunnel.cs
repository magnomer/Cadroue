using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;

namespace Cadroue.Tests.Windows;

internal static class TInterfaceFunnel
{
    internal static QFunnel TFunnelCreate() => new();
    internal static void TFunnelAttach(QFunnel funnel, Action handler) => funnel.QFunnelChanged += handler;
    internal static void TFunnelRuleAttach(QFunnel funnel, Action<QFunnelRule> handler) =>
        funnel.QFunnelRuleChanged += handler;
    internal static QFunnelRule TFunnelRuleAdd(QFunnel funnel, QFunnelForm form) => funnel.QFunnelRuleAdd(form);
    internal static void TFunnelRuleRemove(QFunnel funnel, QFunnelRule rule) => funnel.QFunnelRuleRemove(rule);
    internal static bool TFunnelRuleMove(QFunnel funnel, QFunnelRule rule, int target) =>
        funnel.QFunnelRuleMove(rule, target);
    internal static void TFunnelRuleSelect(QFunnel funnel, QFunnelRule rule) => funnel.QFunnelRuleSelect(rule);
    internal static void TFunnelJoinSet(QFunnel funnel, QFunnelRule rule, QFunnelKind kind, bool and) =>
        funnel.QFunnelCriterion.QFunnelJoinSet(rule, kind, and);
    internal static void TFunnelTextSet(QFunnel funnel, QFunnelRule rule, QFunnelKind kind, string text) =>
        funnel.QFunnelCriterion.QFunnelTextSet(rule, kind, text);
    internal static void TFunnelTargetSet(QFunnel funnel, QFunnelRule rule, Guid target) =>
        funnel.QFunnelRelay.QFunnelTargetSet(rule, target);
    internal static void TFunnelTargetsResolve(QFunnel funnel, params Guid[] tabs) =>
        funnel.QFunnelRelay.QFunnelTargetsResolve(tabs);
    internal static void TFunnelRulesRestore(QFunnel funnel, params LSceneFunnelRule[] records) =>
        funnel.QFunnelRulesRestore(records);
    internal static LSceneFunnelRule TFunnelRecordCreate(QFunnel funnel, QFunnelRule rule) =>
        QFunnelScene.QFunnelSceneCreate(rule);
    internal static LSceneFunnelRule TFunnelRecordCreate(int type, bool remainder, int target, string regex = "") =>
        new()
        {
            LSceneFunnelType = type,
            LSceneFunnelRemainder = remainder,
            LSceneFunnelTarget = target,
            LSceneFunnelRegex = regex,
        };
    internal static bool TFunnelRemainderCheck(QFunnel funnel) => funnel.QFunnelRemainderCheck();

    internal static QFunnelTab TFunnelTabCreate(QViewer viewer, QList list) => new(viewer, list);
    internal static void TFunnelClose(QFunnelTab tab) => tab.QFunnelClose();
    internal static void TFunnelLayoutApply(QFunnelTab tab, LSceneTabEntry? layout) => tab.QFunnelLayoutApply(layout);
    internal static LSceneTabEntry TFunnelLayoutRead(QFunnelTab tab) => tab.QFunnelLayoutRead(new LSceneTabEntry());
    internal static IReadOnlyList<string> TFunnelEligibleRead(QFunnelTab tab) => tab.QFunnelEligibleRead();
    internal static void TFunnelRun(QFunnelTab tab) => tab.QFunnelRun();
    internal static void TFunnelAllRun(QFunnelTab tab) => tab.QFunnelAllRun();
    internal static void TFunnelItemsRun(QFunnelTab tab, params string[] paths) => tab.QFunnelItemsRun(paths);
    internal static void TFunnelStripAttach(QFunnel funnel, QStrip strip, Guid self) =>
        funnel.QFunnelRelay.QFunnelStripAttach(strip, self);
    internal static void TFunnelStripDetach(QFunnel funnel) => funnel.QFunnelRelay.QFunnelStripDetach();
    internal static void TFunnelTargetsResolve(QFunnel funnel) => funnel.QFunnelRelay.QFunnelTargetsResolve();
    internal static IReadOnlyList<QFunnelTarget> TFunnelTargetsRead(QFunnel funnel) =>
        funnel.QFunnelRelay.QFunnelTargetsRead();
    internal static IReadOnlyList<QFunnelTarget> TFunnelOptionsRead(QFunnel funnel) =>
        funnel.QFunnelRelay.QFunnelOptionsRead();
    internal static IReadOnlyList<QFunnelSlot> TFunnelSlotsRead(QFunnel funnel) => funnel.QFunnelSlotsRead();
    internal static int TFunnelIndexRead(QFunnel funnel, Guid target) => funnel.QFunnelRelay.QFunnelIndexRead(target);
    internal static void TFunnelSelectedRemove(QFunnel funnel) => funnel.QFunnelSelectedRemove();
    internal static void TFunnelTargetSelect(QFunnel funnel, QFunnelRule rule, int index) =>
        funnel.QFunnelRelay.QFunnelTargetSelect(rule, index);
    internal static string TFunnelTextResolve(QFunnel funnel, QFunnelRule rule, QFunnelKind kind, string shown) =>
        funnel.QFunnelCriterion.QFunnelTextResolve(rule, kind, shown);
    internal static string TFunnelRegexResolve(QFunnel funnel, QFunnelRule rule, string shown) =>
        funnel.QFunnelCriterion.QFunnelRegexResolve(rule, shown);
    internal static void TFunnelRegexSet(QFunnel funnel, QFunnelRule rule, string regex) =>
        funnel.QFunnelCriterion.QFunnelRegexSet(rule, regex);
    internal static void TFunnelCaseToggle(QFunnel funnel, QFunnelRule rule, QFunnelKind kind) =>
        funnel.QFunnelCriterion.QFunnelCaseToggle(rule, kind);
    internal static void TFunnelCollapsedToggle(QFunnel funnel, QFunnelRule rule) =>
        funnel.QFunnelCollapsedToggle(rule);
    internal static void TFunnelCreateAttach(QFunnel funnel, Action<QFunnelRule> handler) =>
        funnel.QFunnelRuleCreated += handler;
    internal static void TFunnelDeleteAttach(QFunnel funnel, Action<QFunnelRule> handler) =>
        funnel.QFunnelRuleDeleted += handler;
    internal static IReadOnlyList<QFunnelCondition> TFunnelConditionsRead() => QFunnel.QFunnelConditions;
    internal static void TFunnelPressHandle(QFunnel funnel, QFunnelRule rule, double x, double y) =>
        funnel.QFunnelDrag.QFunnelPressHandle(rule, x, y, 0, 0);
    internal static bool TFunnelMoveCheck(QFunnel funnel, QFunnelRule rule, bool pressed) =>
        funnel.QFunnelDrag.QFunnelMoveCheck(rule, pressed);
    internal static bool TFunnelDragResolve(QFunnel funnel, double x, double y, double minimumX, double minimumY) =>
        funnel.QFunnelDrag.QFunnelDragResolve(x, y, minimumX, minimumY);
    internal static void TFunnelDragMove(QFunnel funnel, double pointer, params double[] centers) =>
        funnel.QFunnelDrag.QFunnelDragMove(pointer, centers);
    internal static bool TFunnelReleaseCheck(QFunnel funnel, QFunnelRule rule) =>
        funnel.QFunnelDrag.QFunnelReleaseCheck(rule);
    internal static void TFunnelDragClear(QFunnel funnel) => funnel.QFunnelDrag.QFunnelDragClear();
    internal static bool TFunnelDragCheck(QFunnel funnel) => funnel.QFunnelDrag.QFunnelDragActive;
    internal static int TFunnelIndexResolve(double pointer, params double[] centers) =>
        QFunnelDrag.QFunnelIndexResolve(pointer, centers);
    internal static double TFunnelCenterResolve(double top, double height) =>
        QFunnelDrag.QFunnelCenterResolve(top, height);
}

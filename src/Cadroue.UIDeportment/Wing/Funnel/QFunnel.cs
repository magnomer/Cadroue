using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed class QFunnel
{
    public static readonly IReadOnlyList<QFunnelCondition> QFunnelConditions =
    [
        new(QFunnelKind.QFunnelKindContains, "Inspector.Funnel.Contains", false),
        new(QFunnelKind.QFunnelKindPrefix, "Inspector.Funnel.StartsWith", true),
        new(QFunnelKind.QFunnelKindEnd, "Inspector.Funnel.EndsWith", true),
        new(QFunnelKind.QFunnelKindExtension, "Inspector.Funnel.Extension", true),
    ];

    private readonly List<QFunnelRule> qFunnelRules = [];
    private QFunnelRule? qFunnelSelected;

    public QFunnel()
    {
        QFunnelDrag = new QFunnelDrag(this);
        QFunnelCriterion = new QFunnelCriterion();
        QFunnelRelay = new QFunnelRelay(this);
        QFunnelCriterion.QFunnelCriterionChanged += QFunnelRuleHandle;
        QFunnelRelay.QFunnelRelayChanged += QFunnelRuleHandle;
    }

    public event Action? QFunnelChanged;
    public event Action<QFunnelRule>? QFunnelRuleChanged;
    public event Action<QFunnelRule>? QFunnelRuleCreated;
    public event Action<QFunnelRule>? QFunnelRuleDeleted;

    public QFunnelDrag QFunnelDrag { get; }

    public QFunnelCriterion QFunnelCriterion { get; }

    public QFunnelRelay QFunnelRelay { get; }

    public IReadOnlyList<QFunnelRule> QFunnelRules => qFunnelRules;

    public QFunnelRule? QFunnelSelected => qFunnelSelected;

    public bool QFunnelEmpty => qFunnelRules.Count == 0;

    public bool QFunnelRemainderCheck() =>
        qFunnelRules.Any(lRule => lRule.QFunnelRuleForm == QFunnelForm.QFunnelFormRemainder);

    public IReadOnlyList<QFunnelSlot> QFunnelSlotsRead() =>
        qFunnelRules
            .Select((lRule, lIndex) =>
                new QFunnelSlot(lRule, lIndex, lIndex + 1, ReferenceEquals(qFunnelSelected, lRule)))
            .ToArray();

    public QFunnelRule QFunnelRuleAdd(QFunnelForm lForm)
    {
        var lRule = new QFunnelRule(lForm);
        qFunnelRules.Add(lRule);
        qFunnelSelected = lRule;
        QFunnelRuleCreated?.Invoke(lRule);
        QFunnelChanged?.Invoke();
        return lRule;
    }

    public void QFunnelSelectedRemove()
    {
        if (qFunnelSelected is { } lSelected)
        {
            QFunnelRuleRemove(lSelected);
        }
    }

    public void QFunnelRuleRemove(QFunnelRule lRule)
    {
        int lIndex = qFunnelRules.IndexOf(lRule);
        if (lIndex < 0)
        {
            return;
        }

        qFunnelRules.RemoveAt(lIndex);
        qFunnelSelected = QFunnelSelectedResolve(lRule, lIndex);
        QFunnelRuleDeleted?.Invoke(lRule);
        QFunnelChanged?.Invoke();
    }

    private QFunnelRule? QFunnelSelectedResolve(QFunnelRule lRemoved, int lIndex)
    {
        if (!ReferenceEquals(qFunnelSelected, lRemoved))
        {
            return qFunnelSelected;
        }

        return qFunnelRules.Count == 0
            ? null
            : qFunnelRules[Math.Clamp(lIndex, 0, qFunnelRules.Count - 1)];
    }

    public bool QFunnelRuleMove(QFunnelRule lRule, int lTargetIndex)
    {
        int lSourceIndex = qFunnelRules.IndexOf(lRule);
        if (lSourceIndex < 0)
        {
            return false;
        }

        int lInsertIndex = QFunnelInsertResolve(lSourceIndex, lTargetIndex);
        if (lSourceIndex == lInsertIndex)
        {
            return false;
        }

        qFunnelRules.RemoveAt(lSourceIndex);
        qFunnelRules.Insert(lInsertIndex, lRule);
        QFunnelChanged?.Invoke();
        return true;
    }

    private int QFunnelInsertResolve(int lSourceIndex, int lTargetIndex)
    {
        int lClamped = Math.Clamp(lTargetIndex, 0, qFunnelRules.Count);
        return lSourceIndex < lClamped ? lClamped - 1 : lClamped;
    }

    public void QFunnelRuleSelect(QFunnelRule lRule)
    {
        if (ReferenceEquals(qFunnelSelected, lRule) || !qFunnelRules.Contains(lRule))
        {
            return;
        }

        qFunnelSelected = lRule;
        QFunnelChanged?.Invoke();
    }

    public void QFunnelRulesRestore(IReadOnlyList<LSceneFunnelRule> lRecords)
    {
        foreach (QFunnelRule lGone in qFunnelRules.ToArray())
        {
            qFunnelRules.Remove(lGone);
            QFunnelRuleDeleted?.Invoke(lGone);
        }

        qFunnelSelected = null;
        foreach (LSceneFunnelRule lRecord in lRecords)
        {
            QFunnelRule lRule = QFunnelScene.QFunnelSceneParse(lRecord);
            qFunnelRules.Add(lRule);
            qFunnelSelected = lRule;
            QFunnelRuleCreated?.Invoke(lRule);
        }

        QFunnelChanged?.Invoke();
    }

    public void QFunnelCollapsedSet(QFunnelRule lRule, bool lCollapsed)
    {
        if (lRule.QFunnelRuleCollapsed == lCollapsed)
        {
            return;
        }

        lRule.QFunnelRuleCollapsed = lCollapsed;
        QFunnelRuleChanged?.Invoke(lRule);
    }

    public void QFunnelCollapsedToggle(QFunnelRule lRule) => QFunnelCollapsedSet(lRule, !lRule.QFunnelRuleCollapsed);

    // A rule's criterion or relay target changed; the rows hear it as one rule change.
    private void QFunnelRuleHandle(QFunnelRule lRule) => QFunnelRuleChanged?.Invoke(lRule);
}

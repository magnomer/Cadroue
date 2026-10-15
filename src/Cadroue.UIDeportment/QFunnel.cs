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
    private QStrip? qFunnelStrip;
    private Guid qFunnelSelf;

    public QFunnel()
    {
        QFunnelDrag = new QFunnelDrag(this);
        QFunnelCriterion = new QFunnelCriterion();
        QFunnelCriterion.QFunnelCriterionChanged += QFunnelCriterionHandle;
    }

    public event Action? QFunnelChanged;
    public event Action<QFunnelRule>? QFunnelRuleChanged;
    public event Action<QFunnelRule>? QFunnelRuleCreated;
    public event Action<QFunnelRule>? QFunnelRuleDeleted;

    public QFunnelDrag QFunnelDrag { get; }

    public QFunnelCriterion QFunnelCriterion { get; }

    public IReadOnlyList<QFunnelRule> QFunnelRules => qFunnelRules;

    public QFunnelRule? QFunnelSelected => qFunnelSelected;

    public bool QFunnelEmpty => qFunnelRules.Count == 0;

    public Guid QFunnelSelf => qFunnelSelf;

    public bool QFunnelRemainderCheck() =>
        qFunnelRules.Any(lRule => lRule.QFunnelRuleForm == QFunnelForm.QFunnelFormRemainder);

    public void QFunnelStripAttach(QStrip lStrip, Guid lSelf)
    {
        if (qFunnelStrip is { } lPrevious)
        {
            lPrevious.QStripChanged -= QFunnelTargetsNormalize;
        }

        qFunnelStrip = lStrip;
        qFunnelSelf = lSelf;
        lStrip.QStripChanged += QFunnelTargetsNormalize;
        QFunnelTargetsNormalize();
    }

    public void QFunnelStripDetach()
    {
        if (qFunnelStrip is { } lStrip)
        {
            lStrip.QStripChanged -= QFunnelTargetsNormalize;
        }

        qFunnelStrip = null;
    }

    public IReadOnlyList<QFunnelTarget> QFunnelTargetsRead() =>
        qFunnelStrip?.QStripRelay.QStripRelayRead(qFunnelSelf)
            .Select(lTab => new QFunnelTarget(lTab.QStripTabId, lTab.QStripTabTitle, lTab.QStripTabKey))
            .ToArray()
        ?? [];

    public IReadOnlyList<QFunnelTarget> QFunnelOptionsRead() =>
        QFunnelTargetsRead()
            .Prepend(new QFunnelTarget(
                Guid.Empty, QLocalizationCatalog.QLocalizationTextRead("Inspector.Funnel.RelayNone"), string.Empty))
            .ToArray();

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
        if (ReferenceEquals(qFunnelSelected, lRule))
        {
            qFunnelSelected = qFunnelRules.Count == 0
                ? null
                : qFunnelRules[Math.Clamp(lIndex, 0, qFunnelRules.Count - 1)];
        }

        QFunnelRuleDeleted?.Invoke(lRule);
        QFunnelChanged?.Invoke();
    }

    public bool QFunnelRuleMove(QFunnelRule lRule, int lTargetIndex)
    {
        int lSourceIndex = qFunnelRules.IndexOf(lRule);
        if (lSourceIndex < 0)
        {
            return false;
        }

        lTargetIndex = Math.Clamp(lTargetIndex, 0, qFunnelRules.Count);
        int lInsertIndex = lSourceIndex < lTargetIndex ? lTargetIndex - 1 : lTargetIndex;
        if (lSourceIndex == lInsertIndex)
        {
            return false;
        }

        qFunnelRules.RemoveAt(lSourceIndex);
        qFunnelRules.Insert(lInsertIndex, lRule);
        QFunnelChanged?.Invoke();
        return true;
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
            QFunnelForm lForm = lRecord.LSceneFunnelRemainder
                ? QFunnelForm.QFunnelFormRemainder
                : lRecord.LSceneFunnelType == (int)QFunnelForm.QFunnelFormRegex
                    ? QFunnelForm.QFunnelFormRegex
                    : QFunnelForm.QFunnelFormFilename;
            var lRule = new QFunnelRule(lForm)
            {
                QFunnelRuleContains = lRecord.LSceneFunnelContains.LSceneFunnelClone(),
                QFunnelRulePrefix = lRecord.LSceneFunnelPrefix.LSceneFunnelClone(),
                QFunnelRuleEnd = lRecord.LSceneFunnelEnd.LSceneFunnelClone(),
                QFunnelRuleExtension = lRecord.LSceneFunnelExtension.LSceneFunnelClone(),
                QFunnelRuleRegex = lRecord.LSceneFunnelRegex,
                QFunnelRuleWhole = lRecord.LSceneFunnelWhole,
                QFunnelRulePending = lRecord.LSceneFunnelTarget
            };
            qFunnelRules.Add(lRule);
            qFunnelSelected = lRule;
            QFunnelRuleCreated?.Invoke(lRule);
        }

        QFunnelChanged?.Invoke();
    }

    public void QFunnelTargetsResolve() =>
        QFunnelTargetsResolve(qFunnelStrip?.QStripTabs.Select(lTab => lTab.QStripTabId).ToArray() ?? []);

    public void QFunnelTargetsResolve(IReadOnlyList<Guid> lTabIds)
    {
        foreach (QFunnelRule lRule in qFunnelRules)
        {
            int lPending = lRule.QFunnelRulePending;
            QFunnelTargetSet(lRule, lPending >= 0 && lPending < lTabIds.Count ? lTabIds[lPending] : Guid.Empty);
        }
    }

    public void QFunnelTargetSelect(QFunnelRule lRule, int lIndex)
    {
        IReadOnlyList<QFunnelTarget> lOptions = QFunnelOptionsRead();
        if (lIndex >= 0 && lIndex < lOptions.Count)
        {
            QFunnelTargetSet(lRule, lOptions[lIndex].QFunnelTargetId);
        }
    }

    public void QFunnelTargetSet(QFunnelRule lRule, Guid lTargetId)
    {
        if (lRule.QFunnelRuleTarget == lTargetId && lRule.QFunnelRulePending < 0)
        {
            return;
        }

        lRule.QFunnelRuleTarget = lTargetId;
        lRule.QFunnelRulePending = -1;
        QFunnelRuleChanged?.Invoke(lRule);
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

    public LSceneFunnelRule QFunnelRecordCreate(QFunnelRule lRule) => lRule.QFunnelRuleForm switch
    {
        QFunnelForm.QFunnelFormRemainder => new LSceneFunnelRule
        {
            LSceneFunnelType = (int)QFunnelForm.QFunnelFormFilename,
            LSceneFunnelRemainder = true
        },
        QFunnelForm.QFunnelFormRegex => new LSceneFunnelRule
        {
            LSceneFunnelType = (int)QFunnelForm.QFunnelFormRegex,
            LSceneFunnelRegex = lRule.QFunnelRuleRegex,
            LSceneFunnelWhole = lRule.QFunnelRuleWhole
        },
        _ => new LSceneFunnelRule
        {
            LSceneFunnelType = (int)QFunnelForm.QFunnelFormFilename,
            LSceneFunnelContains = lRule.QFunnelRuleContains.LSceneFunnelClone(),
            LSceneFunnelPrefix = lRule.QFunnelRulePrefix.LSceneFunnelClone(),
            LSceneFunnelEnd = lRule.QFunnelRuleEnd.LSceneFunnelClone(),
            LSceneFunnelExtension = lRule.QFunnelRuleExtension.LSceneFunnelClone()
        }
    };

    public int QFunnelIndexRead(Guid lTargetId)
    {
        if (lTargetId == Guid.Empty || qFunnelStrip is not { } lStrip)
        {
            return -1;
        }

        return lStrip.QStripTabs.ToList().FindIndex(lTab => lTab.QStripTabId == lTargetId);
    }

    private void QFunnelTargetsNormalize()
    {
        var lLive = QFunnelTargetsRead().Select(lTarget => lTarget.QFunnelTargetId).Append(Guid.Empty).ToHashSet();
        foreach (QFunnelRule lRule in qFunnelRules)
        {
            if (lRule.QFunnelRulePending < 0 && !lLive.Contains(lRule.QFunnelRuleTarget))
            {
                QFunnelTargetSet(lRule, Guid.Empty);
            }
            else
            {
                QFunnelRuleChanged?.Invoke(lRule);
            }
        }
    }

    private void QFunnelCriterionHandle(QFunnelRule lRule) => QFunnelRuleChanged?.Invoke(lRule);
}

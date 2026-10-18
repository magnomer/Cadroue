namespace Cadroue.UIDeportment;

// Where the funnel's rules send their files: the strip's tabs other than the funnel's own, and each rule's target kept
// live as tabs come and go. A restored rule holds a pending tab index until the strip's tabs resolve it.
public sealed class QFunnelRelay
{
    private readonly QFunnel qFunnel;
    private QStrip? qFunnelStrip;
    private Guid qFunnelSelf;

    public QFunnelRelay(QFunnel lOwner)
    {
        qFunnel = lOwner;
    }

    public event Action<QFunnelRule>? QFunnelRelayChanged;

    public Guid QFunnelSelf => qFunnelSelf;

    public void QFunnelStripAttach(QStrip lStrip, Guid lSelf)
    {
        QFunnelStripDetach();
        QFunnelSelfSet(lSelf);
        QFunnelStripHook(lStrip);
        QFunnelTargetsNormalize();
    }

    private void QFunnelSelfSet(Guid lSelf) => qFunnelSelf = lSelf;

    private void QFunnelStripHook(QStrip lStrip)
    {
        qFunnelStrip = lStrip;
        lStrip.QStripChanged += QFunnelTargetsNormalize;
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

    public void QFunnelTargetsResolve() =>
        QFunnelTargetsResolve(qFunnelStrip?.QStripTabs.Select(lTab => lTab.QStripTabId).ToArray() ?? []);

    public void QFunnelTargetsResolve(IReadOnlyList<Guid> lTabIds)
    {
        foreach (QFunnelRule lRule in qFunnel.QFunnelRules)
        {
            QFunnelTargetSet(lRule, QFunnelPendingResolve(lRule.QFunnelRulePending, lTabIds));
        }
    }

    private static Guid QFunnelPendingResolve(int lPending, IReadOnlyList<Guid> lTabIds) =>
        lPending >= 0 && lPending < lTabIds.Count ? lTabIds[lPending] : Guid.Empty;

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
        QFunnelRelayChanged?.Invoke(lRule);
    }

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
        foreach (QFunnelRule lRule in qFunnel.QFunnelRules)
        {
            if (lRule.QFunnelRulePending < 0 && !lLive.Contains(lRule.QFunnelRuleTarget))
            {
                QFunnelTargetSet(lRule, Guid.Empty);
            }
            else
            {
                QFunnelRelayChanged?.Invoke(lRule);
            }
        }
    }
}

using Cadroue.Application;
using Cadroue.ShellEngine;

namespace Cadroue.UIDeportment;

public sealed record QActionOption(Guid QActionOptionId, string QActionOptionTitle, string QActionOptionKey);

public sealed class QAction
{
    private Func<IReadOnlyList<string>>? qActionEligibleSource;
    private Func<IReadOnlyList<string>>? qActionSelectionSource;
    private QStrip? qActionRelayStrip;
    private Guid qActionSourceTab;
    private Guid qActionRelayTarget;
    private bool qActionAutoRelay;

    // The urgent choice is raw input: true from the execute button, false from the list button.
    public event Action<bool, Guid, Guid>? QActionExecuted;
    public event Action<Guid, Guid>? QActionAllAdded;
    public event Func<Guid, Guid, Guid, int>? QActionCohortAdded;
    public event Action<IReadOnlyList<string>, Guid, Guid>? QActionItemsAdded;
    public event Action? QActionFaceChanged;
    public event Action? QActionEmptyRaised;

    public bool QActionAutoRelay => qActionAutoRelay;

    public string QActionFaceText => QActionOptionFind() is { } lOption
        ? lOption.QActionOptionTitle
        : QLocalizationCatalog.QLocalizationTextRead(QActionKeyRead());

    public string QActionFaceIcon => QActionOptionFind()?.QActionOptionKey ?? string.Empty;

    public bool QActionFaceShown => QActionOptionFind() is not null;

    public void QActionDocketAttach(LDocket lDocket) =>
        qActionEligibleSource = () => lDocket.LDocketUnlockedRead().Select(lItem => lItem.LDocketEntryPath).ToArray();

    public void QActionEligibleAttach(Func<IReadOnlyList<string>> lSource) => qActionEligibleSource = lSource;

    public void QActionListAttach(QList lList)
    {
        qActionSelectionSource = lList.QListSelectionRead;
        QActionDocketAttach(lList.QListDocket);
    }

    public void QActionRelayAttach(QStrip lStrip, QStripTab lStripTab)
    {
        QActionRelaySet(lStrip, lStripTab);
        lStripTab.QStripActionAttach(this);
        LCartographerSchedule.LCartographerStart();
        QActionTitleAttach(lStrip);
        QActionTargetUpdate();
    }

    private void QActionRelaySet(QStrip lStrip, QStripTab lStripTab)
    {
        qActionRelayStrip = lStrip;
        qActionSourceTab = lStripTab.QStripTabId;
    }

    private void QActionTitleAttach(QStrip lStrip) => lStrip.QStripTitleChanged += QActionTargetUpdate;

    public void QActionAutoSet(bool? lChecked) => qActionAutoRelay = QActionAutoNormalize(lChecked);

    private static bool QActionAutoNormalize(bool? lChecked) => lChecked == true;

    public void QActionRelayApply(Guid lTarget)
    {
        qActionRelayTarget = lTarget;
        QActionFaceUpdate();
    }

    public void QActionRelaySelect(Guid lTarget)
    {
        if (lTarget == qActionRelayTarget)
        {
            return;
        }

        LCartographerRoute.LCartographerTargetSet(qActionSourceTab, lTarget);
        QActionTargetUpdate();
    }

    public IReadOnlyList<QActionOption> QActionOptionsRead() =>
        qActionRelayStrip?.QStripRelay.QStripRelayRead(qActionSourceTab).Select(QActionOptionCreate).ToArray()
        ?? Array.Empty<QActionOption>();

    public IReadOnlyList<QActionOption> QActionMenuRead() =>
        new[] { new QActionOption(Guid.Empty, QLocalizationCatalog.QLocalizationTextRead("Action.Relay.None"),
            string.Empty) }
            .Concat(QActionOptionsRead())
            .Append(new QActionOption(
                LCartographer.LCartographerFinishTarget,
                QLocalizationCatalog.QLocalizationTextRead("Action.Relay.Finish"),
                string.Empty))
            .ToArray();

    public bool QActionEligibleCheck(IReadOnlyList<string> lChosen)
    {
        if (qActionEligibleSource is not { } lSource)
        {
            return true;
        }

        IReadOnlyList<string> lEligible = lSource();
        return lChosen.Count == 0
            ? lEligible.Count > 0
            : lChosen.Any(lPath => lEligible.Contains(lPath, StringComparer.OrdinalIgnoreCase));
    }

    public void QActionAllRun()
    {
        if (QActionEligibleResolve(false))
        {
            QActionAllAdded?.Invoke(qActionRelayTarget, qActionSourceTab);
        }
    }

    public void QActionHighRun()
    {
        if (QActionEligibleResolve(true))
        {
            QActionExecuted?.Invoke(true, qActionRelayTarget, qActionSourceTab);
        }
    }

    public void QActionListRun()
    {
        if (!QActionEligibleResolve(true))
        {
            return;
        }

        if (QActionSelectionRead() is { Count: > 0 } lSelected)
        {
            QActionItemsAdded?.Invoke(lSelected, qActionRelayTarget, qActionSourceTab);
            return;
        }

        QActionExecuted?.Invoke(false, qActionRelayTarget, qActionSourceTab);
    }

    public void QActionItemsRun(IReadOnlyList<string> lPaths) =>
        QActionItemsAdded?.Invoke(lPaths, qActionRelayTarget, qActionSourceTab);

    public bool QActionCohortRun(Guid lCohort) =>
        qActionAutoRelay && QActionCohortAdded?.Invoke(lCohort, qActionRelayTarget, qActionSourceTab) > 0;

    private void QActionTargetUpdate() =>
        QActionRelayApply(LCartographerRoute.LCartographerTargetRead(qActionSourceTab));

    private void QActionFaceUpdate()
    {
        qActionRelayTarget = QActionTargetNormalize();
        QActionFaceChanged?.Invoke();
    }

    private Guid QActionTargetNormalize() =>
        qActionRelayTarget != Guid.Empty
        && qActionRelayTarget != LCartographer.LCartographerFinishTarget
        && QActionOptionFind() is null
            ? Guid.Empty
            : qActionRelayTarget;

    private string QActionKeyRead() =>
        qActionRelayTarget == LCartographer.LCartographerFinishTarget ? "Action.Relay.Finish" : "Action.Relay.None";

    private QActionOption? QActionOptionFind() =>
        qActionRelayTarget == Guid.Empty || qActionRelayTarget == LCartographer.LCartographerFinishTarget
            ? null
            : QActionOptionsRead().FirstOrDefault(lOption => lOption.QActionOptionId == qActionRelayTarget);

    private bool QActionEligibleResolve(bool lSelected)
    {
        bool lAllowed = QActionEligibleCheck(QActionChosenRead(lSelected));
        if (!lAllowed)
        {
            QActionEmptyRaised?.Invoke();
        }

        return lAllowed;
    }

    private IReadOnlyList<string> QActionChosenRead(bool lSelected) =>
        lSelected ? QActionSelectionRead() : Array.Empty<string>();

    private IReadOnlyList<string> QActionSelectionRead() => qActionSelectionSource?.Invoke() ?? Array.Empty<string>();

    private static QActionOption QActionOptionCreate(QStripTab lStripTab) =>
        new(lStripTab.QStripTabId, lStripTab.QStripTabTitle, lStripTab.QStripTabKey);
}

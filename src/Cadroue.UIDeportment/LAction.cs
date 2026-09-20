using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.ShellEngine;

namespace Cadroue.UIDeportment;

public sealed record LActionOption(Guid LActionOptionId, string LActionOptionTitle, string LActionOptionKey);

public sealed class LAction
{
    private static LStrip? lActionStrip;
    private Func<IReadOnlyList<string>>? lActionEligibleSource;
    private Func<IReadOnlyList<string>>? lActionSelectionSource;
    private LStrip? lActionRelayStrip;
    private Guid lActionSourceTab;
    private Guid lActionRelayTarget;
    private bool lActionAutoRelay;

    public event Action<LWorkPriority>? LActionRun;
    public event Action? LActionAllAdd;
    public event Func<Guid, int>? LActionCohortAdd;
    public event Action<IReadOnlyList<string>>? LActionItemsAdd;
    public event Action? LActionFaceChange;
    public event Action? LActionEmptyRaise;

    public Guid LActionSourceTab => lActionSourceTab;

    public Guid LActionRelayTarget => lActionRelayTarget;

    public bool LActionAutoRelay => lActionAutoRelay;

    public string LActionFaceText => LActionOptionFind() is { } lOption
        ? lOption.LActionOptionTitle
        : LLocalization.LLocalizationTextRead(LActionKeyRead());

    public string LActionFaceIcon => LActionOptionFind()?.LActionOptionKey ?? string.Empty;

    public bool LActionFaceShown => LActionOptionFind() is not null;

    public static void LActionStripAttach(LStrip lStrip) => lActionStrip = lStrip;

    public void LActionDocketAttach(LDocket lDocket) =>
        lActionEligibleSource = () => lDocket.LDocketUnlockedRead().Select(lItem => lItem.LDocketEntryPath).ToArray();

    public void LActionEligibleAttach(Func<IReadOnlyList<string>> lSource) => lActionEligibleSource = lSource;

    public void LActionListAttach(LList lList)
    {
        lActionSelectionSource = lList.LListSelectionRead;
        LActionDocketAttach(lList.LListDocket);
    }

    public void LActionRelayAttach(LStrip lStrip, LStripTab lStripTab)
    {
        lActionRelayStrip = lStrip;
        lActionSourceTab = lStripTab.LStripTabId;
        lStripTab.LStripActionAttach(this);
        LCartographer.LCartographerStart();
        lStrip.LStripTitleChange += LActionTargetUpdate;
        LActionTargetUpdate();
    }

    public void LActionAutoSet(bool? lChecked) => lActionAutoRelay = lChecked == true;

    public void LActionRelayApply(Guid lTarget)
    {
        lActionRelayTarget = lTarget;
        LActionFaceUpdate();
    }

    public void LActionRelaySelect(Guid lTarget)
    {
        if (lTarget == lActionRelayTarget)
        {
            return;
        }

        LCartographer.LCartographerTargetSet(lActionSourceTab, lTarget);
        LActionTargetUpdate();
    }

    public IReadOnlyList<LActionOption> LActionOptionsRead() =>
        lActionRelayStrip?.LStripRelayRead(lActionSourceTab).Select(LActionOptionCreate).ToArray()
        ?? Array.Empty<LActionOption>();

    public IReadOnlyList<LActionOption> LActionMenuRead() =>
        new[] { new LActionOption(Guid.Empty, LLocalization.LLocalizationTextRead("Action.Relay.None"), string.Empty) }
            .Concat(LActionOptionsRead())
            .Append(new LActionOption(
                LCartographer.LCartographerFinishTarget,
                LLocalization.LLocalizationTextRead("Action.Relay.Finish"),
                string.Empty))
            .ToArray();

    public bool LActionEligibleCheck(IReadOnlyList<string> lChosen)
    {
        if (lActionEligibleSource is not { } lSource)
        {
            return true;
        }

        IReadOnlyList<string> lEligible = lSource();
        return lChosen.Count == 0
            ? lEligible.Count > 0
            : lChosen.Any(lPath => lEligible.Contains(lPath, StringComparer.OrdinalIgnoreCase));
    }

    public void LActionAllRun()
    {
        if (LActionEligibleResolve(false))
        {
            LActionAllAdd?.Invoke();
        }
    }

    public void LActionHighRun()
    {
        if (LActionEligibleResolve(true))
        {
            LActionRun?.Invoke(LWorkPriority.LWorkPriorityHigh);
        }
    }

    public void LActionListRun()
    {
        if (!LActionEligibleResolve(true))
        {
            return;
        }

        if (LActionSelectionRead() is { Count: > 0 } lSelected)
        {
            LActionItemsAdd?.Invoke(lSelected);
            return;
        }

        LActionRun?.Invoke(LWorkPriority.LWorkPriorityNormal);
    }

    public void LActionItemsRun(IReadOnlyList<string> lPaths) => LActionItemsAdd?.Invoke(lPaths);

    public bool LActionCohortRun(Guid lCohort) => lActionAutoRelay && LActionCohortAdd?.Invoke(lCohort) > 0;

    public static void LActionAccept(Guid lTargetTab, string lPath, Guid lCohort)
    {
        if (LActionAutoFind(lTargetTab) is null)
        {
            return;
        }

        LSeal.LSealPendingAdd(lCohort);
        LProgram.LProgramDefer(() => LActionAcceptRun(lTargetTab, lPath, lCohort));
    }

    private static void LActionAcceptRun(Guid lTargetTab, string lPath, Guid lCohort)
    {
        try
        {
            if (LActionAutoFind(lTargetTab) is { } lAction)
            {
                lAction.LActionItemsRun(new[] { lPath });
            }
            else
            {
                LTraceLog.LTraceWarningRecord(
                    $"Relay left '{System.IO.Path.GetFileName(lPath)}' unprocessed: "
                    + "the destination tab closed or left Auto Relay before it ran");
            }
        }
        finally
        {
            LSeal.LSealPendingRemove(lCohort);
            LSeal.LSealRun();
        }
    }

    private static LAction? LActionAutoFind(Guid lTargetTab) =>
        lActionStrip?.LStripTabFind(lTargetTab)
            is { LStripTabMerge: false, LStripTabAction: { LActionAutoRelay: true } lAction }
            ? lAction
            : null;

    private void LActionTargetUpdate() => LActionRelayApply(LCartographer.LCartographerTargetRead(lActionSourceTab));

    private void LActionFaceUpdate()
    {
        if (lActionRelayTarget != Guid.Empty
            && lActionRelayTarget != LCartographer.LCartographerFinishTarget
            && LActionOptionFind() is null)
        {
            lActionRelayTarget = Guid.Empty;
        }

        LActionFaceChange?.Invoke();
    }

    private string LActionKeyRead() =>
        lActionRelayTarget == LCartographer.LCartographerFinishTarget ? "Action.Relay.Finish" : "Action.Relay.None";

    private LActionOption? LActionOptionFind() =>
        lActionRelayTarget == Guid.Empty || lActionRelayTarget == LCartographer.LCartographerFinishTarget
            ? null
            : LActionOptionsRead().FirstOrDefault(lOption => lOption.LActionOptionId == lActionRelayTarget);

    private bool LActionEligibleResolve(bool lSelected)
    {
        bool lAllowed = LActionEligibleCheck(lSelected ? LActionSelectionRead() : Array.Empty<string>());
        if (!lAllowed)
        {
            LActionEmptyRaise?.Invoke();
        }

        return lAllowed;
    }

    private IReadOnlyList<string> LActionSelectionRead() => lActionSelectionSource?.Invoke() ?? Array.Empty<string>();

    private static LActionOption LActionOptionCreate(LStripTab lStripTab) =>
        new(lStripTab.LStripTabId, lStripTab.LStripTabTitle, lStripTab.LStripTabKey);
}

using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed class LFunnel
{
    public static readonly IReadOnlyList<LFunnelCondition> LFunnelConditions =
    [
        new(LFunnelKind.LFunnelKindContains, "Inspector.Funnel.Contains", false),
        new(LFunnelKind.LFunnelKindPrefix, "Inspector.Funnel.StartsWith", true),
        new(LFunnelKind.LFunnelKindEnd, "Inspector.Funnel.EndsWith", true),
        new(LFunnelKind.LFunnelKindExtension, "Inspector.Funnel.Extension", true),
    ];

    private readonly List<LFunnelRule> lFunnelRules = [];
    private LFunnelRule? lFunnelSelected;
    private LStrip? lFunnelStrip;
    private Guid lFunnelSelf;

    public LFunnel()
    {
        LFunnelDrag = new LFunnelDrag(this);
    }

    public event Action? LFunnelChange;
    public event Action<LFunnelRule>? LFunnelRuleChange;
    public event Action<LFunnelRule>? LFunnelRuleCreate;
    public event Action<LFunnelRule>? LFunnelRuleDelete;

    public LFunnelDrag LFunnelDrag { get; }

    public IReadOnlyList<LFunnelRule> LFunnelRules => lFunnelRules;

    public LFunnelRule? LFunnelSelected => lFunnelSelected;

    public bool LFunnelEmpty => lFunnelRules.Count == 0;

    public Guid LFunnelSelf => lFunnelSelf;

    public bool LFunnelRemainderCheck() =>
        lFunnelRules.Any(lRule => lRule.LFunnelRuleForm == LFunnelForm.LFunnelFormRemainder);

    public void LFunnelStripAttach(LStrip lStrip, Guid lSelf)
    {
        if (lFunnelStrip is { } lPrevious)
        {
            lPrevious.LStripChange -= LFunnelTargetsNormalize;
        }

        lFunnelStrip = lStrip;
        lFunnelSelf = lSelf;
        lStrip.LStripChange += LFunnelTargetsNormalize;
        LFunnelTargetsNormalize();
    }

    public void LFunnelStripDetach()
    {
        if (lFunnelStrip is { } lStrip)
        {
            lStrip.LStripChange -= LFunnelTargetsNormalize;
        }

        lFunnelStrip = null;
    }

    public IReadOnlyList<LFunnelTarget> LFunnelTargetsRead() =>
        lFunnelStrip?.LStripRelayRead(lFunnelSelf)
            .Select(lTab => new LFunnelTarget(lTab.LStripTabId, lTab.LStripTabTitle, lTab.LStripTabKey))
            .ToArray()
        ?? [];

    public IReadOnlyList<LFunnelTarget> LFunnelOptionsRead() =>
        LFunnelTargetsRead()
            .Prepend(new LFunnelTarget(
                Guid.Empty, LLocalization.LLocalizationTextRead("Inspector.Funnel.RelayNone"), string.Empty))
            .ToArray();

    public IReadOnlyList<LFunnelSlot> LFunnelSlotsRead() =>
        lFunnelRules
            .Select((lRule, lIndex) =>
                new LFunnelSlot(lRule, lIndex, lIndex + 1, ReferenceEquals(lFunnelSelected, lRule)))
            .ToArray();

    public LFunnelRule LFunnelRuleAdd(LFunnelForm lForm)
    {
        var lRule = new LFunnelRule(lForm);
        lFunnelRules.Add(lRule);
        lFunnelSelected = lRule;
        LFunnelRuleCreate?.Invoke(lRule);
        LFunnelChange?.Invoke();
        return lRule;
    }

    public void LFunnelSelectedRemove()
    {
        if (lFunnelSelected is { } lSelected)
        {
            LFunnelRuleRemove(lSelected);
        }
    }

    public void LFunnelRuleRemove(LFunnelRule lRule)
    {
        int lIndex = lFunnelRules.IndexOf(lRule);
        if (lIndex < 0)
        {
            return;
        }

        lFunnelRules.RemoveAt(lIndex);
        if (ReferenceEquals(lFunnelSelected, lRule))
        {
            lFunnelSelected = lFunnelRules.Count == 0
                ? null
                : lFunnelRules[Math.Clamp(lIndex, 0, lFunnelRules.Count - 1)];
        }

        LFunnelRuleDelete?.Invoke(lRule);
        LFunnelChange?.Invoke();
    }

    public bool LFunnelRuleMove(LFunnelRule lRule, int lTargetIndex)
    {
        int lSourceIndex = lFunnelRules.IndexOf(lRule);
        if (lSourceIndex < 0)
        {
            return false;
        }

        lTargetIndex = Math.Clamp(lTargetIndex, 0, lFunnelRules.Count);
        int lInsertIndex = lSourceIndex < lTargetIndex ? lTargetIndex - 1 : lTargetIndex;
        if (lSourceIndex == lInsertIndex)
        {
            return false;
        }

        lFunnelRules.RemoveAt(lSourceIndex);
        lFunnelRules.Insert(lInsertIndex, lRule);
        LFunnelChange?.Invoke();
        return true;
    }

    public void LFunnelRuleSelect(LFunnelRule lRule)
    {
        if (ReferenceEquals(lFunnelSelected, lRule) || !lFunnelRules.Contains(lRule))
        {
            return;
        }

        lFunnelSelected = lRule;
        LFunnelChange?.Invoke();
    }

    public void LFunnelRulesRestore(IReadOnlyList<LSceneFunnelRule> lRecords)
    {
        foreach (LFunnelRule lGone in lFunnelRules.ToArray())
        {
            lFunnelRules.Remove(lGone);
            LFunnelRuleDelete?.Invoke(lGone);
        }

        lFunnelSelected = null;
        foreach (LSceneFunnelRule lRecord in lRecords)
        {
            LFunnelForm lForm = lRecord.LSceneFunnelRemainder
                ? LFunnelForm.LFunnelFormRemainder
                : lRecord.LSceneFunnelType == (int)LFunnelForm.LFunnelFormRegex
                    ? LFunnelForm.LFunnelFormRegex
                    : LFunnelForm.LFunnelFormFilename;
            var lRule = new LFunnelRule(lForm)
            {
                LFunnelRuleContains = lRecord.LSceneFunnelContains.LSceneFunnelClone(),
                LFunnelRulePrefix = lRecord.LSceneFunnelPrefix.LSceneFunnelClone(),
                LFunnelRuleEnd = lRecord.LSceneFunnelEnd.LSceneFunnelClone(),
                LFunnelRuleExtension = lRecord.LSceneFunnelExtension.LSceneFunnelClone(),
                LFunnelRuleRegex = lRecord.LSceneFunnelRegex,
                LFunnelRuleWhole = lRecord.LSceneFunnelWhole,
                LFunnelRulePending = lRecord.LSceneFunnelTarget
            };
            lFunnelRules.Add(lRule);
            lFunnelSelected = lRule;
            LFunnelRuleCreate?.Invoke(lRule);
        }

        LFunnelChange?.Invoke();
    }

    public void LFunnelTargetsResolve() =>
        LFunnelTargetsResolve(lFunnelStrip?.LStripTabs.Select(lTab => lTab.LStripTabId).ToArray() ?? []);

    public void LFunnelTargetsResolve(IReadOnlyList<Guid> lTabIds)
    {
        foreach (LFunnelRule lRule in lFunnelRules)
        {
            int lPending = lRule.LFunnelRulePending;
            LFunnelTargetSet(lRule, lPending >= 0 && lPending < lTabIds.Count ? lTabIds[lPending] : Guid.Empty);
        }
    }

    public void LFunnelTargetSelect(LFunnelRule lRule, int lIndex)
    {
        IReadOnlyList<LFunnelTarget> lOptions = LFunnelOptionsRead();
        if (lIndex >= 0 && lIndex < lOptions.Count)
        {
            LFunnelTargetSet(lRule, lOptions[lIndex].LFunnelTargetId);
        }
    }

    public void LFunnelTargetSet(LFunnelRule lRule, Guid lTargetId)
    {
        if (lRule.LFunnelRuleTarget == lTargetId && lRule.LFunnelRulePending < 0)
        {
            return;
        }

        lRule.LFunnelRuleTarget = lTargetId;
        lRule.LFunnelRulePending = -1;
        LFunnelRuleChange?.Invoke(lRule);
    }

    public LSceneFunnelMatch LFunnelMatchRead(LFunnelRule lRule, LFunnelKind lKind) => lKind switch
    {
        LFunnelKind.LFunnelKindPrefix => lRule.LFunnelRulePrefix,
        LFunnelKind.LFunnelKindEnd => lRule.LFunnelRuleEnd,
        LFunnelKind.LFunnelKindExtension => lRule.LFunnelRuleExtension,
        _ => lRule.LFunnelRuleContains
    };

    public string LFunnelTextResolve(LFunnelRule lRule, LFunnelKind lKind, string lShown) =>
        LFunnelEchoResolve(LFunnelMatchRead(lRule, lKind).LSceneFunnelText, lShown);

    public string LFunnelRegexResolve(LFunnelRule lRule, string lShown) =>
        LFunnelEchoResolve(lRule.LFunnelRuleRegex, lShown);

    public void LFunnelTextSet(LFunnelRule lRule, LFunnelKind lKind, string lText)
    {
        LSceneFunnelMatch lMatch = LFunnelMatchRead(lRule, lKind);
        string lTrimmed = lText.Trim();
        if (lMatch.LSceneFunnelText == lTrimmed)
        {
            return;
        }

        lMatch.LSceneFunnelText = lTrimmed;
        LFunnelRuleChange?.Invoke(lRule);
    }

    public void LFunnelCaseSet(LFunnelRule lRule, LFunnelKind lKind, bool lCase)
    {
        LSceneFunnelMatch lMatch = LFunnelMatchRead(lRule, lKind);
        if (lMatch.LSceneFunnelCase == lCase)
        {
            return;
        }

        lMatch.LSceneFunnelCase = lCase;
        LFunnelRuleChange?.Invoke(lRule);
    }

    public void LFunnelCaseToggle(LFunnelRule lRule, LFunnelKind lKind) =>
        LFunnelCaseSet(lRule, lKind, !LFunnelMatchRead(lRule, lKind).LSceneFunnelCase);

    public void LFunnelJoinSet(LFunnelRule lRule, LFunnelKind lKind, bool lAnd)
    {
        LSceneFunnelMatch lMatch = LFunnelMatchRead(lRule, lKind);
        if (lMatch.LSceneFunnelJoin == lAnd)
        {
            return;
        }

        lMatch.LSceneFunnelJoin = lAnd;
        LFunnelRuleChange?.Invoke(lRule);
    }

    public void LFunnelRegexSet(LFunnelRule lRule, string lRegex)
    {
        string lTrimmed = lRegex.Trim();
        if (lRule.LFunnelRuleRegex == lTrimmed)
        {
            return;
        }

        lRule.LFunnelRuleRegex = lTrimmed;
        LFunnelRuleChange?.Invoke(lRule);
    }

    public void LFunnelWholeSet(LFunnelRule lRule, bool lWhole)
    {
        if (lRule.LFunnelRuleWhole == lWhole)
        {
            return;
        }

        lRule.LFunnelRuleWhole = lWhole;
        LFunnelRuleChange?.Invoke(lRule);
    }

    public void LFunnelCollapsedSet(LFunnelRule lRule, bool lCollapsed)
    {
        if (lRule.LFunnelRuleCollapsed == lCollapsed)
        {
            return;
        }

        lRule.LFunnelRuleCollapsed = lCollapsed;
        LFunnelRuleChange?.Invoke(lRule);
    }

    public void LFunnelCollapsedToggle(LFunnelRule lRule) => LFunnelCollapsedSet(lRule, !lRule.LFunnelRuleCollapsed);

    public LSceneFunnelRule LFunnelRecordCreate(LFunnelRule lRule) => lRule.LFunnelRuleForm switch
    {
        LFunnelForm.LFunnelFormRemainder => new LSceneFunnelRule
        {
            LSceneFunnelType = (int)LFunnelForm.LFunnelFormFilename,
            LSceneFunnelRemainder = true
        },
        LFunnelForm.LFunnelFormRegex => new LSceneFunnelRule
        {
            LSceneFunnelType = (int)LFunnelForm.LFunnelFormRegex,
            LSceneFunnelRegex = lRule.LFunnelRuleRegex,
            LSceneFunnelWhole = lRule.LFunnelRuleWhole
        },
        _ => new LSceneFunnelRule
        {
            LSceneFunnelType = (int)LFunnelForm.LFunnelFormFilename,
            LSceneFunnelContains = lRule.LFunnelRuleContains.LSceneFunnelClone(),
            LSceneFunnelPrefix = lRule.LFunnelRulePrefix.LSceneFunnelClone(),
            LSceneFunnelEnd = lRule.LFunnelRuleEnd.LSceneFunnelClone(),
            LSceneFunnelExtension = lRule.LFunnelRuleExtension.LSceneFunnelClone()
        }
    };

    public int LFunnelIndexRead(Guid lTargetId)
    {
        if (lTargetId == Guid.Empty || lFunnelStrip is not { } lStrip)
        {
            return -1;
        }

        return lStrip.LStripTabs.ToList().FindIndex(lTab => lTab.LStripTabId == lTargetId);
    }

    private void LFunnelTargetsNormalize()
    {
        var lLive = LFunnelTargetsRead().Select(lTarget => lTarget.LFunnelTargetId).Append(Guid.Empty).ToHashSet();
        foreach (LFunnelRule lRule in lFunnelRules)
        {
            if (lRule.LFunnelRulePending < 0 && !lLive.Contains(lRule.LFunnelRuleTarget))
            {
                LFunnelTargetSet(lRule, Guid.Empty);
            }
            else
            {
                LFunnelRuleChange?.Invoke(lRule);
            }
        }
    }

    private static string LFunnelEchoResolve(string lStored, string lShown) =>
        lShown.Trim() == lStored ? lShown : lStored;
}

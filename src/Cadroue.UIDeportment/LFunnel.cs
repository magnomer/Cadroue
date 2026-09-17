using Cadroue.Core;

namespace Cadroue.UIDeportment;

public enum LFunnelForm { LFunnelFormFilename, LFunnelFormRegex, LFunnelFormRemainder }

public enum LFunnelKind { LFunnelKindContains, LFunnelKindPrefix, LFunnelKindEnd, LFunnelKindExtension }

public sealed class LFunnelRule
{
    public LFunnelRule(LFunnelForm lForm)
    {
        LFunnelRuleForm = lForm;
    }

    public LFunnelForm LFunnelRuleForm { get; }

    public LSceneFunnelMatch LFunnelRuleContains { get; internal set; } = new();

    public LSceneFunnelMatch LFunnelRulePrefix { get; internal set; } = new();

    public LSceneFunnelMatch LFunnelRuleEnd { get; internal set; } = new();

    public LSceneFunnelMatch LFunnelRuleExtension { get; internal set; } = new();

    public string LFunnelRuleRegex { get; internal set; } = string.Empty;

    public bool LFunnelRuleWhole { get; internal set; }

    public Guid LFunnelRuleTarget { get; internal set; }

    public int LFunnelRulePending { get; internal set; } = -1;

    public bool LFunnelRuleCollapsed { get; internal set; }
}

public sealed class LFunnel
{
    private readonly List<LFunnelRule> lFunnelRules = [];
    private LFunnelRule? lFunnelSelected;

    public event Action? LFunnelChange;
    public event Action<LFunnelRule>? LFunnelRuleChange;

    public IReadOnlyList<LFunnelRule> LFunnelRules => lFunnelRules;

    public LFunnelRule? LFunnelSelected => lFunnelSelected;

    public bool LFunnelRemainderCheck() =>
        lFunnelRules.Any(lRule => lRule.LFunnelRuleForm == LFunnelForm.LFunnelFormRemainder);

    public LFunnelRule LFunnelRuleAdd(LFunnelForm lForm)
    {
        var lRule = new LFunnelRule(lForm);
        lFunnelRules.Add(lRule);
        lFunnelSelected = lRule;
        LFunnelChange?.Invoke();
        return lRule;
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
        }

        LFunnelChange?.Invoke();
    }

    public void LFunnelTargetsResolve(IReadOnlyList<Guid> lTabIds)
    {
        foreach (LFunnelRule lRule in lFunnelRules)
        {
            int lPending = lRule.LFunnelRulePending;
            LFunnelTargetSet(lRule, lPending >= 0 && lPending < lTabIds.Count ? lTabIds[lPending] : Guid.Empty);
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
}

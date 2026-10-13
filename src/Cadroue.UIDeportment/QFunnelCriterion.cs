using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed class QFunnelCriterion
{
    public event Action<QFunnelRule>? QFunnelCriterionChanged;

    public LSceneFunnelCondition QFunnelMatchRead(QFunnelRule lRule, QFunnelKind lKind) => lKind switch
    {
        QFunnelKind.QFunnelKindPrefix => lRule.QFunnelRulePrefix,
        QFunnelKind.QFunnelKindEnd => lRule.QFunnelRuleEnd,
        QFunnelKind.QFunnelKindExtension => lRule.QFunnelRuleExtension,
        _ => lRule.QFunnelRuleContains
    };

    public string QFunnelTextResolve(QFunnelRule lRule, QFunnelKind lKind, string lShown) =>
        QFunnelEchoResolve(QFunnelMatchRead(lRule, lKind).LSceneFunnelText, lShown);

    public string QFunnelRegexResolve(QFunnelRule lRule, string lShown) =>
        QFunnelEchoResolve(lRule.QFunnelRuleRegex, lShown);

    public void QFunnelTextSet(QFunnelRule lRule, QFunnelKind lKind, string lText)
    {
        LSceneFunnelCondition lMatch = QFunnelMatchRead(lRule, lKind);
        string lTrimmed = lText.Trim();
        if (lMatch.LSceneFunnelText == lTrimmed)
        {
            return;
        }

        lMatch.LSceneFunnelText = lTrimmed;
        QFunnelCriterionChanged?.Invoke(lRule);
    }

    public void QFunnelCaseSet(QFunnelRule lRule, QFunnelKind lKind, bool lCase)
    {
        LSceneFunnelCondition lMatch = QFunnelMatchRead(lRule, lKind);
        if (lMatch.LSceneFunnelCase == lCase)
        {
            return;
        }

        lMatch.LSceneFunnelCase = lCase;
        QFunnelCriterionChanged?.Invoke(lRule);
    }

    public void QFunnelCaseToggle(QFunnelRule lRule, QFunnelKind lKind) =>
        QFunnelCaseSet(lRule, lKind, !QFunnelMatchRead(lRule, lKind).LSceneFunnelCase);

    public void QFunnelJoinSet(QFunnelRule lRule, QFunnelKind lKind, bool lAnd)
    {
        LSceneFunnelCondition lMatch = QFunnelMatchRead(lRule, lKind);
        if (lMatch.LSceneFunnelJoin == lAnd)
        {
            return;
        }

        lMatch.LSceneFunnelJoin = lAnd;
        QFunnelCriterionChanged?.Invoke(lRule);
    }

    public void QFunnelRegexSet(QFunnelRule lRule, string lRegex)
    {
        string lTrimmed = lRegex.Trim();
        if (lRule.QFunnelRuleRegex == lTrimmed)
        {
            return;
        }

        lRule.QFunnelRuleRegex = lTrimmed;
        QFunnelCriterionChanged?.Invoke(lRule);
    }

    public void QFunnelWholeSet(QFunnelRule lRule, bool lWhole)
    {
        if (lRule.QFunnelRuleWhole == lWhole)
        {
            return;
        }

        lRule.QFunnelRuleWhole = lWhole;
        QFunnelCriterionChanged?.Invoke(lRule);
    }

    private static string QFunnelEchoResolve(string lStored, string lShown) =>
        lShown.Trim() == lStored ? lShown : lStored;
}

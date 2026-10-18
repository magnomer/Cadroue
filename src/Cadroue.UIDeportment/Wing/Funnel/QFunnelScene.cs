using Cadroue.Core;

namespace Cadroue.UIDeportment;

// A rule as the scene stores it, and back: a remainder rule keeps only its mark, a regex rule its pattern, a filename
// rule copies of its four conditions. The record's target index is the caller's to fill or read.
public static class QFunnelScene
{
    public static LSceneFunnelRule QFunnelSceneCreate(QFunnelRule lRule) => lRule.QFunnelRuleForm switch
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

    public static QFunnelRule QFunnelSceneParse(LSceneFunnelRule lRecord)
    {
        QFunnelForm lForm = lRecord.LSceneFunnelRemainder
            ? QFunnelForm.QFunnelFormRemainder
            : lRecord.LSceneFunnelType == (int)QFunnelForm.QFunnelFormRegex
                ? QFunnelForm.QFunnelFormRegex
                : QFunnelForm.QFunnelFormFilename;
        return new QFunnelRule(lForm)
        {
            QFunnelRuleContains = lRecord.LSceneFunnelContains.LSceneFunnelClone(),
            QFunnelRulePrefix = lRecord.LSceneFunnelPrefix.LSceneFunnelClone(),
            QFunnelRuleEnd = lRecord.LSceneFunnelEnd.LSceneFunnelClone(),
            QFunnelRuleExtension = lRecord.LSceneFunnelExtension.LSceneFunnelClone(),
            QFunnelRuleRegex = lRecord.LSceneFunnelRegex,
            QFunnelRuleWhole = lRecord.LSceneFunnelWhole,
            QFunnelRulePending = lRecord.LSceneFunnelTarget
        };
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Cadroue.Core;

public static class LClassifier
{
    public static int LClassifierRouteRead(IReadOnlyList<LSceneFunnelRule> lClassifierRules, string lClassifierName)
    {
        int lClassifierRemainder = -1;
        for (int lClassifierIndex = 0; lClassifierIndex < lClassifierRules.Count; lClassifierIndex++)
        {
            if (lClassifierRules[lClassifierIndex].LSceneFunnelRemainder)
            {
                if (lClassifierRemainder < 0)
                {
                    lClassifierRemainder = lClassifierIndex;
                }

                continue;
            }

            if (LClassifierMatch(lClassifierRules[lClassifierIndex], lClassifierName))
            {
                return lClassifierIndex;
            }
        }

        return lClassifierRemainder;
    }

    public static bool LClassifierMatch(LSceneFunnelRule lClassifierRule, string lClassifierName)
    {
        if (lClassifierRule.LSceneFunnelType == (int)LSceneFunnelForm.LSceneFunnelRegex)
        {
            if (string.IsNullOrWhiteSpace(lClassifierRule.LSceneFunnelRegex))
            {
                return false;
            }

            try
            {
                string lClassifierSubject = lClassifierRule.LSceneFunnelWhole
                    ? lClassifierName
                    : Path.GetFileNameWithoutExtension(lClassifierName);
                return Regex.IsMatch(lClassifierSubject, lClassifierRule.LSceneFunnelRegex, RegexOptions.IgnoreCase);
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        (LSceneFunnelMatch lClassifierMatch, int lClassifierKind)[] lClassifierParts =
        {
            (lClassifierRule.LSceneFunnelContains, 0),
            (lClassifierRule.LSceneFunnelPrefix, 1),
            (lClassifierRule.LSceneFunnelEnd, 2),
            (lClassifierRule.LSceneFunnelExtension, 3)
        };

        bool lClassifierHasResult = false;
        bool lClassifierResult = false;
        foreach ((LSceneFunnelMatch lClassifierMatch, int lClassifierKind) in lClassifierParts)
        {
            if (string.IsNullOrWhiteSpace(lClassifierMatch.LSceneFunnelText))
            {
                continue;
            }

            StringComparison lClassifierComparison = lClassifierMatch.LSceneFunnelCase
                ? StringComparison.Ordinal
                : StringComparison.OrdinalIgnoreCase;
            bool lClassifierCurrent = lClassifierKind switch
            {
                0 => lClassifierName.Contains(lClassifierMatch.LSceneFunnelText, lClassifierComparison),
                1 => lClassifierName.StartsWith(lClassifierMatch.LSceneFunnelText, lClassifierComparison),
                2 => lClassifierName.EndsWith(lClassifierMatch.LSceneFunnelText, lClassifierComparison),
                _ => string.Equals(Path.GetExtension(lClassifierName).TrimStart('.'),
                    lClassifierMatch.LSceneFunnelText.TrimStart('.'), lClassifierComparison)
            };

            lClassifierResult = !lClassifierHasResult
                ? lClassifierCurrent
                : lClassifierMatch.LSceneFunnelJoin
                    ? lClassifierResult && lClassifierCurrent
                    : lClassifierResult || lClassifierCurrent;
            lClassifierHasResult = true;
        }

        return lClassifierHasResult && lClassifierResult;
    }
}

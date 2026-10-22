using System;
using System.Collections.Generic;
using System.Linq;

namespace Cadroue.Conduct;

public sealed record CChoicePlan(IReadOnlyList<CChoice> CChoiceRows, int CChoiceSelectedIndex)
{
    public IReadOnlyList<CChoice> CChoiceRows { get; } = CChoiceRows.ToList().AsReadOnly();

    public static CChoicePlan CChoicePlanBuild(IReadOnlyList<CChoice> rows)
    {
        List<CChoice> answer = rows.ToList();
        return new CChoicePlan(answer, answer.FindIndex(row => row.CChoiceChosen));
    }
}

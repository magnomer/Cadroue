using System;
using System.Collections.Generic;
using System.Linq;

namespace Cadroue.Conduct;

// The preset rows a tool answers: one row per preset, the matched preset chosen, and a custom row last, chosen when
// no preset is. The custom row shows the preset it drifted from, when no preset matches and one was last chosen.
internal static class CChoicePreset
{
    private const string _cChoicePresetCustom = "Inspector.Common.Custom";
    private const string _cChoicePresetDrifted = "Inspector.Common.PresetCustom";

    // The tool hands its presets in row order, the preset its values match, the token last chosen and its wording.
    internal static IReadOnlyList<CChoice> CChoicePresetRead(
        IEnumerable<string> tokens, string? preset, string? token, Func<string, string> keyRead)
    {
        ArgumentNullException.ThrowIfNull(tokens);
        ArgumentNullException.ThrowIfNull(keyRead);
        List<CChoice> rows = tokens
            .Select(row => new CChoice(row, keyRead(row), null, row == preset))
            .ToList();
        bool drifted = preset is null && token is not null;
        rows.Add(new CChoice(
            null,
            drifted ? _cChoicePresetDrifted : _cChoicePresetCustom,
            drifted ? keyRead(token!) : null,
            !rows.Any(row => row.CChoiceChosen)));
        return rows;
    }
}

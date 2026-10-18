using System.Windows.Controls;
using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.UIVeneer.PWing;

internal static class PSEncoderValue
{
    internal static string PSEncoderTrimRead(TextBox? pBox) => pBox?.Text.Trim() ?? string.Empty;

    internal static Dictionary<string, string> PSEncoderExtrasRead(Dictionary<string, ComboBox> pCombos) =>
        pCombos.ToDictionary(
            pExtra => pExtra.Key, pExtra => PSCombo.PSComboTextRead(pExtra.Value), StringComparer.Ordinal);

    internal static LLocalizationChoice[] PSEncoderChoicesRead(IReadOnlyList<LCapabilityChoice> pChoices) =>
        pChoices
            .Select(pChoice => new LLocalizationChoice(
                pChoice.LCapabilityChoiceValue,
                string.Empty,
                pChoice.LCapabilityChoiceLabel))
            .ToArray();
}

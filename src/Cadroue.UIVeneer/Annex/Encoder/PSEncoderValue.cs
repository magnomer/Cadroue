using System.Windows.Controls;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal static class PSEncoderValue
{
    internal static string PSEncoderTrimRead(TextBox? pBox) => pBox?.Text.Trim() ?? string.Empty;

    internal static Dictionary<string, string> PSEncoderExtrasRead(Dictionary<string, ComboBox> pCombos) =>
        pCombos.ToDictionary(
            pExtra => pExtra.Key,
            pExtra => QSEncoderChoice.QSEncoderChoiceRead(pExtra.Value.SelectedItem),
            StringComparer.Ordinal);

}

using System.Windows.Media;

namespace Cadroue.UIVeneer.PWing;

internal static class PInspectorLook
{
    internal static readonly FontFamily PInspectorFontFamily = new("Segoe UI");
    internal static readonly Brush PInspectorMutedBrush = new SolidColorBrush(Color.FromRgb(0x8A, 0x93, 0x9E));
    internal static readonly Brush PInspectorIconBrush = new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x3D));

    private static readonly Brush pInspectorAccentBrush = new SolidColorBrush(Color.FromRgb(0x2C, 0x6C, 0xCE));

    internal static readonly IReadOnlyDictionary<bool, Brush> PInspectorPickBrush = new Dictionary<bool, Brush>
    {
        [true] = pInspectorAccentBrush,
        [false] = PInspectorIconBrush
    };

    internal const double PInspectorLabelWidth = 80;
    internal const double PInspectorFieldHeight = 26;
    internal const double PInspectorRowHeight = 34;
}

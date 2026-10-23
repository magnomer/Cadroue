using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Cadroue.UIVeneer;

internal static class PSField
{
    internal const double PSFieldFontSize = 12;
    internal const double PSFieldControlHeight = 32;
    internal const double PSFieldChipHeight = 26;

    internal static readonly Brush PSFieldLine = PSFieldBrushCreate(0xD9, 0xDE, 0xE7);
    internal static readonly Brush PSFieldText = PSFieldBrushCreate(0x1D, 0x2A, 0x3D);
    internal static readonly Brush PSFieldMuted = PSFieldBrushCreate(0x62, 0x6F, 0x83);
    internal static readonly Brush PSFieldAccent = PSFieldBrushCreate(0x4C, 0x86, 0xF7);
    internal static readonly Brush PSFieldInactive = PSFieldBrushCreate(0xF0, 0xF3, 0xFA);
    private static Brush PSFieldBrushCreate(byte pRed, byte pGreen, byte pBlue)
    {
        var pBrush = new SolidColorBrush(Color.FromRgb(pRed, pGreen, pBlue));
        pBrush.Freeze();
        return pBrush;
    }

    internal static TextBlock PSFieldLabelBuild(string pText) => new()
    {
        Text = pText,
        Foreground = PSFieldMuted,
        VerticalAlignment = VerticalAlignment.Center
    };
}

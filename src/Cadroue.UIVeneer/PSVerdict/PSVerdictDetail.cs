using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal static class PSVerdictDetail
{
    private static readonly Brush PSVerdictDetailFill = PSVerdictBrushCreate(0xF1, 0xF5, 0xFA);
    private static readonly FontFamily PSVerdictDetailFont = new("Consolas, Cascadia Mono, monospace");

    internal static Brush PSVerdictBrushCreate(byte pRed, byte pGreen, byte pBlue)
    {
        var pBrush = new SolidColorBrush(Color.FromRgb(pRed, pGreen, pBlue));
        pBrush.Freeze();
        return pBrush;
    }

    internal static Border PSVerdictDetailBuild(QSVerdictRow pEntry) => new()
    {
        Background = PSVerdictDetailFill,
        CornerRadius = new CornerRadius(4),
        Padding = new Thickness(10, 6, 10, 6),
        Visibility = Visibility.Collapsed,
        Child = new TextBlock
        {
            Text = pEntry.QSVerdictMessage,
            Foreground = PSField.PSFieldMuted,
            FontFamily = PSVerdictDetailFont,
            TextWrapping = TextWrapping.Wrap
        }
    };

    internal static void PSVerdictChipAttach(Button pChip, Border pDetail) =>
        pChip.Click += (_, _) => PSVerdictDetailToggle(pDetail, pChip);

    internal static Button PSVerdictChipBuild(bool pShown) => QButton.QButtonBuild(
        new()
        {
            Content = PSVerdictChipFormat(pShown),
            Height = PSField.PSFieldChipHeight,
            Padding = new Thickness(10, 0, 10, 0),
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 11
        },
        QButtonLook.QButtonLookWhite);

    private static void PSVerdictDetailToggle(Border pDetail, Button pChip)
    {
        bool pShown = PSVerdictDetailCheck(pDetail);
        PSVerdictDetailApply(pDetail, !pShown);
        PSVerdictChipShow(pChip, PSVerdictChipFormat(!pShown));
    }

    private static bool PSVerdictDetailCheck(Border pDetail) => pDetail.Visibility == Visibility.Visible;

    private static void PSVerdictDetailApply(Border pDetail, bool pShown) =>
        pDetail.Visibility = pShown ? Visibility.Visible : Visibility.Collapsed;

    private static void PSVerdictChipShow(Button pChip, string pText) => pChip.Content = pText;

    private static string PSVerdictChipFormat(bool pShown) =>
        QLocalizationCatalog.QLocalizationTextRead("Encoder.Verification.Details") + (pShown ? "  ▴" : "  ▾");
}

using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

using static Cadroue.UIShell.PSShared.PSField;

namespace Cadroue.UIShell.PPanels;

internal sealed partial class PSEncoder
{
    private const string PSEncoderChipSeparator = " / ";

    private static readonly Brush PSEncoderChipFill = PSEncoderChipCreate(0xEC, 0xF1, 0xFD);
    private static readonly Brush PSEncoderChipText = PSEncoderChipCreate(0x2F, 0x62, 0xD6);

    private static readonly PSEncoderChipConverter psEncoderChipConverter = new();
    private static readonly DataTemplate psEncoderChipTemplate = PSEncoderChipBuild();

    private static Brush PSEncoderChipCreate(byte pRed, byte pGreen, byte pBlue)
    {
        var pBrush = new SolidColorBrush(Color.FromRgb(pRed, pGreen, pBlue));
        pBrush.Freeze();
        return pBrush;
    }

    private static DataTemplate PSEncoderChipBuild()
    {
        var pHead = new FrameworkElementFactory(typeof(TextBlock));
        pHead.SetBinding(TextBlock.TextProperty, PSEncoderChipResolve("head"));
        pHead.SetValue(TextBlock.ForegroundProperty, PSFieldText);
        pHead.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        pHead.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);

        var pToken = new FrameworkElementFactory(typeof(TextBlock));
        pToken.SetBinding(TextBlock.TextProperty, PSEncoderChipResolve("token"));
        pToken.SetValue(TextBlock.ForegroundProperty, PSEncoderChipText);
        pToken.SetValue(TextBlock.FontSizeProperty, PSFieldFontSize - 1);
        pToken.SetValue(TextBlock.FontWeightProperty, FontWeights.Medium);
        pToken.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);

        var pChip = new FrameworkElementFactory(typeof(Border));
        pChip.SetValue(Border.BackgroundProperty, PSEncoderChipFill);
        pChip.SetValue(Border.CornerRadiusProperty, new CornerRadius(9));
        pChip.SetValue(Border.PaddingProperty, new Thickness(9, 2, 9, 2));
        pChip.SetValue(FrameworkElement.MarginProperty, new Thickness(12, 0, 0, 0));
        pChip.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        pChip.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Right);
        pChip.SetBinding(UIElement.VisibilityProperty, PSEncoderChipResolve("chip"));
        pChip.SetValue(DockPanel.DockProperty, Dock.Right);
        pChip.AppendChild(pToken);

        var pRow = new FrameworkElementFactory(typeof(DockPanel));
        pRow.SetValue(DockPanel.LastChildFillProperty, true);
        pRow.AppendChild(pChip);
        pRow.AppendChild(pHead);

        return new DataTemplate { VisualTree = pRow };
    }

    private static Binding PSEncoderChipResolve(string pPart) =>
        new() { Converter = psEncoderChipConverter, ConverterParameter = pPart };

    private sealed class PSEncoderChipConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string pFull = value as string ?? string.Empty;
            int pCut = pFull.LastIndexOf(PSEncoderChipSeparator, StringComparison.Ordinal);
            bool pHasToken = pCut >= 0;

            return (parameter as string) switch
            {
                "token" => pHasToken ? pFull[(pCut + PSEncoderChipSeparator.Length)..] : string.Empty,
                "chip" => pHasToken ? Visibility.Visible : Visibility.Collapsed,
                _ => pHasToken ? pFull[..pCut] : pFull
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            Binding.DoNothing;
    }
}

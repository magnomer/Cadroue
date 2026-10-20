using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace Cadroue.UIDeportment;

public static class QSEncoderChip
{
    private const string QSEncoderChipSeparator = " / ";
    private static readonly Brush QSEncoderChipFill = QSEncoderChipCreate(0xEC, 0xF1, 0xFD);
    private static readonly Brush QSEncoderChipText = QSEncoderChipCreate(0x2F, 0x62, 0xD6);
    private static readonly QSEncoderChipConverter qsEncoderChipConverter = new();
    private static readonly DataTemplate qsEncoderChipTemplate = QSEncoderChipBuild();

    public static void QSEncoderChipApply(ComboBox lCombo)
    {
        lCombo.ItemTemplate = qsEncoderChipTemplate;
        lCombo.HorizontalContentAlignment = HorizontalAlignment.Stretch;
    }

    private static Brush QSEncoderChipCreate(byte lRed, byte lGreen, byte lBlue)
    {
        var lBrush = new SolidColorBrush(Color.FromRgb(lRed, lGreen, lBlue));
        lBrush.Freeze();
        return lBrush;
    }

    private static DataTemplate QSEncoderChipBuild()
    {
        var lHead = new FrameworkElementFactory(typeof(TextBlock));
        lHead.SetBinding(TextBlock.TextProperty, QSEncoderChipResolve("head"));
        lHead.SetValue(TextBlock.ForegroundProperty, Brushes.DarkSlateGray);
        lHead.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        lHead.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);
        var lToken = new FrameworkElementFactory(typeof(TextBlock));
        lToken.SetBinding(TextBlock.TextProperty, QSEncoderChipResolve("token"));
        lToken.SetValue(TextBlock.ForegroundProperty, QSEncoderChipText);
        lToken.SetValue(TextBlock.FontSizeProperty, 11d);
        lToken.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        var lChip = new FrameworkElementFactory(typeof(Border));
        lChip.SetValue(Border.BackgroundProperty, QSEncoderChipFill);
        lChip.SetValue(Border.CornerRadiusProperty, new CornerRadius(9));
        lChip.SetValue(Border.PaddingProperty, new Thickness(9, 2, 9, 2));
        lChip.SetValue(FrameworkElement.MarginProperty, new Thickness(12, 0, 0, 0));
        lChip.SetBinding(UIElement.VisibilityProperty, QSEncoderChipResolve("chip"));
        lChip.SetValue(DockPanel.DockProperty, Dock.Right);
        lChip.AppendChild(lToken);
        var lRow = new FrameworkElementFactory(typeof(DockPanel));
        lRow.AppendChild(lChip);
        lRow.AppendChild(lHead);
        return new DataTemplate { VisualTree = lRow };
    }

    private static Binding QSEncoderChipResolve(string lPart) =>
        new() { Converter = qsEncoderChipConverter, ConverterParameter = lPart };

    private sealed class QSEncoderChipConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string lFull = value as string ?? string.Empty;
            int lCut = lFull.LastIndexOf(QSEncoderChipSeparator, StringComparison.Ordinal);
            bool lHasToken = lCut >= 0;
            return (parameter as string) switch
            {
                "token" => lHasToken ? lFull[(lCut + QSEncoderChipSeparator.Length)..] : string.Empty,
                "chip" => lHasToken ? Visibility.Visible : Visibility.Collapsed,
                _ => lHasToken ? lFull[..lCut] : lFull
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            Binding.DoNothing;
    }
}

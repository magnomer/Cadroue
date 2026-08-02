using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using Cadroue.UIShell.PMainWindow;

using Cadroue.Infrastructure;

namespace Cadroue.UIShell;

internal sealed class PLogRow : INotifyPropertyChanged
{
    internal const double PLogTimeWidth = 84;
    internal const double PLogBadgeWidth = 78;
    internal const double PLogRowHeight = 26;
    internal const double PLogChipHeight = 22;

    private bool pLogRowExpanded;

    internal PLogRow(LTraceEntry pLogEntry)
    {
        PLogRowTime = pLogEntry.LTraceEntryTime;
        PLogRowKind = LTraceEntry.LTraceKindRead(pLogEntry.LTraceEntryKind);
        PLogRowSummary = pLogEntry.LTraceEntrySummary;
        PLogRowDetail = pLogEntry.LTraceEntryDetail ?? string.Empty;
        PLogRowDetailed = pLogEntry.LTraceEntryDetailed;
        PLogRowSpan = pLogEntry.LTraceEntrySpan is double pLogSpan
            ? LTraceEntry.LTraceSpanFormat(pLogSpan)
            : string.Empty;

        (Brush pLogFill, Brush pLogInk) = PLogBadgeRead(pLogEntry.LTraceEntryKind);
        PLogBadgeFill = pLogFill;
        PLogBadgeText = pLogInk;
        PLogRowCategory = pLogEntry.LTraceEntryKind;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string PLogRowTime { get; }

    public string PLogRowKind { get; }

    public string PLogRowSummary { get; }

    public string PLogRowDetail { get; }

    public string PLogRowSpan { get; }

    public bool PLogRowDetailed { get; }

    public Brush PLogBadgeFill { get; }

    public Brush PLogBadgeText { get; }

    internal LTraceKind PLogRowCategory { get; }

    public bool PLogRowExpanded
    {
        get => pLogRowExpanded;
        set
        {
            if (pLogRowExpanded == value)
            {
                return;
            }

            pLogRowExpanded = value;
            PLogChangeRaise(nameof(PLogRowExpanded));
            PLogChangeRaise(nameof(PLogDetailShown));
            PLogChangeRaise(nameof(PLogChipText));
        }
    }

    public Visibility PLogDetailShown => pLogRowExpanded ? Visibility.Visible : Visibility.Collapsed;

    public Visibility PLogChipShown => PLogRowDetailed ? Visibility.Visible : Visibility.Collapsed;

    public string PLogChipText =>
        LLocalization.LLocalizationTextRead("Log.Button.Details") + (pLogRowExpanded ? "  ▴" : "  ▾");

    internal static void PLogRowApply(ListBox pLogFeed)
    {
        pLogFeed.ItemTemplate = PLogTemplateBuild();
        pLogFeed.ItemContainerStyle = PLogStyleBuild();
        pLogFeed.Resources["PLogChipStyle"] = PLogChipBuild();
    }

    internal static (Brush Fill, Brush Ink) PLogBadgeRead(LTraceKind pLogKind) => pLogKind switch
    {
        LTraceKind.LTraceWarning => (PLogBrushCreate(0xFD, 0xF3, 0xDA), PLogBrushCreate(0x8A, 0x60, 0x0A)),
        LTraceKind.LTraceError => (PLogBrushCreate(0xFB, 0xE3, 0xE3), PLogBrushCreate(0x8C, 0x1D, 0x1D)),
        LTraceKind.LTraceDraw => (PLogBrushCreate(0xE8, 0xF1, 0xE7), PLogBrushCreate(0x2E, 0x5B, 0x2B)),
        LTraceKind.LTraceView => (PLogBrushCreate(0xED, 0xE8, 0xF7), PLogBrushCreate(0x46, 0x30, 0x8A)),
        LTraceKind.LTraceWork => (PLogBrushCreate(0xE3, 0xF0, 0xFB), PLogBrushCreate(0x14, 0x52, 0x7E)),
        LTraceKind.LTraceFfmpeg => (PLogBrushCreate(0xFD, 0xF0, 0xE1), PLogBrushCreate(0x8A, 0x4B, 0x0A)),
        _ => (PLogBrushCreate(0xE7, 0xEE, 0xF9), PLogBrushCreate(0x2B, 0x34, 0x43))
    };

    private static Style PLogChipBuild()
    {
        var pLogChipStyle = new Style(typeof(Button), PButton.PButtonWhiteCreate());
        pLogChipStyle.Setters.Add(new Setter(FrameworkElement.HeightProperty, PLogChipHeight));
        pLogChipStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(10, 0, 10, 0)));
        pLogChipStyle.Setters.Add(new Setter(Control.FontSizeProperty, 11d));
        pLogChipStyle.Seal();
        return pLogChipStyle;
    }

    private static Style PLogStyleBuild()
    {
        const string pLogStyleXaml = @"
<Style xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
       xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
       TargetType=""{x:Type ListBoxItem}"">
    <Setter Property=""HorizontalContentAlignment"" Value=""Stretch"" />
    <Setter Property=""FocusVisualStyle"" Value=""{x:Null}"" />
    <Setter Property=""Template"">
        <Setter.Value>
            <ControlTemplate TargetType=""{x:Type ListBoxItem}"">
                <Border x:Name=""pLogRowFrame""
                        Background=""Transparent""
                        Padding=""8,1,8,1""
                        CornerRadius=""5"">
                    <ContentPresenter />
                </Border>
                <ControlTemplate.Triggers>
                    <Trigger Property=""IsMouseOver"" Value=""True"">
                        <Setter TargetName=""pLogRowFrame"" Property=""Background"" Value=""#F4F7FB"" />
                    </Trigger>
                </ControlTemplate.Triggers>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>";
        return (Style)XamlReader.Parse(pLogStyleXaml);
    }

    private static DataTemplate PLogTemplateBuild()
    {
        const string pLogRowXaml = @"
<DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
              xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <StackPanel Margin=""0,2,0,2"">
        <Grid MinHeight=""$RowHeight$"">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width=""$TimeWidth$"" />
                <ColumnDefinition Width=""$BadgeWidth$"" />
                <ColumnDefinition Width=""*"" />
                <ColumnDefinition Width=""Auto"" />
                <ColumnDefinition Width=""Auto"" />
            </Grid.ColumnDefinitions>
            <TextBlock Text=""{Binding PLogRowTime}""
                       Foreground=""#626F83""
                       VerticalAlignment=""Center"" />
            <Border Grid.Column=""1""
                    Background=""{Binding PLogBadgeFill}""
                    CornerRadius=""5""
                    Padding=""8,2,8,2""
                    HorizontalAlignment=""Left""
                    VerticalAlignment=""Center"">
                <TextBlock Text=""{Binding PLogRowKind}""
                           Foreground=""{Binding PLogBadgeText}""
                           FontSize=""11""
                           FontWeight=""SemiBold"" />
            </Border>
            <TextBlock Grid.Column=""2""
                       Text=""{Binding PLogRowSummary}""
                       Foreground=""#1D2A3D""
                       TextWrapping=""Wrap""
                       VerticalAlignment=""Center""
                       Margin=""4,0,10,0"" />
            <TextBlock Grid.Column=""3""
                       Text=""{Binding PLogRowSpan}""
                       Foreground=""#626F83""
                       VerticalAlignment=""Center""
                       Margin=""0,0,10,0"" />
            <Button Grid.Column=""4""
                    Content=""{Binding PLogChipText}""
                    Visibility=""{Binding PLogChipShown}""
                    VerticalAlignment=""Center""
                    Style=""{DynamicResource PLogChipStyle}"" />
        </Grid>
        <Border Visibility=""{Binding PLogDetailShown}""
                Background=""#F4F7FB""
                BorderBrush=""#D9DEE7""
                BorderThickness=""1""
                CornerRadius=""6""
                Margin=""$DetailIndent$,6,0,8""
                Padding=""12,9,12,9"">
            <TextBox Text=""{Binding PLogRowDetail, Mode=OneWay}""
                     IsReadOnly=""True""
                     BorderThickness=""0""
                     Background=""Transparent""
                     Foreground=""#1D2A3D""
                     FontFamily=""Consolas""
                     FontSize=""12""
                     TextWrapping=""NoWrap""
                     HorizontalScrollBarVisibility=""Auto"" />
        </Border>
    </StackPanel>
</DataTemplate>";

        return (DataTemplate)XamlReader.Parse(pLogRowXaml
            .Replace("$RowHeight$", PLogNumberFormat(PLogRowHeight))
            .Replace("$TimeWidth$", PLogNumberFormat(PLogTimeWidth))
            .Replace("$BadgeWidth$", PLogNumberFormat(PLogBadgeWidth))
            .Replace("$DetailIndent$", PLogNumberFormat(PLogTimeWidth + PLogBadgeWidth)));
    }

    private void PLogChangeRaise(string pLogPropertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(pLogPropertyName));

    private static string PLogNumberFormat(double pLogValue) =>
        pLogValue.ToString(CultureInfo.InvariantCulture);

    private static Brush PLogBrushCreate(byte pLogRed, byte pLogGreen, byte pLogBlue)
    {
        var pLogBrush = new SolidColorBrush(Color.FromRgb(pLogRed, pLogGreen, pLogBlue));
        pLogBrush.Freeze();
        return pLogBrush;
    }
}

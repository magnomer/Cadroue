using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal static class PLogTemplate
{
    internal const double PLogRowHeight = 26;
    private const double PLogTimeWidth = 84;
    private const double PLogBadgeWidth = 78;
    private const double PLogDetailIndent = 162;

    internal static void PLogRowApply(ListBox pLogFeed)
    {
        pLogFeed.ItemTemplate = PLogTemplateBuild();
        pLogFeed.ItemContainerStyle = PLogStyleBuild();
        QButton.QButtonChipAttach(pLogFeed);
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
                    Style=""{DynamicResource PButtonChip}"" />
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
            .Replace("$DetailIndent$", PLogNumberFormat(PLogDetailIndent)));
    }

    private static string PLogNumberFormat(double pLogValue) =>
        pLogValue.ToString(CultureInfo.InvariantCulture);
}

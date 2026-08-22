using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using Cadroue.UIShell.PAssets;

namespace Cadroue.UIShell.PSShared;

internal static class PSSheet
{
    internal const double PSSheetFontSize = 13;
    internal const double PSSheetIconSize = 17;
    internal const double PSSheetIconGap = 10;

    private static readonly Brush psSheetIconBrush = new SolidColorBrush(Color.FromRgb(0x2B, 0x34, 0x43));

    internal static TabControl PSSheetControlBuild(double pTabWidth, params TabItem[] pSheets)
    {
        var pTabs = new TabControl
        {
            Background = Brushes.White,
            BorderBrush = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Template = PSSheetTemplateBuild(),
            ItemContainerStyle = PSSheetStyleBuild(pTabWidth)
        };

        foreach (TabItem pSheet in pSheets)
        {
            pTabs.Items.Add(pSheet);
        }

        pTabs.SelectionChanged += (_, _) => PSSheetSeparatorUpdate(pTabs);
        PSSheetSeparatorUpdate(pTabs);
        return pTabs;
    }

    internal static void PSSheetSeparatorUpdate(TabControl pTabs)
    {
        int pSelectedIndex = pTabs.SelectedIndex;
        for (int pIndex = 0; pIndex < pTabs.Items.Count; pIndex++)
        {
            if (pTabs.Items[pIndex] is TabItem pTabItem)
            {
                pTabItem.Tag = pIndex < pTabs.Items.Count - 1
                    && pIndex != pSelectedIndex
                    && pIndex != pSelectedIndex - 1;
            }
        }
    }

    internal static TabItem PSSheetBuild(string pTitle, string? pIconPath, UIElement pContent) =>
        new() { Header = PSSheetHeaderBuild(pTitle, pIconPath), Content = pContent };

    internal static UIElement PSSheetHeaderBuild(string pTitle, string? pIconPath)
    {
        var pHeader = new StackPanel { Orientation = Orientation.Horizontal };
        if (!string.IsNullOrWhiteSpace(pIconPath))
        {
            pHeader.Children.Add(new Image
            {
                Source = PIcon.PIconRead(pIconPath, psSheetIconBrush),
                Width = PSSheetIconSize,
                Height = PSSheetIconSize,
                Stretch = Stretch.Uniform,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, PSSheetIconGap, 0)
            });
        }

        pHeader.Children.Add(new TextBlock { Text = pTitle, VerticalAlignment = VerticalAlignment.Center });
        return pHeader;
    }

    internal static ScrollViewer PSSheetScrollBuild(UIElement pContent) => new()
    {
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        Content = pContent
    };

    private static ControlTemplate PSSheetTemplateBuild()
    {
        const string pXaml = @"
<ControlTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
                 TargetType=""{x:Type TabControl}"">
    <Grid Background=""#DCE8F7"" ClipToBounds=""False"">
        <Grid.RowDefinitions>
            <RowDefinition Height=""$BandHeight$"" />
            <RowDefinition Height=""*"" />
        </Grid.RowDefinitions>
        <Border Grid.Row=""0"" Background=""#EAF2FC"" />
        <TabPanel Grid.Row=""0""
                  IsItemsHost=""True""
                  Margin=""$LeadColumn$,12,$ButtonStrip$,0""
                  Panel.ZIndex=""2""
                  Background=""Transparent""
                  ClipToBounds=""False"" />
        <ContentPresenter x:Name=""PART_SelectedContentHost""
                          Grid.Row=""1""
                          Margin=""0,-$ContentOverlap$,0,0""
                          Panel.ZIndex=""0""
                          ContentSource=""SelectedContent"" />
    </Grid>
</ControlTemplate>";
        return (ControlTemplate)XamlReader.Parse(pXaml
            .Replace("$BandHeight$", PSSheetNumberFormat(PSCasement.PSCasementBandHeight))
            .Replace("$LeadColumn$", PSSheetNumberFormat(PSCasement.PSCasementLeadColumn))
            .Replace("$ButtonStrip$", PSSheetNumberFormat(PSCasement.PSCasementButtonStrip))
            .Replace("$ContentOverlap$", PSSheetNumberFormat(PSCasement.PSCasementContentOverlap)));
    }

    private static Style PSSheetStyleBuild(double pTabWidth)
    {
        const string pXaml = @"
<Style xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
       xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
       TargetType=""{x:Type TabItem}"">
    <Setter Property=""Height"" Value=""42"" />
    <Setter Property=""MinWidth"" Value=""$TabWidth$"" />
    <Setter Property=""Padding"" Value=""11,0,8,0"" />
    <Setter Property=""FocusVisualStyle"" Value=""{x:Null}"" />
    <Setter Property=""Template"">
        <Setter.Value>
            <ControlTemplate TargetType=""{x:Type TabItem}"">
                <Grid ClipToBounds=""False"">
                    <Border x:Name=""pTabFrame""
                            Height=""42""
                            MinWidth=""$TabWidth$""
                            Padding=""11,0,8,0""
                            Cursor=""Hand""
                            Background=""Transparent""
                            BorderBrush=""Transparent""
                            BorderThickness=""1""
                            CornerRadius=""9,9,0,0"">
                        <Grid>
                            <Grid.RowDefinitions>
                                <RowDefinition Height=""9"" />
                                <RowDefinition Height=""22"" />
                                <RowDefinition Height=""11"" />
                            </Grid.RowDefinitions>
                            <ContentPresenter x:Name=""pTabHeader""
                                              Grid.Row=""1""
                                              ContentSource=""Header""
                                              TextElement.Foreground=""#2B3443""
                                              TextElement.FontSize=""$TabFontSize$""
                                              VerticalAlignment=""Top""
                                              HorizontalAlignment=""Left""
                                              Margin=""0,1,0,0"" />
                        </Grid>
                    </Border>
                    <Border x:Name=""pTabSeparator""
                            Width=""1""
                            Height=""16""
                            HorizontalAlignment=""Right""
                            VerticalAlignment=""Center""
                            Margin=""0,0,1,0""
                            Background=""#C9D6E5""
                            IsHitTestVisible=""False""
                            Visibility=""Collapsed"" />
                </Grid>
                <ControlTemplate.Triggers>
                    <Trigger Property=""IsMouseOver"" Value=""True"">
                        <Setter TargetName=""pTabFrame"" Property=""Background"" Value=""#EEF4FC"" />
                        <Setter TargetName=""pTabFrame"" Property=""BorderBrush"" Value=""#D5E0ED"" />
                    </Trigger>
                    <Trigger Property=""IsSelected"" Value=""True"">
                        <Setter TargetName=""pTabFrame"" Property=""Background"" Value=""#FFFFFF"" />
                        <Setter TargetName=""pTabFrame"" Property=""BorderBrush"" Value=""#C9D6E5"" />
                        <Setter TargetName=""pTabFrame"" Property=""BorderThickness"" Value=""1,1,1,0"" />
                        <Setter TargetName=""pTabHeader"" Property=""TextElement.Foreground"" Value=""#111827"" />
                        <Setter TargetName=""pTabHeader"" Property=""TextElement.FontWeight"" Value=""SemiBold"" />
                    </Trigger>
                    <DataTrigger Binding=""{Binding Tag, RelativeSource={RelativeSource Self}}"" Value=""True"">
                        <Setter TargetName=""pTabSeparator"" Property=""Visibility"" Value=""Visible"" />
                    </DataTrigger>
                </ControlTemplate.Triggers>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>";
        return (Style)XamlReader.Parse(pXaml
            .Replace("$TabWidth$", PSSheetNumberFormat(pTabWidth))
            .Replace("$TabFontSize$", PSSheetNumberFormat(PSSheetFontSize)));
    }

    private static string PSSheetNumberFormat(double pValue) => pValue.ToString(CultureInfo.InvariantCulture);
}

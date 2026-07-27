using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using Cadroue.UIShell.PAssets;

namespace Cadroue.UIShell.PPanels;

internal sealed partial class PSEncoder
{
    private const double PSCasementCrestFontSize = 16;

    private const double PSSheetTabFontSize = 13;

    private const double PSCasementBandHeight = 56;

    private const double PSCasementContentOverlap = 2;

    private const double PSCasementButtonHeight = PSCasementBandHeight - PSCasementContentOverlap;

    private const double PSSheetTabWidth = 142;
    private const int PSSheetTabCount = 3;

    private const string PSSheetOutputIconPath = "/PAssets/PTabs/PSSheetOutput.svg";
    private const string PSSheetVideoIconPath = "/PAssets/PTabs/PSSheetVideo.svg";
    private const string PSSheetAudioIconPath = "/PAssets/PTabs/PSSheetAudio.svg";

    private const double PSSheetTabIconSize = 17;

    private const double PSSheetTabIconGap = 10;

    private static readonly Brush psSheetIconBrush = new SolidColorBrush(Color.FromRgb(0x2B, 0x34, 0x43));

    private const double PSSheetStripWidth = PSSheetTabWidth * PSSheetTabCount;

    private UIElement PSCasementOverlayBuild()
    {
        var pGrid = new Grid { Height = PSCasementBandHeight, VerticalAlignment = VerticalAlignment.Top };
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(56) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pGrid.Children.Add(PSCrestBuild());

        var pDragArea = new Border
        {
            Background = Brushes.Transparent,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(PSSheetStripWidth, 0, 0, 0)
        };
        pDragArea.MouseLeftButtonDown += PSCasementDragHandle;
        Grid.SetColumn(pDragArea, 1);
        pGrid.Children.Add(pDragArea);

        var pButtons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Top,
            Background = new SolidColorBrush(Color.FromRgb(0xEA, 0xF2, 0xFC))
        };
        pButtons.Children.Add(PSCasementButtonBuild(PSCasementMinimizeBuild(), (_, _) => WindowState = WindowState.Minimized));
        pButtons.Children.Add(PSCasementButtonBuild(PSCasementMaximizeBuild(), (_, _) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized));
        pButtons.Children.Add(PSCasementButtonBuild(PSCasementCloseBuild(), (_, _) => Close(), pClose: true));
        Grid.SetColumn(pButtons, 2);
        pGrid.Children.Add(pButtons);
        return pGrid;
    }

    private UIElement PSCrestBuild()
    {
        var pLogo = new Border
        {
            Width = 34,
            Height = 34,
            Background = new SolidColorBrush(Color.FromRgb(0x2F, 0x7C, 0xE8)),
            CornerRadius = new CornerRadius(8),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text = "E",
                Foreground = Brushes.White,
                FontSize = PSCasementCrestFontSize,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                IsHitTestVisible = false
            }
        };
        pLogo.MouseLeftButtonDown += PSCasementDragHandle;
        return pLogo;
    }

    private void PSCasementDragHandle(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left || e.ClickCount > 1)
        {
            return;
        }
        DragMove();
    }

    private UIElement PSSheetControlBuild()
    {
        var pTabs = new TabControl
        {
            Background = Brushes.White,
            BorderBrush = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Template = PSSheetTemplateBuild(),
            ItemContainerStyle = PSSheetStyleBuild()
        };
        pTabs.Items.Add(PSSheetBuild("Output", PSSheetOutputIconPath, PSEncoderRootBuild(PSSheetScrollBuild(PSOutputBuild()))));
        pTabs.Items.Add(PSSheetBuild("Video", PSSheetVideoIconPath, PSEncoderRootBuild(PSSheetScrollBuild(PSVideoBuild()))));
        pTabs.Items.Add(PSSheetBuild("Audio", PSSheetAudioIconPath, PSEncoderRootBuild(PSSheetScrollBuild(PSAudioBuild()))));
        pTabs.SelectionChanged += (_, _) => PSSheetSeparatorUpdate(pTabs);
        PSSheetSeparatorUpdate(pTabs);
        return pTabs;
    }

    private static void PSSheetSeparatorUpdate(TabControl pTabs)
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

    private static TabItem PSSheetBuild(string pTitle, string pIconPath, UIElement pContent) =>
        new() { Header = PSSheetHeaderBuild(pTitle, pIconPath), Content = pContent };

    private static UIElement PSSheetHeaderBuild(string pTitle, string pIconPath)
    {
        var pHeader = new StackPanel { Orientation = Orientation.Horizontal };
        pHeader.Children.Add(new Image
        {
            Source = PIcon.PIconRead(pIconPath, psSheetIconBrush),
            Width = PSSheetTabIconSize,
            Height = PSSheetTabIconSize,
            Stretch = Stretch.Uniform,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, PSSheetTabIconGap, 0)
        });
        pHeader.Children.Add(new TextBlock { Text = pTitle, VerticalAlignment = VerticalAlignment.Center });
        return pHeader;
    }

    private static ScrollViewer PSSheetScrollBuild(UIElement pContent) => new()
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
                  Margin=""56,12,144,0""
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
            .Replace("$BandHeight$", PSCasementBandHeight.ToString(System.Globalization.CultureInfo.InvariantCulture))
            .Replace("$ContentOverlap$", PSCasementContentOverlap.ToString(System.Globalization.CultureInfo.InvariantCulture)));
    }

    private static Style PSSheetStyleBuild()
    {
        const string pXaml = @"
<Style xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
       xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
       TargetType=""{x:Type TabItem}"">
    <Setter Property=""Height"" Value=""42"" />
    <Setter Property=""MinWidth"" Value=""142"" />
    <Setter Property=""Padding"" Value=""11,0,8,0"" />
    <Setter Property=""Cursor"" Value=""Hand"" />
    <Setter Property=""FocusVisualStyle"" Value=""{x:Null}"" />
    <Setter Property=""Template"">
        <Setter.Value>
            <ControlTemplate TargetType=""{x:Type TabItem}"">
                <Grid ClipToBounds=""False"">
                    <Border x:Name=""pTabFrame""
                            Height=""42""
                            MinWidth=""142""
                            Padding=""11,0,8,0""
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
        return (Style)XamlReader.Parse(
            pXaml.Replace("$TabFontSize$", PSSheetTabFontSize.ToString(System.Globalization.CultureInfo.InvariantCulture)));
    }

    private static Button PSCasementButtonBuild(UIElement pIcon, RoutedEventHandler pClick, bool pClose = false)
    {
        var pButton = new Button
        {
            Width = 48,
            Height = PSCasementButtonHeight,
            Content = pIcon,
            Style = PMainWindow.PButton.PButtonChromeCreate(pClose)
        };
        pButton.Click += pClick;
        return pButton;
    }

    private static Canvas PSCasementMinimizeBuild()
    {
        var pCanvas = new Canvas { Width = 18, Height = 16 };
        pCanvas.Children.Add(PSRuleBuild(2, 12, 14, 12));
        return pCanvas;
    }

    private static Canvas PSCasementMaximizeBuild()
    {
        var pCanvas = new Canvas { Width = 18, Height = 16 };
        var pSquare = new System.Windows.Shapes.Rectangle
        {
            Width = 12,
            Height = 12,
            StrokeThickness = 1.2,
            Fill = Brushes.Transparent,
            Margin = new Thickness(2, 2, 0, 0)
        };
        PSCasementGlyphBind(pSquare);
        pCanvas.Children.Add(pSquare);
        return pCanvas;
    }

    private static Canvas PSCasementCloseBuild()
    {
        var pCanvas = new Canvas { Width = 18, Height = 16 };
        pCanvas.Children.Add(PSRuleBuild(2.5, 2.5, 13.5, 13.5));
        pCanvas.Children.Add(PSRuleBuild(13.5, 2.5, 2.5, 13.5));
        return pCanvas;
    }

    private static System.Windows.Shapes.Line PSRuleBuild(double pX1, double pY1, double pX2, double pY2)
    {
        var pRule = new System.Windows.Shapes.Line
        {
            X1 = pX1,
            Y1 = pY1,
            X2 = pX2,
            Y2 = pY2,
            StrokeThickness = 1.25,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round
        };
        PSCasementGlyphBind(pRule);
        return pRule;
    }

    private static void PSCasementGlyphBind(System.Windows.Shapes.Shape pGlyph)
    {
        pGlyph.SetBinding(System.Windows.Shapes.Shape.StrokeProperty, new System.Windows.Data.Binding("Foreground")
        {
            RelativeSource = new System.Windows.Data.RelativeSource(
                System.Windows.Data.RelativeSourceMode.FindAncestor, typeof(Button), 1)
        });
    }
}

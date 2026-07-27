using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;

namespace Cadroue.UIShell.PPanels;

internal sealed partial class PSEncoder
{
    private UIElement PSCasementOverlayBuild()
    {
        var pGrid = new Grid { Height = 56, VerticalAlignment = VerticalAlignment.Top };
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(56) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pGrid.Children.Add(PSCrestBuild());
        var pPresetTitle = new TextBlock
        {
            Text = lsExportSpecificEdit.PresetName,
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = PTextBrush,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 160, 0)
        };
        Grid.SetColumn(pPresetTitle, 1);
        pGrid.Children.Add(pPresetTitle);

        var pDragArea = new Border { Background = Brushes.Transparent, HorizontalAlignment = HorizontalAlignment.Right, Width = 150 };
        pDragArea.MouseLeftButtonDown += PSCasementDragHandle;
        Grid.SetColumn(pDragArea, 1);
        pGrid.Children.Add(pDragArea);

        var pButtons = new StackPanel { Orientation = Orientation.Horizontal, Background = new SolidColorBrush(Color.FromRgb(0xEA, 0xF2, 0xFC)) };
        pButtons.Children.Add(PSCasementButtonBuild(PSCasementMinimizeBuild(), (_, _) => WindowState = WindowState.Minimized));
        pButtons.Children.Add(PSCasementButtonBuild(PSCasementMaximizeBuild(), (_, _) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized));
        pButtons.Children.Add(PSCasementButtonBuild(PSCasementCloseBuild(), (_, _) => Close()));
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
                FontSize = 16,
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
        pTabs.Items.Add(PSSheetBuild("Output", PSEncoderRootBuild(PSSheetScrollBuild(PSOutputBuild()))));
        pTabs.Items.Add(PSSheetBuild("Video", PSEncoderRootBuild(PSSheetScrollBuild(PSVideoBuild()))));
        pTabs.Items.Add(PSSheetBuild("Audio", PSEncoderRootBuild(PSSheetScrollBuild(PSAudioBuild()))));
        return pTabs;
    }

    private static TabItem PSSheetBuild(string pTitle, UIElement pContent) => new() { Header = pTitle, Content = pContent };

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
            <RowDefinition Height=""56"" />
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
                          Margin=""0,-2,0,0""
                          Panel.ZIndex=""0""
                          ContentSource=""SelectedContent"" />
    </Grid>
</ControlTemplate>";
        return (ControlTemplate)XamlReader.Parse(pXaml);
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
    <Setter Property=""Foreground"" Value=""#2B3443"" />
    <Setter Property=""FontSize"" Value=""13"" />
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
                            <ContentPresenter Grid.Row=""1""
                                              ContentSource=""Header""
                                              VerticalAlignment=""Top""
                                              HorizontalAlignment=""Left""
                                              Margin=""0,1,0,0"" />
                        </Grid>
                    </Border>
                    <Border x:Name=""pTabAccent""
                            Height=""3""
                            Margin=""7,0,7,0""
                            VerticalAlignment=""Top""
                            Background=""#2E7AEF""
                            CornerRadius=""2,2,0,0""
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
                        <Setter TargetName=""pTabAccent"" Property=""Visibility"" Value=""Visible"" />
                        <Setter Property=""Foreground"" Value=""#111827"" />
                        <Setter Property=""FontWeight"" Value=""SemiBold"" />
                    </Trigger>
                </ControlTemplate.Triggers>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>";
        return (Style)XamlReader.Parse(pXaml);
    }

    private static Button PSCasementButtonBuild(UIElement pIcon, RoutedEventHandler pClick)
    {
        var pButton = new Button
        {
            Width = 48,
            Height = 56,
            Content = pIcon,
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            FocusVisualStyle = null
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
        pCanvas.Children.Add(new System.Windows.Shapes.Rectangle
        {
            Width = 12,
            Height = 12,
            Stroke = PTextBrush,
            StrokeThickness = 1.2,
            Fill = Brushes.Transparent,
            Margin = new Thickness(2, 2, 0, 0)
        });
        return pCanvas;
    }

    private static Canvas PSCasementCloseBuild()
    {
        var pCanvas = new Canvas { Width = 18, Height = 16 };
        pCanvas.Children.Add(PSRuleBuild(2.5, 2.5, 13.5, 13.5));
        pCanvas.Children.Add(PSRuleBuild(13.5, 2.5, 2.5, 13.5));
        return pCanvas;
    }

    private static System.Windows.Shapes.Line PSRuleBuild(double pX1, double pY1, double pX2, double pY2) => new()
    {
        X1 = pX1,
        Y1 = pY1,
        X2 = pX2,
        Y2 = pY2,
        Stroke = PTextBrush,
        StrokeThickness = 1.25,
        StrokeStartLineCap = PenLineCap.Round,
        StrokeEndLineCap = PenLineCap.Round
    };
}

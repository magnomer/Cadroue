using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIShell.PMainWindow;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PExport : UserControl
{
    private static readonly Brush PLineBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7));
    private static readonly Brush PSoftBrush = new SolidColorBrush(Color.FromRgb(0xF7, 0xF9, 0xFC));
    private static readonly Brush PTextBrush = new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x3D));
    private static readonly Brush PMutedBrush = new SolidColorBrush(Color.FromRgb(0x62, 0x6F, 0x83));
    private static readonly Brush PHeaderFillBrush = new SolidColorBrush(Color.FromRgb(0xF3, 0xF5, 0xF8));
    private static readonly Brush PHeaderTextBrush = new SolidColorBrush(Color.FromRgb(0x26, 0x36, 0x4A));

    private readonly LExportSpecificState lExportSpecificState;
    private readonly TextBlock pSummaryContainer;
    private readonly TextBlock pSummaryMode;
    private readonly TextBlock pSummaryVideo;
    private readonly TextBlock pSummaryAudio;
    private readonly TextBlock pSummaryOutput;
    private readonly StackPanel pPresetRowPanel;
    private readonly bool pVideoCopyPresetDisabled;
    private string? pPresetNameSelected;
    private string? pPresetNameEditing;
    private string? pPresetNameDragging;
    private Point? pPresetDragStart;
    private Point pPresetDragOffset;
    private bool pPresetDragActive;
    private PGhost? pPresetDragGhost;

    private bool pPresetRebuilding;

    private TextBox? pPresetNameBoxCurrent;

    private bool pExportPresetBusy;

    public PExport(LExportSpecificState lExportSpecificState, bool pVideoCopyPresetDisabled = false)
    {
        this.lExportSpecificState = lExportSpecificState;
        this.pVideoCopyPresetDisabled = pVideoCopyPresetDisabled;
        FocusVisualStyle = null;
        pPresetRowPanel = new StackPanel();
        pPresetNameSelected = lExportSpecificState.PresetName;

        var pPanel = new Grid();
        pPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        pPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        UIElement pHeader = PHeaderBuild();
        Grid.SetRow(pHeader, 0);
        pPanel.Children.Add(pHeader);

        var pBody = new Grid();
        pBody.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        pBody.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        pBody.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        pBody.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        UIElement pPreset = PExportPresetBuild();
        Grid.SetRow(pPreset, 0);
        pBody.Children.Add(pPreset);

        UIElement pAction = PExportActionBuild();
        Grid.SetRow(pAction, 1);
        pBody.Children.Add(pAction);

        UIElement pSeparator = PSeparatorBuild();
        Grid.SetRow(pSeparator, 2);
        pBody.Children.Add(pSeparator);

        UIElement pSummary = PSummaryBuild(
            PSummaryRowBuild(LLocalization.LLocalizationTextRead("Roster.Field.Container"), out pSummaryContainer),
            PSummaryRowBuild(LLocalization.LLocalizationTextRead("Roster.Field.Mode"), out pSummaryMode),
            PSummaryRowBuild(LLocalization.LLocalizationTextRead("ExportSummary.Video"), out pSummaryVideo),
            PSummaryRowBuild(LLocalization.LLocalizationTextRead("ExportSummary.Audio"), out pSummaryAudio),
            PSummaryRowBuild(LLocalization.LLocalizationTextRead("Roster.Section.Output"), out pSummaryOutput));
        Grid.SetRow(pSummary, 3);
        pBody.Children.Add(pSummary);

        Grid.SetRow(pBody, 1);
        pPanel.Children.Add(pBody);

        PExportSummaryUpdate();
        Content = PExportFrameBuild(pPanel);
    }

    private static Border PExportFrameBuild(UIElement pContent)
    {
        var pInnerBorder = new Border
        {
            Background = PSoftBrush,
            CornerRadius = new CornerRadius(9),
            Child = pContent,
            SnapsToDevicePixels = true
        };
        PExportClipApply(pInnerBorder, 9);

        return new Border
        {
            Margin = new Thickness(8),
            BorderBrush = PLineBrush,
            BorderThickness = new Thickness(1),
            Background = PSoftBrush,
            CornerRadius = new CornerRadius(10),
            Child = pInnerBorder,
            SnapsToDevicePixels = true
        };
    }

    private static void PExportClipApply(Border pBorder, double pRadius)
    {
        pBorder.SizeChanged += (_, _) =>
        {
            pBorder.Clip = new RectangleGeometry(
                new Rect(0, 0, pBorder.ActualWidth, pBorder.ActualHeight),
                pRadius,
                pRadius);
        };
    }

    private void PExportSummaryUpdate()
    {
        pExportPresetBusy = true;
        pPresetNameSelected = lExportSpecificState.PresetName;
        if (!string.Equals(pPresetNameEditing, pPresetNameSelected, StringComparison.OrdinalIgnoreCase))
        {
            pPresetNameEditing = null;
            pPresetNameBoxCurrent = null;
        }

        PExportPresetRebuild();
        pExportPresetBusy = false;

        pSummaryContainer.Text = lExportSpecificState.Container;
        pSummaryMode.Text = lExportSpecificState.ExportMode;
        pSummaryVideo.Text = lExportSpecificState.VideoSummary;
        pSummaryAudio.Text = lExportSpecificState.AudioSummary;
        pSummaryOutput.Text = lExportSpecificState.OutputSummary;
    }

    private static UIElement PHeaderBuild() => new Border
    {
        Padding = new Thickness(12, 10, 12, 10),
        BorderBrush = PLineBrush,
        BorderThickness = new Thickness(0, 0, 0, 1),
        Background = PHeaderFillBrush,
        CornerRadius = new CornerRadius(9, 9, 0, 0),
        Child = new TextBlock
        {
            Text = LLocalization.LLocalizationTextRead("ExportSummary.Header.Export"),
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = PHeaderTextBrush,
            VerticalAlignment = VerticalAlignment.Center
        }
    };

    private static Border PSummaryBuild(params UIElement[] pChildren)
    {
        var pPanel = new StackPanel();
        pPanel.Children.Add(new TextBlock
        {
            Text = LLocalization.LLocalizationTextRead("ExportSummary.Header.Summary"),
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = PTextBrush,
            Margin = new Thickness(0, 0, 0, 6)
        });

        foreach (UIElement pChild in pChildren)
        {
            pPanel.Children.Add(pChild);
        }

        return new Border
        {
            BorderBrush = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Background = Brushes.Transparent,
            Padding = new Thickness(10, 4, 10, 10),
            Child = pPanel
        };
    }

    private static UIElement PSeparatorBuild() => new Border
    {
        Height = 1,
        Background = PLineBrush,
        Margin = new Thickness(0, 8, 0, 8)
    };

    private static UIElement PSummaryRowBuild(string pName, out TextBlock pValueBlock)
    {
        var pGrid = new Grid { Margin = new Thickness(0, 0, 0, 3) };
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(74) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.Children.Add(new TextBlock { Text = pName, Foreground = PMutedBrush, FontSize = 11, VerticalAlignment = VerticalAlignment.Top });

        pValueBlock = new TextBlock { Foreground = PTextBrush, FontSize = 11, TextWrapping = TextWrapping.Wrap };
        Grid.SetColumn(pValueBlock, 1);
        pGrid.Children.Add(pValueBlock);
        return pGrid;
    }
}

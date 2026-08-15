using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIShell.PMainWindow;
using Cadroue.Application;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PExport : UserControl
{
    private static readonly Brush PLineBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7));
    private static readonly Brush PExportSoftBrush = new SolidColorBrush(Color.FromRgb(0xF7, 0xF9, 0xFC));
    private static readonly Brush PExportTextBrush = new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x3D));
    private static readonly Brush PExportMutedBrush = new SolidColorBrush(Color.FromRgb(0x62, 0x6F, 0x83));
    private static readonly Brush PHeaderFillBrush = new SolidColorBrush(Color.FromRgb(0xF3, 0xF5, 0xF8));
    private static readonly Brush PHeaderTextBrush = new SolidColorBrush(Color.FromRgb(0x26, 0x36, 0x4A));

    private readonly LPresetSelection lPresetOwner;
    private readonly TextBlock pExportSummaryBox;
    private readonly TextBlock pExportSummaryMode;
    private readonly TextBlock pExportSummaryVideo;
    private readonly TextBlock pExportSummaryAudio;
    private readonly TextBlock pExportSummaryOutput;
    private readonly StackPanel pPresetRowPanel;
    private readonly bool pExportCopyDisabled;
    private string? pPresetNameSelected;
    private string? pPresetNameEditing;
    private string? pPresetNameDragging;
    private Point? pExportDragOrigin;
    private Point pPresetDragOffset;
    private bool pPresetDragActive;
    private Border? pPresetRowDragging;
    private double pPresetRowOpacity;
    private PGhost? pPresetDragGhost;

    private bool pPresetRebuilding;

    private bool pExportNativeCollapsed;
    private bool pExportUserCollapsed;

    private TextBox? pExportBoxCurrent;

    private bool pExportPresetBusy;

    private bool pExportPresetClean = true;

    public PExport(LPresetSelection lPresetOwner, bool pExportCopyDisabled = false)
    {
        this.lPresetOwner = lPresetOwner;
        this.pExportCopyDisabled = pExportCopyDisabled;
        FocusVisualStyle = null;
        PScrollbar.PScrollbarApply(this);
        pPresetRowPanel = new StackPanel();
        pPresetRowPanel.PreviewMouseMove += PExportMoveHandle;
        pPresetRowPanel.MouseLeftButtonUp += PExportUpHandle;
        pPresetRowPanel.LostMouseCapture += PExportLostHandle;
        pPresetNameSelected = lPresetOwner.LPresetSelectionName;

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

        UIElement pSummary = PExportSummaryBuild(
            PExportLineBuild(LLocalization.LLocalizationTextRead("Roster.Field.Container"), out pExportSummaryBox),
            PExportLineBuild(LLocalization.LLocalizationTextRead("Roster.Field.Mode"), out pExportSummaryMode),
            PExportLineBuild(LLocalization.LLocalizationTextRead("ExportSummary.Video"), out pExportSummaryVideo),
            PExportLineBuild(LLocalization.LLocalizationTextRead("ExportSummary.Audio"), out pExportSummaryAudio),
            PExportLineBuild(LLocalization.LLocalizationTextRead("Roster.Section.Output"), out pExportSummaryOutput));
        Grid.SetRow(pSummary, 3);
        pBody.Children.Add(pSummary);

        Grid.SetRow(pBody, 1);
        pPanel.Children.Add(pBody);

        PExportSummaryUpdate();
        Content = PExportFrameBuild(pPanel);

        Loaded += (_, _) =>
        {
            LPreset.LPresetStoreChange += PExportPresetSync;
            lPresetOwner.LPresetSelectionChange += PExportSummaryUpdate;
            PExportPresetSync();
        };
        Unloaded += (_, _) =>
        {
            LPreset.LPresetStoreChange -= PExportPresetSync;
            lPresetOwner.LPresetSelectionChange -= PExportSummaryUpdate;
        };
    }

    private LPreset PExportWorkingRead() => LPreset.LPresetStateCreate(lPresetOwner.LPresetSelectionValue);

    private static Border PExportFrameBuild(UIElement pContent)
    {
        var pInnerBorder = new Border
        {
            Background = PExportSoftBrush,
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
            Background = PExportSoftBrush,
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
        LPreset lWorking = PExportWorkingRead();
        pExportPresetBusy = true;
        pPresetNameSelected = string.IsNullOrEmpty(lPresetOwner.LPresetSelectionName)
            ? null
            : lPresetOwner.LPresetSelectionName;
        if (!string.Equals(pPresetNameEditing, pPresetNameSelected, StringComparison.OrdinalIgnoreCase))
        {
            pPresetNameEditing = null;
            pExportBoxCurrent = null;
        }

        PExportPresetRebuild();
        pExportPresetBusy = false;

        pExportSummaryBox.Text = lWorking.LPresetContainer;
        pExportSummaryMode.Text = lWorking.LPresetExportMode;
        pExportSummaryVideo.Text = lWorking.LPresetVideoSummary;
        pExportSummaryAudio.Text = lWorking.LPresetAudioSummary;
        pExportSummaryOutput.Text = lWorking.LPresetOutputSummary;

        pExportPresetClean = string.IsNullOrEmpty(pPresetNameSelected)
            || LPreset.LPresetMatch(pPresetNameSelected, lWorking);
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

    private static Border PExportSummaryBuild(params UIElement[] pChildren)
    {
        var pPanel = new StackPanel();
        pPanel.Children.Add(new TextBlock
        {
            Text = LLocalization.LLocalizationTextRead("ExportSummary.Header.Summary"),
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = PExportTextBrush,
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

    private static UIElement PExportLineBuild(string pName, out TextBlock pValueBlock)
    {
        var pGrid = new Grid { Margin = new Thickness(0, 0, 0, 3) };
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(74) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.Children.Add(new TextBlock { Text = pName, Foreground = PExportMutedBrush, FontSize = 11, VerticalAlignment = VerticalAlignment.Top });

        pValueBlock = new TextBlock { Foreground = PExportTextBrush, FontSize = 11, TextWrapping = TextWrapping.Wrap };
        Grid.SetColumn(pValueBlock, 1);
        pGrid.Children.Add(pValueBlock);
        return pGrid;
    }
}

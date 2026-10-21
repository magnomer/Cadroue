using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed class PInspectorCropGrid
{
    private readonly TextBox pInspectorInsetLeft;
    private readonly TextBox pInspectorInsetRight;
    private readonly TextBox pInspectorInsetTop;
    private readonly TextBox pInspectorInsetBottom;
    private readonly TextBlock pInspectorResolution;

    internal PInspectorCropGrid(QInspectorCrop lCrop)
    {
        QInspectorCrop = lCrop;
        pInspectorInsetLeft = PInspectorInsetBuild(0);
        pInspectorInsetRight = PInspectorInsetBuild(2);
        pInspectorInsetTop = PInspectorInsetBuild(1);
        pInspectorInsetBottom = PInspectorInsetBuild(3);
        pInspectorResolution = new TextBlock
        {
            Text = "—",
            FontSize = 11,
            FontFamily = QTool.QToolFontRead(),
            Foreground = QTool.QToolMutedRead(),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        PInspectorGridBody = PInspectorEdgeBuild();
    }

    private QInspectorCrop QInspectorCrop { get; }

    internal UIElement PInspectorGridBody { get; }

    internal void PInspectorGridApply()
    {
        pInspectorInsetLeft.Text = QInspectorCrop.QInspectorEdgeFormat(0, pInspectorInsetLeft.Text);
        pInspectorInsetTop.Text = QInspectorCrop.QInspectorEdgeFormat(1, pInspectorInsetTop.Text);
        pInspectorInsetRight.Text = QInspectorCrop.QInspectorEdgeFormat(2, pInspectorInsetRight.Text);
        pInspectorInsetBottom.Text = QInspectorCrop.QInspectorEdgeFormat(3, pInspectorInsetBottom.Text);
    }

    internal void PInspectorResolutionApply() =>
        pInspectorResolution.Text = QInspectorCrop.QInspectorResolutionRead();

    private TextBox PInspectorInsetBuild(int pEdge)
    {
        TextBox pInsetBox = PInspectorNumber.PInspectorNumberBuild();
        PInspectorInsetAttach(pInsetBox, pEdge);
        return pInsetBox;
    }

    private void PInspectorInsetAttach(TextBox pInsetBox, int pEdge) =>
        pInsetBox.TextChanged += (_, _) =>
            QInspectorCrop.QInspectorEdgeCommit(pEdge, pInsetBox.Text, pInsetBox.IsKeyboardFocusWithin);

    private UIElement PInspectorEdgeBuild()
    {
        var pCropGrid = new Grid { Margin = new Thickness(0, 14, 0, 4) };
        pCropGrid.ColumnDefinitions.Add(PInspectorColumnBuild());
        pCropGrid.ColumnDefinitions.Add(PInspectorColumnBuild());
        pCropGrid.ColumnDefinitions.Add(PInspectorColumnBuild());
        pCropGrid.RowDefinitions.Add(PInspectorRowBuild());
        pCropGrid.RowDefinitions.Add(PInspectorRowBuild());
        pCropGrid.RowDefinitions.Add(PInspectorRowBuild());

        PInspectorCellAdd(
            pCropGrid,
            PInspectorCellBuild(QLocalizationCatalog.QLocalizationTextRead("Inspector.Crop.Top"), pInspectorInsetTop),
            0,
            1);
        PInspectorCellAdd(
            pCropGrid,
            PInspectorCellBuild(QLocalizationCatalog.QLocalizationTextRead("Inspector.Crop.Left"), pInspectorInsetLeft),
            1,
            0);
        PInspectorCellAdd(
            pCropGrid,
            PInspectorCellBuild(QLocalizationCatalog.QLocalizationTextRead("Inspector.Crop.Right"),
                pInspectorInsetRight),
            1,
            2);
        PInspectorCellAdd(
            pCropGrid,
            PInspectorCellBuild(QLocalizationCatalog.QLocalizationTextRead("Inspector.Crop.Bottom"),
                pInspectorInsetBottom),
            2,
            1);
        PInspectorCellAdd(pCropGrid, PInspectorResolutionBuild(), 1, 1);
        PInspectorCellAdd(pCropGrid, PInspectorResetBuild(), 2, 2);
        return pCropGrid;
    }

    private static ColumnDefinition PInspectorColumnBuild() => new() { Width = new GridLength(1, GridUnitType.Star) };

    private static RowDefinition PInspectorRowBuild() => new() { Height = GridLength.Auto };

    private UIElement PInspectorResolutionBuild() => new Border
    {
        MinWidth = 84,
        Height = QTool.QToolFieldHeight,
        Padding = new Thickness(7, 0, 7, 0),
        Background = new SolidColorBrush(Color.FromRgb(0xF7, 0xF9, 0xFC)),
        BorderBrush = QPanel.QPanelLineRead(),
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(8),
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Bottom,
        Margin = new Thickness(3),
        ToolTip = QLocalizationCatalog.QLocalizationTextRead("Inspector.Crop.Resolution"),
        Child = pInspectorResolution
    };

    private UIElement PInspectorResetBuild()
    {
        Button pResetButton = PCropCaptionBuild();
        PInspectorButton.PInspectorClickAttach(pResetButton, () => QInspectorCrop.QInspectorEdgeReset());
        QButton.QButtonAttach(pResetButton, QButtonLook.QButtonLookPanel);
        return pResetButton;
    }

    private static Button PCropCaptionBuild() => new()
    {
        Content = QLocalizationCatalog.QLocalizationTextRead("Inspector.Crop.Reset"),
        ToolTip = QLocalizationCatalog.QLocalizationTextRead("Inspector.Crop.ResetTooltip"),
        Height = QTool.QToolFieldHeight,
        MinWidth = 64,
        Padding = new Thickness(8, 0, 8, 0),
        FontSize = 11,
        FontFamily = QTool.QToolFontRead(),
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Bottom,
        Margin = new Thickness(3)
    };

    private static void PInspectorCellAdd(Grid pCropGrid, UIElement pCell, int pRow, int pColumn)
    {
        Grid.SetRow(pCell, pRow);
        Grid.SetColumn(pCell, pColumn);
        pCropGrid.Children.Add(pCell);
    }

    private static UIElement PInspectorCellBuild(string pCellLabel, TextBox pCellBox)
    {
        var pCellPanel = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 3, 0, 3)
        };
        pCellPanel.Children.Add(new TextBlock
        {
            Text = pCellLabel,
            FontSize = 11,
            FontFamily = QTool.QToolFontRead(),
            Foreground = QTool.QToolMutedRead(),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 3)
        });
        pCellPanel.Children.Add(pCellBox);
        return pCellPanel;
    }
}

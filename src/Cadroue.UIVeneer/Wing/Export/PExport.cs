using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.Application;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

public sealed class PExport : UserControl
{
    private static readonly Brush PExportSoftBrush = new SolidColorBrush(Color.FromRgb(0xF7, 0xF9, 0xFC));
    private static readonly Brush PHeaderFillBrush = new SolidColorBrush(Color.FromRgb(0xF3, 0xF5, 0xF8));
    private static readonly Brush PHeaderTextBrush = new SolidColorBrush(Color.FromRgb(0x26, 0x36, 0x4A));

    private readonly PExportPresetCommand pExportCommand;
    private readonly PExportPreset pExportPreset;
    private readonly PExportSummary pExportSummary;

    public QExport QExport { get; }

    public PExport(QExport lExport, LPresetSelection lPresetOwner)
    {
        QExport = lExport;
        FocusVisualStyle = null;
        QScrollbar.QScrollbarAttach(this);
        pExportCommand = new PExportPresetCommand(QExport, lPresetOwner, this);
        pExportPreset = new PExportPreset(QExport, pExportCommand);
        pExportSummary = new PExportSummary();
        PExportBuild(new PExportPresetFile(QExport, lPresetOwner, this));
        PExportPresetsUpdate();
        PExportAttach();
    }

    private void PExportBuild(PExportPresetFile pExportFile)
    {
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

        UIElement pPreset = pExportPreset.PExportPresetBuild();
        Grid.SetRow(pPreset, 0);
        pBody.Children.Add(pPreset);

        UIElement pAction = PExportPresetAction.PExportActionBuild(pExportCommand, pExportFile);
        Grid.SetRow(pAction, 1);
        pBody.Children.Add(pAction);

        UIElement pSeparator = PSeparatorBuild();
        Grid.SetRow(pSeparator, 2);
        pBody.Children.Add(pSeparator);

        Grid.SetRow(pExportSummary, 3);
        pBody.Children.Add(pExportSummary);

        Grid.SetRow(pBody, 1);
        pPanel.Children.Add(pBody);
        Content = PExportFrameBuild(pPanel);
    }

    private void PExportAttach()
    {
        QExport.QExportPresetsChanged += PExportPresetsUpdate;
        pExportPreset.PExportPresetAttach();
        Loaded += (_, _) => QExport.QExportAttach();
        Unloaded += (_, _) => QExport.QExportDetach();
    }

    private static Border PExportFrameBuild(UIElement pContent)
    {
        Border pInnerBorder = PExportInnerBuild(pContent);
        PExportClipApply(pInnerBorder, 9);
        return PExportOuterBuild(pInnerBorder);
    }

    private static Border PExportInnerBuild(UIElement pContent) => new()
    {
        Background = PExportSoftBrush,
        CornerRadius = new CornerRadius(9),
        Child = pContent,
        SnapsToDevicePixels = true
    };

    private static Border PExportOuterBuild(Border pInnerBorder) => new()
    {
        Margin = new Thickness(8),
        BorderBrush = PExportTheme.PLineBrush,
        BorderThickness = new Thickness(1),
        Background = PExportSoftBrush,
        CornerRadius = new CornerRadius(10),
        Child = pInnerBorder,
        SnapsToDevicePixels = true
    };

    private static void PExportClipApply(Border pBorder, double pRadius) =>
        pBorder.SizeChanged += (_, _) => PExportClipUpdate(pBorder, pRadius);

    private static void PExportClipUpdate(Border pBorder, double pRadius) =>
        pBorder.Clip = new RectangleGeometry(
            new Rect(0, 0, pBorder.ActualWidth, pBorder.ActualHeight),
            pRadius,
            pRadius);

    private void PExportPresetsUpdate()
    {
        LPreset lWorking = pExportCommand.PExportWorkingRead();
        pExportPreset.PExportPresetRebuild();
        pExportSummary.PExportSummarySet(lWorking);
    }

    private static UIElement PHeaderBuild() => new Border
    {
        Padding = new Thickness(12, 10, 12, 10),
        BorderBrush = PExportTheme.PLineBrush,
        BorderThickness = new Thickness(0, 0, 0, 1),
        Background = PHeaderFillBrush,
        CornerRadius = new CornerRadius(9, 9, 0, 0),
        Child = new TextBlock
        {
            Text = QLocalizationCatalog.QLocalizationTextRead("ExportSummary.Header.Export"),
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = PHeaderTextBrush,
            VerticalAlignment = VerticalAlignment.Center
        }
    };

    private static UIElement PSeparatorBuild() => new Border
    {
        Height = 1,
        Background = PExportTheme.PLineBrush,
        Margin = new Thickness(0, 8, 0, 8)
    };
}

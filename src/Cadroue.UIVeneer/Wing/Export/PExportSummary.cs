using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.Application;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed class PExportSummary : Border
{
    private TextBlock pExportSummaryBox = null!;
    private TextBlock pExportSummaryVideo = null!;
    private TextBlock pExportSummaryAudio = null!;
    private TextBlock pExportSummaryOutput = null!;

    public PExportSummary()
    {
        PExportSummaryBuild(
            PExportLineBuild(
                QLocalizationCatalog.QLocalizationTextRead("Roster.Field.Container"), out pExportSummaryBox),
            PExportLineBuild(
                QLocalizationCatalog.QLocalizationTextRead("ExportSummary.Video"), out pExportSummaryVideo),
            PExportLineBuild(
                QLocalizationCatalog.QLocalizationTextRead("ExportSummary.Audio"), out pExportSummaryAudio),
            PExportLineBuild(
                QLocalizationCatalog.QLocalizationTextRead("Roster.Section.Output"), out pExportSummaryOutput));
    }

    internal void PExportSummarySet(LPreset lWorking)
    {
        pExportSummaryBox.Text = lWorking.LPresetContainer;
        pExportSummaryVideo.Text = lWorking.LPresetVideoSummary;
        pExportSummaryAudio.Text = lWorking.LPresetAudioSummary;
        pExportSummaryOutput.Text = lWorking.LPresetOutputSummary;
    }

    private void PExportSummaryBuild(params UIElement[] pChildren)
    {
        var pPanel = new StackPanel();
        pPanel.Children.Add(new TextBlock
        {
            Text = QLocalizationCatalog.QLocalizationTextRead("ExportSummary.Header.Summary"),
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = PExportTheme.PExportTextBrush,
            Margin = new Thickness(0, 0, 0, 6)
        });

        foreach (UIElement pChild in pChildren)
        {
            pPanel.Children.Add(pChild);
        }

        BorderBrush = Brushes.Transparent;
        BorderThickness = new Thickness(0);
        Background = Brushes.Transparent;
        Padding = new Thickness(10, 4, 10, 10);
        Child = pPanel;
    }

    private static UIElement PExportLineBuild(string pName, out TextBlock pValueBlock)
    {
        var pGrid = new Grid { Margin = new Thickness(0, 0, 0, 3) };
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(74) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.Children.Add(new TextBlock
        {
            Text = pName,
            Foreground = PExportTheme.PExportMutedBrush,
            FontSize = 11,
            VerticalAlignment = VerticalAlignment.Top
        });

        pValueBlock = new TextBlock
        {
            Foreground = PExportTheme.PExportTextBrush,
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap
        };
        Grid.SetColumn(pValueBlock, 1);
        pGrid.Children.Add(pValueBlock);
        return pGrid;
    }
}

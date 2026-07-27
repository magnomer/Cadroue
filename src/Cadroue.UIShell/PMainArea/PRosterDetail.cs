using System.Windows;
using System.Windows.Controls;
using Cadroue.Core;

namespace Cadroue.UIShell.PMainArea;

public sealed partial class PRoster
{
    private UIElement PRosterDetailBuild()
    {
        var pPanel = new StackPanel { Margin = new Thickness(12, 0, 0, 0) };
        pPanel.Children.Add(new TextBlock
        {
            Text = "Job detail",
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            Foreground = PRosterTextBrush,
            Margin = new Thickness(0, 0, 0, 10)
        });
        pPanel.Children.Add(pRosterDetailPanel);
        PRosterDetailUpdate();

        return new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            FocusVisualStyle = null,
            Content = pPanel
        };
    }

    private void PRosterDetailUpdate()
    {
        pRosterDetailPanel.Children.Clear();

        if (pRosterTable.SelectedItem is not LWorkItem pWorkItem)
        {
            pRosterDetailPanel.Children.Add(new TextBlock
            {
                Text = "Select a job to see its settings.",
                Foreground = PRosterMutedBrush,
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap
            });
            return;
        }

        LWorkOutput pOutput = pWorkItem.LWorkOutput;
        PRosterRowAdd("State", PRosterStateLabel.PRosterStateFormat(pWorkItem.LWorkStateCurrent));
        PRosterRowAdd("Kind", pWorkItem.LWorkKind.ToString().Replace("LWorkKind", string.Empty));
        PRosterRowAdd("Priority", pWorkItem.LWorkPriority == LWorkPriority.LWorkPriorityHigh ? "High" : "Normal");
        PRosterRowAdd("Source", pWorkItem.LWorkSourcePath);
        PRosterRowAdd("Range", $"{pWorkItem.LWorkStart:hh\\:mm\\:ss} - {pWorkItem.LWorkEnd:hh\\:mm\\:ss}  ({pWorkItem.LWorkDuration:hh\\:mm\\:ss})");
        PRosterRowAdd("Output", pWorkItem.LWorkOutputPath);
        PRosterRowAdd("Container", pOutput.LWorkOutputContainer);
        PRosterRowAdd("Export mode", pOutput.LWorkOutputExportMode);
        PRosterRowAdd("Video", $"{pOutput.LWorkOutputVideoMode} ({pOutput.LWorkOutputVideoStream})");
        PRosterRowAdd("Encoder", pOutput.LWorkOutputVideoEncoder);
        PRosterRowAdd("Rate control", pOutput.LWorkOutputRateControl);
        PRosterRowAdd("Quality", pOutput.LWorkOutputQuality);
        PRosterRowAdd("Speed preset", pOutput.LWorkOutputSpeedPreset);
        PRosterRowAdd("Size / FPS", $"{pOutput.LWorkOutputVideoSize} / {pOutput.LWorkOutputVideoFps}");
        PRosterRowAdd("Pixel format", pOutput.LWorkOutputPixelFormat);

        if (pOutput.LWorkOutputVideoExtras.Count > 0)
        {
            PRosterRowAdd("Extras", string.Join("  ", pOutput.LWorkOutputVideoExtras.Select(pExtra => $"{pExtra.Key} {pExtra.Value}")));
        }

        PRosterRowAdd("Audio", $"{pOutput.LWorkOutputAudioMode} ({pOutput.LWorkOutputAudioStream})");
        PRosterRowAdd("Audio codec", $"{pOutput.LWorkOutputAudioEncoder}  {pOutput.LWorkOutputAudioBitrate}");
        PRosterRowAdd("Audio format", $"{pOutput.LWorkOutputAudioSampleRate}  {pOutput.LWorkOutputAudioChannels}");
        PRosterRowAdd("Queued", pWorkItem.LWorkCreateTime.ToString("yyyy-MM-dd HH:mm:ss"));

        if (!string.IsNullOrWhiteSpace(pWorkItem.LWorkMessage))
        {
            PRosterRowAdd("Message", pWorkItem.LWorkMessage);
        }
    }

    private void PRosterRowAdd(string pLabel, string pValue)
    {
        var pGrid = new Grid { Margin = new Thickness(0, 0, 0, 6) };
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(94) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.Children.Add(new TextBlock
        {
            Text = pLabel,
            Foreground = PRosterMutedBrush,
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Top
        });

        var pValueBlock = new TextBlock
        {
            Text = pValue,
            Foreground = PRosterTextBrush,
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap
        };
        Grid.SetColumn(pValueBlock, 1);
        pGrid.Children.Add(pValueBlock);
        pRosterDetailPanel.Children.Add(pGrid);
    }
}

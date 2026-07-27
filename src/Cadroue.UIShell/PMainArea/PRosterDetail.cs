using System.Windows;
using System.Windows.Controls;
using Cadroue.Core;
using Cadroue.UIShell.PPanels;

namespace Cadroue.UIShell.PMainArea;

public sealed partial class PRoster
{
    private UIElement PRosterDetailBuild()
    {
        var pHeader = new Border
        {
            Padding = PRosterTheme.PRosterHeaderPadding,
            Background = PRosterTheme.PRosterHeaderBrush,
            BorderBrush = PRosterTheme.PRosterLineBrush,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Child = pRosterDetailTitle
        };

        var pScroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            FocusVisualStyle = null,
            Padding = new Thickness(12, 10, 12, 12),
            Content = pRosterDetailPanel
        };

        var pRoot = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(pHeader, Dock.Top);
        pRoot.Children.Add(pHeader);
        pRoot.Children.Add(pScroll);

        PRosterDetailUpdate();
        return PPanel.PPanelBorderBuild(pRoot);
    }

    private void PRosterDetailUpdate()
    {
        pRosterDetailPanel.Children.Clear();

        if (PRosterSelectRead() is not { } pWorkItem)
        {
            pRosterDetailPanel.Children.Add(new TextBlock
            {
                Text = "Select a job to see its settings.",
                Foreground = PRosterTheme.PRosterMutedBrush,
                FontSize = PRosterTheme.PRosterRowSize,
                TextWrapping = TextWrapping.Wrap
            });
            return;
        }

        LWorkOutput pOutput = pWorkItem.LWorkOutput;
        PRosterRowAdd("State", PRosterStateLabel.PRosterStateFormat(pWorkItem.LWorkStateCurrent));
        PRosterRowAdd("Owner", PRosterOwnerFormat(pWorkItem));
        PRosterRowAdd("Attempts", pWorkItem.LWorkAttemptCount.ToString());

        if (pWorkItem.LWorkStateCurrent == LWorkState.LWorkStateRunning)
        {
            PRosterRowAdd("Phase", PRosterPhaseFormat(pWorkItem.LWorkPhaseCurrent));
        }

        PRosterRowAdd("Kind", PRosterKindFormat(pWorkItem.LWorkKind));
        PRosterRowAdd("Priority", PRosterPriorityFormat(pWorkItem.LWorkPriority));
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
        var pGrid = new Grid { Margin = new Thickness(0, 0, 0, 5) };
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(PRosterTheme.PRosterLabelWidth) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.Children.Add(new TextBlock
        {
            Text = pLabel,
            Foreground = PRosterTheme.PRosterMutedBrush,
            FontSize = PRosterTheme.PRosterRowSize,
            VerticalAlignment = VerticalAlignment.Top
        });

        var pValueBlock = new TextBlock
        {
            Text = pValue,
            Foreground = PRosterTheme.PRosterTextBrush,
            FontSize = PRosterTheme.PRosterRowSize,
            TextWrapping = TextWrapping.Wrap
        };
        Grid.SetColumn(pValueBlock, 1);
        pGrid.Children.Add(pValueBlock);
        pRosterDetailPanel.Children.Add(pGrid);
    }

    private static string PRosterPhaseFormat(LWorkPhase pWorkPhase) => pWorkPhase switch
    {
        LWorkPhase.LWorkPhaseEncoding => "Being processed",
        LWorkPhase.LWorkPhaseStarted => "Started",
        _ => "Not started"
    };

    private static string PRosterKindFormat(LWorkKind pWorkKind) => pWorkKind switch
    {
        LWorkKind.LWorkKindSplit => "Split",
        LWorkKind.LWorkKindEdit => "Edit",
        LWorkKind.LWorkKindAudio => "Audio",
        LWorkKind.LWorkKindConvert => "Convert",
        LWorkKind.LWorkKindMerge => "Merge",
        _ => pWorkKind.ToString()
    };
}

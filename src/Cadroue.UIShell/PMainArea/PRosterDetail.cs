using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.Core;
using Cadroue.Media;
using Cadroue.UIShell.PAssets;
using Cadroue.UIShell.PMainWindow;
using Cadroue.UIShell.PPanels;

using Cadroue.Infrastructure;

namespace Cadroue.UIShell.PMainArea;

public sealed partial class PRoster
{
    private const string PRosterOpenIcon = "/PAssets/PPanels/PRosterOpen.svg";

    private static readonly Dictionary<string, LWorkMedia?> pRosterMediaCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> pRosterMediaPending = new(StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, double?> pRosterLoudnessCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, bool> pRosterLoudnessPending = new(StringComparer.OrdinalIgnoreCase);

    private StackPanel pRosterRowTarget = null!;

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
        pRosterRowTarget = pRosterDetailPanel;

        if (pRosterCardId != Guid.Empty)
        {
            PSummaryAdd(pRosterSchedule.LScheduleRecords
                .Where(pRecord => pRecord.LWorkBatchId == pRosterCardId)
                .ToArray());
            return;
        }

        if (PRosterSelectRead() is not { } pWorkItem)
        {
            pRosterDetailPanel.Children.Add(new TextBlock
            {
                Text = LLocalization.LLocalizationTextRead("Roster.Empty.Notice"),
                Foreground = PRosterTheme.PRosterMutedBrush,
                FontSize = PRosterTheme.PRosterRowSize,
                TextWrapping = TextWrapping.Wrap
            });
            return;
        }

        if (pWorkItem.LWorkStateCurrent == LWorkState.LWorkStateFailed
            && !string.IsNullOrWhiteSpace(pWorkItem.LWorkMessage))
        {
            PRosterRowAdd(
                LLocalization.LLocalizationTextRead("Roster.Field.FailureReason"),
                pWorkItem.LWorkMessage,
                pValueBold: true,
                pValueBrush: PRosterTheme.PRosterFailBrush);
        }

        LWorkMedia? pSourceInfo = pWorkItem.LWorkSourceMedia
            ?? PRosterMediaRead(pWorkItem.LWorkSourcePath);
        PRosterOverviewAdd(pWorkItem, pSourceInfo);
        PRosterRecordAdd(pWorkItem);

        pRosterRowTarget.Children.Add(new Border
        {
            Height = 1,
            Background = PRosterTheme.PRosterLineBrush,
            Margin = new Thickness(0, 12, 0, 10)
        });
        PRosterEncodingAdd(pWorkItem);
    }

    private void PRosterRecordAdd(LWorkItem pWorkItem)
    {
        PRosterSectionAdd(LLocalization.LLocalizationTextRead("Roster.Section.Record"), false);
        PRosterRowAdd(LLocalization.LLocalizationTextRead("Roster.Field.Queued"), pWorkItem.LWorkCreateTime.ToString("yyyy-MM-dd HH:mm:ss"));
        PRosterRowAdd(LLocalization.LLocalizationTextRead("Roster.Field.Started"), PRosterStampFormat(pWorkItem.LWorkStartTime));
        PRosterRowAdd(LLocalization.LLocalizationTextRead("Roster.Field.Finished"), PRosterStampFormat(pWorkItem.LWorkFinishTime));
        PRosterRowAdd(LLocalization.LLocalizationTextRead("Roster.Field.Attempts"), pWorkItem.LWorkAttemptCount.ToString());
        PRosterRowAdd(LLocalization.LLocalizationTextRead("Roster.Field.Owner"), PRosterOwnerFormat(pWorkItem));
        PRosterRowAdd(LLocalization.LLocalizationTextRead("Roster.Field.State"), PRosterPhaseFormat(pWorkItem.LWorkStateCurrent, pWorkItem.LWorkPhaseCurrent));
        PRosterRowAdd(LLocalization.LLocalizationTextRead("Roster.Field.Priority"), PRosterPriorityFormat(pWorkItem.LWorkPriority));

        if (pWorkItem.LWorkStateCurrent != LWorkState.LWorkStateFailed
            && !string.IsNullOrWhiteSpace(pWorkItem.LWorkMessage))
        {
            PRosterRowAdd(LLocalization.LLocalizationTextRead("Roster.Field.Message"), pWorkItem.LWorkMessage);
        }
    }

    private void PRosterEncodingAdd(LWorkItem pWorkItem)
    {
        LEncoding pOutput = pWorkItem.LWorkOutput;
        var pVideoPanel = new StackPanel();
        var pAudioPanel = new StackPanel { Margin = new Thickness(14, 0, 0, 0) };

        StackPanel pPreviousTarget = pRosterRowTarget;
        pRosterRowTarget = pVideoPanel;
        PRosterSectionAdd(LLocalization.LLocalizationTextRead("Roster.Section.EncodingVideo"), false);
        PRosterRowAdd(LLocalization.LLocalizationTextRead("Roster.Field.Mode"), $"{pOutput.LEncodingVideo.LEncodingMode} ({pOutput.LEncodingVideo.LEncodingStream})");
        if (PRosterReencodeCheck(pOutput.LEncodingVideo.LEncodingMode))
        {
            PRosterRowAdd(LLocalization.LLocalizationTextRead("Roster.Field.Encoder"), pOutput.LEncodingVideo.LEncodingEncoder);
            PRosterRowAdd(LLocalization.LLocalizationTextRead("Roster.Field.RateControl"), pOutput.LEncodingVideo.LEncodingRateControl);
            PRosterRowAdd(LLocalization.LLocalizationTextRead("Roster.Field.Quality"), pOutput.LEncodingVideo.LEncodingQuality);
            PRosterRowAdd(LLocalization.LLocalizationTextRead("Roster.Field.SpeedPreset"), pOutput.LEncodingVideo.LEncodingSpeedPreset);
            PRosterRowAdd(LLocalization.LLocalizationTextRead("Roster.Field.PixelFormat"), pOutput.LEncodingVideo.LEncodingPixel);

            if (pOutput.LEncodingVideo.LEncodingExtras.Count > 0)
            {
                PRosterRowAdd(LLocalization.LLocalizationTextRead("Roster.Field.Extras"), string.Join("  ", pOutput.LEncodingVideo.LEncodingExtras.Select(pExtra => $"{pExtra.Key} {pExtra.Value}")));
            }
        }

        pRosterRowTarget = pAudioPanel;
        PRosterSectionAdd(LLocalization.LLocalizationTextRead("Roster.Section.EncodingAudio"), false);
        PRosterRowAdd(LLocalization.LLocalizationTextRead("Roster.Field.Mode"), $"{pOutput.LEncodingAudio.LEncodingMode} ({pOutput.LEncodingAudio.LEncodingStream})");
        if (PRosterReencodeCheck(pOutput.LEncodingAudio.LEncodingMode))
        {
            PRosterRowAdd(LLocalization.LLocalizationTextRead("Roster.Field.Encoder"), pOutput.LEncodingAudio.LEncodingEncoder);
            PRosterRowAdd(LLocalization.LLocalizationTextRead("Roster.Field.RateControl"), pOutput.LEncodingAudio.LEncodingRateControl);
            PRosterRowAdd(LLocalization.LLocalizationTextRead("Roster.Field.Quality"), pOutput.LEncodingAudio.LEncodingQuality);
            if (!string.IsNullOrWhiteSpace(pOutput.LEncodingAudio.LEncodingSpeed))
            {
                PRosterRowAdd(LLocalization.LLocalizationTextRead("Roster.Field.SpeedPreset"), pOutput.LEncodingAudio.LEncodingSpeed);
            }

            if (pOutput.LEncodingAudio.LEncodingExtras.Count > 0)
            {
                PRosterRowAdd(LLocalization.LLocalizationTextRead("Roster.Field.Extras"), string.Join("  ", pOutput.LEncodingAudio.LEncodingExtras.Select(pExtra => $"{pExtra.Key} {pExtra.Value}")));
            }

            PRosterRowAdd(LLocalization.LLocalizationTextRead("Roster.Field.SampleRate"), pOutput.LEncodingAudio.LEncodingSampleRate);
            PRosterRowAdd(LLocalization.LLocalizationTextRead("Roster.Field.Channels"), pOutput.LEncodingAudio.LEncodingChannels);
        }

        pRosterRowTarget = pPreviousTarget;

        var pColumnGrid = new Grid();
        pColumnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pColumnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Grid.SetColumn(pAudioPanel, 1);
        pColumnGrid.Children.Add(pVideoPanel);
        pColumnGrid.Children.Add(pAudioPanel);
        pRosterRowTarget.Children.Add(pColumnGrid);
    }

    private static bool PRosterReencodeCheck(string pMode) =>
        !string.Equals(pMode, "Copy", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(pMode, "Exclude", StringComparison.OrdinalIgnoreCase);

    private void PRosterSectionAdd(string pSectionName, bool pSectionRule)
    {
        if (pSectionRule)
        {
            pRosterRowTarget.Children.Add(new Border
            {
                Height = 1,
                Background = PRosterTheme.PRosterLineBrush,
                Margin = new Thickness(0, 12, 0, 10)
            });
        }

        pRosterRowTarget.Children.Add(new TextBlock
        {
            Text = pSectionName,
            Foreground = PRosterTheme.PRosterTextBrush,
            FontSize = PRosterTheme.PRosterRowSize,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 6)
        });
    }

    private void PRosterRowAdd(string pLabel, string pValue, double pIndent = 0, bool pValueBold = false, Brush? pValueBrush = null)
    {
        var pGrid = new Grid { Margin = new Thickness(pIndent, 0, 0, 5) };
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(PRosterTheme.PRosterLabelWidth - pIndent) });
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
            Foreground = pValueBrush ?? PRosterTheme.PRosterTextBrush,
            FontSize = PRosterTheme.PRosterRowSize,
            FontWeight = pValueBold ? FontWeights.SemiBold : FontWeights.Normal,
            TextWrapping = TextWrapping.Wrap
        };
        Grid.SetColumn(pValueBlock, 1);
        pGrid.Children.Add(pValueBlock);
        pRosterRowTarget.Children.Add(pGrid);
    }

    private LWorkMedia? PRosterMediaRead(string pMediaPath)
    {
        if (string.IsNullOrWhiteSpace(pMediaPath) || !File.Exists(pMediaPath))
        {
            return null;
        }

        if (pRosterMediaCache.TryGetValue(pMediaPath, out LWorkMedia? pCached))
        {
            return pCached;
        }

        PRosterMediaDefer(pMediaPath);
        return null;
    }

    private void PRosterMediaDefer(string pMediaPath)
    {
        if (!pRosterMediaPending.Add(pMediaPath))
        {
            return;
        }

        LMediaProbe.LMediaProbeDefer(pMediaPath);
    }

    private void PRosterMediaHandle(LMediaProbeResult pResult)
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            string pMediaPath = pResult.LMediaProbePath;
            if (!pRosterMediaPending.Remove(pMediaPath))
            {
                return;
            }

            if (pResult.LMediaProbeInfo is not { } pProbedInfo)
            {
                LTraceLog.LTraceErrorRecord($"Job detail could not read '{Path.GetFileName(pMediaPath)}': {pResult.LMediaProbeError}");
                return;
            }

            pRosterMediaCache[pMediaPath] = new LWorkMedia(
                pProbedInfo.LMediaVideoWidth,
                pProbedInfo.LMediaVideoHeight,
                pProbedInfo.LMediaVideoRate,
                (long)Math.Round(pProbedInfo.LMediaInfoDuration.TotalMilliseconds),
                pProbedInfo.LMediaVideoPresent)
            {
                LWorkMediaCodec = pProbedInfo.LMediaAudioCodec,
                LWorkMediaBitrate = pProbedInfo.LMediaAudioBitrate,
                LWorkMediaSamplerate = pProbedInfo.LMediaSampleRate
            };

            if (PRosterSelectRead() is { } pSelected
                && (string.Equals(pSelected.LWorkSourcePath, pMediaPath, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(pSelected.LWorkOutputPath, pMediaPath, StringComparison.OrdinalIgnoreCase)))
            {
                PRosterDetailUpdate();
            }
        }));
    }

    private static void PRosterPathOpen(string pPath)
    {
        try
        {
            if (File.Exists(pPath))
            {
                Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{pPath}\"") { UseShellExecute = true });
                return;
            }

            string? pFolder = Path.GetDirectoryName(pPath);
            if (!string.IsNullOrWhiteSpace(pFolder) && Directory.Exists(pFolder))
            {
                Process.Start(new ProcessStartInfo(pFolder) { UseShellExecute = true });
            }
        }
        catch (Exception pOpenError)
        {
            LTraceLog.LTraceErrorRecord($"Could not open '{pPath}': {pOpenError.Message}");
        }
    }

}

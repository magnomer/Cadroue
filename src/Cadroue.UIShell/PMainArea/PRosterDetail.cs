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

namespace Cadroue.UIShell.PMainArea;

public sealed partial class PRoster
{
    private const string PRosterOpenIcon = "/PAssets/PPanels/PRosterOpen.svg";

    private static readonly Dictionary<string, LWorkMedia?> pRosterMediaCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> pRosterMediaPending = new(StringComparer.OrdinalIgnoreCase);

    private readonly StackPanel pRosterEncodingPanel = new();
    private Border pRosterEncodingRow = null!;
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

        pRosterEncodingRow = new Border
        {
            BorderBrush = PRosterTheme.PRosterLineBrush,
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(12, 10, 12, 12),
            Child = pRosterEncodingPanel
        };

        var pRoot = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(pHeader, Dock.Top);
        pRoot.Children.Add(pHeader);
        DockPanel.SetDock(pRosterEncodingRow, Dock.Bottom);
        pRoot.Children.Add(pRosterEncodingRow);
        pRoot.Children.Add(pScroll);

        PRosterDetailUpdate();
        return PPanel.PPanelBorderBuild(pRoot);
    }

    private void PRosterDetailUpdate()
    {
        pRosterDetailPanel.Children.Clear();
        pRosterEncodingPanel.Children.Clear();
        pRosterRowTarget = pRosterDetailPanel;

        if (PRosterSelectRead() is not { } pWorkItem)
        {
            pRosterEncodingRow.Visibility = Visibility.Collapsed;
            pRosterDetailPanel.Children.Add(new TextBlock
            {
                Text = LLocalization.LLocalizationTextRead("Roster.Empty.Notice"),
                Foreground = PRosterTheme.PRosterMutedBrush,
                FontSize = PRosterTheme.PRosterRowSize,
                TextWrapping = TextWrapping.Wrap
            });
            return;
        }

        pRosterEncodingRow.Visibility = Visibility.Visible;
        LWorkMedia? pSourceInfo = pWorkItem.LWorkSourceMedia
            ?? PRosterMediaRead(pWorkItem.LWorkSourcePath);
        PRosterSourceAdd(pWorkItem, pSourceInfo);
        PRosterOutputAdd(pWorkItem);
        PRosterJobAdd(pWorkItem, pSourceInfo);
        PRosterRecordAdd(pWorkItem);
        PRosterInternalAdd(pWorkItem);

        pRosterRowTarget = pRosterEncodingPanel;
        PRosterEncodingAdd(pWorkItem);
        pRosterRowTarget = pRosterDetailPanel;
    }

    private void PRosterSourceAdd(LWorkItem pWorkItem, LWorkMedia? pSourceInfo)
    {
        PRosterSectionAdd(LLocalization.LLocalizationTextRead("Roster.Section.Source"), false);
        PRosterPathAdd(LLocalization.LLocalizationTextRead("Roster.Field.Location"), pWorkItem.LWorkSourcePath);
        PRosterRowAdd(LLocalization.LLocalizationTextRead("Roster.Field.ResolutionFps"), PRosterMediaFormat(pSourceInfo, pWorkItem.LWorkSourcePath));
        PRosterRowAdd(LLocalization.LLocalizationTextRead("Roster.Field.Keyframe"), PRosterKeyframeFormat(pWorkItem.LWorkSourcePath));
        PRosterRowAdd(LLocalization.LLocalizationTextRead("Roster.Field.Size"), PRosterSizeFormat(PRosterSourceRead(pWorkItem)));
        PRosterRowAdd(LLocalization.LLocalizationTextRead("Roster.Field.Container"), PRosterContainerFormat(pWorkItem.LWorkSourcePath));
        PRosterRowAdd(LLocalization.LLocalizationTextRead("Roster.Field.Duration"), pSourceInfo is null
            ? PRosterPendingFormat(pWorkItem.LWorkSourcePath)
            : $"{pSourceInfo.LWorkMediaDuration:hh\\:mm\\:ss}");
    }

    private void PRosterOutputAdd(LWorkItem pWorkItem)
    {
        LWorkOutput pOutput = pWorkItem.LWorkOutput;
        LWorkMedia? pOutputInfo = pWorkItem.LWorkOutputMedia
            ?? PRosterMediaRead(pWorkItem.LWorkOutputPath);

        PRosterSectionAdd(LLocalization.LLocalizationTextRead("Roster.Section.Output"), true);
        PRosterPathAdd(LLocalization.LLocalizationTextRead("Roster.Field.Location"), pWorkItem.LWorkOutputPath);
        PRosterRowAdd(LLocalization.LLocalizationTextRead("Roster.Field.ResolutionFps"), pOutputInfo is not null
            ? PRosterMediaFormat(pOutputInfo, pWorkItem.LWorkOutputPath)
            : $"{pOutput.LWorkOutputVideoSize} / {pOutput.LWorkOutputVideoFps}");
        PRosterRowAdd(LLocalization.LLocalizationTextRead("Roster.Field.Keyframe"), PRosterOutputFormat(pOutputInfo));
        PRosterRowAdd(LLocalization.LLocalizationTextRead("Roster.Field.Size"), PRosterOutputRead(pWorkItem));
        PRosterRowAdd(LLocalization.LLocalizationTextRead("Roster.Field.Container"), pOutput.LWorkOutputContainer);
        PRosterRowAdd(LLocalization.LLocalizationTextRead("Roster.Field.Duration"), pOutputInfo is not null
            ? $"{pOutputInfo.LWorkMediaDuration:hh\\:mm\\:ss}"
            : $"{pWorkItem.LWorkDuration:hh\\:mm\\:ss}");
    }

    private void PRosterJobAdd(LWorkItem pWorkItem, LWorkMedia? pSourceInfo)
    {
        PRosterSectionAdd(LLocalization.LLocalizationTextRead("Roster.Section.Job"), true);

        string pPresetName = pWorkItem.LWorkOutput.LWorkOutputPresetName;
        if (!string.IsNullOrWhiteSpace(pPresetName))
        {
            PRosterRowAdd(LLocalization.LLocalizationTextRead("Roster.Field.Preset"), pPresetName, 0, true, PRosterTheme.PRosterAccentBrush);
        }

        PRosterRowAdd(
            PRosterKindFormat(pWorkItem.LWorkKind),
            $"{pWorkItem.LWorkOrigin:hh\\:mm\\:ss} - {pWorkItem.LWorkEnd:hh\\:mm\\:ss}  ({pWorkItem.LWorkDuration:hh\\:mm\\:ss})");

        LWorkCrop pCrop = pWorkItem.LWorkCrop;
        if (!pCrop.LWorkCropActive)
        {
            return;
        }

        PRosterRowAdd(LLocalization.LLocalizationTextRead("Roster.Field.Crop"), string.Empty);
        if (pCrop.LWorkEdgeActive)
        {
            PRosterRowAdd(LLocalization.LLocalizationTextRead("Roster.Field.Top"), $"{pCrop.LWorkCropTop} px", 14);
            PRosterRowAdd(LLocalization.LLocalizationTextRead("Roster.Field.Bottom"), $"{pCrop.LWorkCropBottom} px", 14);
            PRosterRowAdd(LLocalization.LLocalizationTextRead("Roster.Field.Left"), $"{pCrop.LWorkCropLeft} px", 14);
            PRosterRowAdd(LLocalization.LLocalizationTextRead("Roster.Field.Right"), $"{pCrop.LWorkCropRight} px", 14);
        }

        if (pCrop.LWorkCropRotation != 0)
        {
            PRosterRowAdd(LLocalization.LLocalizationTextRead("Roster.Field.Rotate"), $"{pCrop.LWorkCropRotation}°", 14);
        }

        if (pCrop.LWorkCropFlipHorizontal || pCrop.LWorkCropFlipVertical)
        {
            PRosterRowAdd(LLocalization.LLocalizationTextRead("Roster.Field.Flip"), PRosterFlipFormat(pCrop), 14);
        }

        PRosterRowAdd(LLocalization.LLocalizationTextRead("Roster.Field.Ratio"), PRosterRatioFormat(pWorkItem, pSourceInfo), 14);
        PRosterRowAdd(LLocalization.LLocalizationTextRead("Roster.Field.Result"), PRosterResolutionFormat(pWorkItem, pSourceInfo), 14);
    }

    private void PRosterRecordAdd(LWorkItem pWorkItem)
    {
        PRosterSectionAdd(LLocalization.LLocalizationTextRead("Roster.Section.Record"), true);
        PRosterRowAdd(LLocalization.LLocalizationTextRead("Roster.Field.Started"), PRosterStampFormat(pWorkItem.LWorkStartTime));
        PRosterRowAdd(LLocalization.LLocalizationTextRead("Roster.Field.Finished"), PRosterStampFormat(pWorkItem.LWorkFinishTime));
        PRosterRowAdd(LLocalization.LLocalizationTextRead("Roster.Field.Time"), PRosterSpentFormat(pWorkItem));
        PRosterRowAdd(LLocalization.LLocalizationTextRead("Roster.Field.Speed"), PRosterSpeedFormat(pWorkItem));
    }

    private void PRosterInternalAdd(LWorkItem pWorkItem)
    {
        PRosterSectionAdd(LLocalization.LLocalizationTextRead("Roster.Section.Internal"), true);
        PRosterRowAdd(LLocalization.LLocalizationTextRead("Roster.Field.State"), PRosterStateLabel.PRosterStateFormat(pWorkItem.LWorkStateCurrent));
        PRosterRowAdd(LLocalization.LLocalizationTextRead("Roster.Field.Owner"), PRosterOwnerFormat(pWorkItem));
        PRosterRowAdd(LLocalization.LLocalizationTextRead("Roster.Field.Attempts"), pWorkItem.LWorkAttemptCount.ToString());
        PRosterRowAdd(LLocalization.LLocalizationTextRead("Roster.Field.Phase"), PRosterPhaseFormat(pWorkItem.LWorkStateCurrent, pWorkItem.LWorkPhaseCurrent));
        PRosterRowAdd(LLocalization.LLocalizationTextRead("Roster.Field.Priority"), PRosterPriorityFormat(pWorkItem.LWorkPriority));
        PRosterRowAdd(LLocalization.LLocalizationTextRead("Roster.Field.ExportMode"), pWorkItem.LWorkOutput.LWorkOutputExportMode);
        PRosterRowAdd(LLocalization.LLocalizationTextRead("Roster.Field.Queued"), pWorkItem.LWorkCreateTime.ToString("yyyy-MM-dd HH:mm:ss"));

        if (!string.IsNullOrWhiteSpace(pWorkItem.LWorkMessage))
        {
            PRosterRowAdd(LLocalization.LLocalizationTextRead("Roster.Field.Message"), pWorkItem.LWorkMessage);
        }
    }

    private void PRosterEncodingAdd(LWorkItem pWorkItem)
    {
        LWorkOutput pOutput = pWorkItem.LWorkOutput;
        var pVideoPanel = new StackPanel();
        var pAudioPanel = new StackPanel { Margin = new Thickness(14, 0, 0, 0) };

        StackPanel pPreviousTarget = pRosterRowTarget;
        pRosterRowTarget = pVideoPanel;
        PRosterSectionAdd(LLocalization.LLocalizationTextRead("Roster.Section.EncodingVideo"), false);
        PRosterRowAdd(LLocalization.LLocalizationTextRead("Roster.Field.Mode"), $"{pOutput.LWorkOutputVideoMode} ({pOutput.LWorkOutputVideoStream})");
        PRosterRowAdd(LLocalization.LLocalizationTextRead("Roster.Field.Encoder"), pOutput.LWorkOutputVideoEncoder);
        PRosterRowAdd(LLocalization.LLocalizationTextRead("Roster.Field.RateControl"), pOutput.LWorkOutputRateControl);
        PRosterRowAdd(LLocalization.LLocalizationTextRead("Roster.Field.Quality"), pOutput.LWorkOutputQuality);
        PRosterRowAdd(LLocalization.LLocalizationTextRead("Roster.Field.SpeedPreset"), pOutput.LWorkOutputSpeedPreset);
        PRosterRowAdd(LLocalization.LLocalizationTextRead("Roster.Field.PixelFormat"), pOutput.LWorkOutputPixelFormat);

        if (pOutput.LWorkOutputVideoExtras.Count > 0)
        {
            PRosterRowAdd(LLocalization.LLocalizationTextRead("Roster.Field.Extras"), string.Join("  ", pOutput.LWorkOutputVideoExtras.Select(pExtra => $"{pExtra.Key} {pExtra.Value}")));
        }

        pRosterRowTarget = pAudioPanel;
        PRosterSectionAdd(LLocalization.LLocalizationTextRead("Roster.Section.EncodingAudio"), false);
        PRosterRowAdd(LLocalization.LLocalizationTextRead("Roster.Field.Mode"), $"{pOutput.LWorkOutputAudioMode} ({pOutput.LWorkOutputAudioStream})");
        PRosterRowAdd(LLocalization.LLocalizationTextRead("Roster.Field.Encoder"), pOutput.LWorkOutputAudioEncoder);
        PRosterRowAdd(LLocalization.LLocalizationTextRead("Roster.Field.Bitrate"), pOutput.LWorkOutputAudioBitrate);
        PRosterRowAdd(LLocalization.LLocalizationTextRead("Roster.Field.SampleRate"), pOutput.LWorkOutputAudioSampleRate);
        PRosterRowAdd(LLocalization.LLocalizationTextRead("Roster.Field.Channels"), pOutput.LWorkOutputAudioChannels);

        pRosterRowTarget = pPreviousTarget;

        var pColumnGrid = new Grid();
        pColumnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pColumnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Grid.SetColumn(pAudioPanel, 1);
        pColumnGrid.Children.Add(pVideoPanel);
        pColumnGrid.Children.Add(pAudioPanel);
        pRosterRowTarget.Children.Add(pColumnGrid);
    }

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

    private void PRosterPathAdd(string pLabel, string pPath)
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

        var pOpenButton = new Button
        {
            Content = new Image
            {
                Width = 12,
                Height = 12,
                Source = PIcon.PIconRead(PRosterOpenIcon, PRosterTheme.PRosterTextBrush),
                Stretch = Stretch.Uniform
            },
            Width = 20,
            Height = PRosterTheme.PRosterRowHeight,
            Margin = new Thickness(6, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Top,
            ToolTip = LLocalization.LLocalizationTextRead("Roster.Explorer.Tooltip"),
            Style = PButton.PButtonPanelCreate()
        };
        pOpenButton.Click += (_, _) => PRosterPathOpen(pPath);

        var pValuePanel = new WrapPanel();
        pValuePanel.Children.Add(new TextBlock
        {
            Text = pPath,
            Foreground = PRosterTheme.PRosterTextBrush,
            FontSize = PRosterTheme.PRosterRowSize,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Top
        });
        pValuePanel.Children.Add(pOpenButton);

        Grid.SetColumn(pValuePanel, 1);
        pGrid.Children.Add(pValuePanel);
        pRosterRowTarget.Children.Add(pGrid);
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

        Guid pRosterProbeId = PRosterSelectRead()?.LWorkId ?? Guid.Empty;
        _ = Task.Run(() =>
        {
            LWorkMedia? pProbed = null;
            try
            {
                LMediaInfo pProbedInfo = LMediaInfo.LMediaFfprobeRead(pMediaPath);
                pProbed = new LWorkMedia(
                    pProbedInfo.LMediaVideoWidth,
                    pProbedInfo.LMediaVideoHeight,
                    pProbedInfo.LMediaVideoRate,
                    (long)Math.Round(pProbedInfo.LMediaInfoDuration.TotalMilliseconds),
                    pProbedInfo.LMediaVideoPresent);
            }
            catch (Exception pProbeError)
            {
                LTraceLog.LTraceErrorRecord($"Job detail could not read '{Path.GetFileName(pMediaPath)}': {pProbeError.Message}");
            }

            Dispatcher.BeginInvoke(new Action(() =>
            {
                pRosterMediaPending.Remove(pMediaPath);
                if (pProbed is not null)
                {
                    pRosterMediaCache[pMediaPath] = pProbed;
                }

                if (PRosterSelectRead()?.LWorkId == pRosterProbeId)
                {
                    PRosterDetailUpdate();
                }
            }));
        });
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

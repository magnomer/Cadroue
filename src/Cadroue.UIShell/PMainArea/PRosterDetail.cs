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
    private const string PRosterOpenIconPath = "/PAssets/PPanels/PRosterOpen.svg";

    private static readonly Dictionary<string, LMediaInfo?> pRosterMediaCache = new(StringComparer.OrdinalIgnoreCase);

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
                Text = "Select a job to see its settings.",
                Foreground = PRosterTheme.PRosterMutedBrush,
                FontSize = PRosterTheme.PRosterRowSize,
                TextWrapping = TextWrapping.Wrap
            });
            return;
        }

        pRosterEncodingRow.Visibility = Visibility.Visible;
        LMediaInfo? pSourceInfo = PRosterMediaRead(pWorkItem.LWorkSourcePath);
        PRosterSourceAdd(pWorkItem, pSourceInfo);
        PRosterOutputAdd(pWorkItem);
        PRosterJobAdd(pWorkItem, pSourceInfo);
        PRosterRecordAdd(pWorkItem);
        PRosterInternalAdd(pWorkItem);

        pRosterRowTarget = pRosterEncodingPanel;
        PRosterEncodingAdd(pWorkItem);
        pRosterRowTarget = pRosterDetailPanel;
    }

    private void PRosterSourceAdd(LWorkItem pWorkItem, LMediaInfo? pSourceInfo)
    {
        PRosterSectionAdd("Source", false);
        PRosterPathAdd("Location", pWorkItem.LWorkSourcePath);
        PRosterRowAdd("Size / FPS", PRosterMediaFormat(pSourceInfo));
        PRosterRowAdd("Container", PRosterContainerFormat(pWorkItem.LWorkSourcePath));
        PRosterRowAdd("Duration", pSourceInfo is null
            ? "Unknown"
            : $"{pSourceInfo.LMediaInfoDuration:hh\\:mm\\:ss}");
    }

    private void PRosterOutputAdd(LWorkItem pWorkItem)
    {
        LWorkOutput pOutput = pWorkItem.LWorkOutput;
        LMediaInfo? pOutputInfo = PRosterMediaRead(pWorkItem.LWorkOutputPath);

        PRosterSectionAdd("Output", true);
        PRosterPathAdd("Location", pWorkItem.LWorkOutputPath);
        PRosterRowAdd("Size / FPS", pOutputInfo is not null
            ? PRosterMediaFormat(pOutputInfo)
            : $"{pOutput.LWorkOutputVideoSize} / {pOutput.LWorkOutputVideoFps}");
        PRosterRowAdd("Container", pOutput.LWorkOutputContainer);
        PRosterRowAdd("Duration", pOutputInfo is not null
            ? $"{pOutputInfo.LMediaInfoDuration:hh\\:mm\\:ss}"
            : $"{pWorkItem.LWorkDuration:hh\\:mm\\:ss}");
    }

    private void PRosterJobAdd(LWorkItem pWorkItem, LMediaInfo? pSourceInfo)
    {
        PRosterSectionAdd("Job", true);
        PRosterRowAdd(
            PRosterKindFormat(pWorkItem.LWorkKind),
            $"{pWorkItem.LWorkStart:hh\\:mm\\:ss} - {pWorkItem.LWorkEnd:hh\\:mm\\:ss}  ({pWorkItem.LWorkDuration:hh\\:mm\\:ss})");

        LWorkCrop pCrop = pWorkItem.LWorkCrop;
        if (!pCrop.LWorkCropActive)
        {
            return;
        }

        PRosterRowAdd("Crop", string.Empty);
        if (pCrop.LWorkCropEdgeActive)
        {
            PRosterRowAdd("Top", $"{pCrop.LWorkCropTop} px", 14);
            PRosterRowAdd("Bottom", $"{pCrop.LWorkCropBottom} px", 14);
            PRosterRowAdd("Left", $"{pCrop.LWorkCropLeft} px", 14);
            PRosterRowAdd("Right", $"{pCrop.LWorkCropRight} px", 14);
        }

        if (pCrop.LWorkCropRotation != 0)
        {
            PRosterRowAdd("Rotate", $"{pCrop.LWorkCropRotation}°", 14);
        }

        if (pCrop.LWorkCropFlipHorizontal || pCrop.LWorkCropFlipVertical)
        {
            PRosterRowAdd("Flip", PRosterFlipFormat(pCrop), 14);
        }

        PRosterRowAdd("Ratio", PRosterRatioFormat(pWorkItem, pSourceInfo), 14);
        PRosterRowAdd("Result", PRosterResolutionFormat(pWorkItem, pSourceInfo), 14);
    }

    private void PRosterRecordAdd(LWorkItem pWorkItem)
    {
        PRosterSectionAdd("Record", true);
        PRosterRowAdd("Started", PRosterStampFormat(pWorkItem.LWorkStartTime));
        PRosterRowAdd("Finished", PRosterStampFormat(pWorkItem.LWorkFinishTime));
        PRosterRowAdd("Time", PRosterSpentFormat(pWorkItem));
        PRosterRowAdd("Speed", PRosterSpeedFormat(pWorkItem));
    }

    private void PRosterInternalAdd(LWorkItem pWorkItem)
    {
        PRosterSectionAdd("Internal", true);
        PRosterRowAdd("State", PRosterStateLabel.PRosterStateFormat(pWorkItem.LWorkStateCurrent));
        PRosterRowAdd("Owner", PRosterOwnerFormat(pWorkItem));
        PRosterRowAdd("Attempts", pWorkItem.LWorkAttemptCount.ToString());

        if (pWorkItem.LWorkStateCurrent == LWorkState.LWorkStateRunning)
        {
            PRosterRowAdd("Phase", PRosterPhaseFormat(pWorkItem.LWorkPhaseCurrent));
        }

        PRosterRowAdd("Priority", PRosterPriorityFormat(pWorkItem.LWorkPriority));
        PRosterRowAdd("Export mode", pWorkItem.LWorkOutput.LWorkOutputExportMode);
        PRosterRowAdd("Queued", pWorkItem.LWorkCreateTime.ToString("yyyy-MM-dd HH:mm:ss"));

        if (!string.IsNullOrWhiteSpace(pWorkItem.LWorkMessage))
        {
            PRosterRowAdd("Message", pWorkItem.LWorkMessage);
        }
    }

    private void PRosterEncodingAdd(LWorkItem pWorkItem)
    {
        LWorkOutput pOutput = pWorkItem.LWorkOutput;
        var pVideoPanel = new StackPanel();
        var pAudioPanel = new StackPanel { Margin = new Thickness(14, 0, 0, 0) };

        StackPanel pPreviousTarget = pRosterRowTarget;
        pRosterRowTarget = pVideoPanel;
        PRosterSectionAdd("Encoding (Video)", false);
        PRosterRowAdd("Mode", $"{pOutput.LWorkOutputVideoMode} ({pOutput.LWorkOutputVideoStream})");
        PRosterRowAdd("Encoder", pOutput.LWorkOutputVideoEncoder);
        PRosterRowAdd("Rate control", pOutput.LWorkOutputRateControl);
        PRosterRowAdd("Quality", pOutput.LWorkOutputQuality);
        PRosterRowAdd("Speed preset", pOutput.LWorkOutputSpeedPreset);
        PRosterRowAdd("Pixel format", pOutput.LWorkOutputPixelFormat);

        if (pOutput.LWorkOutputVideoExtras.Count > 0)
        {
            PRosterRowAdd("Extras", string.Join("  ", pOutput.LWorkOutputVideoExtras.Select(pExtra => $"{pExtra.Key} {pExtra.Value}")));
        }

        pRosterRowTarget = pAudioPanel;
        PRosterSectionAdd("Encoding (Audio)", false);
        PRosterRowAdd("Mode", $"{pOutput.LWorkOutputAudioMode} ({pOutput.LWorkOutputAudioStream})");
        PRosterRowAdd("Encoder", pOutput.LWorkOutputAudioEncoder);
        PRosterRowAdd("Bitrate", pOutput.LWorkOutputAudioBitrate);
        PRosterRowAdd("Sample rate", pOutput.LWorkOutputAudioSampleRate);
        PRosterRowAdd("Channels", pOutput.LWorkOutputAudioChannels);

        pRosterRowTarget = pPreviousTarget;

        var pColumnGrid = new Grid();
        pColumnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pColumnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Grid.SetColumn(pAudioPanel, 1);
        pColumnGrid.Children.Add(pVideoPanel);
        pColumnGrid.Children.Add(pAudioPanel);
        pRosterRowTarget.Children.Add(pColumnGrid);
    }

    private static string PRosterStampFormat(DateTimeOffset? pStamp) =>
        pStamp is { } pValue ? pValue.ToString("yyyy-MM-dd HH:mm:ss") : "Not yet";

    private static string PRosterSpentFormat(LWorkItem pWorkItem)
    {
        if (PRosterSpentRead(pWorkItem) is not { } pSpent)
        {
            return "Not yet";
        }

        return $"{pSpent:hh\\:mm\\:ss\\.fff}";
    }

    private static string PRosterSpeedFormat(LWorkItem pWorkItem)
    {
        if (PRosterSpentRead(pWorkItem) is not { } pSpent
            || pSpent.TotalSeconds <= 0
            || !File.Exists(pWorkItem.LWorkOutputPath))
        {
            return "Not yet";
        }

        double pMebibytes = new FileInfo(pWorkItem.LWorkOutputPath).Length / 1048576d;
        return $"{pMebibytes / pSpent.TotalSeconds:0.##} MiB/s";
    }

    private static TimeSpan? PRosterSpentRead(LWorkItem pWorkItem)
    {
        if (pWorkItem.LWorkStartTime is not { } pStarted)
        {
            return null;
        }

        DateTimeOffset pFinished = pWorkItem.LWorkFinishTime ?? DateTimeOffset.Now;
        TimeSpan pSpent = pFinished - pStarted;
        return pSpent < TimeSpan.Zero ? null : pSpent;
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
                Width = 13,
                Height = 13,
                Source = PIcon.PIconRead(PRosterOpenIconPath, PRosterTheme.PRosterTextBrush),
                Stretch = Stretch.Uniform
            },
            Width = 22,
            Height = 20,
            Margin = new Thickness(6, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Bottom,
            ToolTip = "Show this file in Explorer",
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
            VerticalAlignment = VerticalAlignment.Bottom
        });
        pValuePanel.Children.Add(pOpenButton);

        Grid.SetColumn(pValuePanel, 1);
        pGrid.Children.Add(pValuePanel);
        pRosterRowTarget.Children.Add(pGrid);
    }

    private void PRosterRowAdd(string pLabel, string pValue, double pIndent = 0)
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
            Foreground = PRosterTheme.PRosterTextBrush,
            FontSize = PRosterTheme.PRosterRowSize,
            TextWrapping = TextWrapping.Wrap
        };
        Grid.SetColumn(pValueBlock, 1);
        pGrid.Children.Add(pValueBlock);
        pRosterRowTarget.Children.Add(pGrid);
    }

    private static LMediaInfo? PRosterMediaRead(string pMediaPath)
    {
        if (string.IsNullOrWhiteSpace(pMediaPath) || !File.Exists(pMediaPath))
        {
            return null;
        }

        if (pRosterMediaCache.TryGetValue(pMediaPath, out LMediaInfo? pCached))
        {
            return pCached;
        }

        LMediaInfo? pProbed = null;
        try
        {
            pProbed = LMediaInfo.LMediaFfprobeRead(pMediaPath);
        }
        catch (Exception pProbeError)
        {
            LAppLog.LError($"Job detail could not read '{Path.GetFileName(pMediaPath)}': {pProbeError.Message}");
        }

        if (pProbed is not null)
        {
            pRosterMediaCache[pMediaPath] = pProbed;
        }

        return pProbed;
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
            LAppLog.LError($"Could not open '{pPath}': {pOpenError.Message}");
        }
    }

    private static string PRosterMediaFormat(LMediaInfo? pMediaInfo)
    {
        if (pMediaInfo is null || !pMediaInfo.LMediaInfoVideoPresent)
        {
            return pMediaInfo is null ? "Unknown" : "Audio only";
        }

        return $"{pMediaInfo.LMediaInfoVideoWidth} x {pMediaInfo.LMediaInfoVideoHeight}  /  " +
            $"{pMediaInfo.LMediaInfoVideoFrameRate:0.###} fps";
    }

    private static string PRosterContainerFormat(string pMediaPath)
    {
        string pExtension = Path.GetExtension(pMediaPath).TrimStart('.');
        return pExtension.Length == 0 ? "Unknown" : pExtension.ToUpperInvariant();
    }

    private static string PRosterFlipFormat(LWorkCrop pCrop)
    {
        if (pCrop.LWorkCropFlipHorizontal && pCrop.LWorkCropFlipVertical)
        {
            return "Horizontal and vertical";
        }

        return pCrop.LWorkCropFlipHorizontal ? "Horizontal" : "Vertical";
    }

    private static string PRosterRatioFormat(LWorkItem pWorkItem, LMediaInfo? pSourceInfo)
    {
        if (PRosterCropSizeRead(pWorkItem, pSourceInfo) is not { } pCropSize)
        {
            return "Unknown";
        }

        int pDivisor = PRosterDivisorRead(pCropSize.PRosterWidth, pCropSize.PRosterHeight);
        return $"{pCropSize.PRosterWidth / pDivisor} : {pCropSize.PRosterHeight / pDivisor}";
    }

    private static string PRosterResolutionFormat(LWorkItem pWorkItem, LMediaInfo? pSourceInfo)
    {
        if (PRosterCropSizeRead(pWorkItem, pSourceInfo) is not { } pCropSize)
        {
            return "Unknown";
        }

        int pWidth = pCropSize.PRosterWidth;
        int pHeight = pCropSize.PRosterHeight;

        string[] pSizeParts = pWorkItem.LWorkOutput.LWorkOutputVideoSize.Split(
            ['x', 'X', '×'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (pSizeParts.Length == 2
            && int.TryParse(pSizeParts[0], out int pSizeWidth)
            && int.TryParse(pSizeParts[1], out int pSizeHeight))
        {
            bool pPortrait = pHeight > pWidth;
            int pShortEdge = Math.Min(pSizeWidth, pSizeHeight);
            int pLongEdge = Math.Max(pSizeWidth, pSizeHeight);
            pWidth = pWorkItem.LWorkOutput.LWorkSizeReactive && pPortrait ? pShortEdge : pSizeWidth;
            pHeight = pWorkItem.LWorkOutput.LWorkSizeReactive && pPortrait ? pLongEdge : pSizeHeight;
        }

        return $"{pWidth} x {pHeight}";
    }

    private static (int PRosterWidth, int PRosterHeight)? PRosterCropSizeRead(LWorkItem pWorkItem, LMediaInfo? pSourceInfo)
    {
        if (pSourceInfo is null || !pSourceInfo.LMediaInfoVideoPresent)
        {
            return null;
        }

        LWorkCrop pCrop = pWorkItem.LWorkCrop;
        int pWidth = pSourceInfo.LMediaInfoVideoWidth;
        int pHeight = pSourceInfo.LMediaInfoVideoHeight;
        if (pCrop.LWorkCropRotation is 90 or 270)
        {
            (pWidth, pHeight) = (pHeight, pWidth);
        }

        pWidth -= pCrop.LWorkCropLeft + pCrop.LWorkCropRight;
        pHeight -= pCrop.LWorkCropTop + pCrop.LWorkCropBottom;
        return pWidth > 0 && pHeight > 0 ? (pWidth, pHeight) : null;
    }

    private static int PRosterDivisorRead(int pFirst, int pSecond)
    {
        while (pSecond != 0)
        {
            (pFirst, pSecond) = (pSecond, pFirst % pSecond);
        }

        return pFirst == 0 ? 1 : pFirst;
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

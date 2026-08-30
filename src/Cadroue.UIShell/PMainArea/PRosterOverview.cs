using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.Core;
using Cadroue.UIShell.PAssets;
using Cadroue.UIShell.PMainWindow;
using Cadroue.ShellEngine;

namespace Cadroue.UIShell.PMainArea;

public sealed partial class PRoster
{
    private void PRosterOverviewAdd(LWorkItem pWorkItem, LWorkMedia? pSourceInfo)
    {
        PRosterSectionAdd(LLocalization.LLocalizationTextRead("Roster.Section.Overview"), false);

        string pTabName = PRosterTabRead(pWorkItem);
        if (!string.IsNullOrWhiteSpace(pTabName))
        {
            pRosterRowTarget.Children.Add(new TextBlock
            {
                Text = pTabName,
                Foreground = PRosterTheme.PRosterAccentBrush,
                FontSize = PRosterTheme.PRosterRowSize,
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 6)
            });
        }

        long? pSourceBytes = PRosterSourceRead(pWorkItem);
        long? pOutputBytes = PRosterBytesRead(pWorkItem);
        if (pSourceBytes is { } pSourceWhole && pSourceWhole > 0 && pOutputBytes is { } pOutputWhole && pOutputWhole >= 0)
        {
            if (PRosterMeterBuild(pWorkItem) is { } pMeter)
            {
                pRosterRowTarget.Children.Add(pMeter);
            }

            pRosterRowTarget.Children.Add(PRosterOverviewBuild(pSourceWhole, pOutputWhole));
        }

        pRosterRowTarget.Children.Add(PRosterComparisonBuild(pWorkItem, pSourceInfo, pSourceBytes, pOutputBytes));

        pRosterRowTarget.Children.Add(new Border
        {
            Height = 1,
            Background = PRosterTheme.PRosterLineBrush,
            Margin = new Thickness(0, 12, 0, 10)
        });
    }

    private static string PRosterTabRead(LWorkItem pWorkItem)
        => LCartographer.LCartographerTitleRead(pWorkItem);

    private static UIElement? PRosterMeterBuild(LWorkItem pWorkItem)
    {
        if (PRosterSpentRead(pWorkItem) is null)
        {
            return null;
        }

        return new TextBlock
        {
            Text = $"{PRosterSpentFormat(pWorkItem)} / {PRosterSpeedFormat(pWorkItem)}",
            Foreground = PRosterTheme.PRosterMutedBrush,
            FontSize = PRosterTheme.PRosterRowSize,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 2)
        };
    }

    private static UIElement PRosterOverviewBuild(long pSourceBytes, long pOutputBytes)
    {
        bool pOverGrown = pOutputBytes > pSourceBytes;
        double pRestBytes = pOverGrown ? pSourceBytes : (double)pSourceBytes - pOutputBytes;
        double pMarkBytes = pOverGrown ? (double)pOutputBytes - pSourceBytes : pOutputBytes;
        Brush pMarkBrush = pOverGrown ? PRosterTheme.PRosterFailBrush : PRosterTheme.PRosterAccentBrush;
        const double pOverviewRadius = 4;

        var pBar = new Grid { Height = 18 };
        pBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(Math.Max(pRestBytes, 0.0001), GridUnitType.Star) });
        pBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(Math.Max(pMarkBytes, 0.0001), GridUnitType.Star) });

        var pRestFill = new Border
        {
            Background = PRosterTheme.PRosterTrackBrush,
            CornerRadius = new CornerRadius(pOverviewRadius, 0, 0, pOverviewRadius)
        };
        var pMarkFill = new Border
        {
            Background = pMarkBrush,
            CornerRadius = new CornerRadius(0, pOverviewRadius, pOverviewRadius, 0)
        };
        Grid.SetColumn(pMarkFill, 1);
        pBar.Children.Add(pRestFill);
        pBar.Children.Add(pMarkFill);

        double pShownPercent = Math.Round((double)pOutputBytes / pSourceBytes * 100);
        Brush pTextBrush = pShownPercent > 100
            ? PRosterTheme.PRosterFailBrush
            : pShownPercent < 100
                ? PRosterTheme.PRosterAccentBrush
                : PRosterTheme.PRosterTextBrush;

        var pPercent = new TextBlock
        {
            Text = $"{pShownPercent:0}%",
            Foreground = pTextBrush,
            FontSize = PRosterTheme.PRosterRowSize,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 0, 0)
        };
        DockPanel.SetDock(pPercent, Dock.Right);

        var pRow = new DockPanel { Margin = new Thickness(0, 2, 0, 6) };
        pRow.Children.Add(pPercent);
        pRow.Children.Add(pBar);
        return pRow;
    }

    private UIElement PRosterComparisonBuild(LWorkItem pWorkItem, LWorkMedia? pSourceInfo, long? pSourceBytes, long? pOutputBytes)
    {
        LEncoding pOutput = pWorkItem.LWorkOutput;
        LWorkMedia? pOutputInfo = pWorkItem.LWorkOutputMedia;

        // A source with no figures yet is still being measured (low-priority, one at a time);
        // only once measurement has been attempted does a missing figure mean "Unknown".
        string pSourceUnknown = LLocalization.LLocalizationTextRead(
            pWorkItem.LWorkSourceMeasured ? "Roster.Value.Unknown" : "Roster.Value.Measuring");

        bool pSourceAudio = (pSourceInfo?.LWorkMediaSamplerate ?? 0) > 0;
        bool pOutputAudio = (pOutputInfo?.LWorkMediaSamplerate ?? 0) > 0;
        double? pSourceLoudness = pSourceAudio ? pSourceInfo?.LWorkMediaLoudness : null;
        double? pOutputLoudness = pWorkItem.LWorkAudio.LWorkAudioActive
            ? (pOutputAudio ? pOutputInfo?.LWorkMediaLoudness : null)
            : pSourceLoudness;

        var pSourceStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Stretch };
        var pOutputStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Stretch };

        pSourceStack.Children.Add(PRosterOpenBuild(pWorkItem.LWorkSourcePath));
        pOutputStack.Children.Add(PRosterOpenBuild(pWorkItem.LWorkOutputPath));

        void PairAdd(string pLeft, string pRight, bool pHeader)
        {
            pSourceStack.Children.Add(PRosterLineBuild(pLeft, pHeader));
            pOutputStack.Children.Add(PRosterLineBuild(pRight, pHeader, !pHeader && !string.Equals(pLeft, pRight, StringComparison.Ordinal)));
        }

        PairAdd(
            LLocalization.LLocalizationTextRead("Roster.Section.Source"),
            LLocalization.LLocalizationTextRead("Roster.Section.Output"), true);
        PairAdd(
            pSourceInfo is null ? pSourceUnknown : PRosterMebiFormat(pSourceBytes),
            PRosterMebiFormat(pOutputBytes), false);
        PairAdd(
            pSourceInfo is null ? pSourceUnknown : PRosterDimensionFormat(pSourceInfo, pWorkItem.LWorkSourcePath),
            pOutputInfo is { LWorkMediaVideo: true }
                ? $"{pOutputInfo.LWorkMediaWidth} x {pOutputInfo.LWorkMediaHeight}"
                : PRosterDimensionFormat(pOutput.LEncodingVideo.LEncodingSize), false);
        PairAdd(
            pSourceInfo is null ? pSourceUnknown : PRosterFpsFormat(pSourceInfo, pWorkItem.LWorkSourcePath),
            pOutputInfo is { LWorkMediaVideo: true }
                ? $"{pOutputInfo.LWorkMediaFramerate:0.###} fps"
                : PRosterFpsFormat(pOutput.LEncodingVideo.LEncodingFps), false);
        PairAdd(
            pSourceInfo is null
                ? pSourceUnknown
                : PRosterRateFormat(pSourceInfo.LWorkKeyframeInterval is { } pSourceInterval && pSourceInterval > 0
                    ? pSourceInterval / 1000d
                    : null),
            PRosterRateFormat(pOutputInfo?.LWorkKeyframeInterval is { } pOutputInterval && pOutputInterval > 0
                ? pOutputInterval / 1000d
                : null), false);
        PairAdd(
            pSourceInfo is null
                ? pSourceUnknown
                : PRosterClockFormat(pSourceInfo.LWorkMediaDuration),
            PRosterClockFormat(pOutputInfo?.LWorkMediaDuration ?? pWorkItem.LWorkDuration), false);
        PairAdd(
            PRosterContainerFormat(pWorkItem.LWorkSourcePath),
            PRosterContainerFormat(pWorkItem.LWorkOutputPath), false);

        Grid pColumnsGrid = PRosterGridBuild(pSourceStack, pOutputStack);

        var pSourcePaths = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 0, 8, 0)
        };
        IReadOnlyList<string> pSources = pWorkItem.LWorkMergeSources.Count > 1
            ? pWorkItem.LWorkMergeSources
            : new[] { pWorkItem.LWorkSourcePath };
        foreach (string pSourcePath in pSources)
        {
            pSourcePaths.Children.Add(PRosterPathBuild(pSourcePath));
        }

        var pOutputPaths = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(8, 0, 0, 0)
        };
        pOutputPaths.Children.Add(PRosterPathBuild(pWorkItem.LWorkOutputPath));

        var pPathGrid = new Grid { Margin = new Thickness(0, 8, 0, 0) };
        pPathGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pPathGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Grid.SetColumn(pOutputPaths, 1);
        pPathGrid.Children.Add(pSourcePaths);
        pPathGrid.Children.Add(pOutputPaths);

        var pRoot = new StackPanel { Margin = new Thickness(0, 10, 0, 0) };
        Grid.SetIsSharedSizeScope(pRoot, true);
        pRoot.Children.Add(pColumnsGrid);

        if (pSourceAudio || pOutputAudio)
        {
            pRoot.Children.Add(PRosterDividerBuild());
            pRoot.Children.Add(PRosterPanelBuild(new[]
            {
                (PRosterCodecFormat(pSourceInfo), PRosterCodecFormat(pOutputInfo)),
                (PRosterBitrateFormat(pSourceInfo), PRosterBitrateFormat(pOutputInfo)),
                (PRosterSampleFormat(pSourceInfo), PRosterSampleFormat(pOutputInfo)),
                (PRosterLoudnessFormat(pSourceLoudness), PRosterLoudnessFormat(pOutputLoudness))
            }));
        }

        pRoot.Children.Add(PRosterDividerBuild());
        pRoot.Children.Add(pPathGrid);
        return pRoot;
    }

    private static UIElement PRosterPathBuild(string pPath) =>
        new TextBlock
        {
            Text = pPath,
            Foreground = PRosterTheme.PRosterMutedBrush,
            FontSize = PRosterTheme.PRosterRowSize,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 2)
        };

    private static Border PRosterDividerBuild() =>
        new()
        {
            Height = 1,
            Background = PRosterTheme.PRosterLineBrush,
            Margin = new Thickness(0, 8, 0, 0)
        };

    private static Grid PRosterPanelBuild(IReadOnlyList<(string pLeft, string pRight)> pRows)
    {
        var pLeftStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Stretch };
        var pRightStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Stretch };
        foreach ((string pLeft, string pRight) in pRows)
        {
            pLeftStack.Children.Add(PRosterLineBuild(pLeft, false));
            pRightStack.Children.Add(PRosterLineBuild(pRight, false, !string.Equals(pLeft, pRight, StringComparison.Ordinal)));
        }

        Grid pGrid = PRosterGridBuild(pLeftStack, pRightStack);
        pGrid.Margin = new Thickness(0, 8, 0, 0);
        return pGrid;
    }

    private static Grid PRosterGridBuild(UIElement pSource, UIElement pOutput)
    {
        var pGrid = new Grid();
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto, SharedSizeGroup = "RosterOverviewSource" });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto, SharedSizeGroup = "RosterOverviewOutput" });
        Grid.SetColumn(pOutput, 2);
        pGrid.Children.Add(pSource);
        pGrid.Children.Add(pOutput);
        return pGrid;
    }

    private static string PRosterCodecFormat(LWorkMedia? pMediaInfo) =>
        pMediaInfo is { LWorkMediaSamplerate: > 0 } && !string.IsNullOrWhiteSpace(pMediaInfo.LWorkMediaCodec)
            ? pMediaInfo.LWorkMediaCodec.ToUpperInvariant()
            : "-";

    private static string PRosterBitrateFormat(LWorkMedia? pMediaInfo) =>
        pMediaInfo is { LWorkMediaSamplerate: > 0, LWorkMediaBitrate: > 0 }
            ? $"{Math.Round(pMediaInfo.LWorkMediaBitrate / 1000d)}k"
            : "-";

    private static string PRosterSampleFormat(LWorkMedia? pMediaInfo) =>
        pMediaInfo is { LWorkMediaSamplerate: > 0 }
            ? $"{pMediaInfo.LWorkMediaSamplerate} Hz"
            : "-";

    private static string PRosterLoudnessFormat(double? pLoudness) =>
        pLoudness is { } pLufs ? $"{pLufs:0.#} LUFS" : "-";

    private static Button PRosterOpenBuild(string pPath)
    {
        var pButton = new Button
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
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 4),
            ToolTip = LLocalization.LLocalizationTextRead("Roster.Explorer.Tooltip"),
            Style = PButton.PButtonPanelCreate()
        };
        pButton.Click += (_, _) => PRosterPathOpen(pPath);
        return pButton;
    }

    private static UIElement PRosterLineBuild(string pText, bool pHeader, bool pChanged = false) =>
        new TextBlock
        {
            Text = pText,
            Foreground = pHeader
                ? PRosterTheme.PRosterMutedBrush
                : pChanged
                    ? PRosterTheme.PRosterAccentBrush
                    : PRosterTheme.PRosterTextBrush,
            FontSize = pHeader ? PRosterTheme.PRosterRowSize - 1 : PRosterTheme.PRosterRowSize,
            FontWeight = pHeader || pChanged ? FontWeights.SemiBold : FontWeights.Normal,
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, pHeader ? 0 : 1, 0, pHeader ? 3 : 1)
        };

    private static string PRosterDimensionFormat(LWorkMedia? pMediaInfo, string pMediaPath) =>
        pMediaInfo is { LWorkMediaVideo: true }
            ? $"{pMediaInfo.LWorkMediaWidth} x {pMediaInfo.LWorkMediaHeight}"
            : pMediaInfo is null
                ? LLocalization.LLocalizationTextRead("Roster.Value.Unknown")
                : LLocalization.LLocalizationTextRead("Roster.AudioOnly");

    private static string PRosterDimensionFormat(string pVideoSize) =>
        string.IsNullOrWhiteSpace(pVideoSize)
            ? LLocalization.LLocalizationTextRead("Roster.Value.Unknown")
            : pVideoSize.Replace("x", " x ").Replace("X", " x ").Replace("×", " x ");

    private static string PRosterFpsFormat(LWorkMedia? pMediaInfo, string pMediaPath) =>
        pMediaInfo is { LWorkMediaVideo: true }
            ? $"{pMediaInfo.LWorkMediaFramerate:0.###} fps"
            : pMediaInfo is null
                ? LLocalization.LLocalizationTextRead("Roster.Value.Unknown")
                : LLocalization.LLocalizationTextRead("Roster.AudioOnly");

    private static string PRosterFpsFormat(string pVideoFps) =>
        string.IsNullOrWhiteSpace(pVideoFps)
            ? LLocalization.LLocalizationTextRead("Roster.Value.Unknown")
            : $"{pVideoFps} fps";
}

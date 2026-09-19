using System.Windows;
using System.Windows.Controls;
using Cadroue.Application;
using Cadroue.UIDeportment;
using static Cadroue.UIVeneer.PCabin.PRosterOverview;
using static Cadroue.UIVeneer.PWing.PPanel;

namespace Cadroue.UIVeneer.PCabin;

internal static class PRosterDetail
{
    private static readonly IReadOnlyDictionary<LRosterDetailKind, Action<StackPanel, LRoster>> PRosterFillers =
        new Dictionary<LRosterDetailKind, Action<StackPanel, LRoster>>
        {
            [LRosterDetailKind.LRosterDetailNone] = PRosterEmptyAdd,
            [LRosterDetailKind.LRosterDetailJob] = PRosterJobAdd,
            [LRosterDetailKind.LRosterDetailCard] = PRosterCardAdd,
        };

    public static UIElement PRosterDetailBuild(TextBlock pTitle, StackPanel pPanel)
    {
        var pHeader = new Border
        {
            Padding = PRosterTheme.PRosterHeaderPadding,
            Background = PRosterTheme.PRosterHeaderBrush,
            BorderBrush = PRosterTheme.PRosterLineBrush,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Child = pTitle
        };

        var pScroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            FocusVisualStyle = null,
            Padding = new Thickness(12, 10, 12, 12),
            Content = pPanel
        };

        var pRoot = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(pHeader, Dock.Top);
        pRoot.Children.Add(pHeader);
        pRoot.Children.Add(pScroll);
        return PPanelBorderBuild(pRoot);
    }

    public static void PRosterDetailUpdate(StackPanel pPanel, LRoster lRoster)
    {
        pPanel.Children.Clear();
        PRosterFillers[LRosterDetail.LRosterKindRead(lRoster)](pPanel, lRoster);
    }

    private static void PRosterEmptyAdd(StackPanel pPanel, LRoster lRoster) =>
        pPanel.Children.Add(
            PRosterNoteBuild(LLocalization.LLocalizationTextRead("Roster.Empty.Notice"), new Thickness(0)));

    private static void PRosterCardAdd(StackPanel pPanel, LRoster lRoster) =>
        PRosterSummary.PRosterSummaryAdd(pPanel, LSummary.LSummaryRead(lRoster.LRosterBatchRead()));

    private static void PRosterJobAdd(StackPanel pPanel, LRoster lRoster)
    {
        LRosterDetail lDetail = LRosterDetail.LRosterDetailRead(lRoster);
        lDetail.LRosterDetailFailures.ToList().ForEach(lRow => pPanel.Children.Add(PRosterRowBuild(lRow)));

        pPanel.Children.Add(PRosterSectionBuild(LLocalization.LLocalizationTextRead("Roster.Section.Overview")));
        lDetail.LRosterDetailTabs.ToList().ForEach(lTab => pPanel.Children.Add(PRosterTabBuild(lTab)));
        lDetail.LRosterDetailMeters.ToList().ForEach(lMeter => pPanel.Children.Add(PRosterMeterBuild(lMeter)));
        lDetail.LRosterDetailBars.ToList().ForEach(lBar => pPanel.Children.Add(PRosterBarBuild(lBar)));
        pPanel.Children.Add(PRosterComparisonBuild(lDetail));
        pPanel.Children.Add(PRosterRuleBuild());

        pPanel.Children.Add(PRosterSectionBuild(LLocalization.LLocalizationTextRead("Roster.Section.Record")));
        lDetail.LRosterDetailRecords.ToList().ForEach(lRow => pPanel.Children.Add(PRosterRowBuild(lRow)));
        pPanel.Children.Add(PRosterRuleBuild());

        var pVideoPanel = new StackPanel();
        pVideoPanel.Children.Add(
            PRosterSectionBuild(LLocalization.LLocalizationTextRead("Roster.Section.EncodingVideo")));
        lDetail.LRosterDetailVideos.ToList().ForEach(lRow => pVideoPanel.Children.Add(PRosterRowBuild(lRow)));

        var pAudioPanel = new StackPanel { Margin = new Thickness(14, 0, 0, 0) };
        pAudioPanel.Children.Add(
            PRosterSectionBuild(LLocalization.LLocalizationTextRead("Roster.Section.EncodingAudio")));
        lDetail.LRosterDetailAudios.ToList().ForEach(lRow => pAudioPanel.Children.Add(PRosterRowBuild(lRow)));

        pPanel.Children.Add(PRosterHalvesBuild(pVideoPanel, pAudioPanel));
    }

    private static Grid PRosterRowBuild(LRosterDetailRow lRow)
    {
        var pGrid = new Grid { Margin = new Thickness(0, 0, 0, 5) };
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(PRosterTheme.PRosterLabelWidth) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.Children.Add(new TextBlock
        {
            Text = lRow.LRosterDetailLabel,
            Foreground = PRosterTheme.PRosterMutedBrush,
            FontSize = PRosterTheme.PRosterRowSize,
            VerticalAlignment = VerticalAlignment.Top
        });

        var pValueBlock = new TextBlock
        {
            Text = lRow.LRosterDetailValue,
            Foreground = PRosterTheme.PRosterValueBrushes[lRow.LRosterDetailKey],
            FontSize = PRosterTheme.PRosterRowSize,
            FontWeight = PRosterTheme.PRosterValueWeights[lRow.LRosterDetailKey],
            TextWrapping = TextWrapping.Wrap
        };
        Grid.SetColumn(pValueBlock, 1);
        pGrid.Children.Add(pValueBlock);
        return pGrid;
    }
}

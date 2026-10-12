using System.Windows;
using System.Windows.Controls;
using Cadroue.Application;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PWing;

namespace Cadroue.UIVeneer.PCabin;

internal static class PRosterDetail
{
    private static readonly IReadOnlyDictionary<QRosterDetailKind, Action<StackPanel, QRoster>> PRosterFillers =
        new Dictionary<QRosterDetailKind, Action<StackPanel, QRoster>>
        {
            [QRosterDetailKind.QRosterDetailNone] = PRosterEmptyAdd,
            [QRosterDetailKind.QRosterDetailJob] = PRosterJobAdd,
            [QRosterDetailKind.QRosterDetailCard] = PRosterCardAdd,
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
        return PPanel.PPanelBorderBuild(pRoot);
    }

    public static void PRosterDetailUpdate(StackPanel pPanel, QRoster lRoster)
    {
        pPanel.Children.Clear();
        PRosterFillers[QRosterDetail.QRosterKindRead(lRoster)](pPanel, lRoster);
    }

    private static void PRosterEmptyAdd(StackPanel pPanel, QRoster lRoster) =>
        pPanel.Children.Add(
            PRosterOverview.PRosterNoteBuild(LLocalization.LLocalizationTextRead("Roster.Empty.Notice"),
                new Thickness(0)));

    private static void PRosterCardAdd(StackPanel pPanel, QRoster lRoster) =>
        PRosterSummary.PRosterSummaryAdd(pPanel, QSummary.QSummaryRead(lRoster));

    private static void PRosterJobAdd(StackPanel pPanel, QRoster lRoster)
    {
        QRosterDetail lDetail = QRosterDetail.QRosterDetailRead(lRoster);
        lDetail.QRosterDetailFailures.ToList().ForEach(lRow => pPanel.Children.Add(PRosterRowBuild(lRow)));

        pPanel.Children.Add(
            PRosterOverview.PRosterSectionBuild(LLocalization.LLocalizationTextRead("Roster.Section.Overview")));
        lDetail.QRosterDetailTabs.ToList().ForEach(lTab => pPanel.Children.Add(PRosterOverview.PRosterTabBuild(lTab)));
        lDetail.QRosterDetailMeters.ToList().ForEach(
            lMeter => pPanel.Children.Add(PRosterOverview.PRosterMeterBuild(lMeter)));
        lDetail.QRosterDetailBars.ToList().ForEach(lBar => pPanel.Children.Add(PRosterOverview.PRosterBarBuild(lBar)));
        pPanel.Children.Add(PRosterOverview.PRosterComparisonBuild(lRoster, lDetail));
        pPanel.Children.Add(PRosterOverview.PRosterRuleBuild());

        pPanel.Children.Add(
            PRosterOverview.PRosterSectionBuild(LLocalization.LLocalizationTextRead("Roster.Section.Record")));
        lDetail.QRosterDetailRecords.ToList().ForEach(lRow => pPanel.Children.Add(PRosterRowBuild(lRow)));
        pPanel.Children.Add(PRosterOverview.PRosterRuleBuild());

        var pVideoPanel = new StackPanel();
        pVideoPanel.Children.Add(
            PRosterOverview.PRosterSectionBuild(LLocalization.LLocalizationTextRead("Roster.Section.EncodingVideo")));
        lDetail.QRosterDetailVideos.ToList().ForEach(lRow => pVideoPanel.Children.Add(PRosterRowBuild(lRow)));

        var pAudioPanel = new StackPanel { Margin = new Thickness(14, 0, 0, 0) };
        pAudioPanel.Children.Add(
            PRosterOverview.PRosterSectionBuild(LLocalization.LLocalizationTextRead("Roster.Section.EncodingAudio")));
        lDetail.QRosterDetailAudios.ToList().ForEach(lRow => pAudioPanel.Children.Add(PRosterRowBuild(lRow)));

        pPanel.Children.Add(PRosterOverview.PRosterHalvesBuild(pVideoPanel, pAudioPanel));
    }

    private static Grid PRosterRowBuild(QRosterDetailRow lRow)
    {
        var pGrid = new Grid { Margin = new Thickness(0, 0, 0, 5) };
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(PRosterTheme.PRosterLabelWidth) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.Children.Add(new TextBlock
        {
            Text = lRow.QRosterDetailLabel,
            Foreground = PRosterTheme.PRosterMutedBrush,
            FontSize = PRosterTheme.PRosterRowSize,
            VerticalAlignment = VerticalAlignment.Top
        });

        var pValueBlock = new TextBlock
        {
            Text = lRow.QRosterDetailValue,
            Foreground = PRosterTheme.PRosterValueBrushes[lRow.QRosterDetailKey],
            FontSize = PRosterTheme.PRosterRowSize,
            FontWeight = PRosterTheme.PRosterValueWeights[lRow.QRosterDetailKey],
            TextWrapping = TextWrapping.Wrap
        };
        Grid.SetColumn(pValueBlock, 1);
        pGrid.Children.Add(pValueBlock);
        return pGrid;
    }
}

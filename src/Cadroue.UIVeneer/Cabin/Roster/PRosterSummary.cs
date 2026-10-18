using System.Windows;
using System.Windows.Controls;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal static class PRosterSummary
{
    public static void PRosterSummaryAdd(StackPanel pPanel, QSummary lSummary)
    {
        pPanel.Children.Add(
            PRosterOverview.PRosterSectionBuild(QLocalizationCatalog.QLocalizationTextRead("Roster.Section.Overview")));
        pPanel.Children.Add(
            PRosterOverview.PRosterNoteBuild(
                QLocalizationCatalog.QLocalizationTextRead("Roster.Card.OverviewSubtitle"), new Thickness(0, 0, 0, 8)));
        lSummary.QSummaryMeters.ToList().ForEach(
            lMeter => pPanel.Children.Add(PRosterOverview.PRosterMeterBuild(lMeter)));
        lSummary.QSummaryBars.ToList().ForEach(lBar => pPanel.Children.Add(PRosterOverview.PRosterBarBuild(lBar)));

        (StackPanel pSourceStack, StackPanel pOutputStack) = PRosterOverview.PRosterStacksBuild(
            lSummary.QSummaryCompares);
        var pRoot = new StackPanel { Margin = new Thickness(0, 6, 0, 0) };
        Grid.SetIsSharedSizeScope(pRoot, true);
        pRoot.Children.Add(PRosterOverview.PRosterGridBuild(pSourceStack, pOutputStack));
        pRoot.Children.Add(PRosterOverview.PRosterDividerBuild());
        pRoot.Children.Add(PRosterCountsBuild(lSummary));
        pPanel.Children.Add(pRoot);
    }

    private static UIElement PRosterCountsBuild(QSummary lSummary)
    {
        Grid pCountGrid = PRosterOverview.PRosterHalvesBuild(
            PRosterCountBuild(lSummary.QSummarySourceCount, HorizontalAlignment.Left),
            PRosterCountBuild(lSummary.QSummaryOutputCount, HorizontalAlignment.Right));
        pCountGrid.Margin = new Thickness(0, 6, 0, 0);

        var pRoot = new StackPanel();
        pRoot.Children.Add(pCountGrid);
        pRoot.Children.Add(PRosterOverview.PRosterPathsBuild(lSummary.QSummarySources, lSummary.QSummaryOutputs, 4));
        return pRoot;
    }

    private static TextBlock PRosterCountBuild(string pText, HorizontalAlignment pAlign) => new()
    {
        Text = pText,
        Foreground = PRosterTheme.PRosterMutedBrush,
        FontSize = PRosterTheme.PRosterRowSize,
        FontWeight = FontWeights.SemiBold,
        HorizontalAlignment = pAlign,
        Margin = new Thickness(0, 0, 0, 2)
    };
}

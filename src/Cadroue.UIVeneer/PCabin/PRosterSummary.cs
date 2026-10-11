using System.Windows;
using System.Windows.Controls;
using Cadroue.Application;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PCabin;

internal static class PRosterSummary
{
    public static void PRosterSummaryAdd(StackPanel pPanel, LSummary lSummary)
    {
        pPanel.Children.Add(
            PRosterOverview.PRosterSectionBuild(LLocalization.LLocalizationTextRead("Roster.Section.Overview")));
        pPanel.Children.Add(
            PRosterOverview.PRosterNoteBuild(
                LLocalization.LLocalizationTextRead("Roster.Card.OverviewSubtitle"), new Thickness(0, 0, 0, 8)));
        lSummary.LSummaryMeters.ToList().ForEach(
            lMeter => pPanel.Children.Add(PRosterOverview.PRosterMeterBuild(lMeter)));
        lSummary.LSummaryBars.ToList().ForEach(lBar => pPanel.Children.Add(PRosterOverview.PRosterBarBuild(lBar)));

        (StackPanel pSourceStack, StackPanel pOutputStack) = PRosterOverview.PRosterStacksBuild(
            lSummary.LSummaryCompares);
        var pRoot = new StackPanel { Margin = new Thickness(0, 6, 0, 0) };
        Grid.SetIsSharedSizeScope(pRoot, true);
        pRoot.Children.Add(PRosterOverview.PRosterGridBuild(pSourceStack, pOutputStack));
        pRoot.Children.Add(PRosterOverview.PRosterDividerBuild());
        pRoot.Children.Add(PRosterCountsBuild(lSummary));
        pPanel.Children.Add(pRoot);
    }

    private static UIElement PRosterCountsBuild(LSummary lSummary)
    {
        Grid pCountGrid = PRosterOverview.PRosterHalvesBuild(
            PRosterCountBuild(lSummary.LSummarySourceCount, HorizontalAlignment.Left),
            PRosterCountBuild(lSummary.LSummaryOutputCount, HorizontalAlignment.Right));
        pCountGrid.Margin = new Thickness(0, 6, 0, 0);

        var pRoot = new StackPanel();
        pRoot.Children.Add(pCountGrid);
        pRoot.Children.Add(PRosterOverview.PRosterPathsBuild(lSummary.LSummarySources, lSummary.LSummaryOutputs, 4));
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

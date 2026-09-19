using System.Windows;
using System.Windows.Controls;
using Cadroue.Application;
using Cadroue.UIDeportment;
using static Cadroue.UIVeneer.PCabin.PRosterOverview;

namespace Cadroue.UIVeneer.PCabin;

internal static class PRosterSummary
{
    public static void PRosterSummaryAdd(StackPanel pPanel, LSummary lSummary)
    {
        pPanel.Children.Add(PRosterSectionBuild(LLocalization.LLocalizationTextRead("Roster.Section.Overview")));
        pPanel.Children.Add(
            PRosterNoteBuild(
                LLocalization.LLocalizationTextRead("Roster.Card.OverviewSubtitle"), new Thickness(0, 0, 0, 8)));
        lSummary.LSummaryMeters.ToList().ForEach(lMeter => pPanel.Children.Add(PRosterMeterBuild(lMeter)));
        lSummary.LSummaryBars.ToList().ForEach(lBar => pPanel.Children.Add(PRosterBarBuild(lBar)));

        (StackPanel pSourceStack, StackPanel pOutputStack) = PRosterStacksBuild(lSummary.LSummaryCompares);
        var pRoot = new StackPanel { Margin = new Thickness(0, 6, 0, 0) };
        Grid.SetIsSharedSizeScope(pRoot, true);
        pRoot.Children.Add(PRosterGridBuild(pSourceStack, pOutputStack));
        pRoot.Children.Add(PRosterDividerBuild());
        pRoot.Children.Add(PRosterCountsBuild(lSummary));
        pPanel.Children.Add(pRoot);
    }

    private static UIElement PRosterCountsBuild(LSummary lSummary)
    {
        Grid pCountGrid = PRosterHalvesBuild(
            PRosterCountBuild(lSummary.LSummarySourceCount, HorizontalAlignment.Left),
            PRosterCountBuild(lSummary.LSummaryOutputCount, HorizontalAlignment.Right));
        pCountGrid.Margin = new Thickness(0, 6, 0, 0);

        var pRoot = new StackPanel();
        pRoot.Children.Add(pCountGrid);
        pRoot.Children.Add(PRosterPathsBuild(lSummary.LSummarySources, lSummary.LSummaryOutputs, 4));
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

using System.Windows;
using System.Windows.Controls;

namespace Cadroue.UIShell.PMainArea;

public sealed partial class PRoster
{
    public LPreferenceTabLayoutRecord PRosterLayoutRead()
    {
        var lPreferenceTabLayout = new LPreferenceTabLayoutRecord();
        if (pRosterBody.Tag is not PColumn pRosterLayout)
        {
            return lPreferenceTabLayout;
        }

        foreach (double pWeight in pRosterLayout.PColumnWeightsRead())
        {
            lPreferenceTabLayout.LPreferencePanelWidths.Add(pWeight);
        }

        return lPreferenceTabLayout;
    }

    public double PRosterWidthRead() =>
        pRosterBody.Tag is PColumn pRosterLayout ? pRosterLayout.PColumnTotalRead() + 16 : 0;

    private UIElement PRosterBuild(LPreferenceTabLayoutRecord? lPreferenceTabLayout)
    {
        var pRoot = new Grid { Margin = new Thickness(8, 8, 8, 8) };
        pRoot.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var pLeftColumn = new ColumnDefinition
        {
            Width = new GridLength(2, GridUnitType.Star),
            MinWidth = 320
        };
        pRosterBody.ColumnDefinitions.Add(pLeftColumn);
        pRosterBody.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6) });
        var pRightColumn = new ColumnDefinition
        {
            Width = new GridLength(1, GridUnitType.Star),
            MinWidth = 240
        };
        pRosterBody.ColumnDefinitions.Add(pRightColumn);

        UIElement pQueue = PRosterPanelBuild();
        Grid.SetColumn(pQueue, 0);
        pRosterBody.Children.Add(pQueue);

        var pRosterLayout = PColumn.PColumnAttach(
            pRosterBody,
            new[] { pLeftColumn, pRightColumn },
            lPreferenceTabLayout?.LPreferencePanelWidths);
        var pSplitter = pRosterLayout.PColumnSplitterBuild(0);
        Grid.SetColumn(pSplitter, 1);
        pRosterBody.Children.Add(pSplitter);
        pRosterBody.Tag = pRosterLayout;

        UIElement pDetail = PRosterDetailBuild();
        Grid.SetColumn(pDetail, 2);
        pRosterBody.Children.Add(pDetail);

        Grid.SetRow(pRosterBody, 0);
        pRoot.Children.Add(pRosterBody);
        return pRoot;
    }
}

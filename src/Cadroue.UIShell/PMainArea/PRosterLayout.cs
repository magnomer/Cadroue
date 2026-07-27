using System.Windows;
using System.Windows.Controls;

namespace Cadroue.UIShell.PMainArea;

public sealed partial class PRoster
{
    public LPreferenceTabLayoutRecord PRosterLayoutRead()
    {
        var lPreferenceTabLayout = new LPreferenceTabLayoutRecord();
        if (pRosterBody.Tag is not PResizableColumnLayout pRosterLayout)
        {
            return lPreferenceTabLayout;
        }

        foreach (double pWidth in pRosterLayout.PWidthsRead())
        {
            lPreferenceTabLayout.PanelWidths.Add(pWidth);
        }

        return lPreferenceTabLayout;
    }

    private UIElement PRosterBuild(LPreferenceTabLayoutRecord? lPreferenceTabLayout)
    {
        var pRoot = new Grid { Margin = new Thickness(8, 8, 8, 8) };
        pRoot.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        pRoot.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        UIElement pTransport = PRosterTransportBuild();
        Grid.SetRow(pTransport, 0);
        pRoot.Children.Add(pTransport);

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

        UIElement pQueue = PRosterQueuePanelBuild();
        Grid.SetColumn(pQueue, 0);
        pRosterBody.Children.Add(pQueue);

        var pRosterLayout = PResizableColumnLayout.PAttach(
            pRosterBody,
            new[] { pLeftColumn, pRightColumn },
            lPreferenceTabLayout?.PanelWidths);
        var pSplitter = pRosterLayout.PSplitterBuild(0);
        Grid.SetColumn(pSplitter, 1);
        pRosterBody.Children.Add(pSplitter);
        pRosterBody.Tag = pRosterLayout;

        UIElement pDetail = PRosterDetailBuild();
        Grid.SetColumn(pDetail, 2);
        pRosterBody.Children.Add(pDetail);

        Grid.SetRow(pRosterBody, 1);
        pRoot.Children.Add(pRosterBody);
        return pRoot;
    }
}

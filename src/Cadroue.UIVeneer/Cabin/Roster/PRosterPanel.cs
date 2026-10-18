using System.Windows;
using System.Windows.Controls;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal static class PRosterPanel
{
    public static UIElement PRosterPanelBuild(PRosterQueue pQueue, CheckBox pSharedBox, CheckBox pCompletedBox)
    {
        StackPanel pOptions = PRosterOptionsBuild(pSharedBox, pCompletedBox);
        Border pOptionRow = PRosterBandBuild(pOptions);
        Border pColumnHeader = PRosterBandBuild(PRosterHeaderBuild());
        QScrollbar.QScrollbarAttach(pQueue);
        PRosterScrollAttach(pQueue, pColumnHeader);
        return PRosterDockBuild(pQueue, pOptionRow, pColumnHeader);
    }

    private static StackPanel PRosterOptionsBuild(CheckBox pSharedBox, CheckBox pCompletedBox)
    {
        var pOptions = new StackPanel { Orientation = Orientation.Horizontal };
        pOptions.Children.Add(pSharedBox);
        pOptions.Children.Add(pCompletedBox);
        return pOptions;
    }

    private static void PRosterScrollAttach(PRosterQueue pQueue, Border pColumnHeader) =>
        pQueue.AddHandler(
            ScrollViewer.ScrollChangedEvent,
            new ScrollChangedEventHandler((pSender, pArgs) =>
                PRosterPaddingSet(pColumnHeader, PRosterPaddingResolve(pArgs))));

    private static Thickness PRosterPaddingResolve(ScrollChangedEventArgs pArgs) =>
        new(21, 10, 5 + ((ScrollViewer)pArgs.OriginalSource).ActualWidth - pArgs.ViewportWidth, 10);

    private static void PRosterPaddingSet(Border pColumnHeader, Thickness pPadding) =>
        pColumnHeader.Padding = pPadding;

    private static UIElement PRosterDockBuild(PRosterQueue pQueue, Border pOptionRow, Border pColumnHeader)
    {
        var pRoot = new DockPanel { LastChildFill = true };
        Grid.SetIsSharedSizeScope(pRoot, true);
        DockPanel.SetDock(pOptionRow, Dock.Top);
        DockPanel.SetDock(pColumnHeader, Dock.Top);
        pRoot.Children.Add(pOptionRow);
        pRoot.Children.Add(pColumnHeader);
        pRoot.Children.Add(pQueue);
        return QPanel.QPanelBuild(pRoot);
    }

    private static Border PRosterBandBuild(UIElement pChild) => new()
    {
        Padding = PRosterTheme.PRosterHeaderPadding,
        Background = PRosterTheme.PRosterHeaderBrush,
        BorderBrush = PRosterTheme.PRosterLineBrush,
        BorderThickness = new Thickness(0, 0, 0, 1),
        Child = pChild
    };

    private static Grid PRosterHeaderBuild()
    {
        Grid pGrid = PRosterRow.PRosterColumnsCreate();
        PRosterHeadAdd(pGrid, 0, QLocalizationCatalog.QLocalizationTextRead("Roster.Queue.Step"));
        PRosterHeadAdd(pGrid, 1, QLocalizationCatalog.QLocalizationTextRead("Roster.Queue.Priority"));
        PRosterHeadAdd(pGrid, 2, QLocalizationCatalog.QLocalizationTextRead("Roster.Queue.Length"));
        PRosterHeadAdd(pGrid, 3, QLocalizationCatalog.QLocalizationTextRead("Roster.Queue.Progress"));
        PRosterHeadAdd(pGrid, 4, QLocalizationCatalog.QLocalizationTextRead("Roster.Queue.Percentage"));
        PRosterHeadAdd(pGrid, 5, QLocalizationCatalog.QLocalizationTextRead("Roster.Queue.State"));
        PRosterHeadAdd(pGrid, 6, QLocalizationCatalog.QLocalizationTextRead("Roster.Queue.Owner"));
        return pGrid;
    }

    private static void PRosterHeadAdd(Grid pGrid, int pColumn, string pText)
    {
        var pCell = new TextBlock
        {
            Text = pText,
            FontSize = PRosterTheme.PRosterRowSize,
            FontWeight = FontWeights.SemiBold,
            Foreground = PRosterTheme.PRosterMutedBrush
        };
        Grid.SetColumn(pCell, pColumn);
        pGrid.Children.Add(pCell);
    }
}

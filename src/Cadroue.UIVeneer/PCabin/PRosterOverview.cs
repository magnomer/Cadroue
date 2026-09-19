using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.Application;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PAsset;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PCabin;

internal static class PRosterOverview
{
    private const string PRosterOpenIcon = "/PAsset/PPanel/PRosterOpen.svg";
    private const double PRosterBarRadius = 4;

    public static TextBlock PRosterSectionBuild(string pSectionName) => new()
    {
        Text = pSectionName,
        Foreground = PRosterTheme.PRosterTextBrush,
        FontSize = PRosterTheme.PRosterRowSize,
        FontWeight = FontWeights.SemiBold,
        Margin = new Thickness(0, 0, 0, 6)
    };

    public static TextBlock PRosterNoteBuild(string pText, Thickness pMargin) => new()
    {
        Text = pText,
        Foreground = PRosterTheme.PRosterMutedBrush,
        FontSize = PRosterTheme.PRosterRowSize,
        TextWrapping = TextWrapping.Wrap,
        Margin = pMargin
    };

    public static TextBlock PRosterTabBuild(string pTabName) => new()
    {
        Text = pTabName,
        Foreground = PRosterTheme.PRosterAccentBrush,
        FontSize = PRosterTheme.PRosterRowSize,
        FontWeight = FontWeights.SemiBold,
        TextWrapping = TextWrapping.Wrap,
        Margin = new Thickness(0, 0, 0, 6)
    };

    public static TextBlock PRosterMeterBuild(string pMeterText) => new()
    {
        Text = pMeterText,
        Foreground = PRosterTheme.PRosterMutedBrush,
        FontSize = PRosterTheme.PRosterRowSize,
        HorizontalAlignment = HorizontalAlignment.Center,
        Margin = new Thickness(0, 0, 0, 2)
    };

    public static UIElement PRosterBarBuild(LRosterBar lBar)
    {
        var pBar = new Grid { Height = 18 };
        pBar.ColumnDefinitions.Add(
            new ColumnDefinition { Width = new GridLength(lBar.LRosterBarRest, GridUnitType.Star) });
        pBar.ColumnDefinitions.Add(
            new ColumnDefinition { Width = new GridLength(lBar.LRosterBarMark, GridUnitType.Star) });

        var pRestFill = new Border
        {
            Background = PRosterTheme.PRosterTrackBrush,
            CornerRadius = new CornerRadius(PRosterBarRadius, 0, 0, PRosterBarRadius)
        };
        var pMarkFill = new Border
        {
            Background = PRosterTheme.PRosterMarkBrushes[lBar.LRosterBarOver],
            CornerRadius = new CornerRadius(0, PRosterBarRadius, PRosterBarRadius, 0)
        };
        Grid.SetColumn(pMarkFill, 1);
        pBar.Children.Add(pRestFill);
        pBar.Children.Add(pMarkFill);

        var pPercent = new TextBlock
        {
            Text = lBar.LRosterBarPercent,
            Foreground = PRosterTheme.PRosterPercentBrushes[lBar.LRosterBarKey],
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

    public static UIElement PRosterComparisonBuild(LRosterDetail lDetail)
    {
        (StackPanel pSourceStack, StackPanel pOutputStack) = PRosterStacksBuild(lDetail.LRosterDetailCompares);
        pSourceStack.Children.Insert(0, PRosterOpenBuild(lDetail.LRosterDetailSource));
        pOutputStack.Children.Insert(0, PRosterOpenBuild(lDetail.LRosterDetailOutput));

        var pRoot = new StackPanel { Margin = new Thickness(0, 10, 0, 0) };
        Grid.SetIsSharedSizeScope(pRoot, true);
        pRoot.Children.Add(PRosterGridBuild(pSourceStack, pOutputStack));
        pRoot.Children.Add(PRosterSoundBuild(lDetail));
        pRoot.Children.Add(PRosterDividerBuild());
        pRoot.Children.Add(PRosterPathsBuild(lDetail.LRosterDetailSources, lDetail.LRosterDetailOutputs, 8));
        return pRoot;
    }

    private static UIElement PRosterSoundBuild(LRosterDetail lDetail)
    {
        (StackPanel pSourceStack, StackPanel pOutputStack) = PRosterStacksBuild(lDetail.LRosterDetailSounds);
        Grid pGrid = PRosterGridBuild(pSourceStack, pOutputStack);
        pGrid.Margin = new Thickness(0, 8, 0, 0);
        var pBlock = new StackPanel { Visibility = PLook.PLookVisible[lDetail.LRosterDetailSound] };
        pBlock.Children.Add(PRosterDividerBuild());
        pBlock.Children.Add(pGrid);
        return pBlock;
    }

    public static (StackPanel, StackPanel) PRosterStacksBuild(IReadOnlyList<LRosterCompareRow> lRows)
    {
        var pSourceStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Stretch };
        var pOutputStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Stretch };
        lRows.ToList().ForEach(lRow => pSourceStack.Children.Add(PRosterLineBuild(lRow.LRosterCompareSource)));
        lRows.ToList().ForEach(lRow => pOutputStack.Children.Add(PRosterLineBuild(lRow.LRosterCompareOutput)));
        return (pSourceStack, pOutputStack);
    }

    public static Grid PRosterGridBuild(UIElement pSource, UIElement pOutput)
    {
        var pGrid = new Grid();
        pGrid.ColumnDefinitions.Add(
            new ColumnDefinition { Width = GridLength.Auto, SharedSizeGroup = "RosterOverviewSource" });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.ColumnDefinitions.Add(
            new ColumnDefinition { Width = GridLength.Auto, SharedSizeGroup = "RosterOverviewOutput" });
        Grid.SetColumn(pOutput, 2);
        pGrid.Children.Add(pSource);
        pGrid.Children.Add(pOutput);
        return pGrid;
    }

    public static Grid PRosterPathsBuild(IReadOnlyList<string> pSources, IReadOnlyList<string> pOutputs, double pTop)
    {
        var pSourcePaths = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 0, 8, 0)
        };
        pSources.ToList().ForEach(pPath => pSourcePaths.Children.Add(PRosterPathBuild(pPath)));

        var pOutputPaths = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(8, 0, 0, 0)
        };
        pOutputs.ToList().ForEach(pPath => pOutputPaths.Children.Add(PRosterPathBuild(pPath)));

        Grid pPathGrid = PRosterHalvesBuild(pSourcePaths, pOutputPaths);
        pPathGrid.Margin = new Thickness(0, pTop, 0, 0);
        return pPathGrid;
    }

    public static Grid PRosterHalvesBuild(UIElement pLeft, UIElement pRight)
    {
        var pGrid = new Grid();
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Grid.SetColumn(pRight, 1);
        pGrid.Children.Add(pLeft);
        pGrid.Children.Add(pRight);
        return pGrid;
    }

    private static TextBlock PRosterPathBuild(string pPath) => new()
    {
        Text = pPath,
        Foreground = PRosterTheme.PRosterMutedBrush,
        FontSize = PRosterTheme.PRosterRowSize,
        TextWrapping = TextWrapping.Wrap,
        Margin = new Thickness(0, 0, 0, 2)
    };

    public static Border PRosterDividerBuild() => new()
    {
        Height = 1,
        Background = PRosterTheme.PRosterLineBrush,
        Margin = new Thickness(0, 8, 0, 0)
    };

    public static Border PRosterRuleBuild() => new()
    {
        Height = 1,
        Background = PRosterTheme.PRosterLineBrush,
        Margin = new Thickness(0, 12, 0, 10)
    };

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
        pButton.Click += (_, _) => LRosterDetail.LRosterDetailOpen(pPath);
        return pButton;
    }

    private static TextBlock PRosterLineBuild(LRosterLine lLine) => new()
    {
        Text = lLine.LRosterLineText,
        Foreground = PRosterTheme.PRosterLineBrushes[lLine.LRosterLineKey],
        FontSize = PRosterTheme.PRosterLineSizes[lLine.LRosterLineKey],
        FontWeight = PRosterTheme.PRosterLineWeights[lLine.LRosterLineKey],
        TextWrapping = TextWrapping.Wrap,
        HorizontalAlignment = HorizontalAlignment.Center,
        Margin = PRosterTheme.PRosterLineMargins[lLine.LRosterLineKey]
    };
}

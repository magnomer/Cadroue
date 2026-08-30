using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.Core;
using Cadroue.Application;

namespace Cadroue.UIShell.PMainArea;

public sealed partial class PRoster
{
    private void PSummaryAdd(IReadOnlyList<LWorkItem> pBatchItems)
    {
        PRosterSectionAdd(LLocalization.LLocalizationTextRead("Roster.Section.Overview"), false);
        pRosterRowTarget.Children.Add(new TextBlock
        {
            Text = LLocalization.LLocalizationTextRead("Roster.Card.OverviewSubtitle"),
            Foreground = PRosterTheme.PRosterMutedBrush,
            FontSize = PRosterTheme.PRosterRowSize,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 8)
        });

        if (pBatchItems.Count == 0)
        {
            return;
        }

        (long? pSourceBytes, long? pOutputBytes) = PSummarySizeRead(pBatchItems);

        if (PSummaryMeterBuild(pBatchItems, pOutputBytes) is { } pMeter)
        {
            pRosterRowTarget.Children.Add(pMeter);
        }

        if (pSourceBytes is { } pSourceWhole && pSourceWhole > 0 && pOutputBytes is { } pOutputWhole && pOutputWhole >= 0)
        {
            pRosterRowTarget.Children.Add(PRosterOverviewBuild(pSourceWhole, pOutputWhole));
        }

        var pSourceStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Stretch };
        var pOutputStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Stretch };
        pSourceStack.Children.Add(PRosterLineBuild(LLocalization.LLocalizationTextRead("Roster.Section.Source"), true));
        pOutputStack.Children.Add(PRosterLineBuild(LLocalization.LLocalizationTextRead("Roster.Section.Output"), true));
        pSourceStack.Children.Add(PRosterLineBuild(PRosterMebiFormat(pSourceBytes), false));
        pOutputStack.Children.Add(PRosterLineBuild(PRosterMebiFormat(pOutputBytes), false));

        var pRoot = new StackPanel { Margin = new Thickness(0, 6, 0, 0) };
        Grid.SetIsSharedSizeScope(pRoot, true);
        pRoot.Children.Add(PRosterGridBuild(pSourceStack, pOutputStack));

        (IReadOnlyList<string> pSourcePaths, IReadOnlyList<string> pOutputPaths) = PSummaryPathsRead(pBatchItems);
        pRoot.Children.Add(PRosterDividerBuild());
        pRoot.Children.Add(PSummaryPathBuild(pSourcePaths, pOutputPaths));
        pRosterRowTarget.Children.Add(pRoot);
    }

    private static UIElement? PSummaryMeterBuild(IReadOnlyList<LWorkItem> pBatchItems, long? pOutputBytes)
    {
        TimeSpan pSpentTotal = TimeSpan.Zero;
        bool pAnySpent = false;
        foreach (LWorkItem pWorkItem in pBatchItems)
        {
            if (PRosterSpentRead(pWorkItem) is { } pSpent)
            {
                pSpentTotal += pSpent;
                pAnySpent = true;
            }
        }

        if (!pAnySpent || pSpentTotal.TotalSeconds <= 0)
        {
            return null;
        }

        var pRounded = TimeSpan.FromSeconds(Math.Max(1, (long)Math.Ceiling(pSpentTotal.TotalSeconds)));
        int pHours = (int)pRounded.TotalHours;
        string pSpentText = pHours > 0
            ? $"{pHours}:{pRounded.Minutes:00}:{pRounded.Seconds:00}"
            : $"{pRounded.Minutes:00}:{pRounded.Seconds:00}";

        string pSpeedText = pOutputBytes is { } pOutputWhole && pOutputWhole > 0
            ? $"{pOutputWhole / 1048576d / pSpentTotal.TotalSeconds:0.##} MiB/s"
            : LLocalization.LLocalizationTextRead("Roster.Value.NotYet");

        return new TextBlock
        {
            Text = $"{pSpentText} / {pSpeedText}",
            Foreground = PRosterTheme.PRosterMutedBrush,
            FontSize = PRosterTheme.PRosterRowSize,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 2)
        };
    }

    private static UIElement PSummaryPathBuild(IReadOnlyList<string> pSourcePaths, IReadOnlyList<string> pOutputPaths)
    {
        var pCountGrid = new Grid { Margin = new Thickness(0, 6, 0, 0) };
        pCountGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pCountGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var pSourceCount = PSummaryCountBuild(PSummaryFilesFormat(pSourcePaths.Count), HorizontalAlignment.Left);
        var pOutputCount = PSummaryCountBuild(PSummaryFilesFormat(pOutputPaths.Count), HorizontalAlignment.Right);
        Grid.SetColumn(pOutputCount, 1);
        pCountGrid.Children.Add(pSourceCount);
        pCountGrid.Children.Add(pOutputCount);

        var pSourceStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 8, 0) };
        foreach (string pSourcePath in pSourcePaths)
        {
            pSourceStack.Children.Add(PRosterPathBuild(pSourcePath));
        }

        var pOutputStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(8, 0, 0, 0) };
        foreach (string pOutputPath in pOutputPaths)
        {
            pOutputStack.Children.Add(PRosterPathBuild(pOutputPath));
        }

        var pPathGrid = new Grid { Margin = new Thickness(0, 4, 0, 0) };
        pPathGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pPathGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Grid.SetColumn(pOutputStack, 1);
        pPathGrid.Children.Add(pSourceStack);
        pPathGrid.Children.Add(pOutputStack);

        var pRoot = new StackPanel();
        pRoot.Children.Add(pCountGrid);
        pRoot.Children.Add(pPathGrid);
        return pRoot;
    }

    private static TextBlock PSummaryCountBuild(string pText, HorizontalAlignment pAlign) => new()
    {
        Text = pText,
        Foreground = PRosterTheme.PRosterMutedBrush,
        FontSize = PRosterTheme.PRosterRowSize,
        FontWeight = FontWeights.SemiBold,
        HorizontalAlignment = pAlign,
        Margin = new Thickness(0, 0, 0, 2)
    };

    private static string PSummaryFilesFormat(int pCount) =>
        pCount == 1
            ? LLocalization.LLocalizationTextRead("Roster.Summary.FileOne")
            : LLocalization.LLocalizationFormat("Roster.Summary.FileMany", pCount);

    private static (IReadOnlyList<string> pSources, IReadOnlyList<string> pOutputs) PSummaryPathsRead(
        IReadOnlyList<LWorkItem> pBatchItems)
    {
        var pOutputKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (LWorkItem pWorkItem in pBatchItems)
        {
            if (PLineagePathRead(pWorkItem.LWorkOutputPath) is { } pOutputKey)
            {
                pOutputKeys.Add(pOutputKey);
            }
        }

        var pConsumed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pSeenSources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pSources = new List<string>();
        foreach (LWorkItem pWorkItem in pBatchItems)
        {
            IEnumerable<string> pInputs = pWorkItem.LWorkKind == LWorkKind.LWorkKindMerge
                ? pWorkItem.LWorkMergeSources
                : new[] { pWorkItem.LWorkSourcePath };
            foreach (string pInput in pInputs)
            {
                if (PLineagePathRead(pInput) is not { } pInputKey)
                {
                    continue;
                }

                pConsumed.Add(pInputKey);
                if (!pOutputKeys.Contains(pInputKey) && pSeenSources.Add(pInputKey))
                {
                    pSources.Add(pInput);
                }
            }
        }

        var pSeenOutputs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pOutputs = new List<string>();
        foreach (LWorkItem pWorkItem in pBatchItems)
        {
            if (PLineagePathRead(pWorkItem.LWorkOutputPath) is { } pOutputKey
                && !pConsumed.Contains(pOutputKey)
                && pSeenOutputs.Add(pOutputKey))
            {
                pOutputs.Add(pWorkItem.LWorkOutputPath);
            }
        }

        return (pSources, pOutputs);
    }

    private static (long? pSourceTotal, long? pOutputTotal) PSummarySizeRead(IReadOnlyList<LWorkItem> pBatchItems)
    {
        var pOutputs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (LWorkItem pWorkItem in pBatchItems)
        {
            if (PLineagePathRead(pWorkItem.LWorkOutputPath) is { } pOutputKey)
            {
                pOutputs.Add(pOutputKey);
            }
        }

        var pConsumed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pInputBytes = new Dictionary<string, long?>(StringComparer.OrdinalIgnoreCase);
        foreach (LWorkItem pWorkItem in pBatchItems)
        {
            bool pMerge = pWorkItem.LWorkKind == LWorkKind.LWorkKindMerge;
            IReadOnlyList<string> pInputs = pMerge
                ? pWorkItem.LWorkMergeSources
                : new[] { pWorkItem.LWorkSourcePath };
            for (int pIndex = 0; pIndex < pInputs.Count; pIndex++)
            {
                string pInput = pInputs[pIndex];
                if (PLineagePathRead(pInput) is not { } pInputKey)
                {
                    continue;
                }

                pConsumed.Add(pInputKey);
                if (!pInputBytes.ContainsKey(pInputKey))
                {
                    pInputBytes[pInputKey] = pMerge
                        ? pIndex < pWorkItem.LWorkMergeBytes.Count
                            && pWorkItem.LWorkMergeBytes[pIndex] > 0
                                ? pWorkItem.LWorkMergeBytes[pIndex]
                                : null
                        : pWorkItem.LWorkSourceBytes;
                }
            }
        }

        long pSourceTotal = 0;
        bool pSourceAny = false, pSourceOk = true;
        foreach ((string pInputKey, long? pInputByte) in pInputBytes)
        {
            if (pOutputs.Contains(pInputKey))
            {
                continue;
            }

            pSourceAny = true;
            if (pInputByte is { } pInputWhole)
            {
                pSourceTotal += pInputWhole;
            }
            else
            {
                pSourceOk = false;
            }
        }

        // Only completed outputs are totalled: a progressing job's growing file must not be
        // accounted, so the comparison stays source vs finished output and grows as jobs land.
        long pOutputTotal = 0;
        bool pOutputAny = false;
        var pSeenOutputs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (LWorkItem pWorkItem in pBatchItems)
        {
            if (pWorkItem.LWorkStateCurrent != LWorkState.LWorkStateDone
                || PLineagePathRead(pWorkItem.LWorkOutputPath) is not { } pOutputKey
                || pConsumed.Contains(pOutputKey)
                || !pSeenOutputs.Add(pOutputKey)
                || PRosterBytesRead(pWorkItem) is not { } pOutputWhole)
            {
                continue;
            }

            pOutputAny = true;
            pOutputTotal += pOutputWhole;
        }

        return (
            pSourceAny && pSourceOk ? pSourceTotal : null,
            pOutputAny ? pOutputTotal : null);
    }
}

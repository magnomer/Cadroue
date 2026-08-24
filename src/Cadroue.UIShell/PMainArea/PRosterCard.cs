using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.Core;
using Cadroue.Application;
using Cadroue.UIShell.PAssets;

namespace Cadroue.UIShell.PMainArea;

public sealed partial class PRoster
{
    private UIElement PRosterCardBuild(IReadOnlyList<LWorkItem> pBatchItems, StackPanel pDetail)
    {
        DateTimeOffset pCreateTime = pBatchItems.Min(pWorkItem => pWorkItem.LWorkCreateTime);
        int pInitialCount = PRosterInitialRead(pBatchItems);
        string pTitle = pCreateTime.LocalDateTime.ToString(
            "yyyy-MM-dd tt h:mm", CultureInfo.CurrentUICulture);

        var pGrid = new Grid();
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var pTitleCell = new TextBlock
        {
            Text = pInitialCount == 1
                ? LLocalization.LLocalizationFormat("Roster.Card.One", pTitle)
                : LLocalization.LLocalizationFormat("Roster.Card.Many", pTitle, pInitialCount),
            FontSize = PRosterTheme.PRosterRowSize,
            FontWeight = FontWeights.SemiBold,
            Foreground = PRosterTheme.PRosterMutedBrush,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Cursor = Cursors.Hand
        };
        Guid pBatchId = pBatchItems[0].LWorkBatchId;
        pTitleCell.MouseLeftButtonUp += (_, _) => PRosterCardSelect(pBatchId);
        Grid.SetColumn(pTitleCell, 0);
        pGrid.Children.Add(pTitleCell);

        var pControls = new StackPanel { Orientation = Orientation.Horizontal };
        pControls.Children.Add(PRosterMinimizeBuild(pBatchId, pDetail));
        pControls.Children.Add(PRosterCloseBuild(pBatchItems));
        Grid.SetColumn(pControls, 1);
        pGrid.Children.Add(pControls);

        var pHeader = new Border
        {
            Padding = new Thickness(12, 6, 6, 6),
            Background = pBatchId == pRosterCardId ? PRosterTheme.PRosterSelectBrush : Brushes.Transparent,
            BorderBrush = PRosterTheme.PRosterLineBrush,
            Child = pGrid
        };
        pRosterCardHeaders[pBatchId] = pHeader;
        PRosterCollapseApply(pHeader, pRosterCollapsedIds.Contains(pBatchId));
        return pHeader;
    }

    private static void PRosterCollapseApply(Border pHeader, bool pCollapsed)
    {
        pHeader.BorderThickness = pCollapsed ? new Thickness(0) : new Thickness(0, 0, 0, 1);
        pHeader.CornerRadius = pCollapsed
            ? new CornerRadius(PRosterTheme.PRosterCorner)
            : new CornerRadius(PRosterTheme.PRosterCorner, PRosterTheme.PRosterCorner, 0, 0);
    }

    private void PRosterCardApply()
    {
        foreach ((Guid pBatchId, Border pHeader) in pRosterCardHeaders)
        {
            pHeader.Background = pBatchId == pRosterCardId
                ? PRosterTheme.PRosterSelectBrush
                : Brushes.Transparent;
        }
    }

    private UIElement PRosterCloseBuild(IReadOnlyList<LWorkItem> pBatchItems)
    {
        var pGlyph = new TextBlock
        {
            Text = "✕",
            FontSize = PRosterTheme.PRosterRowSize,
            Foreground = PRosterTheme.PRosterMutedBrush,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        var pButton = new Border
        {
            Width = 20,
            Height = 20,
            CornerRadius = new CornerRadius(4),
            Background = Brushes.Transparent,
            Cursor = Cursors.Hand,
            ToolTip = LLocalization.LLocalizationTextRead("Roster.Card.Remove"),
            Child = pGlyph
        };

        pButton.MouseEnter += (_, _) =>
        {
            pButton.Background = PRosterTheme.PRosterHeaderBrush;
            pGlyph.Foreground = PRosterTheme.PRosterFailBrush;
        };
        pButton.MouseLeave += (_, _) =>
        {
            pButton.Background = Brushes.Transparent;
            pGlyph.Foreground = PRosterTheme.PRosterMutedBrush;
        };
        pButton.MouseLeftButtonDown += (_, pArgs) => pArgs.Handled = true;
        pButton.MouseLeftButtonUp += (_, _) => PRosterCardRemove(pBatchItems);

        return pButton;
    }

    private const string pRosterMaximizeIcon = "/PAssets/PPanels/PRosterBatchMaximize.svg";
    private const string pRosterMinimizeIcon = "/PAssets/PPanels/PRosterBatchMinimize.svg";

    private static ImageSource PRosterMinimizeRead(bool pCollapsed, Brush pTint) =>
        PIcon.PIconRead(pCollapsed ? pRosterMaximizeIcon : pRosterMinimizeIcon, pTint);

    private UIElement PRosterMinimizeBuild(Guid pBatchId, StackPanel pDetail)
    {
        bool pCollapsed = pRosterCollapsedIds.Contains(pBatchId);
        var pIcon = new Image
        {
            Source = PRosterMinimizeRead(pCollapsed, PRosterTheme.PRosterMutedBrush),
            Width = 14,
            Height = 14,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        var pButton = new Border
        {
            Width = 20,
            Height = 20,
            Margin = new Thickness(0, 0, 2, 0),
            CornerRadius = new CornerRadius(4),
            Background = Brushes.Transparent,
            Cursor = Cursors.Hand,
            ToolTip = LLocalization.LLocalizationTextRead(pCollapsed ? "Roster.Card.Expand" : "Roster.Card.Collapse"),
            Child = pIcon
        };

        pButton.MouseEnter += (_, _) =>
        {
            pButton.Background = PRosterTheme.PRosterHeaderBrush;
            pIcon.Source = PRosterMinimizeRead(pRosterCollapsedIds.Contains(pBatchId), PRosterTheme.PRosterTextBrush);
        };
        pButton.MouseLeave += (_, _) =>
        {
            pButton.Background = Brushes.Transparent;
            pIcon.Source = PRosterMinimizeRead(pRosterCollapsedIds.Contains(pBatchId), PRosterTheme.PRosterMutedBrush);
        };
        pButton.MouseLeftButtonDown += (_, pArgs) => pArgs.Handled = true;
        pButton.MouseLeftButtonUp += (_, _) => PRosterMinimizeToggle(pBatchId, pDetail, pButton, pIcon);

        return pButton;
    }

    private void PRosterMinimizeToggle(Guid pBatchId, StackPanel pDetail, Border pButton, Image pIcon)
    {
        bool pCollapsed = !pRosterCollapsedIds.Contains(pBatchId);
        if (pCollapsed)
        {
            pRosterCollapsedIds.Add(pBatchId);
        }
        else
        {
            pRosterCollapsedIds.Remove(pBatchId);
        }

        pDetail.Visibility = pCollapsed ? Visibility.Collapsed : Visibility.Visible;
        if (pRosterCardHeaders.TryGetValue(pBatchId, out Border? pHeader))
        {
            PRosterCollapseApply(pHeader, pCollapsed);
        }

        pIcon.Source = PRosterMinimizeRead(pCollapsed, PRosterTheme.PRosterTextBrush);
        pButton.ToolTip = LLocalization.LLocalizationTextRead(pCollapsed ? "Roster.Card.Expand" : "Roster.Card.Collapse");
    }

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

    private static HashSet<string> PRosterConsumedRead(IReadOnlyList<LWorkItem> pBatchItems)
    {
        var pConsumed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (LWorkItem pWorkItem in pBatchItems)
        {
            IEnumerable<string> pInputs = pWorkItem.LWorkKind == LWorkKind.LWorkKindMerge
                ? pWorkItem.LWorkMergeSources
                : new[] { pWorkItem.LWorkSourcePath };
            foreach (string pInput in pInputs)
            {
                if (PLineagePathRead(pInput) is { } pInputKey)
                {
                    pConsumed.Add(pInputKey);
                }
            }
        }

        return pConsumed;
    }

    private static HashSet<string> PRosterProducedRead(IReadOnlyList<LWorkItem> pBatchItems)
    {
        var pProduced = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (LWorkItem pWorkItem in pBatchItems)
        {
            if (PLineagePathRead(pWorkItem.LWorkOutputPath) is { } pOutputKey)
            {
                pProduced.Add(pOutputKey);
            }
        }

        return pProduced;
    }

    private static bool PRosterStageCheck(LWorkItem pWorkItem, HashSet<string> pConsumed, HashSet<string> pProduced)
    {
        if (PLineagePathRead(pWorkItem.LWorkOutputPath) is not { } pOutputKey || !pConsumed.Contains(pOutputKey))
        {
            return false;
        }

        IEnumerable<string> pInputs = pWorkItem.LWorkKind == LWorkKind.LWorkKindMerge
            ? pWorkItem.LWorkMergeSources
            : new[] { pWorkItem.LWorkSourcePath };
        return pInputs.Any(pInput => PLineagePathRead(pInput) is { } pInputKey && pProduced.Contains(pInputKey));
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
            IEnumerable<string> pInputs = pMerge ? pWorkItem.LWorkMergeSources : new[] { pWorkItem.LWorkSourcePath };
            foreach (string pInput in pInputs)
            {
                if (PLineagePathRead(pInput) is not { } pInputKey)
                {
                    continue;
                }

                pConsumed.Add(pInputKey);
                if (!pInputBytes.ContainsKey(pInputKey))
                {
                    pInputBytes[pInputKey] = (pMerge ? null : pWorkItem.LWorkSourceBytes) ?? PRosterSizeRead(pInput);
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

        long pOutputTotal = 0;
        bool pOutputAny = false, pOutputOk = true;
        var pSeenOutputs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (LWorkItem pWorkItem in pBatchItems)
        {
            if (PLineagePathRead(pWorkItem.LWorkOutputPath) is not { } pOutputKey
                || pConsumed.Contains(pOutputKey)
                || !pSeenOutputs.Add(pOutputKey))
            {
                continue;
            }

            pOutputAny = true;
            if (PRosterBytesRead(pWorkItem) is { } pOutputWhole)
            {
                pOutputTotal += pOutputWhole;
            }
            else
            {
                pOutputOk = false;
            }
        }

        return (
            pSourceAny && pSourceOk ? pSourceTotal : null,
            pOutputAny && pOutputOk ? pOutputTotal : null);
    }

    private void PRosterCardSelect(Guid pBatchId)
    {
        pRosterCardId = pBatchId;
        pRosterSelectedIds.Clear();
        pRosterCurrentId = Guid.Empty;
        PRosterSelectApply();
        PRosterCardApply();
        PRosterSelectHandle();
    }

    private void PRosterCardRemove(IReadOnlyList<LWorkItem> pBatchItems)
    {
        IReadOnlyList<Guid> pRemovable = pRosterSchedule.LScheduleRemovableRead(
            pBatchItems.Select(pWorkItem => pWorkItem.LWorkId));
        if (pRemovable.Count == 0 || !PRosterCardConfirm(pRemovable.Count))
        {
            return;
        }

        pRosterSchedule.LScheduleBatchRemove(pRemovable);
    }

    private static bool PRosterCardConfirm(int pRemovableCount)
    {
        if (!LPreference.LPreferenceStateCurrent.LPreferenceConfirmDestructive)
        {
            return true;
        }

        return MessageBox.Show(
            LLocalization.LLocalizationFormat("Roster.Card.Confirm", pRemovableCount),
            LLocalization.LLocalizationTextRead("Console.Confirm.Title"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No) == MessageBoxResult.Yes;
    }

    private static int PRosterInitialRead(IReadOnlyList<LWorkItem> pBatchItems)
    {
        var pOutputs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (LWorkItem pWorkItem in pBatchItems)
        {
            if (PLineagePathRead(pWorkItem.LWorkOutputPath) is { } pOutputKey)
            {
                pOutputs.Add(pOutputKey);
            }
        }

        var pInitials = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (LWorkItem pWorkItem in pBatchItems)
        {
            IEnumerable<string> pInputs = pWorkItem.LWorkKind == LWorkKind.LWorkKindMerge
                ? pWorkItem.LWorkMergeSources
                : new[] { pWorkItem.LWorkSourcePath };
            foreach (string pInput in pInputs)
            {
                if (PLineagePathRead(pInput) is { } pInputKey && !pOutputs.Contains(pInputKey))
                {
                    pInitials.Add(pInputKey);
                }
            }
        }

        return pInitials.Count;
    }
}

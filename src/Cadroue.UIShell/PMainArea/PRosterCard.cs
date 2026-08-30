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
        pButton.MouseLeftButtonUp += (_, _) => PRosterMinimizeToggle(pBatchId);
        pRosterBatchControls[pBatchId] = new PRosterBatchControl(pDetail, pButton, pIcon);

        return pButton;
    }

    private void PRosterMinimizeToggle(Guid pBatchId)
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

        PRosterBatchApply(pBatchId, pCollapsed);
    }

    private void PRosterBatchApply(Guid pBatchId, bool pCollapsed)
    {
        if (pRosterCardHeaders.TryGetValue(pBatchId, out Border? pHeader))
        {
            PRosterCollapseApply(pHeader, pCollapsed);
        }

        if (pRosterBatchControls.TryGetValue(pBatchId, out PRosterBatchControl? pControl))
        {
            pControl.PRosterBatchDetail.Visibility = pCollapsed ? Visibility.Collapsed : Visibility.Visible;
            pControl.PRosterBatchIcon.Source = PRosterMinimizeRead(pCollapsed, PRosterTheme.PRosterMutedBrush);
            pControl.PRosterBatchButton.ToolTip = LLocalization.LLocalizationTextRead(
                pCollapsed ? "Roster.Card.Expand" : "Roster.Card.Collapse");
        }
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

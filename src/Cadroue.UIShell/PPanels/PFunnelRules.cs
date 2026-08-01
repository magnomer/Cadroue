using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIShell.PControlBar;
using Cadroue.UIShell.PMainArea;

namespace Cadroue.UIShell.PPanels;

public sealed class PFunnelRules : PPanel
{
    private const string pFunnelAddIcon = "/PAssets/PPanels/PFunnelAdd.svg";
    private const string pFunnelRemoveIcon = "/PAssets/PPanels/PFunnelRemove.svg";
    private static readonly FontFamily pFunnelFontFamily = new("Segoe UI");
    private static readonly Brush pFunnelLineBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7));
    private static readonly Brush pFunnelTitleBrush = new SolidColorBrush(Color.FromRgb(0x26, 0x36, 0x4A));
    private static readonly Brush pFunnelMutedBrush = new SolidColorBrush(Color.FromRgb(0x8A, 0x93, 0x9E));
    private static readonly Brush pFunnelIconBrush = new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x3D));

    private readonly StackPanel pFunnelRowPanel;
    private readonly TextBlock pFunnelEmptyNotice;
    private readonly List<PFunnelRuleRow> pFunnelRows = new();
    private Func<IReadOnlyList<LCourierOption>> pFunnelOptionsRead = static () => Array.Empty<LCourierOption>();
    private PFunnelRuleRow? pFunnelRowDragging;
    private PFunnelRuleRow? pFunnelRowSelected;
    private Point? pFunnelDragOrigin;
    private Point pFunnelGrabOffset;
    private bool pFunnelDragActive;
    private PMainWindow.PGhost? pFunnelGhost;

    public PFunnelRules() : base("")
    {
        MinWidth = 300;

        pFunnelRowPanel = new StackPanel { Margin = new Thickness(12, 12, 12, 12) };

        pFunnelEmptyNotice = new TextBlock
        {
            Text = LLocalization.LLocalizationTextRead("Inspector.Funnel.Empty"),
            FontSize = 12,
            FontFamily = pFunnelFontFamily,
            Foreground = pFunnelMutedBrush,
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(16, 24, 16, 16),
            IsHitTestVisible = false
        };

        var pBody = new Grid();
        pBody.Children.Add(pFunnelEmptyNotice);
        pBody.Children.Add(pFunnelRowPanel);

        var pScroll = new ScrollViewer
        {
            Content = pBody,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };

        var pRoot = new DockPanel { LastChildFill = true };
        UIElement pHeader = PFunnelHeaderBuild();
        UIElement pActionBar = PFunnelActionBuild();
        DockPanel.SetDock(pHeader, Dock.Top);
        DockPanel.SetDock(pActionBar, Dock.Bottom);
        pRoot.Children.Add(pHeader);
        pRoot.Children.Add(pActionBar);
        pRoot.Children.Add(pScroll);

        Content = PPanelBorderBuild(pRoot);
        PFunnelEmptyUpdate();
    }

    public void PFunnelOptionsSet(Func<IReadOnlyList<LCourierOption>> pOptionsRead)
    {
        pFunnelOptionsRead = pOptionsRead;
    }

    public IReadOnlyList<PFunnelRuleRow> PFunnelRulesRead() => pFunnelRows;

    public void PFunnelRulesSeed(IReadOnlyList<LSceneFunnelRule> pRuleRecords)
    {
        foreach (LSceneFunnelRule pRecord in pRuleRecords)
        {
            PFunnelForm pForm = pRecord.LSceneFunnelType == (int)PFunnelForm.Regex
                ? PFunnelForm.Regex
                : PFunnelForm.Filename;
            PFunnelRuleRow pRow = PFunnelRuleAdd(pForm);
            pRow.PFunnelRowRestore(pRecord);
        }
    }

    public void PFunnelTargetsResolve(IReadOnlyList<PTabRecord> pTabRecords)
    {
        foreach (PFunnelRuleRow pRow in pFunnelRows)
        {
            int pTargetIndex = pRow.PFunnelTargetPending;
            if (pTargetIndex >= 0 && pTargetIndex < pTabRecords.Count)
            {
                pRow.PFunnelTargetSet(pTabRecords[pTargetIndex].PTabId);
            }
            else
            {
                pRow.PFunnelTargetSet(Guid.Empty);
            }
        }
    }

    public PFunnelRuleRow PFunnelRuleAdd(PFunnelForm pForm = PFunnelForm.Filename)
    {
        var pRow = new PFunnelRuleRow(pFunnelOptionsRead, pForm);
        pRow.PFunnelRowRemove += PFunnelRuleRemove;
        pRow.PFunnelHeader.MouseLeftButtonDown += (_, pEvent) => PFunnelPressHandle(pRow, pEvent);
        pRow.PFunnelHeader.MouseMove += (_, pEvent) => PFunnelMoveHandle(pRow, pEvent);
        pRow.PFunnelHeader.MouseLeftButtonUp += (_, pEvent) => PFunnelUpHandle(pRow, pEvent);
        pFunnelRows.Add(pRow);
        pFunnelRowPanel.Children.Add(pRow);
        PFunnelOrderApply();
        PFunnelEmptyUpdate();
        return pRow;
    }

    private void PFunnelRuleRemove(PFunnelRuleRow pRow)
    {
        if (ReferenceEquals(pFunnelRowSelected, pRow))
        {
            pFunnelRowSelected = null;
        }

        pFunnelRows.Remove(pRow);
        pFunnelRowPanel.Children.Remove(pRow);
        PFunnelOrderApply();
        PFunnelEmptyUpdate();
    }

    private void PFunnelOrderApply()
    {
        for (int pIndex = 0; pIndex < pFunnelRows.Count; pIndex++)
        {
            pFunnelRows[pIndex].PFunnelOrderSet(pIndex + 1);
        }
    }

    private void PFunnelPressHandle(PFunnelRuleRow pRow, MouseButtonEventArgs pEvent)
    {
        pFunnelRowDragging = pRow;
        pFunnelDragOrigin = pEvent.GetPosition(pFunnelRowPanel);
        pFunnelGrabOffset = pEvent.GetPosition(pRow);
        pFunnelDragActive = false;
        pRow.PFunnelHeader.CaptureMouse();
    }

    private void PFunnelMoveHandle(PFunnelRuleRow pRow, MouseEventArgs pEvent)
    {
        if (!ReferenceEquals(pFunnelRowDragging, pRow)
            || pFunnelDragOrigin is not Point pStart
            || pEvent.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        Point pCurrent = pEvent.GetPosition(pFunnelRowPanel);
        if (!pFunnelDragActive
            && Math.Abs(pCurrent.X - pStart.X) < SystemParameters.MinimumHorizontalDragDistance
            && Math.Abs(pCurrent.Y - pStart.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        if (!pFunnelDragActive)
        {
            pFunnelDragActive = true;
            pRow.Opacity = 0.72;
            pFunnelGhost = PMainWindow.PGhost.PGhostShow(pRow, pFunnelGrabOffset);
        }

        pFunnelGhost?.PGhostCursorSync();
        PFunnelRowMove(pRow, PFunnelIndexResolve(pCurrent));
        pEvent.Handled = true;
    }

    private void PFunnelUpHandle(PFunnelRuleRow pRow, MouseButtonEventArgs pEvent)
    {
        if (!ReferenceEquals(pFunnelRowDragging, pRow))
        {
            return;
        }

        bool pDragMoved = pFunnelDragActive;
        pRow.Opacity = 1;
        pRow.PFunnelHeader.ReleaseMouseCapture();
        pFunnelGhost?.PGhostClear();
        pFunnelGhost = null;
        pFunnelRowDragging = null;
        pFunnelDragOrigin = null;
        pFunnelDragActive = false;

        if (!pDragMoved)
        {
            PFunnelRowSelect(pRow);
        }

        pEvent.Handled = true;
    }

    private int PFunnelIndexResolve(Point pMousePoint)
    {
        int pTargetIndex = 0;
        for (int pIndex = 0; pIndex < pFunnelRowPanel.Children.Count; pIndex++)
        {
            if (pFunnelRowPanel.Children[pIndex] is not FrameworkElement pRow)
            {
                continue;
            }

            Point pRowPoint = pRow.TransformToAncestor(pFunnelRowPanel).Transform(new Point(0, 0));
            if (pMousePoint.Y > pRowPoint.Y + pRow.ActualHeight / 2)
            {
                pTargetIndex = pIndex + 1;
            }
        }

        return Math.Clamp(pTargetIndex, 0, pFunnelRowPanel.Children.Count);
    }

    private void PFunnelRowMove(PFunnelRuleRow pRow, int pTargetIndex)
    {
        int pSourceIndex = pFunnelRows.IndexOf(pRow);
        if (pSourceIndex < 0)
        {
            return;
        }

        pTargetIndex = Math.Clamp(pTargetIndex, 0, pFunnelRows.Count);
        int pInsertIndex = pSourceIndex < pTargetIndex ? pTargetIndex - 1 : pTargetIndex;
        if (pSourceIndex == pInsertIndex)
        {
            return;
        }

        pFunnelRows.RemoveAt(pSourceIndex);
        pFunnelRows.Insert(pInsertIndex, pRow);
        pFunnelRowPanel.Children.RemoveAt(pSourceIndex);
        pFunnelRowPanel.Children.Insert(pInsertIndex, pRow);
        PFunnelOrderApply();
    }

    private void PFunnelRowSelect(PFunnelRuleRow pRow)
    {
        if (ReferenceEquals(pFunnelRowSelected, pRow))
        {
            return;
        }

        pFunnelRowSelected?.PFunnelSelectSet(false);
        pFunnelRowSelected = pRow;
        pRow.PFunnelSelectSet(true);
    }

    private void PFunnelEmptyUpdate()
    {
        pFunnelEmptyNotice.Visibility = pFunnelRows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private static Border PFunnelHeaderBuild()
    {
        var pTitleLabel = new TextBlock
        {
            Text = LLocalization.LLocalizationTextRead("Inspector.Funnel.Title"),
            FontSize = 12,
            FontFamily = pFunnelFontFamily,
            FontWeight = FontWeights.SemiBold,
            Foreground = pFunnelTitleBrush,
            VerticalAlignment = VerticalAlignment.Center
        };

        return new Border
        {
            Padding = new Thickness(12, 5, 6, 5),
            MinHeight = 36,
            BorderBrush = pFunnelLineBrush,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Background = Brushes.White,
            Child = pTitleLabel
        };
    }

    private Border PFunnelActionBuild()
    {
        var pButtonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        Button pAddButton = PFunnelButtonBuild(pFunnelAddIcon, "Inspector.Funnel.Add");
        pAddButton.Click += (_, _) => PFunnelMenuShow(pAddButton);
        pButtonPanel.Children.Add(pAddButton);

        Button pRemoveButton = PFunnelButtonBuild(pFunnelRemoveIcon, "Inspector.Funnel.Remove");
        pRemoveButton.Click += (_, _) =>
        {
            if (pFunnelRowSelected is { } pSelectedRow)
            {
                PFunnelRuleRemove(pSelectedRow);
            }
        };
        pButtonPanel.Children.Add(pRemoveButton);

        return new Border
        {
            Padding = new Thickness(10, 4, 10, 6),
            BorderBrush = pFunnelLineBrush,
            BorderThickness = new Thickness(0, 1, 0, 0),
            Background = Brushes.White,
            Child = pButtonPanel
        };
    }

    private static Button PFunnelButtonBuild(string pIconPath, string pTooltipKey)
    {
        return new Button
        {
            Content = new Image
            {
                Width = 14,
                Height = 14,
                Source = PAssets.PIcon.PIconRead(pIconPath, pFunnelIconBrush),
                Stretch = Stretch.Uniform
            },
            Width = 28,
            Height = 26,
            Margin = new Thickness(0, 0, 2, 0),
            Style = PMainWindow.PButton.PButtonPanelCreate(),
            ToolTip = LLocalization.LLocalizationTextRead(pTooltipKey)
        };
    }

    private void PFunnelMenuShow(UIElement pTarget)
    {
        MenuItem pFilenameItem = PMenu.PMenuItemCreate(
            LLocalization.LLocalizationTextRead("Inspector.Funnel.Filename"), null);
        pFilenameItem.Click += (_, _) => PFunnelRuleAdd(PFunnelForm.Filename);

        MenuItem pRegexItem = PMenu.PMenuItemCreate(
            LLocalization.LLocalizationTextRead("Inspector.Funnel.Regex"), null);
        pRegexItem.Click += (_, _) => PFunnelRuleAdd(PFunnelForm.Regex);

        var pAddMenu = PMenu.PMenuContextCreate();
        pAddMenu.PlacementTarget = pTarget;
        pAddMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Top;
        pAddMenu.VerticalOffset = -4;
        pAddMenu.Items.Add(pFilenameItem);
        pAddMenu.Items.Add(pRegexItem);
        pAddMenu.IsOpen = true;
    }
}

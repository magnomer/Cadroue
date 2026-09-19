using Cadroue.Core;
using Cadroue.Application;
using Cadroue.UIDeportment;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIVeneer.PPorch;
using Cadroue.UIVeneer.PCabin;

namespace Cadroue.UIVeneer.PWing;

public sealed class PFunnelRules : PPanel
{
    private const string pFunnelAddIcon = "/PAsset/PPanel/PFunnelAdd.svg";
    private const string pFunnelRemoveIcon = "/PAsset/PPanel/PFunnelRemove.svg";
    private static readonly FontFamily pFunnelFontFamily = new("Segoe UI");
    private static readonly Brush pFunnelLineBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7));
    private static readonly Brush pFunnelTitleBrush = new SolidColorBrush(Color.FromRgb(0x26, 0x36, 0x4A));
    private static readonly Brush pFunnelMutedBrush = new SolidColorBrush(Color.FromRgb(0x8A, 0x93, 0x9E));
    private static readonly Brush pFunnelIconBrush = new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x3D));

    private readonly StackPanel pFunnelRowPanel;
    private readonly TextBlock pFunnelEmptyNotice;
    private readonly Dictionary<LFunnelRule, PFunnelRuleRow> pFunnelRows = new();
    private Func<IReadOnlyList<PActionRelayOption>> pFunnelOptionsSource =
        static () => Array.Empty<PActionRelayOption>();
    private PFunnelRuleRow? pFunnelRowDragging;
    private Point? pFunnelDragOrigin;
    private Point pFunnelGrabOffset;
    private bool pFunnelDragActive;
    private PHouse.PGhost? pFunnelGhost;

    public LFunnel LFunnel { get; } = new();

    public PFunnelRules() : base("")
    {
        MinWidth = 300;
        LFunnel.LFunnelChange += PFunnelRowsUpdate;
        LFunnel.LFunnelRuleChange += PFunnelRuleHandle;

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

    public void PFunnelOptionsSet(Func<IReadOnlyList<PActionRelayOption>> pOptionsRead)
    {
        pFunnelOptionsSource = pOptionsRead;
    }

    private PFunnelRuleRow PFunnelRowBuild(LFunnelRule lRule)
    {
        var pRow = new PFunnelRuleRow(LFunnel, lRule, pFunnelOptionsSource);
        pRow.PFunnelHeader.MouseLeftButtonDown += (_, pEvent) => PFunnelPressHandle(pRow, pEvent);
        pRow.PFunnelHeader.MouseMove += (_, pEvent) => PFunnelMoveHandle(pRow, pEvent);
        pRow.PFunnelHeader.MouseLeftButtonUp += (_, pEvent) => PFunnelUpHandle(pRow, pEvent);
        pRow.PFunnelHeader.LostMouseCapture += (_, _) => PFunnelDragReset(pRow);
        pRow.PreviewMouseLeftButtonDown += (_, _) => LFunnel.LFunnelRuleSelect(lRule);
        pRow.GotKeyboardFocus += (_, _) => LFunnel.LFunnelRuleSelect(lRule);
        return pRow;
    }

    private void PFunnelRuleHandle(LFunnelRule lRule)
    {
        if (pFunnelRows.TryGetValue(lRule, out PFunnelRuleRow? pRow))
        {
            pRow.PFunnelRowUpdate();
        }
    }

    private void PFunnelRowsUpdate()
    {
        IReadOnlyList<LFunnelRule> lRules = LFunnel.LFunnelRules;
        foreach (LFunnelRule lGone in pFunnelRows.Keys.Where(lRule => !lRules.Contains(lRule)).ToArray())
        {
            pFunnelRowPanel.Children.Remove(pFunnelRows[lGone]);
            pFunnelRows.Remove(lGone);
        }

        for (int pIndex = 0; pIndex < lRules.Count; pIndex++)
        {
            LFunnelRule lRule = lRules[pIndex];
            if (!pFunnelRows.TryGetValue(lRule, out PFunnelRuleRow? pRow))
            {
                pRow = PFunnelRowBuild(lRule);
                pFunnelRows[lRule] = pRow;
                pFunnelRowPanel.Children.Insert(pIndex, pRow);
            }
            else if (pFunnelRowPanel.Children.IndexOf(pRow) != pIndex)
            {
                pFunnelRowPanel.Children.Remove(pRow);
                pFunnelRowPanel.Children.Insert(pIndex, pRow);
            }

            pRow.PFunnelOrderSet(pIndex + 1);
            pRow.PFunnelSelectSet(ReferenceEquals(LFunnel.LFunnelSelected, lRule));
        }

        PFunnelEmptyUpdate();
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
            pFunnelGhost = PHouse.PGhost.PGhostShow(pRow, pFunnelGrabOffset);
        }

        pFunnelGhost?.PGhostCursorSync();
        LFunnel.LFunnelRuleMove(pRow.PFunnelRule, PFunnelIndexResolve(pCurrent));
        pEvent.Handled = true;
    }

    private void PFunnelUpHandle(PFunnelRuleRow pRow, MouseButtonEventArgs pEvent)
    {
        if (!ReferenceEquals(pFunnelRowDragging, pRow))
        {
            return;
        }

        pRow.PFunnelHeader.ReleaseMouseCapture();
        PFunnelDragReset(pRow);
        pEvent.Handled = true;
    }

    private void PFunnelDragReset(PFunnelRuleRow pRow)
    {
        if (!ReferenceEquals(pFunnelRowDragging, pRow))
        {
            return;
        }

        pRow.Opacity = 1;
        pFunnelGhost?.PGhostClear();
        pFunnelGhost = null;
        pFunnelRowDragging = null;
        pFunnelDragOrigin = null;
        pFunnelDragActive = false;
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
            if (LFunnel.LFunnelSelected is { } lSelected)
            {
                LFunnel.LFunnelRuleRemove(lSelected);
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
                Source = PAsset.PIcon.PIconRead(pIconPath, pFunnelIconBrush),
                Stretch = Stretch.Uniform
            },
            Width = 28,
            Height = 26,
            Margin = new Thickness(0, 0, 2, 0),
            Style = PHouse.PButton.PButtonPanelCreate(),
            ToolTip = LLocalization.LLocalizationTextRead(pTooltipKey)
        };
    }

    private void PFunnelMenuShow(UIElement pTarget)
    {
        MenuItem pFilenameItem = PMenu.PMenuItemCreate(
            LLocalization.LLocalizationTextRead("Inspector.Funnel.Filename"), null);
        pFilenameItem.Click += (_, _) => LFunnel.LFunnelRuleAdd(LFunnelForm.LFunnelFormFilename);

        MenuItem pRegexItem = PMenu.PMenuItemCreate(
            LLocalization.LLocalizationTextRead("Inspector.Funnel.Regex"), null);
        pRegexItem.Click += (_, _) => LFunnel.LFunnelRuleAdd(LFunnelForm.LFunnelFormRegex);

        MenuItem pRemainderItem = PMenu.PMenuItemCreate(
            LLocalization.LLocalizationTextRead("Inspector.Funnel.Remainder"), null);
        pRemainderItem.IsEnabled = !LFunnel.LFunnelRemainderCheck();
        pRemainderItem.Click += (_, _) => LFunnel.LFunnelRuleAdd(LFunnelForm.LFunnelFormRemainder);

        var pAddMenu = PMenu.PMenuContextCreate();
        pAddMenu.PlacementTarget = pTarget;
        pAddMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Top;
        pAddMenu.VerticalOffset = -4;
        pAddMenu.Items.Add(pFilenameItem);
        pAddMenu.Items.Add(pRegexItem);
        pAddMenu.Items.Add(pRemainderItem);
        pAddMenu.IsOpen = true;
    }
}

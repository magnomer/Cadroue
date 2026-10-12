using Cadroue.Application;
using Cadroue.UIDeportment;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIVeneer.PHouse;
using Cadroue.UIVeneer.PPorch;

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

    private static readonly IReadOnlyDictionary<bool, Action<PFunnelRules, PFunnelRuleRow, MouseEventArgs>>
        pFunnelMoves = new Dictionary<bool, Action<PFunnelRules, PFunnelRuleRow, MouseEventArgs>>
        {
            [true] = (pRules, pRow, pEvent) => pRules.PFunnelMoveRun(pRow, pEvent),
            [false] = (pRules, pRow, pEvent) => { },
        };

    private static readonly IReadOnlyDictionary<bool, Action<PFunnelRules, PFunnelRuleRow>> pFunnelGhosts =
        new Dictionary<bool, Action<PFunnelRules, PFunnelRuleRow>>
        {
            [true] = (pRules, pRow) => pRules.PFunnelGhostShow(pRow),
            [false] = (pRules, pRow) => { },
        };

    private static readonly IReadOnlyDictionary<bool, Action<PFunnelRules, PFunnelRuleRow>> pFunnelReleases =
        new Dictionary<bool, Action<PFunnelRules, PFunnelRuleRow>>
        {
            [true] = (pRules, pRow) => pRules.PFunnelDragClear(pRow),
            [false] = (pRules, pRow) => { },
        };

    private readonly QFunnel lFunnel;
    private readonly Grid pFunnelRowGrid;
    private readonly TextBlock pFunnelEmptyNotice;
    private readonly Dictionary<QFunnelRule, PFunnelRuleRow> pFunnelRows = new();

    public PFunnelRules(QFunnel lFunnelOwner) : base("")
    {
        lFunnel = lFunnelOwner;
        MinWidth = 300;
        pFunnelRowGrid = new Grid { Margin = new Thickness(12, 12, 12, 12) };
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
        pBody.Children.Add(pFunnelRowGrid);

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
        lFunnel.QFunnelRuleCreated += PFunnelRowAdd;
        lFunnel.QFunnelRuleDeleted += PFunnelRowRemove;
        lFunnel.QFunnelChanged += PFunnelRowsUpdate;
        lFunnel.QFunnelRuleChanged += PFunnelRuleHandle;
        lFunnel.QFunnelRules.ToList().ForEach(PFunnelRowAdd);
        PFunnelRowsUpdate();
    }

    public QFunnel QFunnel => lFunnel;

    private void PFunnelRowAdd(QFunnelRule lRule)
    {
        var pRow = new PFunnelRuleRow(lFunnel, lRule);
        pRow.PFunnelHeader.MouseLeftButtonDown += (_, pEvent) => PFunnelPressHandle(pRow, pEvent);
        pRow.PFunnelHeader.MouseMove += (_, pEvent) => PFunnelMoveHandle(pRow, pEvent);
        pRow.PFunnelHeader.MouseLeftButtonUp += (_, pEvent) => PFunnelUpHandle(pRow, pEvent);
        pRow.PFunnelHeader.LostMouseCapture += (_, _) => PFunnelReleaseHandle(pRow);
        pRow.PreviewMouseLeftButtonDown += (_, _) => lFunnel.QFunnelRuleSelect(lRule);
        pRow.GotKeyboardFocus += (_, _) => lFunnel.QFunnelRuleSelect(lRule);
        pFunnelRows.Add(lRule, pRow);
        pFunnelRowGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        pFunnelRowGrid.Children.Add(pRow);
    }

    private void PFunnelRowRemove(QFunnelRule lRule)
    {
        pFunnelRowGrid.Children.Remove(pFunnelRows[lRule]);
        pFunnelRows.Remove(lRule);
    }

    private void PFunnelRuleHandle(QFunnelRule lRule) => pFunnelRows[lRule].PFunnelRowUpdate();

    private void PFunnelRowsUpdate()
    {
        lFunnel.QFunnelSlotsRead().ToList().ForEach(PFunnelSlotApply);
        pFunnelEmptyNotice.Visibility = PLook.PLookVisible[lFunnel.QFunnelEmpty];
    }

    private void PFunnelSlotApply(QFunnelSlot lSlot)
    {
        PFunnelRuleRow pRow = pFunnelRows[lSlot.QFunnelSlotRule];
        Grid.SetRow(pRow, lSlot.QFunnelSlotIndex);
        pRow.PFunnelOrderSet(lSlot.QFunnelSlotOrder);
        pRow.PFunnelSelectSet(lSlot.QFunnelSlotSelected);
    }

    private void PFunnelPressHandle(PFunnelRuleRow pRow, MouseButtonEventArgs pEvent)
    {
        Point pGridPoint = pEvent.GetPosition(pFunnelRowGrid);
        Point pOffset = pEvent.GetPosition(pRow);
        lFunnel.QFunnelDrag.QFunnelPressHandle(pRow.PFunnelRule, pGridPoint.X, pGridPoint.Y, pOffset.X, pOffset.Y);
        pRow.PFunnelHeader.CaptureMouse();
    }

    private void PFunnelMoveHandle(PFunnelRuleRow pRow, MouseEventArgs pEvent) =>
        pFunnelMoves[lFunnel.QFunnelDrag.QFunnelMoveCheck(pRow.PFunnelRule, PLook.PLookPressed[pEvent.LeftButton])](
            this, pRow, pEvent);

    private void PFunnelMoveRun(PFunnelRuleRow pRow, MouseEventArgs pEvent)
    {
        Point pCurrent = pEvent.GetPosition(pFunnelRowGrid);
        bool pStart = lFunnel.QFunnelDrag.QFunnelDragResolve(
            pCurrent.X,
            pCurrent.Y,
            SystemParameters.MinimumHorizontalDragDistance,
            SystemParameters.MinimumVerticalDragDistance);
        pFunnelGhosts[pStart](this, pRow);
        PGhost.PGhostSync(pRow);
        lFunnel.QFunnelDrag.QFunnelDragMove(pCurrent.Y, lFunnel.QFunnelRules.Select(PFunnelCenterRead).ToList());
        pEvent.Handled = true;
    }

    private void PFunnelGhostShow(PFunnelRuleRow pRow)
    {
        pRow.Opacity = 0.72;
        PGhost.PGhostShow(pRow, lFunnel.QFunnelDrag.QFunnelGhost);
    }

    private double PFunnelCenterRead(QFunnelRule lRule)
    {
        PFunnelRuleRow pRow = pFunnelRows[lRule];
        Point pRowPoint = pRow.TransformToAncestor(pFunnelRowGrid).Transform(new Point(0, 0));
        return QFunnelDrag.QFunnelCenterResolve(pRowPoint.Y, pRow.ActualHeight);
    }

    private void PFunnelUpHandle(PFunnelRuleRow pRow, MouseButtonEventArgs pEvent)
    {
        bool pReleased = lFunnel.QFunnelDrag.QFunnelReleaseCheck(pRow.PFunnelRule);
        pFunnelReleases[pReleased](this, pRow);
        pRow.PFunnelHeader.ReleaseMouseCapture();
        pEvent.Handled = pReleased;
    }

    private void PFunnelReleaseHandle(PFunnelRuleRow pRow) =>
        pFunnelReleases[lFunnel.QFunnelDrag.QFunnelReleaseCheck(pRow.PFunnelRule)](this, pRow);

    private void PFunnelDragClear(PFunnelRuleRow pRow)
    {
        pRow.Opacity = 1;
        PGhost.PGhostClear(pRow);
        lFunnel.QFunnelDrag.QFunnelDragClear();
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
        pRemoveButton.Click += (_, _) => lFunnel.QFunnelSelectedRemove();
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
                Width = 20,
                Height = 20,
                Source = PAsset.PIcon.PIconRead(pIconPath, pFunnelIconBrush),
                Stretch = Stretch.Uniform
            },
            Width = 28,
            Height = 26,
            Margin = new Thickness(0, 0, 2, 0),
            Style = PButton.PButtonPanelCreate(),
            ToolTip = LLocalization.LLocalizationTextRead(pTooltipKey)
        };
    }

    private void PFunnelMenuShow(UIElement pTarget)
    {
        MenuItem pFilenameItem = PMenu.PMenuItemCreate(
            LLocalization.LLocalizationTextRead("Inspector.Funnel.Filename"));
        pFilenameItem.Click += (_, _) => lFunnel.QFunnelRuleAdd(QFunnelForm.QFunnelFormFilename);

        MenuItem pRegexItem = PMenu.PMenuItemCreate(
            LLocalization.LLocalizationTextRead("Inspector.Funnel.Regex"));
        pRegexItem.Click += (_, _) => lFunnel.QFunnelRuleAdd(QFunnelForm.QFunnelFormRegex);

        MenuItem pRemainderItem = PMenu.PMenuItemCreate(
            LLocalization.LLocalizationTextRead("Inspector.Funnel.Remainder"));
        pRemainderItem.IsEnabled = !lFunnel.QFunnelRemainderCheck();
        pRemainderItem.Click += (_, _) => lFunnel.QFunnelRuleAdd(QFunnelForm.QFunnelFormRemainder);

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

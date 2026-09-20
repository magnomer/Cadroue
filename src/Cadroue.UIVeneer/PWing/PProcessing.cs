using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.Application;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PAsset;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PWing;

public sealed class PProcessing : PPanel
{
    private static readonly FontFamily pProcessingFontFamily = new("Segoe UI");
    private static readonly Brush pProcessingIconBrush = new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x3D));
    private static readonly Brush pProcessingLineBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7));
    private static readonly Thickness pProcessingStepPadding = new(12, 7, 12, 7);
    private static readonly Thickness pProcessingStepLine = new(0, 0, 0, 1);
    private static readonly Thickness pProcessingSkipPadding = new(12, 9, 12, 9);
    private static readonly Thickness pProcessingSkipLine = new(0, 1, 0, 0);

    private static readonly IReadOnlyDictionary<Key, bool> pProcessingActivateKeys = new Dictionary<Key, bool>
    {
        [Key.Enter] = true,
        [Key.Space] = true,
    };

    private const string PProcessingUpIcon = "/PAsset/PPanel/PProcessingUp.svg";
    private const string PProcessingDownIcon = "/PAsset/PPanel/PProcessingDown.svg";
    private const string PProcessingMonitorIcon = "/PAsset/PPanel/PProcessingViewer.svg";
    private const string PProcessingSkipIcon = "/PAsset/PPanel/PProcessingSkip.svg";

    public const double PProcessingStripWidth = 48;

    public event Action<string?>? PProcessingStepChange;
    public event Action<string>? PProcessingStepOpen;
    public event Action<bool>? PProcessingMinimizeChange;
    public event Action? PProcessingMonitorShow;

    private readonly StackPanel pProcessingRowPanel;
    private readonly UIElement pProcessingFullBody;
    private readonly UIElement pProcessingStripBody;
    private readonly UIElement pProcessingActionBar;
    private readonly Button pProcessingMonitorButton;
    private readonly PProcessingRow pProcessingSkipRow;
    private readonly Dictionary<string, PProcessingRow> pProcessingRows = new(StringComparer.Ordinal);

    public LProcessing LProcessing { get; } = new();

    public PProcessing() : base("")
    {
        LProcessing.LProcessingChange += PProcessingUpdate;
        LProcessing.LProcessingOrderChange += PProcessingOrderUpdate;
        LProcessing.LProcessingMinimizeChange += PProcessingMinimizeHandle;
        LProcessing.LProcessingStepChange += PProcessingStepHandle;
        LProcessing.LProcessingDragCancel += PProcessingCaptureClear;
        UIElement pHeader = PProcessingHeaderBuild();

        pProcessingRowPanel = new StackPanel();
        pProcessingRowPanel.PreviewMouseMove += PProcessingMoveHandle;
        pProcessingRowPanel.MouseLeftButtonUp += PProcessingUpHandle;
        pProcessingRowPanel.LostMouseCapture += PProcessingLostHandle;

        var pScroll = new ScrollViewer
        {
            Content = pProcessingRowPanel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };

        pProcessingMonitorButton = PProcessingButtonBuild(
            PProcessingMonitorIcon,
            LLocalization.LLocalizationTextRead("NormalizePreview.Button.Tooltip"),
            () => PProcessingMonitorShow?.Invoke());
        pProcessingMonitorButton.HorizontalAlignment = HorizontalAlignment.Right;
        pProcessingMonitorButton.Visibility = Visibility.Collapsed;
        pProcessingActionBar = PProcessingActionBuild();
        pProcessingActionBar.Visibility = Visibility.Collapsed;
        pProcessingSkipRow = PProcessingSkipBuild();

        var pRoot = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(pHeader, Dock.Top);
        pRoot.Children.Add(pHeader);
        DockPanel.SetDock(pProcessingActionBar, Dock.Bottom);
        pRoot.Children.Add(pProcessingActionBar);
        DockPanel.SetDock(pProcessingSkipRow.PProcessingRowBorder, Dock.Bottom);
        pRoot.Children.Add(pProcessingSkipRow.PProcessingRowBorder);
        pRoot.Children.Add(pScroll);

        pProcessingFullBody = pRoot;
        pProcessingStripBody = PProcessingStripBuild();
        pProcessingStripBody.Visibility = Visibility.Collapsed;

        var pBodyHost = new Grid();
        pBodyHost.Children.Add(pProcessingFullBody);
        pBodyHost.Children.Add(pProcessingStripBody);

        FocusVisualStyle = null;
        Content = PPanelBorderBuild(pBodyHost);
    }

    public bool PProcessingMinimizedCheck() => LProcessing.LProcessingMinimized;

    public void PProcessingMinimizeSet(bool pProcessingMinimizeRequest) =>
        LProcessing.LProcessingMinimizedSet(pProcessingMinimizeRequest);

    public void PProcessingMonitorSet() => pProcessingMonitorButton.Visibility = Visibility.Visible;

    public void PProcessingRowAdd(LProcessingRow lRow)
    {
        PProcessingRow pRow = PProcessingStepBuild(lRow);
        pProcessingRows.Add(lRow.LProcessingRowKey, pRow);
        pProcessingRowPanel.Children.Add(pRow.PProcessingRowBorder);
        LProcessing.LProcessingRowAdd(lRow);
    }

    private PProcessingRow PProcessingStepBuild(LProcessingRow lRow)
    {
        string pStepName = lRow.LProcessingRowKey;
        var pRow = new PProcessingRow(
            lRow.LProcessingRowIcon,
            LLocalization.LLocalizationTextRead(lRow.LProcessingRowLabel),
            pProcessingStepPadding,
            pProcessingStepLine);
        Border pBorder = pRow.PProcessingRowBorder;
        pBorder.Focusable = true;
        ToolTipService.SetShowOnDisabled(pBorder, true);
        pBorder.MouseLeftButtonDown += (_, pEvent) => PProcessingPressHandle(pStepName, pEvent);
        pBorder.KeyDown += (_, pEvent) => PProcessingKeyHandle(pStepName, pEvent);
        return pRow;
    }

    private PProcessingRow PProcessingSkipBuild()
    {
        var pRow = new PProcessingRow(
            PProcessingSkipIcon,
            LLocalization.LLocalizationTextRead("Processing.Skip.Label"),
            pProcessingSkipPadding,
            pProcessingSkipLine);
        pRow.PProcessingRowBorder.ToolTip = LLocalization.LLocalizationTextRead("Processing.Skip.Tooltip");
        pRow.PProcessingRowBorder.MouseLeftButtonUp += (_, _) =>
            LProcessing.LProcessingStepSelect(LProcessing.LProcessingSkipStep);
        return pRow;
    }

    private void PProcessingPressHandle(string pStepName, MouseButtonEventArgs pEvent)
    {
        Point pPoint = pEvent.GetPosition(pProcessingRowPanel);
        LProcessing.LProcessingDragStart(pStepName, pPoint.X, pPoint.Y);
        pEvent.Handled = true;
    }

    private void PProcessingKeyHandle(string pStepName, KeyEventArgs pEvent)
    {
        bool pActivate = pProcessingActivateKeys.GetValueOrDefault(pEvent.Key);
        LProcessing.LProcessingKeyHandle(pStepName, pActivate);
        pEvent.Handled = pActivate;
    }

    private void PProcessingMoveHandle(object pSender, MouseEventArgs pEvent)
    {
        Point pPoint = pEvent.GetPosition(pProcessingRowPanel);
        LProcessing.LProcessingDragMove(
            pPoint.X,
            pPoint.Y,
            PLook.PLookPressed[pEvent.LeftButton],
            SystemParameters.MinimumHorizontalDragDistance,
            SystemParameters.MinimumVerticalDragDistance,
            LProcessing.LProcessingSteps.Select(PProcessingTopRead).ToList(),
            LProcessing.LProcessingSteps.Select(PProcessingHeightRead).ToList());
    }

    private void PProcessingUpHandle(object pSender, MouseButtonEventArgs pEvent) => LProcessing.LProcessingDragClear();

    private void PProcessingLostHandle(object pSender, MouseEventArgs pEvent) => LProcessing.LProcessingDragClear();

    private static void PProcessingCaptureClear() => Mouse.Capture(null);

    private double PProcessingTopRead(string pStepName) =>
        pProcessingRows[pStepName].PProcessingRowBorder.TranslatePoint(new Point(0, 0), pProcessingRowPanel).Y;

    private double PProcessingHeightRead(string pStepName) =>
        pProcessingRows[pStepName].PProcessingRowBorder.ActualHeight;

    private Border PProcessingBorderRead(string pStepName) => pProcessingRows[pStepName].PProcessingRowBorder;

    private void PProcessingStepHandle(string pStepName)
    {
        PProcessingStepChange?.Invoke(pStepName);
        PProcessingStepOpen?.Invoke(pStepName);
    }

    private void PProcessingMinimizeHandle(bool pProcessingMinimized)
    {
        pProcessingFullBody.Visibility = PLook.PLookVisible[!pProcessingMinimized];
        pProcessingStripBody.Visibility = PLook.PLookVisible[pProcessingMinimized];
        PProcessingMinimizeChange?.Invoke(pProcessingMinimized);
    }

    private void PProcessingOrderUpdate()
    {
        pProcessingRowPanel.Children.Clear();
        LProcessing.LProcessingSteps
            .Select(PProcessingBorderRead)
            .ToList()
            .ForEach(pBorder => pProcessingRowPanel.Children.Add(pBorder));
        PProcessingUpdate();
    }

    private void PProcessingUpdate()
    {
        pProcessingActionBar.Visibility = PLook.PLookVisible[LProcessing.LProcessingOrdered];
        LProcessing.LProcessingSteps.ToList().ForEach(PProcessingRowUpdate);
        pProcessingSkipRow.PProcessingSelectApply(LProcessing.LProcessingSkipSelected);
        pProcessingSkipRow.PProcessingActiveApply(LProcessing.LProcessingSkipActive);
        pProcessingRowPanel.Opacity = LProcessing.LProcessingSkipOpacity;
        pProcessingActionBar.IsEnabled = !LProcessing.LProcessingSkipActive;
        pProcessingActionBar.Opacity = LProcessing.LProcessingSkipOpacity;
    }

    private void PProcessingRowUpdate(string pStepName)
    {
        PProcessingRow pRow = pProcessingRows[pStepName];
        pRow.PProcessingStateApply(
            LProcessing.LProcessingEnabledCheck(pStepName),
            LProcessing.LProcessingOpacityRead(pStepName),
            LProcessing.LProcessingNoticeRead(pStepName),
            LProcessing.LProcessingNoticeFormat(pStepName));
        pRow.PProcessingSelectApply(LProcessing.LProcessingSelectedCheck(pStepName));
        pRow.PProcessingActiveApply(LProcessing.LProcessingActiveCheck(pStepName));
        pRow.PProcessingNumberApply(LProcessing.LProcessingNumberRead(pStepName), LProcessing.LProcessingOrdered);
    }

    private UIElement PProcessingHeaderBuild()
    {
        var pTitleLabel = new TextBlock
        {
            Text = LLocalization.LLocalizationTextRead("Processing.Header.Title"),
            FontSize = 12,
            FontFamily = pProcessingFontFamily,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(0x26, 0x36, 0x4A)),
            VerticalAlignment = VerticalAlignment.Center
        };

        Button pMinimizeButton = PProcessingButtonBuild(
            "/PAsset/PPanel/PListMinimize.svg",
            LLocalization.LLocalizationTextRead("Processing.Hide.Tooltip"),
            () => PProcessingMinimizeSet(true));
        pMinimizeButton.HorizontalAlignment = HorizontalAlignment.Right;

        var pHeaderGrid = new Grid();
        pHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(pMinimizeButton, 1);
        pHeaderGrid.Children.Add(pTitleLabel);
        pHeaderGrid.Children.Add(pMinimizeButton);

        return new Border
        {
            Padding = new Thickness(12, 5, 6, 5),
            BorderBrush = pProcessingLineBrush,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Background = Brushes.White,
            Child = pHeaderGrid
        };
    }

    private UIElement PProcessingActionBuild()
    {
        Button pUpButton = PProcessingButtonBuild(
            PProcessingUpIcon,
            LLocalization.LLocalizationTextRead("Processing.MoveUp.Tooltip"),
            () => LProcessing.LProcessingStepMove(-1));
        pUpButton.Margin = new Thickness(0, 0, 2, 0);
        Button pDownButton = PProcessingButtonBuild(
            PProcessingDownIcon,
            LLocalization.LLocalizationTextRead("Processing.MoveDown.Tooltip"),
            () => LProcessing.LProcessingStepMove(1));

        var pLeftPanel = new StackPanel { Orientation = Orientation.Horizontal };
        pLeftPanel.Children.Add(pUpButton);
        pLeftPanel.Children.Add(pDownButton);

        var pActionGrid = new Grid { Margin = new Thickness(10, 4, 10, 6) };
        pActionGrid.Children.Add(pLeftPanel);
        pActionGrid.Children.Add(pProcessingMonitorButton);

        return new Border
        {
            BorderBrush = pProcessingLineBrush,
            BorderThickness = new Thickness(0, 1, 0, 0),
            Background = Brushes.White,
            Child = pActionGrid
        };
    }

    private UIElement PProcessingStripBuild()
    {
        Button pMaximizeButton = PProcessingButtonBuild(
            "/PAsset/PPanel/PListMaximize.svg",
            LLocalization.LLocalizationTextRead("Processing.Show.Tooltip"),
            () => PProcessingMinimizeSet(false));
        pMaximizeButton.Margin = new Thickness(0, 6, 0, 0);
        pMaximizeButton.HorizontalAlignment = HorizontalAlignment.Center;

        var pStrip = new StackPanel { Background = Brushes.White };
        pStrip.Children.Add(pMaximizeButton);
        return pStrip;
    }

    private static Button PProcessingButtonBuild(string pIconPath, string pTooltip, Action pClick)
    {
        var pButton = new Button
        {
            Content = new Image
            {
                Width = 14,
                Height = 14,
                Source = PIcon.PIconRead(pIconPath, pProcessingIconBrush),
                Stretch = Stretch.Uniform
            },
            ToolTip = pTooltip,
            Width = 28,
            Height = 26,
            Style = PButton.PButtonPanelCreate()
        };
        pButton.Click += (_, _) => pClick();
        return pButton;
    }
}

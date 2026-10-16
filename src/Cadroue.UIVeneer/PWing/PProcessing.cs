using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PWing;

public sealed class PProcessing : PPanel
{
    private static readonly FontFamily pProcessingFontFamily = new("Segoe UI");
    private static readonly Brush pProcessingIconBrush = new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x3D));
    private static readonly Brush pProcessingLineBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7));
    private static readonly Thickness pProcessingStepPadding = new(12, 5, 12, 5);
    private static readonly Thickness pProcessingStepLine = new(0, 0, 0, 1);
    private static readonly Thickness pProcessingSkipPadding = new(12, 5, 12, 5);
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

    public event Action<string?>? PProcessingStepChanged;
    public event Action<string>? PProcessingStepOpened;
    public event Action<bool>? PProcessingMinimizeChanged;
    public event Action? PProcessingMonitorShown;

    private readonly StackPanel pProcessingRowPanel;
    private readonly UIElement pProcessingFullBody;
    private readonly UIElement pProcessingStripBody;
    private readonly UIElement pProcessingActionBar;
    private readonly Button pProcessingMonitorButton;
    private readonly PProcessingRow pProcessingSkipRow;
    private readonly Dictionary<string, PProcessingRow> pProcessingRows = new(StringComparer.Ordinal);

    public QProcessing QProcessing { get; }

    public PProcessing(QProcessing lProcessing) : base("")
    {
        QProcessing = lProcessing;
        QProcessing.QProcessingChanged += PProcessingUpdate;
        QProcessing.QProcessingOrderChanged += PProcessingOrderUpdate;
        QProcessing.QProcessingMinimizeChanged += PProcessingMinimizeHandle;
        QProcessing.QProcessingStepChanged += PProcessingStepHandle;
        QProcessing.QProcessingDragCanceled += PProcessingCaptureClear;
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
            QLocalizationCatalog.QLocalizationTextRead("NormalizePreview.Button.Tooltip"),
            () => PProcessingMonitorShown?.Invoke());
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
        PProcessingUpdate();
    }

    public bool PProcessingMinimizedCheck() => QProcessing.QProcessingMinimized;

    public void PProcessingMinimizeSet(bool pProcessingMinimizeRequest) =>
        QProcessing.QProcessingMinimizedSet(pProcessingMinimizeRequest);

    public void PProcessingMonitorSet() => pProcessingMonitorButton.Visibility = Visibility.Visible;

    public void PProcessingRowAdd(QProcessingRow lRow)
    {
        PProcessingRow pRow = PProcessingStepBuild(lRow);
        pProcessingRows.Add(lRow.QProcessingRowKey, pRow);
        pProcessingRowPanel.Children.Add(pRow.PProcessingRowBorder);
        QProcessing.QProcessingRowAdd(lRow);
    }

    private PProcessingRow PProcessingStepBuild(QProcessingRow lRow)
    {
        string pStepName = lRow.QProcessingRowKey;
        var pRow = new PProcessingRow(
            lRow.QProcessingRowIcon,
            QLocalizationCatalog.QLocalizationTextRead(lRow.QProcessingRowLabel),
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
            QLocalizationCatalog.QLocalizationTextRead("Processing.Skip.Label"),
            pProcessingSkipPadding,
            pProcessingSkipLine);
        pRow.PProcessingRowBorder.ToolTip = QLocalizationCatalog.QLocalizationTextRead("Processing.Skip.Tooltip");
        pRow.PProcessingRowBorder.MouseLeftButtonUp += (_, _) =>
            QProcessing.QProcessingStepSelect(QProcessing.QProcessingSkipStep);
        return pRow;
    }

    private void PProcessingPressHandle(string pStepName, MouseButtonEventArgs pEvent)
    {
        Point pPoint = pEvent.GetPosition(pProcessingRowPanel);
        QProcessing.QProcessingDragStart(pStepName, pPoint.X, pPoint.Y);
        pEvent.Handled = true;
    }

    private void PProcessingKeyHandle(string pStepName, KeyEventArgs pEvent)
    {
        bool pActivate = pProcessingActivateKeys.GetValueOrDefault(pEvent.Key);
        QProcessing.QProcessingKeyHandle(pStepName, pActivate);
        pEvent.Handled = pActivate;
    }

    private void PProcessingMoveHandle(object pSender, MouseEventArgs pEvent)
    {
        Point pPoint = pEvent.GetPosition(pProcessingRowPanel);
        QProcessing.QProcessingDragMove(
            pPoint.X,
            pPoint.Y,
            PLook.PLookPressed[pEvent.LeftButton],
            SystemParameters.MinimumHorizontalDragDistance,
            SystemParameters.MinimumVerticalDragDistance,
            QProcessing.QProcessingSteps.Select(PProcessingTopRead).ToList(),
            QProcessing.QProcessingSteps.Select(PProcessingHeightRead).ToList());
    }

    private void PProcessingUpHandle(object pSender, MouseButtonEventArgs pEvent) => QProcessing.QProcessingDragClear();

    private void PProcessingLostHandle(object pSender, MouseEventArgs pEvent) => QProcessing.QProcessingDragClear();

    private static void PProcessingCaptureClear() => Mouse.Capture(null);

    private double PProcessingTopRead(string pStepName) =>
        pProcessingRows[pStepName].PProcessingRowBorder.TranslatePoint(new Point(0, 0), pProcessingRowPanel).Y;

    private double PProcessingHeightRead(string pStepName) =>
        pProcessingRows[pStepName].PProcessingRowBorder.ActualHeight;

    private Border PProcessingBorderRead(string pStepName) => pProcessingRows[pStepName].PProcessingRowBorder;

    private void PProcessingStepHandle(string pStepName)
    {
        PProcessingStepChanged?.Invoke(pStepName);
        PProcessingStepOpened?.Invoke(pStepName);
    }

    private void PProcessingMinimizeHandle(bool pProcessingMinimized)
    {
        pProcessingFullBody.Visibility = PLook.PLookVisible[!pProcessingMinimized];
        pProcessingStripBody.Visibility = PLook.PLookVisible[pProcessingMinimized];
        PProcessingMinimizeChanged?.Invoke(pProcessingMinimized);
    }

    private void PProcessingOrderUpdate()
    {
        pProcessingRowPanel.Children.Clear();
        QProcessing.QProcessingSteps
            .Select(PProcessingBorderRead)
            .ToList()
            .ForEach(pBorder => pProcessingRowPanel.Children.Add(pBorder));
        PProcessingUpdate();
    }

    private void PProcessingUpdate()
    {
        pProcessingActionBar.Visibility = PLook.PLookVisible[QProcessing.QProcessingOrdered];
        QProcessing.QProcessingSteps.ToList().ForEach(PProcessingRowUpdate);
        pProcessingSkipRow.PProcessingSelectApply(QProcessing.QProcessingSkipSelected);
        pProcessingSkipRow.PProcessingActiveApply(QProcessing.QProcessingSkipActive);
        pProcessingRowPanel.Opacity = QProcessing.QProcessingSkipOpacity;
        pProcessingActionBar.IsEnabled = !QProcessing.QProcessingSkipActive;
        pProcessingActionBar.Opacity = QProcessing.QProcessingSkipOpacity;
    }

    private void PProcessingRowUpdate(string pStepName)
    {
        PProcessingRow pRow = pProcessingRows[pStepName];
        pRow.PProcessingStateApply(
            QProcessing.QProcessingEnabledCheck(pStepName),
            QProcessing.QProcessingOpacityRead(pStepName),
            QProcessing.QProcessingNoticeRead(pStepName),
            QProcessing.QProcessingNoticeFormat(pStepName));
        pRow.PProcessingSelectApply(QProcessing.QProcessingSelectedCheck(pStepName));
        pRow.PProcessingActiveApply(QProcessing.QProcessingActiveCheck(pStepName));
        pRow.PProcessingNumberApply(QProcessing.QProcessingNumberRead(pStepName), QProcessing.QProcessingOrdered);
    }

    private UIElement PProcessingHeaderBuild()
    {
        var pTitleLabel = new TextBlock
        {
            Text = QLocalizationCatalog.QLocalizationTextRead("Processing.Header.Title"),
            FontSize = 12,
            FontFamily = pProcessingFontFamily,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(0x26, 0x36, 0x4A)),
            VerticalAlignment = VerticalAlignment.Center
        };

        Button pMinimizeButton = PProcessingButtonBuild(
            "/PAsset/PPanel/PListMinimize.svg",
            QLocalizationCatalog.QLocalizationTextRead("Processing.Hide.Tooltip"),
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
            QLocalizationCatalog.QLocalizationTextRead("Processing.MoveUp.Tooltip"),
            () => QProcessing.QProcessingStepMove(-1));
        pUpButton.Margin = new Thickness(0, 0, 2, 0);
        Button pDownButton = PProcessingButtonBuild(
            PProcessingDownIcon,
            QLocalizationCatalog.QLocalizationTextRead("Processing.MoveDown.Tooltip"),
            () => QProcessing.QProcessingStepMove(1));

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
            QLocalizationCatalog.QLocalizationTextRead("Processing.Show.Tooltip"),
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
                Width = 20,
                Height = 20,
                Source = QIcon.QIconRead(pIconPath, pProcessingIconBrush),
                Stretch = Stretch.Uniform
            },
            ToolTip = pTooltip,
            Width = 28,
            Height = 26
        };
        pButton.Click += (_, _) => pClick();
        QButton.QButtonAttach(pButton, QButtonLook.QButtonLookPanel);
        return pButton;
    }
}

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PWing;

public sealed class PProcessingChrome
{
    private static readonly FontFamily pProcessingFontFamily = new("Segoe UI");
    private static readonly Brush pProcessingIconBrush = new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x3D));
    private static readonly Brush pProcessingLineBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7));

    private const string PProcessingUpIcon = "/PAsset/PPanel/PProcessingUp.svg";
    private const string PProcessingDownIcon = "/PAsset/PPanel/PProcessingDown.svg";
    private const string PProcessingMonitorIcon = "/PAsset/PPanel/PProcessingViewer.svg";

    private readonly QProcessing lProcessing;
    private readonly Button pProcessingMonitorButton;
    private readonly UIElement pProcessingActionBar;

    public PProcessingChrome(QProcessing lProcessingOwner, Action pMonitorShow)
    {
        lProcessing = lProcessingOwner;
        PProcessingHeader = PProcessingHeaderBuild();
        pProcessingMonitorButton = PProcessingMonitorBuild(pMonitorShow);
        pProcessingActionBar = PProcessingActionBuild();
        PProcessingStrip = PProcessingStripBuild();
    }

    public UIElement PProcessingHeader { get; }

    public UIElement PProcessingActionBar => pProcessingActionBar;

    public UIElement PProcessingStrip { get; }

    public void PProcessingMonitorSet() => pProcessingMonitorButton.Visibility = Visibility.Visible;

    public void PProcessingActionShow(bool lOrdered) =>
        pProcessingActionBar.Visibility = PLook.PLookVisible[lOrdered];

    public void PProcessingActionApply(bool lSkipActive, double lSkipOpacity)
    {
        pProcessingActionBar.IsEnabled = !lSkipActive;
        pProcessingActionBar.Opacity = lSkipOpacity;
    }

    private static Button PProcessingMonitorBuild(Action pMonitorShow)
    {
        Button pMonitorButton = PProcessingButtonBuild(
            PProcessingMonitorIcon,
            QLocalizationCatalog.QLocalizationTextRead("NormalizePreview.Button.Tooltip"),
            pMonitorShow);
        pMonitorButton.HorizontalAlignment = HorizontalAlignment.Right;
        pMonitorButton.Visibility = Visibility.Collapsed;
        return pMonitorButton;
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
            () => lProcessing.QProcessingMinimizedSet(true));
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
            () => lProcessing.QProcessingStepMove(-1));
        pUpButton.Margin = new Thickness(0, 0, 2, 0);
        Button pDownButton = PProcessingButtonBuild(
            PProcessingDownIcon,
            QLocalizationCatalog.QLocalizationTextRead("Processing.MoveDown.Tooltip"),
            () => lProcessing.QProcessingStepMove(1));

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
            Child = pActionGrid,
            Visibility = Visibility.Collapsed
        };
    }

    private UIElement PProcessingStripBuild()
    {
        Button pMaximizeButton = PProcessingButtonBuild(
            "/PAsset/PPanel/PListMaximize.svg",
            QLocalizationCatalog.QLocalizationTextRead("Processing.Show.Tooltip"),
            () => lProcessing.QProcessingMinimizedSet(false));
        pMaximizeButton.Margin = new Thickness(0, 6, 0, 0);
        pMaximizeButton.HorizontalAlignment = HorizontalAlignment.Center;

        var pStrip = new StackPanel { Background = Brushes.White, Visibility = Visibility.Collapsed };
        pStrip.Children.Add(pMaximizeButton);
        return pStrip;
    }

    private static Button PProcessingButtonBuild(string pIconPath, string pTooltip, Action pClick)
    {
        Button pButton = PProcessingIconBuild(pIconPath, pTooltip);
        PProcessingClickAttach(pButton, pClick);
        QButton.QButtonAttach(pButton, QButtonLook.QButtonLookPanel);
        return pButton;
    }

    private static Button PProcessingIconBuild(string pIconPath, string pTooltip) => new()
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

    private static void PProcessingClickAttach(Button pButton, Action pClick) =>
        pButton.Click += (_, _) => pClick();
}

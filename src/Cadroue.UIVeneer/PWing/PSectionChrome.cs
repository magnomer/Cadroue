using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PWing;

public sealed class PSectionChrome
{
    private static readonly FontFamily pSectionFontFamily = new("Segoe UI");
    private static readonly Brush pSectionLineBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7));
    private static readonly Brush pSectionIconBrush = new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x3D));

    private const double PSectionActionGap = 16;

    private readonly QSection lSection;
    private readonly TextBlock pSectionCountLabel;
    private readonly UIElement pSectionActionBar;

    public PSectionChrome(QSection lSectionOwner)
    {
        lSection = lSectionOwner;
        pSectionCountLabel = PSectionCountBuild();
        PSectionHeader = PSectionHeaderBuild();
        pSectionActionBar = PSectionActionBuild();
        PSectionActionApply(lSection.QSectionEditable);
        PSectionStrip = PSectionStripBuild();
    }

    public UIElement PSectionHeader { get; }

    public UIElement PSectionActionBar => pSectionActionBar;

    public UIElement PSectionStrip { get; }

    private TextBlock PSectionCountBuild() => new()
    {
        Text = lSection.QSectionTitleRead(),
        FontSize = 12,
        FontFamily = pSectionFontFamily,
        FontWeight = FontWeights.SemiBold,
        Foreground = new SolidColorBrush(Color.FromRgb(0x26, 0x36, 0x4A)),
        VerticalAlignment = VerticalAlignment.Center
    };

    public void PSectionActionApply(bool lEditable) => pSectionActionBar.IsEnabled = lEditable;

    public void PSectionTitleApply() => pSectionCountLabel.Text = lSection.QSectionTitleRead();

    private UIElement PSectionHeaderBuild()
    {
        Button pMinimizeButton = PSectionButtonBuild(
            "/PAsset/PPanel/PListMinimize.svg",
            QLocalizationCatalog.QLocalizationTextRead("Section.Panel.HideTooltip"),
            (_, _) => lSection.QSectionMinimizedSet(true));
        pMinimizeButton.Margin = new Thickness(0);
        pMinimizeButton.HorizontalAlignment = HorizontalAlignment.Right;

        var pHeaderGrid = new Grid();
        pHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(pMinimizeButton, 1);
        pHeaderGrid.Children.Add(pSectionCountLabel);
        pHeaderGrid.Children.Add(pMinimizeButton);

        return new Border
        {
            Padding = new Thickness(12, 5, 6, 5),
            BorderBrush = pSectionLineBrush,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Background = Brushes.White,
            Child = pHeaderGrid
        };
    }

    private UIElement PSectionStripBuild()
    {
        Button pMaximizeButton = PSectionButtonBuild(
            "/PAsset/PPanel/PListMaximize.svg",
            QLocalizationCatalog.QLocalizationTextRead("Section.Panel.ShowTooltip"),
            (_, _) => lSection.QSectionMinimizedSet(false));
        pMaximizeButton.Margin = new Thickness(0, 6, 0, 0);
        pMaximizeButton.HorizontalAlignment = HorizontalAlignment.Center;

        var pStrip = new StackPanel { Background = Brushes.White, Visibility = Visibility.Collapsed };
        pStrip.Children.Add(pMaximizeButton);
        return pStrip;
    }

    private UIElement PSectionActionBuild()
    {
        var pActionLeft = new StackPanel { Orientation = Orientation.Horizontal };
        pActionLeft.Children.Add(PSectionButtonBuild(
            "/PAsset/PPanel/PSort.svg",
            QLocalizationCatalog.QLocalizationTextRead("Section.Sort.Tooltip"),
            (_, _) => lSection.QSectionSortRun()));

        Button pSectionRemoveAllButton = PSectionButtonBuild(
            "/PAsset/PPanel/PListRemoveAll.svg",
            QLocalizationCatalog.QLocalizationTextRead("Section.RemoveAll.Tooltip"),
            (_, _) => lSection.QSectionClearRun());
        pSectionRemoveAllButton.Margin = new Thickness(PSectionActionGap, 0, 0, 0);
        var pActionRight = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        pActionRight.Children.Add(PSectionButtonBuild(
            "/PAsset/PPanel/PExportMinus.svg",
            QLocalizationCatalog.QLocalizationTextRead("Section.Delete.Tooltip"),
            (_, _) => lSection.QSectionDeleteRun()));
        pActionRight.Children.Add(pSectionRemoveAllButton);

        var pActionPanel = new Grid { Margin = new Thickness(10, 4, 10, 4) };
        pActionPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pActionPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(pActionRight, 1);
        pActionPanel.Children.Add(pActionLeft);
        pActionPanel.Children.Add(pActionRight);

        return new Border
        {
            BorderBrush = pSectionLineBrush,
            BorderThickness = new Thickness(0, 1, 0, 0),
            Background = Brushes.White,
            Child = pActionPanel
        };
    }

    private static Button PSectionButtonBuild(string pIconPath, string pTooltip, RoutedEventHandler pClick)
    {
        Button pButton = PSectionIconBuild(pIconPath, pTooltip);
        PSectionClickAttach(pButton, pClick);
        QButton.QButtonAttach(pButton, QButtonLook.QButtonLookPanel);
        return pButton;
    }

    private static Button PSectionIconBuild(string pIconPath, string pTooltip) => new()
    {
        Content = new Image
        {
            Width = 20,
            Height = 20,
            Source = QIcon.QIconRead(pIconPath, pSectionIconBrush),
            Stretch = Stretch.Uniform
        },
        ToolTip = pTooltip,
        Width = 28,
        Height = 26,
        Margin = new Thickness(0, 0, 2, 0)
    };

    private static void PSectionClickAttach(Button pButton, RoutedEventHandler pClick) => pButton.Click += pClick;
}

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.Application;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PAsset;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PWing;

public sealed class PSection : UserControl
{
    private static readonly FontFamily pSectionFontFamily = new("Segoe UI");
    private static readonly Brush pSectionLineBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7));
    private static readonly Brush pSectionIconBrush = new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x3D));

    internal const double PSectionNameSize = 12;
    public const double PSectionStripWidth = 48;

    private const double PSectionActionGap = 16;
    private const double PSectionDragOpacity = 0.72;

    private readonly TextBlock pSectionCountLabel;
    private readonly StackPanel pSectionRowPanel;
    private readonly UIElement pSectionActionBar;
    private readonly UIElement pSectionFullBody;
    private readonly UIElement pSectionStripBody;

    public LSection LSection { get; }

    public PSection(LSection lSection)
    {
        LSection = lSection;
        pSectionCountLabel = new TextBlock
        {
            Text = LSection.LSectionTitleRead(),
            FontSize = 12,
            FontFamily = pSectionFontFamily,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(0x26, 0x36, 0x4A)),
            VerticalAlignment = VerticalAlignment.Center
        };

        UIElement pHeader = PSectionHeaderBuild();

        pSectionRowPanel = new StackPanel();
        pSectionRowPanel.PreviewMouseMove += PSectionMoveHandle;
        pSectionRowPanel.MouseLeftButtonUp += PSectionUpHandle;
        pSectionRowPanel.LostMouseCapture += PSectionLostHandle;

        var pScroll = new ScrollViewer
        {
            Content = pSectionRowPanel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };

        var pRoot = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(pHeader, Dock.Top);
        pSectionActionBar = PSectionActionBuild();
        pSectionActionBar.IsEnabled = LSection.LSectionEditable;
        DockPanel.SetDock(pSectionActionBar, Dock.Bottom);
        pRoot.Children.Add(pHeader);
        pRoot.Children.Add(pSectionActionBar);
        pRoot.Children.Add(pScroll);

        pSectionFullBody = pRoot;
        pSectionStripBody = PSectionStripBuild();
        pSectionStripBody.Visibility = Visibility.Collapsed;

        var pBodyHost = new Grid();
        pBodyHost.Children.Add(pSectionFullBody);
        pBodyHost.Children.Add(pSectionStripBody);

        FocusVisualStyle = null;
        PScrollbar.PScrollbarApply(this);
        Content = PPanel.PPanelBorderBuild(pBodyHost);
        PSectionRebuild();
        LSection.LSectionChange += PSectionRebuild;
        LSection.LSectionSelectApply += PSectionSelectApply;
        LSection.LSectionEnabledApply += PSectionEnabledApply;
        LSection.LSectionMinimizeChange += PSectionMinimizeHandle;
        LSection.LSectionDrag.LSectionDragStart += PSectionDragShow;
        LSection.LSectionDrag.LSectionDragEnd += PSectionDragHide;
        LSection.LSectionDrag.LSectionRowMove += PSectionRowMove;
    }

    public bool PSectionMinimizedCheck() => LSection.LSectionMinimized;

    public void PSectionMinimizeSet(bool pSectionMinimizeRequest) =>
        LSection.LSectionMinimizedSet(pSectionMinimizeRequest);

    internal int PSectionIndexRead(PSectionRow pRow) => pSectionRowPanel.Children.IndexOf(pRow.PSectionRowBorder);

    internal void PSectionCaptureRelease() => pSectionRowPanel.ReleaseMouseCapture();

    internal void PSectionCaptureClaim() => pSectionRowPanel.CaptureMouse();

    internal Point PSectionPointRead(MouseEventArgs pEvent) => pEvent.GetPosition(pSectionRowPanel);

    private void PSectionRebuild()
    {
        pSectionRowPanel.Children.Clear();
        pSectionCountLabel.Text = LSection.LSectionTitleRead();
        LSection.LSectionRowsRead()
            .Select(PSectionRowBuild)
            .ToList()
            .ForEach(pRow => pSectionRowPanel.Children.Add(pRow));
    }

    private Border PSectionRowBuild(LSectionRow lRow) => new PSectionRow(this, lRow).PSectionRowBorder;

    private List<PSectionRow> PSectionRowsRead() =>
        pSectionRowPanel.Children.OfType<Border>().Select(PSectionRowRead).ToList();

    private static PSectionRow PSectionRowRead(Border pBorder) => (PSectionRow)pBorder.Tag;

    private PSectionRow PSectionRowRead(int lIndex) => PSectionRowRead((Border)pSectionRowPanel.Children[lIndex]);

    private void PSectionSelectApply() => PSectionRowsRead().ForEach(PSectionSelectedApply);

    private void PSectionSelectedApply(PSectionRow pRow) =>
        pRow.PSectionSelectedSet(LSection.LSectionSelectedCheck(PSectionIndexRead(pRow)));

    private void PSectionEnabledApply(bool lEditable)
    {
        pSectionActionBar.IsEnabled = lEditable;
        pSectionRowPanel.ReleaseMouseCapture();
    }

    private void PSectionMinimizeHandle(bool pSectionMinimized)
    {
        pSectionFullBody.Visibility = PLook.PLookVisible[!pSectionMinimized];
        pSectionStripBody.Visibility = PLook.PLookVisible[pSectionMinimized];
    }

    private void PSectionDragShow(int lIndex)
    {
        Border pRow = PSectionRowRead(lIndex).PSectionRowBorder;
        pRow.Opacity = PSectionDragOpacity;
        PGhost.PGhostShow(pRow, new Point(LSection.LSectionDrag.LSectionGrabX, LSection.LSectionDrag.LSectionGrabY));
    }

    private void PSectionDragHide(int lIndex)
    {
        Border pRow = PSectionRowRead(lIndex).PSectionRowBorder;
        pRow.Opacity = 1;
        PGhost.PGhostClear(pRow);
    }

    private void PSectionRowMove(int lSource, int lInsert)
    {
        UIElement pRow = pSectionRowPanel.Children[lSource];
        pSectionRowPanel.Children.RemoveAt(lSource);
        pSectionRowPanel.Children.Insert(lInsert, pRow);
        PSectionRowsRead().ForEach(PSectionNumberApply);
    }

    private void PSectionNumberApply(PSectionRow pRow) =>
        pRow.PSectionNumberSet(LSection.LSectionNumberRead(PSectionIndexRead(pRow)));

    private Border? PSectionDragRead() =>
        pSectionRowPanel.Children.OfType<Border>().ElementAtOrDefault(LSection.LSectionDrag.LSectionDragRead());

    private void PSectionMoveHandle(object pSender, MouseEventArgs pEvent)
    {
        Point pCurrent = pEvent.GetPosition(pSectionRowPanel);
        bool pHandled = LSection.LSectionDrag.LSectionMoveHandle(
            pCurrent.X,
            pCurrent.Y,
            SystemParameters.MinimumHorizontalDragDistance,
            SystemParameters.MinimumVerticalDragDistance,
            PLook.PLookPressed[pEvent.LeftButton],
            PSectionRowsRead().Select(PSectionTopRead).ToList(),
            PSectionRowsRead().Select(PSectionHeightRead).ToList());
        PGhost.PGhostSync(PSectionDragRead());
        pEvent.Handled = pHandled;
    }

    private double PSectionTopRead(PSectionRow pRow) =>
        pRow.PSectionRowBorder.TransformToAncestor(pSectionRowPanel).Transform(new Point(0, 0)).Y;

    private static double PSectionHeightRead(PSectionRow pRow) => pRow.PSectionRowBorder.ActualHeight;

    private void PSectionUpHandle(object pSender, MouseButtonEventArgs pEvent)
    {
        bool pHandled = LSection.LSectionDrag.LSectionReleaseHandle(
            Keyboard.Modifiers.HasFlag(ModifierKeys.Shift), Keyboard.Modifiers.HasFlag(ModifierKeys.Control));
        pSectionRowPanel.ReleaseMouseCapture();
        pEvent.Handled = pHandled;
    }

    private void PSectionLostHandle(object pSender, MouseEventArgs pEvent) =>
        LSection.LSectionDrag.LSectionLostHandle();

    private UIElement PSectionHeaderBuild()
    {
        Button pMinimizeButton = PSectionButtonBuild(
            "/PAsset/PPanel/PListMinimize.svg",
            LLocalization.LLocalizationTextRead("Section.Panel.HideTooltip"),
            (_, _) => PSectionMinimizeSet(true));
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
            LLocalization.LLocalizationTextRead("Section.Panel.ShowTooltip"),
            (_, _) => PSectionMinimizeSet(false));
        pMaximizeButton.Margin = new Thickness(0, 6, 0, 0);
        pMaximizeButton.HorizontalAlignment = HorizontalAlignment.Center;

        var pStrip = new StackPanel { Background = Brushes.White };
        pStrip.Children.Add(pMaximizeButton);
        return pStrip;
    }

    private UIElement PSectionActionBuild()
    {
        var pActionLeft = new StackPanel { Orientation = Orientation.Horizontal };
        pActionLeft.Children.Add(PSectionButtonBuild(
            "/PAsset/PPanel/PSort.svg",
            LLocalization.LLocalizationTextRead("Section.Sort.Tooltip"),
            (_, _) => LSection.LSectionSortRun()));

        Button pSectionRemoveAllButton = PSectionButtonBuild(
            "/PAsset/PPanel/PListRemoveAll.svg",
            LLocalization.LLocalizationTextRead("Section.RemoveAll.Tooltip"),
            (_, _) => LSection.LSectionClearRun());
        pSectionRemoveAllButton.Margin = new Thickness(PSectionActionGap, 0, 0, 0);
        var pActionRight = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        pActionRight.Children.Add(PSectionButtonBuild(
            "/PAsset/PPanel/PExportMinus.svg",
            LLocalization.LLocalizationTextRead("Section.Delete.Tooltip"),
            (_, _) => LSection.LSectionDeleteRun()));
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
        var pButton = new Button
        {
            Content = new Image
            {
                Width = 14,
                Height = 14,
                Source = PIcon.PIconRead(pIconPath, pSectionIconBrush),
                Stretch = Stretch.Uniform
            },
            ToolTip = pTooltip,
            Width = 28,
            Height = 26,
            Margin = new Thickness(0, 0, 2, 0),
            Style = PButton.PButtonPanelCreate()
        };
        pButton.Click += pClick;
        return pButton;
    }
}

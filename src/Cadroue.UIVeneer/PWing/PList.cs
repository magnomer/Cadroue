using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PHouse;
using Microsoft.Win32;

namespace Cadroue.UIVeneer.PWing;

public sealed class PList : PPanel
{
    private const double PListActionGap = 16;

    private static readonly FontFamily pListFontFamily = new("Segoe UI");
    private static readonly Brush pListLineBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7));
    private static readonly Brush pListMutedBrush = new SolidColorBrush(Color.FromRgb(0x8A, 0x93, 0x9E));
    private static readonly Brush pListIconBrush = new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x3D));
    private static readonly Brush pListTitleBrush = new SolidColorBrush(Color.FromRgb(0x26, 0x36, 0x4A));
    private static readonly Brush pListLockedBrush = new SolidColorBrush(Color.FromRgb(0xF3, 0xF5, 0xF8));

    public const double PListStripWidth = 48;
    public const string PListDragKind = "CadrouePaths";

    private readonly StackPanel pListRowPanel;
    private readonly TextBlock pListEmptyNotice;
    private readonly List<PListRow> pListRows = [];
    private readonly UIElement pListFullBody;
    private readonly UIElement pListStripBody;

    public QList QList { get; }

    public PList(QList lList) : base("")
    {
        QList = lList;
        pListRowPanel = new StackPanel();

        pListEmptyNotice = new TextBlock
        {
            Text = QLocalizationCatalog.QLocalizationTextRead("List.Empty.Notice"),
            FontSize = 12,
            FontFamily = pListFontFamily,
            Foreground = pListMutedBrush,
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(16, 24, 16, 16)
        };

        var pBody = new Grid();
        pBody.Children.Add(pListEmptyNotice);
        pBody.Children.Add(pListRowPanel);

        var pScroll = new ScrollViewer
        {
            Content = pBody,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };

        var pRoot = new DockPanel { LastChildFill = true };
        UIElement pHeader = PListHeaderBuild();
        DockPanel.SetDock(pHeader, Dock.Top);
        UIElement pActionBar = PListActionBuild();
        DockPanel.SetDock(pActionBar, Dock.Bottom);
        pRoot.Children.Add(pHeader);
        pRoot.Children.Add(pActionBar);
        pRoot.Children.Add(pScroll);

        pListFullBody = pRoot;
        pListStripBody = PListStripBuild();
        pListStripBody.Visibility = Visibility.Collapsed;

        var pBodyHost = new Grid();
        pBodyHost.Children.Add(pListFullBody);
        pBodyHost.Children.Add(pListStripBody);

        FocusVisualStyle = null;
        Focusable = true;
        KeyDown += PListKeyHandle;
        Content = PPanelBorderBuild(pBodyHost);
        QList.QListChanged += PListRowsRebuild;
        QList.QListPathChanged += PListSelectionUpdate;
        QList.QListMinimizeChanged += PListMinimizeHandle;
        PListRowsRebuild();
    }

    public void PListClose() => QList.QListClose();

    private void PListRowsRebuild()
    {
        pListRowPanel.Children.Clear();
        pListRows.Clear();
        QList.QListFace.QListCardsRead()
            .Select(PListCardBuild)
            .ToList()
            .ForEach(pCard => pListRowPanel.Children.Add(pCard));
        pListEmptyNotice.Visibility = PLook.PLookVisible[QList.QListEmpty];
    }

    private UIElement PListCardBuild(QListCard lCard) => pListCards[lCard.QListCardLocked](this, lCard);

    private static readonly IReadOnlyDictionary<bool, Func<PList, QListCard, UIElement>> pListCards =
        new Dictionary<bool, Func<PList, QListCard, UIElement>>
        {
            [false] = (pList, lCard) => pList.PListRowBuild(lCard.QListCardRows[0]),
            [true] = (pList, lCard) => pList.PListLockedBuild(lCard),
        };

    private Border PListRowBuild(QListRow lRow)
    {
        var pRow = new PListRow(this, lRow);
        pListRows.Add(pRow);
        return pRow.PListRowBorder;
    }

    private UIElement PListLockedBuild(QListCard lCard)
    {
        var pListCardRows = new StackPanel();
        var pListHeader = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(10, 7, 10, 5)
        };
        pListHeader.Children.Add(new TextBlock
        {
            Text = "",
            FontSize = 11,
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            Foreground = pListMutedBrush,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 6, 0)
        });
        pListHeader.Children.Add(new TextBlock
        {
            Text = lCard.QListCardTitle,
            FontSize = 11,
            FontFamily = pListFontFamily,
            FontWeight = FontWeights.SemiBold,
            Foreground = pListMutedBrush,
            VerticalAlignment = VerticalAlignment.Center
        });
        pListCardRows.Children.Add(pListHeader);
        lCard.QListCardRows.Select(PListRowBuild).ToList().ForEach(pRow => pListCardRows.Children.Add(pRow));

        return new Border
        {
            Margin = new Thickness(6, 6, 6, 0),
            Background = pListLockedBrush,
            BorderBrush = pListLineBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(7),
            Padding = new Thickness(0, 0, 0, 4),
            Child = pListCardRows
        };
    }

    private void PListSelectionUpdate(string? lPath) => pListRows.ForEach(PListRowUpdate);

    private void PListRowUpdate(PListRow pRow) =>
        pRow.PListStateApply(QList.QListFace.QListStateRead(pRow.PListRowPath));

    private void PListMinimizeHandle(bool pListMinimized)
    {
        pListFullBody.Visibility = PLook.PLookVisible[!pListMinimized];
        pListStripBody.Visibility = PLook.PLookVisible[pListMinimized];
    }

    public bool PListMinimizedCheck() => QList.QListMinimized;

    public void PListMinimizeSet(bool pListMinimizeRequest) => QList.QListMinimizedSet(pListMinimizeRequest);

    private void PListKeyHandle(object pKeySender, KeyEventArgs pKeyEvent) =>
        pKeyEvent.Handled = QList.QListKeyRun(
            pKeyEvent.Key.ToString(), Keyboard.Modifiers.HasFlag(ModifierKeys.Control));

    private UIElement PListStripBuild()
    {
        Button pMaximizeButton = PListButtonBuild(
            "/PAsset/PPanel/PListMaximize.svg",
            QLocalizationCatalog.QLocalizationTextRead("List.Show.Tooltip"),
            () => PListMinimizeSet(false));
        pMaximizeButton.Margin = new Thickness(0, 6, 0, 0);
        pMaximizeButton.HorizontalAlignment = HorizontalAlignment.Center;

        var pStrip = new StackPanel { Background = Brushes.White };
        pStrip.Children.Add(pMaximizeButton);
        return pStrip;
    }

    private UIElement PListHeaderBuild()
    {
        var pTitleLabel = new TextBlock
        {
            Text = QLocalizationCatalog.QLocalizationTextRead("List.Header.Files"),
            FontSize = 12,
            FontFamily = pListFontFamily,
            FontWeight = FontWeights.SemiBold,
            Foreground = pListTitleBrush,
            VerticalAlignment = VerticalAlignment.Center
        };

        Button pMinimizeButton = PListButtonBuild(
            "/PAsset/PPanel/PListMinimize.svg",
            QLocalizationCatalog.QLocalizationTextRead("List.Hide.Tooltip"),
            () => PListMinimizeSet(true));
        pMinimizeButton.Margin = new Thickness(0);
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
            BorderBrush = pListLineBrush,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Background = Brushes.White,
            Child = pHeaderGrid
        };
    }

    private UIElement PListActionBuild()
    {
        Button pAddFolderButton = PListButtonBuild(
            "/PAsset/PPanel/PFolder.svg",
            QLocalizationCatalog.QLocalizationTextRead("List.Button.AddFolder"),
            PListFolderOpen);
        pAddFolderButton.Margin = new Thickness(PListActionGap, 0, 2, 0);
        var pLeftPanel = new StackPanel { Orientation = Orientation.Horizontal };
        pLeftPanel.Children.Add(PListButtonBuild(
            "/PAsset/PPanel/PExportPlus.svg",
            QLocalizationCatalog.QLocalizationTextRead("List.Button.AddFiles"),
            PListFilesOpen));
        pLeftPanel.Children.Add(pAddFolderButton);

        Button pRemoveAllButton = PListButtonBuild(
            "/PAsset/PPanel/PListRemoveAll.svg",
            QLocalizationCatalog.QLocalizationTextRead("List.Button.RemoveAll"),
            QList.QListClear);
        pRemoveAllButton.Margin = new Thickness(PListActionGap, 0, 2, 0);
        var pRightPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        pRightPanel.Children.Add(PListButtonBuild(
            "/PAsset/PPanel/PExportMinus.svg",
            QLocalizationCatalog.QLocalizationTextRead("List.Button.RemoveFile"),
            QList.QListRemove));
        pRightPanel.Children.Add(pRemoveAllButton);

        var pActionGrid = new Grid { Margin = new Thickness(10, 4, 10, 6) };
        pActionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pActionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pActionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(pLeftPanel, 0);
        Grid.SetColumn(pRightPanel, 2);
        pActionGrid.Children.Add(pLeftPanel);
        pActionGrid.Children.Add(pRightPanel);

        return new Border
        {
            BorderBrush = pListLineBrush,
            BorderThickness = new Thickness(0, 1, 0, 0),
            Background = Brushes.White,
            Child = pActionGrid
        };
    }

    private static Button PListButtonBuild(string pIconPath, string pTooltip, Action pClick)
    {
        var pButton = new Button
        {
            Content = new Image
            {
                Width = 20,
                Height = 20,
                Source = QIcon.QIconRead(pIconPath, pListIconBrush),
                Stretch = Stretch.Uniform
            },
            ToolTip = pTooltip,
            Width = 28,
            Height = 26,
            Margin = new Thickness(0, 0, 2, 0),
            Style = PButton.PButtonPanelCreate()
        };
        pButton.Click += (_, _) => pClick();
        return pButton;
    }

    private void PListFilesOpen()
    {
        var pDialog = new OpenFileDialog
        {
            Title = QLocalizationCatalog.QLocalizationTextRead("List.Dialog.AddFiles"),
            Multiselect = true,
            Filter = QLocalizationCatalog.QLocalizationTextRead("List.Dialog.MediaFilter")
        };
        QList.QListDialogAdd(pDialog.ShowDialog(), pDialog.FileNames, "file");
    }

    private void PListFolderOpen()
    {
        var pDialog = new OpenFolderDialog
        {
            Title = QLocalizationCatalog.QLocalizationTextRead("List.Dialog.AddFolder"),
            Multiselect = true
        };
        QList.QListDialogAdd(pDialog.ShowDialog(), pDialog.FolderNames, "folder");
    }
}

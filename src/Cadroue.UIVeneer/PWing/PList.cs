using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.Application;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PAsset;
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

    public LList LList { get; }

    public PList(LDocket pListOwner) : base("")
    {
        LList = new LList(pListOwner);
        pListRowPanel = new StackPanel();

        pListEmptyNotice = new TextBlock
        {
            Text = LLocalization.LLocalizationTextRead("List.Empty.Notice"),
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
        LList.LListChange += PListRowsRebuild;
        LList.LListPathChange += PListSelectionUpdate;
        LList.LListMinimizeChange += PListMinimizeHandle;
        PListRowsRebuild();
    }

    public void PListClose() => LList.LListClose();

    private void PListRowsRebuild()
    {
        pListRowPanel.Children.Clear();
        pListRows.Clear();
        LList.LListFace.LListCardsRead()
            .Select(PListCardBuild)
            .ToList()
            .ForEach(pCard => pListRowPanel.Children.Add(pCard));
        pListEmptyNotice.Visibility = PLook.PLookVisible[LList.LListEmpty];
    }

    private UIElement PListCardBuild(LListCard lCard) => pListCards[lCard.LListCardLocked](this, lCard);

    private static readonly IReadOnlyDictionary<bool, Func<PList, LListCard, UIElement>> pListCards =
        new Dictionary<bool, Func<PList, LListCard, UIElement>>
        {
            [false] = (pList, lCard) => pList.PListRowBuild(lCard.LListCardRows[0]),
            [true] = (pList, lCard) => pList.PListLockedBuild(lCard),
        };

    private Border PListRowBuild(LListRow lRow)
    {
        var pRow = new PListRow(this, lRow);
        pListRows.Add(pRow);
        return pRow.PListRowBorder;
    }

    private UIElement PListLockedBuild(LListCard lCard)
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
            Text = lCard.LListCardTitle,
            FontSize = 11,
            FontFamily = pListFontFamily,
            FontWeight = FontWeights.SemiBold,
            Foreground = pListMutedBrush,
            VerticalAlignment = VerticalAlignment.Center
        });
        pListCardRows.Children.Add(pListHeader);
        lCard.LListCardRows.Select(PListRowBuild).ToList().ForEach(pRow => pListCardRows.Children.Add(pRow));

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
        pRow.PListStateApply(LList.LListFace.LListStateRead(pRow.PListRowPath));

    private void PListMinimizeHandle(bool pListMinimized)
    {
        pListFullBody.Visibility = PLook.PLookVisible[!pListMinimized];
        pListStripBody.Visibility = PLook.PLookVisible[pListMinimized];
    }

    public bool PListMinimizedCheck() => LList.LListMinimized;

    public void PListMinimizeSet(bool pListMinimizeRequest) => LList.LListMinimizedSet(pListMinimizeRequest);

    private void PListKeyHandle(object pKeySender, KeyEventArgs pKeyEvent) =>
        pKeyEvent.Handled = LList.LListKeyRun(
            pKeyEvent.Key.ToString(), Keyboard.Modifiers.HasFlag(ModifierKeys.Control));

    private UIElement PListStripBuild()
    {
        Button pMaximizeButton = PListButtonBuild(
            "/PAsset/PPanel/PListMaximize.svg",
            LLocalization.LLocalizationTextRead("List.Show.Tooltip"),
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
            Text = LLocalization.LLocalizationTextRead("List.Header.Files"),
            FontSize = 12,
            FontFamily = pListFontFamily,
            FontWeight = FontWeights.SemiBold,
            Foreground = pListTitleBrush,
            VerticalAlignment = VerticalAlignment.Center
        };

        Button pMinimizeButton = PListButtonBuild(
            "/PAsset/PPanel/PListMinimize.svg",
            LLocalization.LLocalizationTextRead("List.Hide.Tooltip"),
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
            LLocalization.LLocalizationTextRead("List.Button.AddFolder"),
            PListFolderOpen);
        pAddFolderButton.Margin = new Thickness(PListActionGap, 0, 2, 0);
        var pLeftPanel = new StackPanel { Orientation = Orientation.Horizontal };
        pLeftPanel.Children.Add(PListButtonBuild(
            "/PAsset/PPanel/PExportPlus.svg",
            LLocalization.LLocalizationTextRead("List.Button.AddFiles"),
            PListFilesOpen));
        pLeftPanel.Children.Add(pAddFolderButton);

        Button pRemoveAllButton = PListButtonBuild(
            "/PAsset/PPanel/PListRemoveAll.svg",
            LLocalization.LLocalizationTextRead("List.Button.RemoveAll"),
            LList.LListClear);
        pRemoveAllButton.Margin = new Thickness(PListActionGap, 0, 2, 0);
        var pRightPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        pRightPanel.Children.Add(PListButtonBuild(
            "/PAsset/PPanel/PExportMinus.svg",
            LLocalization.LLocalizationTextRead("List.Button.RemoveFile"),
            LList.LListRemove));
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
                Width = 14,
                Height = 14,
                Source = PIcon.PIconRead(pIconPath, pListIconBrush),
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
            Title = LLocalization.LLocalizationTextRead("List.Dialog.AddFiles"),
            Multiselect = true,
            Filter = LLocalization.LLocalizationTextRead("List.Dialog.MediaFilter")
        };
        LList.LListDialogAdd(pDialog.ShowDialog(), pDialog.FileNames, "file");
    }

    private void PListFolderOpen()
    {
        var pDialog = new OpenFolderDialog
        {
            Title = LLocalization.LLocalizationTextRead("List.Dialog.AddFolder"),
            Multiselect = true
        };
        LList.LListDialogAdd(pDialog.ShowDialog(), pDialog.FolderNames, "folder");
    }
}

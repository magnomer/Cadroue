using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PWing;

public sealed class PListChrome
{
    private const double PListActionGap = 16;

    private static readonly FontFamily pListFontFamily = new("Segoe UI");
    private static readonly Brush pListLineBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7));
    private static readonly Brush pListIconBrush = new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x3D));
    private static readonly Brush pListTitleBrush = new SolidColorBrush(Color.FromRgb(0x26, 0x36, 0x4A));

    private readonly QList lList;
    private readonly PListDialog pListDialog;

    public PListChrome(QList lListOwner, PListDialog pDialog)
    {
        lList = lListOwner;
        pListDialog = pDialog;
    }

    public UIElement PListStripBuild()
    {
        Button pMaximizeButton = PListButtonBuild(
            "/PAsset/PPanel/PListMaximize.svg",
            QLocalizationCatalog.QLocalizationTextRead("List.Show.Tooltip"),
            () => lList.QListMinimizedSet(false));
        pMaximizeButton.Margin = new Thickness(0, 6, 0, 0);
        pMaximizeButton.HorizontalAlignment = HorizontalAlignment.Center;

        var pStrip = new StackPanel { Background = Brushes.White, Visibility = Visibility.Collapsed };
        pStrip.Children.Add(pMaximizeButton);
        return pStrip;
    }

    public UIElement PListHeaderBuild()
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
            () => lList.QListMinimizedSet(true));
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

    public UIElement PListActionBuild()
    {
        Button pAddFolderButton = PListButtonBuild(
            "/PAsset/PPanel/PFolder.svg",
            QLocalizationCatalog.QLocalizationTextRead("List.Button.AddFolder"),
            pListDialog.PListFolderOpen);
        pAddFolderButton.Margin = new Thickness(PListActionGap, 0, 2, 0);
        var pLeftPanel = new StackPanel { Orientation = Orientation.Horizontal };
        pLeftPanel.Children.Add(PListButtonBuild(
            "/PAsset/PPanel/PExportPlus.svg",
            QLocalizationCatalog.QLocalizationTextRead("List.Button.AddFiles"),
            pListDialog.PListFilesOpen));
        pLeftPanel.Children.Add(pAddFolderButton);

        Button pRemoveAllButton = PListButtonBuild(
            "/PAsset/PPanel/PListRemoveAll.svg",
            QLocalizationCatalog.QLocalizationTextRead("List.Button.RemoveAll"),
            lList.QListClear);
        pRemoveAllButton.Margin = new Thickness(PListActionGap, 0, 2, 0);
        var pRightPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        pRightPanel.Children.Add(PListButtonBuild(
            "/PAsset/PPanel/PExportMinus.svg",
            QLocalizationCatalog.QLocalizationTextRead("List.Button.RemoveFile"),
            lList.QListRemove));
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
        Button pButton = PListIconBuild(pIconPath, pTooltip);
        PListClickAttach(pButton, pClick);
        QButton.QButtonAttach(pButton, QButtonLook.QButtonLookPanel);
        return pButton;
    }

    private static Button PListIconBuild(string pIconPath, string pTooltip) => new()
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
        Margin = new Thickness(0, 0, 2, 0)
    };

    private static void PListClickAttach(Button pButton, Action pClick) => pButton.Click += (_, _) => pClick();
}

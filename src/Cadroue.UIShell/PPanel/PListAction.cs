using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIShell.PAsset;
using Cadroue.UIShell.PHouse;
using Microsoft.Win32;

using Cadroue.Infrastructure;

namespace Cadroue.UIShell.PPanel;

public sealed partial class PList
{
    private const double PListActionGap = 16;

    private static readonly Brush pListIconBrush = new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x3D));
    private static readonly Brush pListTitleBrush = new SolidColorBrush(Color.FromRgb(0x26, 0x36, 0x4A));

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
            PListClear);
        pRemoveAllButton.Margin = new Thickness(PListActionGap, 0, 2, 0);
        var pRightPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        pRightPanel.Children.Add(PListButtonBuild(
            "/PAsset/PPanel/PExportMinus.svg",
            LLocalization.LLocalizationTextRead("List.Button.RemoveFile"),
            PListRemove));
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

        if (pDialog.ShowDialog() == true)
        {
            LTraceLog.LTraceInfoRecord($"List manual file dialog confirmed: {pDialog.FileNames.Length} file(s)");
            _ = PListPathsAdd(pDialog.FileNames);
        }
    }

    private void PListFolderOpen()
    {
        var pDialog = new OpenFolderDialog
        {
            Title = LLocalization.LLocalizationTextRead("List.Dialog.AddFolder"),
            Multiselect = true
        };
        if (pDialog.ShowDialog() == true)
        {
            LTraceLog.LTraceInfoRecord($"List manual folder dialog confirmed: {pDialog.FolderNames.Length} folder(s)");
            _ = PListPathsAdd(pDialog.FolderNames);
        }
    }
}

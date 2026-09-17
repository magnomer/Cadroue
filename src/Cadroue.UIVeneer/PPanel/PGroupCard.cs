using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.Application;

namespace Cadroue.UIVeneer.PPanel;

public sealed partial class PGroup
{
    private void PGroupRebuild()
    {
        pGroupRowPanel.Children.Clear();
        for (int pIndex = 0; pIndex < pGroupRecords.Count; pIndex++)
        {
            pGroupRowPanel.Children.Add(PGroupCardBuild(pIndex, pGroupRecords[pIndex]));
        }

        pGroupEmptyNotice.Visibility = pGroupRecords.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private Border PGroupCardBuild(int pGroupIndex, PGroupRecord pRecord)
    {
        var pFileRows = new StackPanel();
        for (int pOrderIndex = 0; pOrderIndex < pRecord.PGroupRecordPaths.Count; pOrderIndex++)
        {
            pFileRows.Children.Add(PGroupFileBuild(pGroupIndex, pOrderIndex, pRecord.PGroupRecordPaths[pOrderIndex]));
        }

        var pCardBody = new StackPanel();
        pCardBody.Children.Add(PGroupCrestBuild(pGroupIndex, pRecord));
        pCardBody.Children.Add(pFileRows);

        var pCard = new Border
        {
            Margin = new Thickness(8, 8, 8, 0),
            Padding = new Thickness(0, 0, 0, 6),
            CornerRadius = new CornerRadius(8),
            BorderBrush = pGroupLineBrush,
            BorderThickness = new Thickness(1),
            Background = pGroupCardBrush,
            AllowDrop = true,
            Child = pCardBody,
            Tag = pFileRows
        };
        pCard.DragOver += PGroupOverHandle;
        pCard.Drop += (pSender, pEvent) => PGroupCardHandle(pGroupIndex, pFileRows, pEvent);
        return pCard;
    }

    private UIElement PGroupCrestBuild(int pGroupIndex, PGroupRecord pRecord)
    {
        var pHeaderGrid = new Grid { Margin = new Thickness(10, 4, 4, 4) };
        pHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var pNameLabel = new TextBlock
        {
            Text = pRecord.PGroupRecordName,
            FontSize = 12,
            FontFamily = pGroupFontFamily,
            FontWeight = FontWeights.SemiBold,
            Foreground = pGroupTitleBrush,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
            ToolTip = LLocalization.LLocalizationTextRead("Group.Rename.Tooltip")
        };
        pNameLabel.MouseLeftButtonDown += (_, pNameEvent) =>
        {
            if (pNameEvent.ClickCount == 2)
            {
                PGroupEditStart(pGroupIndex, pHeaderGrid, pRecord);
                pNameEvent.Handled = true;
            }
        };

        Button pRemoveButton = PGroupButtonBuild(
            "/PAsset/PPanel/PExportMinus.svg",
            LLocalization.LLocalizationTextRead("Group.Remove.Tooltip"),
            (_, _) => PGroupRemove(pGroupIndex));
        pRemoveButton.HorizontalAlignment = HorizontalAlignment.Right;

        Grid.SetColumn(pRemoveButton, 1);
        pHeaderGrid.Children.Add(pNameLabel);
        pHeaderGrid.Children.Add(pRemoveButton);
        return pHeaderGrid;
    }


    private Border PGroupFileBuild(int pGroupIndex, int pOrderIndex, string pPath)
    {
        var pRowContent = new StackPanel { Orientation = Orientation.Horizontal };
        pRowContent.Children.Add(new TextBlock
        {
            Text = (pOrderIndex + 1).ToString(),
            FontSize = 12,
            FontFamily = pGroupFontFamily,
            FontWeight = FontWeights.SemiBold,
            Foreground = pGroupMutedBrush,
            Width = 18,
            TextAlignment = TextAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        });
        pRowContent.Children.Add(new TextBlock
        {
            Text = Path.GetFileName(pPath),
            FontSize = 12,
            FontFamily = pGroupFontFamily,
            Foreground = pGroupRowBrush,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        });

        Button pItemRemoveButton = PGroupButtonBuild(
            "/PAsset/PPanel/PExportMinus.svg",
            LLocalization.LLocalizationTextRead("Group.Item.RemoveTooltip"),
            (_, _) => PGroupItemRemove(pGroupIndex, pPath));
        pItemRemoveButton.Width = 22;
        pItemRemoveButton.Height = 20;
        pItemRemoveButton.Margin = new Thickness(6, 0, 0, 0);
        pItemRemoveButton.VerticalAlignment = VerticalAlignment.Center;

        var pRowGrid = new Grid();
        pRowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pRowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(pItemRemoveButton, 1);
        pRowGrid.Children.Add(pRowContent);
        pRowGrid.Children.Add(pItemRemoveButton);

        var pRowBorder = new Border
        {
            Padding = new Thickness(20, 4, 8, 4),
            Background = Brushes.Transparent,
            Cursor = Cursors.Hand,
            ToolTip = pPath,
            Child = pRowGrid,
            Tag = pPath
        };
        pRowBorder.MouseLeftButtonDown += (_, pRowEvent) =>
        {
            pGroupDragOrigin = pRowEvent.GetPosition(null);
            pGroupDragOffset = pRowEvent.GetPosition(pRowBorder);
            pGroupSourceIndex = pGroupIndex;
            pGroupDragPath = pPath;
            pRowBorder.CaptureMouse();
            PGroupItemOpen?.Invoke(pPath);
        };
        pRowBorder.MouseMove += (pRowSender, pRowEvent) => PGroupDragHandle(pRowSender, pRowEvent);
        pRowBorder.MouseLeftButtonUp += (_, _) =>
        {
            pRowBorder.ReleaseMouseCapture();
            PGroupDragClear();
        };
        return pRowBorder;
    }

    private void PGroupItemRemove(int pGroupIndex, string pPath)
    {
        if (pGroupIndex < 0 || pGroupIndex >= pGroupRecords.Count)
        {
            return;
        }

        List<string> pGroupPaths = pGroupRecords[pGroupIndex].PGroupRecordPaths;
        if (pGroupPaths.RemoveAll(
            pExisting => string.Equals(pExisting, pPath, StringComparison.OrdinalIgnoreCase)) == 0)
        {
            return;
        }

        PGroupDragClear();
        PGroupRebuild();
    }

    private void PGroupRemove(int pGroupIndex)
    {
        if (pGroupIndex < 0 || pGroupIndex >= pGroupRecords.Count)
        {
            return;
        }

        pGroupRecords.RemoveAt(pGroupIndex);
        PGroupRebuild();
    }
}

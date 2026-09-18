using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.Application;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PPanel;

public sealed partial class PGroup
{
    private void PGroupRebuild()
    {
        IReadOnlyList<LGroupRecord> pRecords = LGroup.LGroupRecords;
        pGroupRowPanel.Children.Clear();
        for (int pIndex = 0; pIndex < pRecords.Count; pIndex++)
        {
            pGroupRowPanel.Children.Add(PGroupCardBuild(pIndex, pRecords[pIndex]));
        }

        pGroupEmptyNotice.Visibility = pRecords.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private Border PGroupCardBuild(int pGroupIndex, LGroupRecord pRecord)
    {
        var pFileRows = new StackPanel();
        for (int pOrderIndex = 0; pOrderIndex < pRecord.LGroupRecordPaths.Count; pOrderIndex++)
        {
            pFileRows.Children.Add(PGroupFileBuild(pGroupIndex, pOrderIndex, pRecord.LGroupRecordPaths[pOrderIndex]));
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

    private UIElement PGroupCrestBuild(int pGroupIndex, LGroupRecord pRecord)
    {
        var pHeaderGrid = new Grid { Margin = new Thickness(10, 4, 4, 4) };
        pHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        UIElement pNameElement = LGroup.LGroupEditingIndex == pGroupIndex
            ? PGroupEditBuild(pRecord)
            : PGroupLabelBuild(pGroupIndex, pRecord);

        Button pRemoveButton = PGroupButtonBuild(
            "/PAsset/PPanel/PExportMinus.svg",
            LLocalization.LLocalizationTextRead("Group.Remove.Tooltip"),
            (_, _) => LGroup.LGroupRemove(pGroupIndex));
        pRemoveButton.HorizontalAlignment = HorizontalAlignment.Right;

        Grid.SetColumn(pRemoveButton, 1);
        pHeaderGrid.Children.Add(pNameElement);
        pHeaderGrid.Children.Add(pRemoveButton);
        return pHeaderGrid;
    }

    private TextBlock PGroupLabelBuild(int pGroupIndex, LGroupRecord pRecord)
    {
        var pNameLabel = new TextBlock
        {
            Text = pRecord.LGroupRecordName,
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
                LGroup.LGroupEditStart(pGroupIndex);
                pNameEvent.Handled = true;
            }
        };
        return pNameLabel;
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
            Text = System.IO.Path.GetFileName(pPath),
            FontSize = 12,
            FontFamily = pGroupFontFamily,
            Foreground = pGroupRowBrush,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        });

        Button pItemRemoveButton = PGroupButtonBuild(
            "/PAsset/PPanel/PExportMinus.svg",
            LLocalization.LLocalizationTextRead("Group.Item.RemoveTooltip"),
            (_, _) => LGroup.LGroupItemRemove(pGroupIndex, pPath));
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
            LGroup.LGroupDragSet(pGroupIndex, pPath);
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

}

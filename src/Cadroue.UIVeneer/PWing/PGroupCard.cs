using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PWing;

public sealed class PGroupCard
{
    private static readonly FontFamily pGroupFontFamily = new("Segoe UI");
    private static readonly Brush pGroupLineBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7));
    private static readonly Brush pGroupRowBrush = new SolidColorBrush(Color.FromRgb(0x11, 0x18, 0x27));
    private static readonly Brush pGroupMutedBrush = new SolidColorBrush(Color.FromRgb(0x8A, 0x93, 0x9E));
    private static readonly Brush pGroupCardBrush = new SolidColorBrush(Color.FromRgb(0xF6, 0xF8, 0xFB));

    private readonly QGroup lGroup;
    private readonly PGroupCardName pGroupName;
    private readonly PGroupCardDrag pGroupDrag;
    private readonly StackPanel pGroupFileRows = new();
    private readonly List<Border> pGroupRowBorders = [];

    public PGroupCard(QGroup lGroupOwner, QGroupCard lCard)
    {
        lGroup = lGroupOwner;
        pGroupName = new PGroupCardName(lGroupOwner.QGroupName);
        pGroupDrag = new PGroupCardDrag(lGroupOwner.QGroupDrag);
        lCard.QGroupCardFiles.ToList().ForEach(PGroupFileAdd);
        PGroupCardBorder = PGroupBorderBuild(lCard);
        PGroupBorderAttach(lCard);
    }

    public Border PGroupCardBorder { get; }

    private Border PGroupBorderBuild(QGroupCard lCard)
    {
        var pCardBody = new StackPanel();
        pCardBody.Children.Add(PGroupCrestBuild(lCard));
        pCardBody.Children.Add(pGroupFileRows);

        return new Border
        {
            Margin = new Thickness(8, 8, 8, 0),
            Padding = new Thickness(0, 0, 0, 6),
            CornerRadius = new CornerRadius(8),
            BorderBrush = pGroupLineBrush,
            BorderThickness = new Thickness(1),
            Background = pGroupCardBrush,
            AllowDrop = true,
            Child = pCardBody,
            Tag = pGroupFileRows
        };
    }

    private void PGroupBorderAttach(QGroupCard lCard)
    {
        PGroupCardBorder.DragOver += PGroupDrop.PGroupOverHandle;
        PGroupCardBorder.Drop += (_, pEvent) => PGroupDropHandle(lCard.QGroupCardIndex, pEvent);
    }

    private void PGroupDropHandle(int pGroupIndex, DragEventArgs pEvent)
    {
        var pMove = pEvent.Data.GetData(PGroupDrop.PGroupMoveKind) as PGroupDrop.PGroupMovePayload;
        pEvent.Handled = lGroup.QGroupDrag.QGroupCardAccept(
            pGroupIndex,
            QGroupDrag.QGroupInsertResolve(
                pEvent.GetPosition(pGroupFileRows).Y,
                pGroupRowBorders.Select(PGroupTopRead).ToList(),
                pGroupRowBorders.Select(PGroupHeightRead).ToList()),
            pEvent.Data.GetData(DataFormats.FileDrop) as string[],
            pMove?.PGroupMoveIndex,
            pMove?.PGroupMovePath,
            pEvent.Data.GetData(PList.PListDragKind) as string[]);
    }

    private double PGroupTopRead(Border pRow) => pRow.TranslatePoint(new Point(0, 0), pGroupFileRows).Y;

    private static double PGroupHeightRead(Border pRow) => pRow.ActualHeight;

    private UIElement PGroupCrestBuild(QGroupCard lCard)
    {
        var pHeaderGrid = new Grid { Margin = new Thickness(10, 4, 4, 4) };
        pHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        UIElement pNameElement = pGroupName.PGroupNameBuild(lCard);

        Button pRemoveButton = PGroupButton.PGroupButtonBuild(
            "/PAsset/PPanel/PExportMinus.svg",
            QLocalizationCatalog.QLocalizationTextRead("Group.Remove.Tooltip"),
            (_, _) => lGroup.QGroupRemove(lCard.QGroupCardIndex));
        pRemoveButton.HorizontalAlignment = HorizontalAlignment.Right;

        Grid.SetColumn(pRemoveButton, 1);
        pHeaderGrid.Children.Add(pNameElement);
        pHeaderGrid.Children.Add(pRemoveButton);
        return pHeaderGrid;
    }

    private void PGroupFileAdd(QGroupFile lFile) => pGroupFileRows.Children.Add(PGroupFileBuild(lFile));

    private Border PGroupFileBuild(QGroupFile lFile)
    {
        Border pRowBorder = PGroupRowBuild(lFile);
        pGroupDrag.PGroupRowAttach(lFile, pRowBorder);
        PGroupRowAdd(pRowBorder);
        return pRowBorder;
    }

    private Border PGroupRowBuild(QGroupFile lFile)
    {
        var pRowContent = new StackPanel { Orientation = Orientation.Horizontal };
        pRowContent.Children.Add(new TextBlock
        {
            Text = lFile.QGroupFileNumber,
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
            Text = lFile.QGroupFileName,
            FontSize = 12,
            FontFamily = pGroupFontFamily,
            Foreground = pGroupRowBrush,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        });

        Button pItemRemoveButton = PGroupButton.PGroupButtonBuild(
            "/PAsset/PPanel/PExportMinus.svg",
            QLocalizationCatalog.QLocalizationTextRead("Group.Item.RemoveTooltip"),
            (_, _) => lGroup.QGroupItemRemove(lFile.QGroupFileGroup, lFile.QGroupFilePath));
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

        return new Border
        {
            Padding = new Thickness(20, 4, 8, 4),
            Background = Brushes.Transparent,
            Cursor = Cursors.Hand,
            ToolTip = lFile.QGroupFilePath,
            Child = pRowGrid,
            Tag = lFile.QGroupFilePath
        };
    }

    private void PGroupRowAdd(Border pRowBorder) => pGroupRowBorders.Add(pRowBorder);
}

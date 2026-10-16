using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PWing;

public sealed class PGroupCard
{
    private static readonly FontFamily pGroupFontFamily = new("Segoe UI");
    private static readonly Brush pGroupLineBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7));
    private static readonly Brush pGroupTitleBrush = new SolidColorBrush(Color.FromRgb(0x26, 0x36, 0x4A));
    private static readonly Brush pGroupRowBrush = new SolidColorBrush(Color.FromRgb(0x11, 0x18, 0x27));
    private static readonly Brush pGroupMutedBrush = new SolidColorBrush(Color.FromRgb(0x8A, 0x93, 0x9E));
    private static readonly Brush pGroupCardBrush = new SolidColorBrush(Color.FromRgb(0xF6, 0xF8, 0xFB));

    private static readonly IReadOnlyDictionary<bool, Func<PGroupCard, QGroupCard, UIElement>> pGroupNames =
        new Dictionary<bool, Func<PGroupCard, QGroupCard, UIElement>>
        {
            [true] = (pCard, lCard) => pCard.PGroupEditBuild(lCard),
            [false] = (pCard, lCard) => pCard.PGroupLabelBuild(lCard),
        };

    private static readonly IReadOnlyDictionary<bool, Action<PGroupCard, Border>> pGroupDrags =
        new Dictionary<bool, Action<PGroupCard, Border>>
        {
            [true] = (pCard, pRow) => pCard.PGroupDragRun(pRow),
            [false] = (pCard, pRow) => { },
        };

    private readonly QGroup lGroup;
    private readonly StackPanel pGroupFileRows = new();
    private readonly List<Border> pGroupRowBorders = [];

    public PGroupCard(PGroup pPanel, QGroupCard lCard)
    {
        lGroup = pPanel.QGroup;
        lCard.QGroupCardFiles.ToList().ForEach(PGroupFileAdd);

        var pCardBody = new StackPanel();
        pCardBody.Children.Add(PGroupCrestBuild(lCard));
        pCardBody.Children.Add(pGroupFileRows);

        PGroupCardBorder = new Border
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
        PGroupCardBorder.DragOver += PGroup.PGroupOverHandle;
        PGroupCardBorder.Drop += (_, pEvent) => PGroupDropHandle(lCard.QGroupCardIndex, pEvent);
    }

    public Border PGroupCardBorder { get; }

    private void PGroupDropHandle(int pGroupIndex, DragEventArgs pEvent)
    {
        var pMove = pEvent.Data.GetData(PGroup.PGroupMoveKind) as PGroup.PGroupMovePayload;
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

        UIElement pNameElement = pGroupNames[lCard.QGroupCardEditing](this, lCard);

        Button pRemoveButton = PGroup.PGroupButtonBuild(
            "/PAsset/PPanel/PExportMinus.svg",
            QLocalizationCatalog.QLocalizationTextRead("Group.Remove.Tooltip"),
            (_, _) => lGroup.QGroupRemove(lCard.QGroupCardIndex));
        pRemoveButton.HorizontalAlignment = HorizontalAlignment.Right;

        Grid.SetColumn(pRemoveButton, 1);
        pHeaderGrid.Children.Add(pNameElement);
        pHeaderGrid.Children.Add(pRemoveButton);
        return pHeaderGrid;
    }

    private TextBlock PGroupLabelBuild(QGroupCard lCard)
    {
        var pNameLabel = new TextBlock
        {
            Text = lCard.QGroupCardName,
            FontSize = 12,
            FontFamily = pGroupFontFamily,
            FontWeight = FontWeights.SemiBold,
            Foreground = pGroupTitleBrush,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
            ToolTip = QLocalizationCatalog.QLocalizationTextRead("Group.Rename.Tooltip")
        };
        pNameLabel.MouseLeftButtonDown += (_, pNameEvent) =>
            pNameEvent.Handled = lGroup.QGroupName.QGroupLabelHandle(lCard.QGroupCardIndex, pNameEvent.ClickCount);
        return pNameLabel;
    }

    private TextBox PGroupEditBuild(QGroupCard lCard)
    {
        var pNameBox = new TextBox
        {
            Text = lCard.QGroupCardName,
            FontSize = 12,
            FontFamily = pGroupFontFamily,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            Padding = new Thickness(2, 0, 2, 0),
            Margin = new Thickness(0, 0, 6, 0)
        };
        pNameBox.Loaded += (_, _) =>
        {
            pNameBox.Focus();
            pNameBox.SelectAll();
        };
        pNameBox.KeyDown += (_, pKeyEvent) =>
            pKeyEvent.Handled = lGroup.QGroupName.QGroupKeyRun(pKeyEvent.Key.ToString(), pNameBox.Text);
        pNameBox.LostKeyboardFocus += (_, _) => lGroup.QGroupName.QGroupNameCommit(pNameBox.Text);
        return pNameBox;
    }

    private void PGroupFileAdd(QGroupFile lFile) => pGroupFileRows.Children.Add(PGroupFileBuild(lFile));

    private Border PGroupFileBuild(QGroupFile lFile)
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

        Button pItemRemoveButton = PGroup.PGroupButtonBuild(
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

        var pRowBorder = new Border
        {
            Padding = new Thickness(20, 4, 8, 4),
            Background = Brushes.Transparent,
            Cursor = Cursors.Hand,
            ToolTip = lFile.QGroupFilePath,
            Child = pRowGrid,
            Tag = lFile.QGroupFilePath
        };
        pRowBorder.MouseLeftButtonDown += (_, pRowEvent) => PGroupPressHandle(lFile, pRowBorder, pRowEvent);
        pRowBorder.MouseMove += (_, pRowEvent) => PGroupMoveHandle(pRowBorder, pRowEvent);
        pRowBorder.MouseLeftButtonUp += (_, _) =>
        {
            pRowBorder.ReleaseMouseCapture();
            lGroup.QGroupDrag.QGroupDragClear();
        };
        pGroupRowBorders.Add(pRowBorder);
        return pRowBorder;
    }

    private void PGroupPressHandle(QGroupFile lFile, Border pRowBorder, MouseButtonEventArgs pRowEvent)
    {
        Point pOrigin = pRowEvent.GetPosition(null);
        Point pGrab = pRowEvent.GetPosition(pRowBorder);
        lGroup.QGroupDrag.QGroupPressHandle(
            lFile.QGroupFileGroup, lFile.QGroupFilePath, pOrigin.X, pOrigin.Y, pGrab.X, pGrab.Y);
        pRowBorder.CaptureMouse();
    }

    private void PGroupMoveHandle(Border pRowBorder, MouseEventArgs pRowEvent)
    {
        Point pCurrent = pRowEvent.GetPosition(null);
        bool pStart = lGroup.QGroupDrag.QGroupDragResolve(
            pCurrent.X,
            pCurrent.Y,
            SystemParameters.MinimumHorizontalDragDistance,
            SystemParameters.MinimumVerticalDragDistance,
            PLook.PLookPressed[pRowEvent.LeftButton]);
        pGroupDrags[pStart](this, pRowBorder);
    }

    private void PGroupDragRun(Border pRowBorder)
    {
        var pData = new DataObject(
            PGroup.PGroupMoveKind,
            new PGroup.PGroupMovePayload(lGroup.QGroupDrag.QGroupDragIndex, lGroup.QGroupDrag.QGroupDragPath));
        QGhost lGhost = lGroup.QGroupDrag.QGroupGhost;
        lGroup.QGroupDrag.QGroupDragClear();
        pRowBorder.ReleaseMouseCapture();
        QGhostLayer.QGhostLayerRun(
            pRowBorder,
            lGhost,
            () => DragDrop.DoDragDrop(pRowBorder, pData, DragDropEffects.Move));
    }
}

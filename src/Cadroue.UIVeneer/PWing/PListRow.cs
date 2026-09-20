using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PAsset;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PWing;

public sealed class PListRow
{
    private static readonly FontFamily pListFontFamily = new("Segoe UI");
    private static readonly Brush pListLineBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7));
    private static readonly Brush pListSelectBrush = new SolidColorBrush(Color.FromRgb(0xEE, 0xF4, 0xFB));
    private static readonly Brush pListRowBrush = new SolidColorBrush(Color.FromRgb(0x11, 0x18, 0x27));
    private static readonly Brush pListMutedBrush = new SolidColorBrush(Color.FromRgb(0x8A, 0x93, 0x9E));
    private static readonly Brush pListIconBrush = new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x3D));
    private static readonly Brush pListLockedAccent = new SolidColorBrush(Color.FromRgb(0xE4, 0xEB, 0xF3));

    private static readonly IReadOnlyDictionary<string, Brush> pListBackgrounds = new Dictionary<string, Brush>
    {
        ["Plain"] = Brushes.White,
        ["Selected"] = pListSelectBrush,
        ["Locked"] = Brushes.Transparent,
        ["LockedSelected"] = pListLockedAccent,
    };

    private static readonly IReadOnlyDictionary<bool, Brush> pListForegrounds = new Dictionary<bool, Brush>
    {
        [true] = pListMutedBrush,
        [false] = pListRowBrush,
    };

    private static readonly IReadOnlyDictionary<bool, Brush> pListIcons = new Dictionary<bool, Brush>
    {
        [true] = pListMutedBrush,
        [false] = pListIconBrush,
    };

    private static readonly IReadOnlyDictionary<bool, Thickness> pListBorders = new Dictionary<bool, Thickness>
    {
        [true] = new Thickness(0, 0, 0, 1),
        [false] = new Thickness(0),
    };

    private static readonly IReadOnlyDictionary<bool, Func<Border, IInputElement?>> pListCaptures =
        new Dictionary<bool, Func<Border, IInputElement?>>
        {
            [true] = pBorder => pBorder,
            [false] = pBorder => null,
        };

    private static readonly IReadOnlyDictionary<bool, Action<PListRow>> pListDrags =
        new Dictionary<bool, Action<PListRow>>
        {
            [true] = pRow => pRow.PListDragRun(),
            [false] = pRow => { },
        };

    private readonly PList pListPanel;
    private readonly LList lList;

    public PListRow(PList pPanel, LListRow lRow)
    {
        pListPanel = pPanel;
        lList = pPanel.LList;
        PListRowPath = lRow.LListRowPath;
        var pRowContent = new StackPanel { Orientation = Orientation.Horizontal };
        pRowContent.Children.Add(new Image
        {
            Width = 14,
            Height = 14,
            Source = PIcon.PIconRead("/PAsset/PPanel/PVideo.svg", pListIcons[lRow.LListRowLocked]),
            Stretch = Stretch.Uniform,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        });
        pRowContent.Children.Add(new TextBlock
        {
            Text = lRow.LListRowName,
            FontSize = 12,
            FontFamily = pListFontFamily,
            Foreground = pListForegrounds[lRow.LListRowLocked],
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        });

        PListRowBorder = new Border
        {
            Padding = new Thickness(12, 7, 12, 7),
            Background = pListBackgrounds[lRow.LListRowState],
            BorderBrush = pListLineBrush,
            BorderThickness = pListBorders[lRow.LListRowLast],
            Cursor = Cursors.Hand,
            ToolTip = lRow.LListRowTip,
            Child = pRowContent,
            Tag = lRow.LListRowPath
        };
        PListRowBorder.MouseLeftButtonDown += PListPressHandle;
        PListRowBorder.MouseMove += PListMoveHandle;
        PListRowBorder.MouseLeftButtonUp += PListReleaseHandle;
    }

    public string PListRowPath { get; }

    public Border PListRowBorder { get; }

    public void PListStateApply(string lState) => PListRowBorder.Background = pListBackgrounds[lState];

    private void PListPressHandle(object pSender, MouseButtonEventArgs pEvent)
    {
        pListPanel.Focus();
        Point pOrigin = pEvent.GetPosition(null);
        Point pGrab = pEvent.GetPosition(PListRowBorder);
        bool pCapture = lList.LListDrag.LListPressHandle(
            PListRowPath,
            Keyboard.Modifiers.HasFlag(ModifierKeys.Shift),
            Keyboard.Modifiers.HasFlag(ModifierKeys.Control),
            pOrigin.X,
            pOrigin.Y,
            pGrab.X,
            pGrab.Y);
        Mouse.Capture(pListCaptures[pCapture](PListRowBorder));
        pEvent.Handled = true;
    }

    private void PListMoveHandle(object pSender, MouseEventArgs pEvent)
    {
        Point pCurrent = pEvent.GetPosition(null);
        bool pStart = lList.LListDrag.LListDragResolve(
            pCurrent.X,
            pCurrent.Y,
            SystemParameters.MinimumHorizontalDragDistance,
            SystemParameters.MinimumVerticalDragDistance,
            PLook.PLookPressed[pEvent.LeftButton]);
        pListDrags[pStart](this);
    }

    private void PListDragRun()
    {
        var pDragData = new DataObject(PList.PListDragKind, lList.LListDrag.LListDragPaths.ToArray());
        var pGrabOffset = new Point(lList.LListDrag.LListGrabX, lList.LListDrag.LListGrabY);
        PListRowBorder.ReleaseMouseCapture();
        PGhost.PGhostDragRun(
            PListRowBorder,
            pGrabOffset,
            () => DragDrop.DoDragDrop(PListRowBorder, pDragData, DragDropEffects.Copy));
    }

    private void PListReleaseHandle(object pSender, MouseButtonEventArgs pEvent)
    {
        PListRowBorder.ReleaseMouseCapture();
        lList.LListDrag.LListReleaseHandle();
    }
}

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

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

    public PListRow(UIElement pFocus, QList lList, QListRow lRow)
    {
        PListRowPath = lRow.QListRowPath;
        PListRowBorder = PListBorderBuild(lRow);
        new PListRowDrag(pFocus, lList, PListRowPath, PListRowBorder).PListRowAttach();
    }

    private static Border PListBorderBuild(QListRow lRow)
    {
        var pRowContent = new StackPanel { Orientation = Orientation.Horizontal };
        pRowContent.Children.Add(new Image
        {
            Width = 18,
            Height = 18,
            Source = QIcon.QIconRead("/PAsset/PPanel/PVideo.svg", pListIcons[lRow.QListRowLocked]),
            Stretch = Stretch.Uniform,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 4, 0)
        });
        pRowContent.Children.Add(new TextBlock
        {
            Text = lRow.QListRowName,
            FontSize = 12,
            FontFamily = pListFontFamily,
            Foreground = pListForegrounds[lRow.QListRowLocked],
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        });

        return new Border
        {
            Padding = new Thickness(12, 5, 12, 5),
            Background = pListBackgrounds[lRow.QListRowState],
            BorderBrush = pListLineBrush,
            BorderThickness = pListBorders[lRow.QListRowLast],
            Cursor = Cursors.Hand,
            ToolTip = lRow.QListRowTip,
            Child = pRowContent,
            Tag = lRow.QListRowPath
        };
    }

    public string PListRowPath { get; }

    public Border PListRowBorder { get; }

    public void PListStateApply(string lState) => PListRowBorder.Background = pListBackgrounds[lState];
}

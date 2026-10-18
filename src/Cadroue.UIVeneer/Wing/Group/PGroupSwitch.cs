using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal static class PGroupSwitch
{
    private static readonly FontFamily pGroupFontFamily = new("Segoe UI");
    private static readonly Brush pGroupLineBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7));
    private static readonly Brush pGroupTitleBrush = new SolidColorBrush(Color.FromRgb(0x26, 0x36, 0x4A));
    private static readonly Brush pGroupMutedBrush = new SolidColorBrush(Color.FromRgb(0x8A, 0x93, 0x9E));
    private static readonly Brush pGroupActiveBrush = new SolidColorBrush(Color.FromRgb(0xCE, 0xE1, 0xFB));

    private static readonly IReadOnlyDictionary<bool, Brush> pGroupSegmentBrushes = new Dictionary<bool, Brush>
    {
        [true] = pGroupTitleBrush,
        [false] = pGroupMutedBrush,
    };

    private static readonly IReadOnlyDictionary<bool, Brush> pGroupSegmentFills = new Dictionary<bool, Brush>
    {
        [true] = pGroupActiveBrush,
        [false] = Brushes.Transparent,
    };

    public static Border PGroupToggleBuild(QGroupFace lFace, QGroupSwitch lToggle)
    {
        var pRow = new StackPanel { Orientation = Orientation.Horizontal };
        pRow.Children.Add(PGroupSegmentBuild(
            lToggle.QGroupToggleLeft, () => lFace.QGroupToggleRun(lToggle.QGroupToggleKey, false)));
        pRow.Children.Add(new Border { Width = 1, Background = pGroupLineBrush });
        pRow.Children.Add(PGroupSegmentBuild(
            lToggle.QGroupToggleRight, () => lFace.QGroupToggleRun(lToggle.QGroupToggleKey, true)));

        return new Border
        {
            BorderBrush = pGroupLineBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Background = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center,
            SnapsToDevicePixels = true,
            Child = pRow
        };
    }

    private static Border PGroupSegmentBuild(QGroupSide lSide, Action pGroupClick)
    {
        Border pSegment = PGroupSegmentCreate(lSide);
        PGroupSegmentAttach(pSegment, pGroupClick);
        return pSegment;
    }

    private static Border PGroupSegmentCreate(QGroupSide lSide)
    {
        var pLabel = new TextBlock
        {
            Text = lSide.QGroupSideText,
            FontSize = 12,
            FontFamily = pGroupFontFamily,
            FontWeight = PLook.PLookWeight[lSide.QGroupSideActive],
            Foreground = pGroupSegmentBrushes[lSide.QGroupSideActive],
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        return new Border
        {
            Background = pGroupSegmentFills[lSide.QGroupSideActive],
            Padding = new Thickness(12, 3, 12, 3),
            Cursor = Cursors.Hand,
            ToolTip = lSide.QGroupSideTip,
            Child = pLabel
        };
    }

    private static void PGroupSegmentAttach(Border pSegment, Action pGroupClick) =>
        pSegment.MouseLeftButtonUp += (_, _) => pGroupClick();
}

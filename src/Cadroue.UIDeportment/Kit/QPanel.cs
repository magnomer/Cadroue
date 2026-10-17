using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Cadroue.UIDeportment;

public static class QPanel
{
    private const string QPanelStyle = "PPanel";

    private const string QPanelFrameHost = "PPanelFrame";

    private const string QPanelLineSheet = "PPanelLine";

    private const string QPanelTextSheet = "PPanelText";

    private const string QPanelMarginSheet = "PPanelMargin";

    public static void QPanelAttach(UserControl lPanel)
    {
        ArgumentNullException.ThrowIfNull(lPanel);

        lPanel.Style = QContract.QContractSheetFind<Style>(QPanelStyle);
        QScrollbar.QScrollbarAttach(lPanel);
    }

    public static Border QPanelBuild(UIElement lContent)
    {
        Border lFrame = QPanelFrameBuild(lContent);
        QPanelClipApply((Border)lFrame.Child);
        return lFrame;
    }

    public static Brush QPanelLineRead() => QContract.QContractSheetFind<Brush>(QPanelLineSheet);

    public static Brush QPanelTextRead() => QContract.QContractSheetFind<Brush>(QPanelTextSheet);

    public static Thickness QPanelMarginRead() => (Thickness)QContract.QContractSheetFind<object>(QPanelMarginSheet);

    private static void QPanelClipApply(Border lBorder)
    {
        double lRadius = QPanelRadiusRead(lBorder);
        QPanelClipAttach(lBorder, lRadius);
    }

    private static double QPanelRadiusRead(Border lBorder) => lBorder.CornerRadius.TopLeft;

    private static void QPanelClipAttach(Border lBorder, double lRadius) =>
        lBorder.SizeChanged += (_, _) => QPanelClipUpdate(lBorder, lRadius);

    private static Border QPanelFrameBuild(UIElement lContent)
    {
        Border lFrame = QContract.QContractSheetFind<Border>(QPanelFrameHost);
        Border lInner = (Border)lFrame.Child;
        lInner.Child = lContent;
        return lFrame;
    }

    private static void QPanelClipUpdate(Border lBorder, double lRadius) =>
        lBorder.Clip = new RectangleGeometry(
            new Rect(0, 0, lBorder.ActualWidth, lBorder.ActualHeight),
            lRadius,
            lRadius);
}

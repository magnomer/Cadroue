using System.Windows;
using System.Windows.Controls;
using Cadroue.Application;
using Cadroue.Media;

namespace Cadroue.UIDeportment;

// Wires an info surface to its watch: each row of the report is shown through the surface's template for its kind,
// redrawn on every change, and the watch is detached when the surface unloads.
public static class QInfoFace
{
    private static readonly IReadOnlyDictionary<string, Func<UserControl, string, UIElement>> qInfoFaceSheets =
        new Dictionary<string, Func<UserControl, string, UIElement>>
        {
            [LInfoRow.LInfoRowMuted] = (lSurface, lCaption) =>
                QInfoFaceBuild(QContract.QContractSheetFind<DataTemplate>(lSurface, "pInfoMuted"), lCaption),
            [LInfoRow.LInfoRowText] = (lSurface, lCaption) =>
                QInfoFaceBuild(QContract.QContractSheetFind<DataTemplate>(lSurface, "pInfoText"), lCaption),
            [LInfoRow.LInfoRowGrey] = (lSurface, lCaption) =>
                QInfoFaceBuild(QContract.QContractSheetFind<DataTemplate>(lSurface, "pInfoGrey"), lCaption),
            [LInfoRow.LInfoRowGood] = (lSurface, lCaption) =>
                QInfoFaceBuild(QContract.QContractSheetFind<DataTemplate>(lSurface, "pInfoGood"), lCaption),
            [LInfoRow.LInfoRowBad] = (lSurface, lCaption) =>
                QInfoFaceBuild(QContract.QContractSheetFind<DataTemplate>(lSurface, "pInfoBad"), lCaption),
            [LInfoRow.LInfoRowSeparator] = (lSurface, _) =>
                (UIElement)QContract.QContractSheetFind<DataTemplate>(lSurface, "pInfoSeparator").LoadContent(),
        };

    public static UserControl QInfoFaceAttach(QInfo info, UserControl surface)
    {
        QInfoFaceApply(info, surface);
        QInfoHandlerAttach(info, surface);
        return surface;
    }

    private static void QInfoHandlerAttach(QInfo info, UserControl surface)
    {
        info.QInfoChanged += () => surface.Dispatcher.BeginInvoke(() => QInfoFaceApply(info, surface));
        surface.Unloaded += (_, _) => info.QInfoDetach();
    }

    private static void QInfoFaceApply(QInfo info, UserControl surface)
    {
        UIElementCollection items = QContract.QContractFind<StackPanel>(surface, "pInfoItems").Children;
        items.Clear();
        foreach (LInfoRow row in LInfoReport.LInfoReportRead(
                     info.QInfoViewer.QViewerCargo, LMediaProbe.LMediaAvailabilityCurrent))
        {
            items.Add(qInfoFaceSheets[row.LInfoRowKind](surface, row.LInfoRowCaption));
        }
    }

    private static UIElement QInfoFaceBuild(DataTemplate sheet, string caption)
    {
        var item = (FrameworkElement)sheet.LoadContent();
        QContract.QContractFind<TextBlock>(item, "pInfoCaption").Text = caption;
        return item;
    }
}

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Cadroue.UIDeportment;

public sealed class QExportHeader(string label, bool collapsed, Brush iconBrush, Action toggle)
{
    private Button? qExportHeader;

    public Border QExportHeaderBuild(Border surface)
    {
        surface.Tag = "Header";
        var button = (Button)((Grid)surface.Child).Children[1];
        ((TextBlock)((Grid)surface.Child).Children[0]).Text = label;
        ((Image)button.Content).Source = QIcon.QIconRead(
            collapsed
                ? "/PAsset/PPanel/PExportExpand.svg"
                : "/PAsset/PPanel/PExportCollapse.svg",
            iconBrush);
        button.ToolTip = QLocalizationCatalog.QLocalizationTextRead(
            collapsed
                ? "ExportPreset.Group.Expand"
                : "ExportPreset.Group.Collapse");
        QButton.QButtonAttach(button, QButtonLook.QButtonLookPanel);
        qExportHeader = button;
        qExportHeader.Click += QExportHeaderHandle;
        return surface;
    }

    public void QExportHeaderDetach()
    {
        if (qExportHeader is null)
        {
            return;
        }

        qExportHeader.Click -= QExportHeaderHandle;
        qExportHeader = null;
    }

    private void QExportHeaderHandle(object sender, RoutedEventArgs pEvent) => toggle();
}

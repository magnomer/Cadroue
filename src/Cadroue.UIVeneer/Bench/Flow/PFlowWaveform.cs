using System.Windows;
using System.Windows.Media;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal static class PFlowWaveform
{
    public static Geometry PFlowWaveformBuild(QFlowWaveformOutline lOutline)
    {
        var pFlowWaveformGeometry = new StreamGeometry();
        using (StreamGeometryContext pFlowWaveformContext = pFlowWaveformGeometry.Open())
        {
            pFlowWaveformContext.BeginFigure(new Point(lOutline.QFlowOutlineX, lOutline.QFlowOutlineY), true, true);
            lOutline.QFlowOutlinePoints.ToList().ForEach(lPoint =>
                pFlowWaveformContext.LineTo(new Point(lPoint.QFlowPointX, lPoint.QFlowPointY), false, false));
        }

        pFlowWaveformGeometry.Freeze();
        return pFlowWaveformGeometry;
    }
}

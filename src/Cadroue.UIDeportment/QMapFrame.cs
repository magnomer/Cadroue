using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

// One map draw as the surface's shapes, built from the map gate's frame.
public sealed record QMapFrame(
    IReadOnlyList<QMapShape> QMapFrameUnder,
    QFlowWaveformOutline QMapFrameWaveform,
    IReadOnlyList<QMapBand> QMapFrameBands,
    IReadOnlyList<QMapShape> QMapFrameOver,
    IReadOnlyList<double> QMapFrameCursor)
{
    // The gate's frame as the surface's shapes: the background and the rail under the bands, then the coverage strip,
    // its scans and the navigator over them.
    internal static QMapFrame QMapFrameBuild(CMapFrame lFrame, double lWidth, double lHeight)
    {
        var lUnder = new List<QMapShape>();
        var lOver = new List<QMapShape>();
        if (lFrame.CMapFrameSized)
        {
            lUnder.Add(new QMapShape(QMapShapeKind.QMapShapeBackground, 0, 0, lWidth, lHeight, 0));
        }

        if (lFrame.CMapFrameRailed)
        {
            lUnder.Add(new QMapShape(
                lFrame.CMapFrameWaveform ? QMapShapeKind.QMapShapeWaveform : QMapShapeKind.QMapShapeRail,
                0, QMap.QMapRailTop, lWidth, lFrame.CMapFrameBottom, 3));
            QMapCoverageAdd(lOver, lFrame, lWidth);
        }

        (double lOriginX, double lOriginY, IReadOnlyList<(double, double)> lPoints) = lFrame.CMapFrameOutline;
        return new QMapFrame(
            lUnder,
            new QFlowWaveformOutline(
                lOriginX,
                lOriginY,
                lPoints.Select(lPoint => new QFlowWaveformPoint(lPoint.Item1, lPoint.Item2)).ToArray()),
            lFrame.CMapFrameBands.Select(QMapBandBuild).ToArray(),
            lOver,
            lFrame.CMapFrameCursor);
    }

    private static void QMapCoverageAdd(List<QMapShape> lOver, CMapFrame lFrame, double lWidth)
    {
        (double lTop, double lBottom) = lFrame.CMapFrameCoverage;
        lOver.Add(new QMapShape(QMapShapeKind.QMapShapeCoverage, 0, lTop, lWidth, lBottom, 0));
        lOver.AddRange(lFrame.CMapFrameScans.Select(lScan =>
            new QMapShape(QMapShapeKind.QMapShapeScanned, lScan.Item1, lTop, lScan.Item1 + lScan.Item2, lBottom, 0)));
        if (lFrame.CMapFrameBody is { } lBody)
        {
            QMapNavigator.QMapNavigatorAdd(lOver, lBody.Item1, lBody.Item2, lBody.Item3, lWidth);
        }
    }

    private static QMapBand QMapBandBuild(CMapBand lBand) =>
        new(
            lBand.CMapBandLeft,
            lBand.CMapBandTop,
            lBand.CMapBandRight,
            lBand.CMapBandBottom,
            lBand.CMapBandColor,
            lBand.CMapBandBadge,
            lBand.CMapBandShown,
            lBand.CMapBandSelected);
}

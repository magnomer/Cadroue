namespace Cadroue.UIDeportment;

public enum QViewfinderShapeKind
{
    QViewfinderShapeBackground,
    QViewfinderShapeRail,
    QViewfinderShapeWaveform,
    QViewfinderShapeCoverage,
    QViewfinderShapeScanned,
    QViewfinderShapeKeyframe,
}

public sealed record QViewfinderShape(
    QViewfinderShapeKind QViewfinderShapeKind,
    double QViewfinderShapeLeft,
    double QViewfinderShapeTop,
    double QViewfinderShapeRight,
    double QViewfinderShapeBottom);

public sealed record QViewfinderGraduation(
    double QViewfinderTickX,
    double QViewfinderTickTop,
    double QViewfinderTickBottom,
    string QViewfinderTickLabel);

public sealed record QViewfinderBand(
    double QViewfinderBandLeft,
    double QViewfinderBandTop,
    double QViewfinderBandRight,
    double QViewfinderBandBottom,
    int QViewfinderBandColor,
    string QViewfinderBandBadge,
    string QViewfinderBandName,
    bool QViewfinderBandShown,
    bool QViewfinderBandSelected);

public sealed record QViewfinderCursor(double QViewfinderCursorX, string QViewfinderCursorText);

public sealed record QViewfinderFrame(
    IReadOnlyList<QViewfinderShape> QViewfinderFrameUnder,
    QFlowWaveformOutline QViewfinderFrameWaveform,
    IReadOnlyList<QViewfinderGraduation> QViewfinderFrameTicks,
    IReadOnlyList<QViewfinderBand> QViewfinderFrameBands,
    IReadOnlyList<QViewfinderShape> QViewfinderFrameOver,
    IReadOnlyList<QViewfinderShape> QViewfinderFrameKeyframes,
    IReadOnlyList<QViewfinderCursor> QViewfinderFrameCursor);

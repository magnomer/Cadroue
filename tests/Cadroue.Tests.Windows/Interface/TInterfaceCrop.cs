using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.ShellEngine;
using Cadroue.UIDeportment;

namespace Cadroue.Tests.Windows;

internal static class TInterfaceCrop
{
    internal static LCropboxState TCropboxStateCreate() => new();
    internal static void TCropboxStateAttach(LCropboxState state, Action handler) =>
        state.LCropboxStateChanged += handler;
    internal static void TCropboxCropSet(LCropboxState state, LWorkCrop crop) => state.LCropboxCropSet(crop);
    internal static void TCropboxApplySet(LCropboxState state, bool apply) => state.LCropboxApplySet(apply);
    internal static void TCropboxPersistentSet(LCropboxState state, bool persistent) =>
        state.LCropboxPersistentSet(persistent);
    internal static void TCropboxRatioSet(LCropboxState state, bool fixedRatio, bool lenient, int width, int height) =>
        state.LCropboxRatioSet(fixedRatio, lenient, width, height);

    internal static QCrop TCropCreate()
    {
        CAtelier atelier = TInterface.TAtelierCreate();
        return new QViewer(atelier.CAtelierUsher, atelier.CAtelierWorkspaceCreate()).QCrop;
    }
    internal static QCropDrag TCropDragCreate() => TInterfaceViewer.TViewerCreate().QCropDrag;
    internal static void TCropGripSet(QCropDrag drag, int edgeX, int edgeY) => drag.QCropGripSet(edgeX, edgeY);
    internal static void TCropBodySet(QCropDrag drag) => drag.QCropBodySet();
    internal static void TCropDrawSet(QCropDrag drag) => drag.QCropDrawSet();
    internal static bool TCropMoveCheck(QCropDrag drag) => drag.QCropMoveCheck();
    internal static void TCropRatioSet(QCrop crop, double width, double height) =>
        crop.QCropRatioSet(width, height);
    internal static void TCropLockSet(QCrop crop, bool locked) => crop.QCropLockSet(locked);

    internal static QCrop TCropRead(QViewer viewer) => viewer.QCrop;
    internal static QCropDrag TCropDragRead(QViewer viewer) => viewer.QCropDrag;
    internal static void TCropAttach(QCrop crop, Action handler) => crop.QCropApplied += handler;
    internal static void TCropVideoAttach(QCrop crop, Action handler) => crop.QCropVideoChanged += handler;
    internal static void TCropCaptureAttach(QCropDrag drag, Action<bool> handler) =>
        drag.QCropCaptureApplied += handler;
    internal static void TCropSizeHandle(QCrop crop, double width, double height) =>
        crop.QCropSizeHandle(width, height);
    internal static void TCropActiveSet(QCrop crop, bool active) => crop.QCropActiveSet(active);
    internal static void TCropToolSet(QCrop crop, bool armed) => crop.QCropToolSet(armed);
    internal static void TCropRectSet(QCrop crop, LCropbox? rect) => crop.QCropRectSet(rect);
    internal static void TCropHide(QCrop crop) => crop.QCropHide();
    internal static QCropBox TCropBoxRead(QCrop crop) => crop.QCropBoxRead();
    internal static IReadOnlyList<QCropGrip> TCropHandlesRead(QCrop crop) => crop.QCropHandlesRead();
    internal static QCropShade TCropShadeRead(QCrop crop) => crop.QCropShadeRead();
    internal static LCropbox TCropVideoRead(QCrop crop) => crop.QCropVideoRead();
    internal static LCropbox? TCropPixelRead(QCrop crop) => crop.QCropPixelRead();
    internal static bool TCropGripHandle(QCropDrag drag, int index, double x, double y) =>
        drag.QCropGripHandle(index, x, y);
    internal static bool TCropBodyHandle(QCropDrag drag, double x, double y) => drag.QCropBodyHandle(x, y);
    internal static bool TCropPressHandle(QCropDrag drag, double x, double y) => drag.QCropPressHandle(x, y);
    internal static bool TCropMoveHandle(QCropDrag drag, bool pressed, double x, double y) =>
        drag.QCropMoveHandle(pressed, x, y);
    internal static bool TCropReleaseHandle(QCropDrag drag, double x, double y) => drag.QCropReleaseHandle(x, y);
}

internal sealed class TCropPort : LCropPort
{
    private readonly LCropPort tCropPortInner;

    internal TCropPort(QInspector inspector) => tCropPortInner = new LCropOutlet(
        inspector.QInspectorCrop.QInspectorCropbox,
        () => (inspector.QInspectorSourcePresent,
            inspector.QInspectorSourceWidth, inspector.QInspectorSourceHeight));

    internal int TCropPortRequests { get; private set; }

    public event Action? LCropChanged
    {
        add => tCropPortInner.LCropChanged += value;
        remove => tCropPortInner.LCropChanged -= value;
    }

    public (int LCropRotation, bool LCropHorizontal, bool LCropVertical) LCropOrientationRead() =>
        tCropPortInner.LCropOrientationRead();
    public (bool LCropActive, bool LCropPersistent, bool LCropFixed, bool LCropLenient, int LCropWidth,
        int LCropHeight) LCropStateRead() => tCropPortInner.LCropStateRead();
    public (int LCropLeft, int LCropTop, int LCropRight, int LCropBottom) LCropEdgesRead() =>
        tCropPortInner.LCropEdgesRead();
    public (int LCropLeft, int LCropTop, int LCropRight, int LCropBottom) LCropNormalRead() =>
        tCropPortInner.LCropNormalRead();
    public (bool LCropPresent, double LCropWidth, double LCropHeight) LCropSourceRead() =>
        tCropPortInner.LCropSourceRead();
    public bool LCropRectCheck() => tCropPortInner.LCropRectCheck();
    public (bool LCropTolerated, int LCropExcess, bool LCropWide) LCropNoticeRead(double lWidth, double lHeight) =>
        tCropPortInner.LCropNoticeRead(lWidth, lHeight);
    public (int LCropWidth, int LCropHeight) LCropRatioNormalize(int lWidth, int lHeight) =>
        tCropPortInner.LCropRatioNormalize(lWidth, lHeight);

    public void LCropRotateSet(int lRotation) { TCropPortRequests++; tCropPortInner.LCropRotateSet(lRotation); }
    public void LCropFlipSet(bool lHorizontal, bool lFlipped)
    { TCropPortRequests++; tCropPortInner.LCropFlipSet(lHorizontal, lFlipped); }
    public void LCropEdgeSet(int lSide, double lEdge, bool lLocked)
    { TCropPortRequests++; tCropPortInner.LCropEdgeSet(lSide, lEdge, lLocked); }
    public void LCropEdgeReset() { TCropPortRequests++; tCropPortInner.LCropEdgeReset(); }
    public void LCropApplySet(bool lApply, bool lRatioReset)
    { TCropPortRequests++; tCropPortInner.LCropApplySet(lApply, lRatioReset); }
    public void LCropWorkApply(LInspectorPort lPlan)
    { TCropPortRequests++; tCropPortInner.LCropWorkApply(lPlan); }
    public void LCropPersistentSet(bool lPersistent)
    { TCropPortRequests++; tCropPortInner.LCropPersistentSet(lPersistent); }
    public void LCropReset() { TCropPortRequests++; tCropPortInner.LCropReset(); }
    public void LCropRatioSet(bool lFixed, bool lLenient, int lWidth, int lHeight, bool lUnlock)
    { TCropPortRequests++; tCropPortInner.LCropRatioSet(lFixed, lLenient, lWidth, lHeight, lUnlock); }
    public void LCropRatioReset() { TCropPortRequests++; tCropPortInner.LCropRatioReset(); }
    public void LCropPresetSet(int lWidth, int lHeight)
    { TCropPortRequests++; tCropPortInner.LCropPresetSet(lWidth, lHeight); }
}

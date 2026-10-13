using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;
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
    internal static void TCropGripSet(QCrop crop, int edgeX, int edgeY) => crop.QCropGripSet(edgeX, edgeY);
    internal static void TCropBodySet(QCrop crop) => crop.QCropBodySet();
    internal static void TCropDrawSet(QCrop crop) => crop.QCropDrawSet();
    internal static bool TCropMoveCheck(QCrop crop) => crop.QCropMoveCheck();
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

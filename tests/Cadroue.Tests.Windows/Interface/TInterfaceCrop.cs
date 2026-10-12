using Cadroue.Application;
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

    internal static LCrop TCropCreate() => new LViewer(TInterface.TAtelierCreate().CAtelierUsher).LCrop;
    internal static void TCropGripSet(LCrop crop, int edgeX, int edgeY) => crop.LCropGripSet(edgeX, edgeY);
    internal static void TCropBodySet(LCrop crop) => crop.LCropBodySet();
    internal static void TCropDrawSet(LCrop crop) => crop.LCropDrawSet();
    internal static bool TCropMoveCheck(LCrop crop) => crop.LCropMoveCheck();
    internal static void TCropRatioSet(LCrop crop, double width, double height) =>
        crop.LCropRatioSet(width, height);
    internal static void TCropLockSet(LCrop crop, bool locked) => crop.LCropLockSet(locked);

    internal static LCrop TCropRead(LViewer viewer) => viewer.LCrop;
    internal static LCropDrag TCropDragRead(LViewer viewer) => viewer.LCropDrag;
    internal static void TCropAttach(LCrop crop, Action handler) => crop.LCropApplied += handler;
    internal static void TCropVideoAttach(LCrop crop, Action handler) => crop.LCropVideoChanged += handler;
    internal static void TCropCaptureAttach(LCropDrag drag, Action<bool> handler) =>
        drag.LCropCaptureApplied += handler;
    internal static void TCropSizeHandle(LCrop crop, double width, double height) =>
        crop.LCropSizeHandle(width, height);
    internal static void TCropActiveSet(LCrop crop, bool active) => crop.LCropActiveSet(active);
    internal static void TCropToolSet(LCrop crop, bool armed) => crop.LCropToolSet(armed);
    internal static void TCropRectSet(LCrop crop, LCropbox? rect) => crop.LCropRectSet(rect);
    internal static void TCropHide(LCrop crop) => crop.LCropHide();
    internal static LCropBox TCropBoxRead(LCrop crop) => crop.LCropBoxRead();
    internal static IReadOnlyList<LCropGrip> TCropHandlesRead(LCrop crop) => crop.LCropHandlesRead();
    internal static LCropShade TCropShadeRead(LCrop crop) => crop.LCropShadeRead();
    internal static LCropbox TCropVideoRead(LCrop crop) => crop.LCropVideoRead();
    internal static LCropbox? TCropPixelRead(LCrop crop) => crop.LCropPixelRead();
    internal static bool TCropGripHandle(LCropDrag drag, int index, double x, double y) =>
        drag.LCropGripHandle(index, x, y);
    internal static bool TCropBodyHandle(LCropDrag drag, double x, double y) => drag.LCropBodyHandle(x, y);
    internal static bool TCropPressHandle(LCropDrag drag, double x, double y) => drag.LCropPressHandle(x, y);
    internal static bool TCropMoveHandle(LCropDrag drag, bool pressed, double x, double y) =>
        drag.LCropMoveHandle(pressed, x, y);
    internal static bool TCropReleaseHandle(LCropDrag drag, double x, double y) => drag.LCropReleaseHandle(x, y);
}

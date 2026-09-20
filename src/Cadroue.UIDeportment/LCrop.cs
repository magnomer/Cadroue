using Cadroue.Application;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

public sealed record LCropBox(
    double LCropBoxX,
    double LCropBoxY,
    double LCropBoxWidth,
    double LCropBoxHeight,
    bool LCropBoxShown);

public sealed record LCropHandle(int LCropHandleIndex, double LCropHandleX, double LCropHandleY, bool LCropHandleShown);

public sealed record LCropShade(LCropbox LCropShadeVideo, LCropbox LCropShadeBox, bool LCropShadeShown);

public sealed class LCrop
{
    public const double LCropHandleSize = 10;
    public const int LCropHandleCount = 8;

    private static readonly int[] lCropGripX = [-1, 0, 1, 1, 1, 0, -1, -1];
    private static readonly int[] lCropGripY = [-1, -1, -1, 0, 1, 1, 1, 0];

    private readonly LViewer lViewer;
    private bool lCropActive = true;
    private bool lCropPersistent;
    private bool lCropLocked;
    private int lCropEdgeX;
    private int lCropEdgeY;
    private int lCropDrive = -1;
    private int lCropAnchorX = -1;
    private int lCropAnchorY = -1;
    private double lCropRatioWidth;
    private double lCropRatioHeight;
    private double lCropAreaWidth;
    private double lCropAreaHeight;
    private double lCropBoxX;
    private double lCropBoxY;
    private double lCropBoxWidth;
    private double lCropBoxHeight;
    private bool lCropBoxShown;

    public LCrop(LViewer lOwner)
    {
        lViewer = lOwner;
    }

    public event Action? LCropApply;
    public event Action? LCropVideoChange;

    public bool LCropActive => lCropActive;

    public bool LCropPersistent => lCropPersistent;

    public bool LCropLocked => lCropLocked;

    public int LCropEdgeX => lCropEdgeX;

    public int LCropEdgeY => lCropEdgeY;

    public int LCropDrive => lCropDrive;

    public int LCropAnchorX => lCropAnchorX;

    public int LCropAnchorY => lCropAnchorY;

    public double LCropRatioWidth => lCropRatioWidth;

    public double LCropRatioHeight => lCropRatioHeight;

    public bool LCropBoxShown => lCropBoxShown;

    public bool LCropEditable =>
        lCropActive && !lCropLocked && lViewer.LViewerNeutral.LViewerTool != LViewerTool.LViewerToolNeutral;

    public bool LCropCrossShown =>
        lViewer.LViewerNeutral.LViewerTool == LViewerTool.LViewerToolNeutral
        || (lViewer.LViewerNeutral.LViewerTool == LViewerTool.LViewerToolCrop && !lCropLocked);

    public bool LCropMoveCheck() => lCropEdgeX == 0 && lCropEdgeY == 0;

    public LCropBox LCropBoxRead() => new(lCropBoxX, lCropBoxY, lCropBoxWidth, lCropBoxHeight, lCropBoxShown);

    public IReadOnlyList<LCropHandle> LCropHandlesRead() =>
        Enumerable.Range(0, LCropHandleCount).Select(LCropHandleResolve).ToList();

    public LCropShade LCropShadeRead() => new(
        LCropVideoRead(),
        new LCropbox(lCropBoxX, lCropBoxY, lCropBoxWidth, lCropBoxHeight),
        lCropBoxShown && lCropBoxWidth > 0 && lCropBoxHeight > 0);

    public LCropboxSize LCropDisplayRead()
    {
        if (lViewer.LViewerMediaInfo is not { LMediaVideoPresent: true } lMediaInfo)
        {
            return new LCropboxSize(0, 0);
        }

        return LCropbox.LCropboxSourceResolve(
            lMediaInfo.LMediaVideoWidth,
            lMediaInfo.LMediaVideoHeight,
            lViewer.LViewerPreview.LRotateFlip.LRotateKind is LRotateKind.LRotate90 or LRotateKind.LRotate270);
    }

    public LCropbox LCropVideoRead()
    {
        if (!lViewer.LViewerVideoPresent || lCropAreaWidth <= 0 || lCropAreaHeight <= 0)
        {
            return new LCropbox(0, 0, lCropAreaWidth, lCropAreaHeight);
        }

        LCropboxSize lDisplay = LCropDisplayRead();
        return LCropbox.LCropboxDisplayResolve(
            lDisplay.LCropboxSizeWidth, lDisplay.LCropboxSizeHeight, lCropAreaWidth, lCropAreaHeight);
    }

    public LCropbox? LCropPixelRead()
    {
        if (!lViewer.LViewerVideoPresent || !lCropBoxShown)
        {
            return null;
        }

        LCropbox lVideo = LCropVideoRead();
        if (lVideo.LCropboxWidth <= 0 || lVideo.LCropboxHeight <= 0 || lCropBoxWidth <= 1 || lCropBoxHeight <= 1)
        {
            return null;
        }

        LCropboxSize lDisplay = LCropDisplayRead();
        return LCropbox.LCropboxPixelResolve(
            new LCropbox(lCropBoxX, lCropBoxY, lCropBoxWidth, lCropBoxHeight),
            lVideo,
            lDisplay.LCropboxSizeWidth,
            lDisplay.LCropboxSizeHeight);
    }

    public void LCropActiveSet(bool lActive)
    {
        if (lCropActive == lActive)
        {
            return;
        }

        lCropActive = lActive;
        if (!lActive)
        {
            LCropToolSet(false);
            lViewer.LCropDrag.LCropDragClear();
        }

        LCropBoxUpdate();
        LCropOverlayUpdate();
        lViewer.LViewerPreviewApply();
    }

    public void LCropPersistentSet(bool lPersistent) => lCropPersistent = lPersistent;

    public void LCropLockSet(bool lLocked)
    {
        if (lCropLocked == lLocked)
        {
            return;
        }

        lCropLocked = lLocked;
        if (lLocked)
        {
            lViewer.LCropDrag.LCropDragClear();
        }

        LCropOverlayUpdate();
    }

    public void LCropToolSet(bool lArmed)
    {
        LViewerNeutral lNeutral = lViewer.LViewerNeutral;
        if (lArmed && lNeutral.LViewerTool == LViewerTool.LViewerToolNeutral)
        {
            lNeutral.LViewerNeutralCancel();
        }

        lNeutral.LViewerToolApply(lArmed
            ? LViewerTool.LViewerToolCrop
            : lNeutral.LViewerTool == LViewerTool.LViewerToolCrop ? LViewerTool.LViewerToolNone : lNeutral.LViewerTool);
        LCropOverlayUpdate();
    }

    public void LCropRatioSet(double lWidth, double lHeight)
    {
        bool lValid = lWidth > 0 && lHeight > 0;
        lCropRatioWidth = lValid ? lWidth : 0;
        lCropRatioHeight = lValid ? lHeight : 0;
    }

    public void LCropGripSet(int lEdgeX, int lEdgeY)
    {
        lCropEdgeX = lEdgeX;
        lCropEdgeY = lEdgeY;
        lCropDrive = lEdgeX != 0 && lEdgeY != 0 ? -1 : lEdgeX != 0 ? 0 : 1;
        lCropAnchorX = -lEdgeX;
        lCropAnchorY = -lEdgeY;
    }

    public void LCropBodySet()
    {
        lCropEdgeX = 0;
        lCropEdgeY = 0;
        LCropDrawSet();
    }

    public void LCropDrawSet()
    {
        lCropDrive = -1;
        lCropAnchorX = -1;
        lCropAnchorY = -1;
    }

    public void LCropRectSet(LCropbox? lCropbox)
    {
        LCropbox? lClamped = LCropSourceClamp(lCropbox);
        if (lClamped is not { LCropboxWidth: > 0, LCropboxHeight: > 0 })
        {
            LTraceLog.LTraceInfoRecord("Viewer crop cleared: overlay hidden");
            LCropHide();
            return;
        }

        LCropboxSize lDisplay = LCropDisplayRead();
        bool lFull = lClamped.LCropboxWidth >= lDisplay.LCropboxSizeWidth
            && lClamped.LCropboxHeight >= lDisplay.LCropboxSizeHeight;
        LTraceLog.LTraceInfoRecord(
            $"Viewer crop set: {lClamped.LCropboxX:0},{lClamped.LCropboxY:0} "
            + $"{lClamped.LCropboxWidth:0}x{lClamped.LCropboxHeight:0} "
            + $"over display {lDisplay.LCropboxSizeWidth:0}x{lDisplay.LCropboxSizeHeight:0}"
            + (lFull ? " (full frame)" : string.Empty));

        lViewer.LViewerPreviewSet(lViewer.LViewerPreview.LCropboxChange(lClamped));
        LCropBoxUpdate();
        LCropBoxRestore();
        lViewer.LViewerFilterUpdate();
    }

    public void LCropHide()
    {
        lCropBoxShown = false;
        lCropBoxWidth = 0;
        lCropBoxHeight = 0;
        lViewer.LViewerPreviewSet(lViewer.LViewerPreview.LCropboxChange(null));
        LCropOverlayUpdate();
        lViewer.LViewerFilterUpdate();
    }

    public void LCropOverlayUpdate() => LCropApply?.Invoke();

    public void LCropShownSet(bool lShown) => lCropBoxShown = lShown;

    public void LCropSizeHandle(double lWidth, double lHeight)
    {
        lCropAreaWidth = Math.Max(0, lWidth);
        lCropAreaHeight = Math.Max(0, lHeight);
        LCropBoxRestore();
    }

    public void LCropBoxSet(LCropbox lBox)
    {
        lCropBoxX = lBox.LCropboxX;
        lCropBoxY = lBox.LCropboxY;
        lCropBoxWidth = lBox.LCropboxWidth;
        lCropBoxHeight = lBox.LCropboxHeight;
    }

    public void LCropVideoCommit()
    {
        lViewer.LViewerPreviewSet(lViewer.LViewerPreview.LCropboxChange(LCropPixelRead()));
        lViewer.LViewerFilterUpdate();
        LCropVideoChange?.Invoke();
    }

    private LCropHandle LCropHandleResolve(int lIndex)
    {
        int lEdgeX = lCropGripX[lIndex];
        int lEdgeY = lCropGripY[lIndex];
        double lPointX = lEdgeX == 0
            ? lCropBoxX + (lCropBoxWidth / 2)
            : lEdgeX < 0 ? lCropBoxX : lCropBoxX + lCropBoxWidth;
        double lPointY = lEdgeY == 0
            ? lCropBoxY + (lCropBoxHeight / 2)
            : lEdgeY < 0 ? lCropBoxY : lCropBoxY + lCropBoxHeight;
        bool lShown = LCropEditable && lCropBoxShown && lCropBoxWidth > 0 && lCropBoxHeight > 0;
        return new LCropHandle(lIndex, lPointX - (LCropHandleSize / 2), lPointY - (LCropHandleSize / 2), lShown);
    }

    private LCropbox? LCropSourceClamp(LCropbox? lCropbox)
    {
        if (lCropbox is not { LCropboxWidth: > 0, LCropboxHeight: > 0 } || !lViewer.LViewerVideoPresent)
        {
            return lCropbox;
        }

        LCropboxSize lSource = LCropDisplayRead();
        LCropbox lClamped = LCropbox.LCropboxRectClamp(
            lCropbox, new LCropbox(0, 0, lSource.LCropboxSizeWidth, lSource.LCropboxSizeHeight), false);
        return lClamped is { LCropboxWidth: > 0, LCropboxHeight: > 0 } ? lClamped : null;
    }

    private void LCropBoxUpdate() =>
        lCropBoxShown = lCropActive && lViewer.LViewerPreview.LCropbox is { LCropboxWidth: > 0, LCropboxHeight: > 0 };

    private void LCropBoxRestore()
    {
        if (lViewer.LViewerPreview.LCropbox is not { } lCropbox || !lViewer.LViewerVideoPresent)
        {
            return;
        }

        LCropbox lVideo = LCropVideoRead();
        LCropboxSize lDisplay = LCropDisplayRead();
        if (lDisplay.LCropboxSizeWidth <= 0 || lDisplay.LCropboxSizeHeight <= 0)
        {
            return;
        }

        LCropBoxSet(LCropbox.LCropboxOverlayResolve(
            lCropbox, lVideo, lDisplay.LCropboxSizeWidth, lDisplay.LCropboxSizeHeight));
        LCropOverlayUpdate();
    }
}

namespace Cadroue.UIDeportment;

public sealed class LCrop
{
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

    public bool LCropMoveCheck() => lCropEdgeX == 0 && lCropEdgeY == 0;

    public void LCropActiveSet(bool lActive) => lCropActive = lActive;

    public void LCropPersistentSet(bool lPersistent) => lCropPersistent = lPersistent;

    public void LCropLockSet(bool lLocked) => lCropLocked = lLocked;

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
}

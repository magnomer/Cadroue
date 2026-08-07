namespace Cadroue.Application;

public sealed partial record LCropbox(double LCropboxX, double LCropboxY, double LCropboxWidth, double LCropboxHeight)
{
    public double LCropboxRight => LCropboxX + LCropboxWidth;

    public double LCropboxBottom => LCropboxY + LCropboxHeight;
}

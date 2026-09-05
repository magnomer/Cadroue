namespace Cadroue.Core;

public sealed record LWorkCrop(
    int LWorkCropLeft,
    int LWorkCropTop,
    int LWorkCropRight,
    int LWorkCropBottom,
    int LWorkCropRotation,
    bool LWorkFlipHorizontal,
    bool LWorkFlipVertical)
{
    public static LWorkCrop LWorkCropCreate() => new(0, 0, 0, 0, 0, false, false);

    public bool LWorkEdgeActive =>
        LWorkCropLeft > 0 || LWorkCropTop > 0 || LWorkCropRight > 0 || LWorkCropBottom > 0;

    public bool LWorkCropActive =>
        LWorkEdgeActive
        || LWorkCropRotation != 0
        || LWorkFlipHorizontal
        || LWorkFlipVertical;
}

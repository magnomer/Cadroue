namespace Cadroue.Application;

public readonly record struct LCropboxSize(double LCropboxSizeWidth, double LCropboxSizeHeight);

public readonly record struct LCropboxExtent(int LCropboxExtentWidth, int LCropboxExtentHeight);

public readonly record struct LCropboxPoint(double LCropboxPointX, double LCropboxPointY);

public readonly record struct LCropboxEdges(
    double LCropboxLeft,
    double LCropboxTop,
    double LCropboxRight,
    double LCropboxBottom);

public readonly record struct LCropboxRatio(
    bool LCropboxRatioFixed,
    bool LCropboxRatioLenient,
    int LCropboxRatioWidth,
    int LCropboxRatioHeight);

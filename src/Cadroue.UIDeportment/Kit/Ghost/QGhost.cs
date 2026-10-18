namespace Cadroue.UIDeportment;

public readonly record struct QGhostSize(
    int QGhostPixelWidth,
    int QGhostPixelHeight,
    double QGhostDpiX,
    double QGhostDpiY);

public sealed class QGhost
{
    private const double QGhostDpiBase = 96;
    private readonly double qGhostGrabX;
    private readonly double qGhostGrabY;
    private double qGhostLeft;
    private double qGhostTop;
    private bool qGhostShown = true;

    public QGhost(double lGrabX, double lGrabY)
    {
        qGhostGrabX = lGrabX;
        qGhostGrabY = lGrabY;
    }

    public bool QGhostShown => qGhostShown;

    public double QGhostLeft => qGhostLeft;

    public double QGhostTop => qGhostTop;

    public void QGhostPointSet(double lX, double lY)
    {
        qGhostLeft = lX - qGhostGrabX;
        qGhostTop = lY - qGhostGrabY;
    }

    public bool QGhostClear()
    {
        bool lWasShown = qGhostShown;
        qGhostShown = false;
        return lWasShown;
    }

    public static QGhostSize QGhostSizeResolve(double lWidth, double lHeight, double lScaleX, double lScaleY) =>
        new(
            Math.Max(1, (int)Math.Ceiling(lWidth * lScaleX)),
            Math.Max(1, (int)Math.Ceiling(lHeight * lScaleY)),
            QGhostDpiBase * lScaleX,
            QGhostDpiBase * lScaleY);
}

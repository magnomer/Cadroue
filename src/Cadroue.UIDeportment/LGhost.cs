namespace Cadroue.UIDeportment;

public readonly record struct LGhostSize(
    int LGhostPixelWidth,
    int LGhostPixelHeight,
    double LGhostDpiX,
    double LGhostDpiY);

public sealed class LGhost
{
    private const double LGhostDpiBase = 96;
    private readonly double lGhostGrabX;
    private readonly double lGhostGrabY;
    private double lGhostLeft;
    private double lGhostTop;
    private bool lGhostShown = true;

    public LGhost(double lGrabX, double lGrabY)
    {
        lGhostGrabX = lGrabX;
        lGhostGrabY = lGrabY;
    }

    public bool LGhostShown => lGhostShown;

    public double LGhostLeft => lGhostLeft;

    public double LGhostTop => lGhostTop;

    public void LGhostPointSet(double lX, double lY)
    {
        lGhostLeft = lX - lGhostGrabX;
        lGhostTop = lY - lGhostGrabY;
    }

    public bool LGhostClear()
    {
        bool lWasShown = lGhostShown;
        lGhostShown = false;
        return lWasShown;
    }

    public static LGhostSize LGhostSizeResolve(double lWidth, double lHeight, double lScaleX, double lScaleY) =>
        new(
            Math.Max(1, (int)Math.Ceiling(lWidth * lScaleX)),
            Math.Max(1, (int)Math.Ceiling(lHeight * lScaleY)),
            LGhostDpiBase * lScaleX,
            LGhostDpiBase * lScaleY);
}

using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

// The main window's frame: the least width it keeps and the bounds it saves for the next start.
public static class QWindowFrame
{
    private const double QWindowWidthFloor = 900;

    public static (double QWindowMinimum, double QWindowWidth) QWindowWidthResolve(
        double? lRequired,
        double lReserved,
        double lWidth)
    {
        double lMinimum = Math.Max(QWindowWidthFloor, lRequired ?? 0) + lReserved;
        return (lMinimum, Math.Max(lWidth, lMinimum));
    }

    public static void QWindowFrameSave(bool lNormal, QSashBounds lBounds, QSashBounds lRestore)
    {
        if (QWindowFrameResolve(lNormal, lBounds, lRestore) is { } lState)
        {
            LFrameStore.LFrameSave(lState);
        }
    }

    private static LFrameState? QWindowFrameResolve(bool lNormal, QSashBounds lBounds, QSashBounds lRestore)
    {
        QSashBounds lFrame = lNormal ? lBounds : lRestore;
        if (!(lFrame.QSashWidth > 0 && lFrame.QSashHeight > 0))
        {
            return null;
        }

        return new LFrameState
        {
            LFrameLeft = lFrame.QSashLeft,
            LFrameTop = lFrame.QSashTop,
            LFrameWidth = lFrame.QSashWidth,
            LFrameHeight = lFrame.QSashHeight,
            LFrameFlowHeight = LFrameStore.LFrameStateCurrent.LFrameFlowHeight
        };
    }
}

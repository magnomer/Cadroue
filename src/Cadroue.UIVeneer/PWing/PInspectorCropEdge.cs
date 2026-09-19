using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.UIVeneer.PWing;

public sealed partial class PInspector
{
    private void PInspectorInsetChange(int pEdge)
    {
        TextBox pEdgeBox = pEdge switch
        {
            0 => pInspectorInsetLeft,
            2 => pInspectorInsetRight,
            1 => pInspectorInsetTop,
            _ => pInspectorInsetBottom
        };
        LInspector.LInspectorEdgeLock.LCropboxEdgeSet(pEdge, !string.IsNullOrWhiteSpace(pEdgeBox.Text));

        var pInsets = new LCropboxEdges(
            PInspectorInsetRead(pInspectorInsetLeft),
            PInspectorInsetRead(pInspectorInsetTop),
            PInspectorInsetRead(pInspectorInsetRight),
            PInspectorInsetRead(pInspectorInsetBottom));
        LCropboxRatio pRatio = LCropboxState.LCropboxStateRatio;
        if (pRatio.LCropboxRatioFixed && LInspector.LInspectorSourcePresent)
        {
            LCropboxEdges? pFit = LInspector.LInspectorEdgeLock.LCropboxEdgeResolve(
                LInspector.LInspectorSourceWidth,
                LInspector.LInspectorSourceHeight,
                pInsets,
                pRatio.LCropboxRatioWidth,
                pRatio.LCropboxRatioHeight,
                pEdge is 0 or 2);
            if (pFit is { } pEdges)
            {
                pInsets = pEdges;
            }
        }

        PInspectorEdgesSet(pInsets);
    }

    private void PInspectorEdgesSet(LCropboxEdges pInsets)
    {
        LWorkCrop pCrop = LCropboxState.LCropboxStateCrop with
        {
            LWorkCropLeft = PInspectorEdgeResolve(pInsets.LCropboxLeft),
            LWorkCropTop = PInspectorEdgeResolve(pInsets.LCropboxTop),
            LWorkCropRight = PInspectorEdgeResolve(pInsets.LCropboxRight),
            LWorkCropBottom = PInspectorEdgeResolve(pInsets.LCropboxBottom)
        };
        if (LCropboxState.LCropboxStateRatio.LCropboxRatioFixed)
        {
            LCropboxState.LCropboxCropSet(pCrop);
            return;
        }

        LCropboxState.LCropboxStateSet(pCrop, LCropboxState.LCropboxStateActive, false, false, 0, 0);
    }

    public void PInspectorCropSet(Rect? pCropVideo, int pDriveAxis, int pAnchorX, int pAnchorY)
    {
        LInspector.LInspectorEdgeLock.LCropboxEdgeClear();
        Rect? pCropSnapped = pCropVideo is { Width: > 0, Height: > 0 } pCropDrawn
            ? PInspectorRatioResolve(pCropDrawn, pDriveAxis, pAnchorX, pAnchorY) ?? pCropDrawn
            : pCropVideo;

        if (pCropSnapped is not { Width: > 0, Height: > 0 } pCropRect)
        {
            PInspectorEdgesSet(new LCropboxEdges(0, 0, 0, 0));
            return;
        }

        PInspectorEdgesSet(new LCropboxEdges(
            pCropRect.X,
            pCropRect.Y,
            LInspector.LInspectorSourceWidth - pCropRect.X - pCropRect.Width,
            LInspector.LInspectorSourceHeight - pCropRect.Y - pCropRect.Height));
    }

    private void PInspectorEdgesReset()
    {
        LInspector.LInspectorEdgeLock.LCropboxEdgeClear();
        PInspectorEdgesSet(new LCropboxEdges(0, 0, 0, 0));
    }

    private static int PInspectorEdgeResolve(double pEdgeValue) =>
        (int)Math.Max(0, Math.Round(pEdgeValue));

    private static double PInspectorInsetRead(TextBox pInsetBox) => Math.Max(0, PInspectorNumberRead(pInsetBox));

    private static double PInspectorNumberRead(TextBox pNumberBox) =>
        double.TryParse(pNumberBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out double pNumber)
            ? pNumber
            : 0;
}

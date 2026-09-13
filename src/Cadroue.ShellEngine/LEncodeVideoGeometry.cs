using System.Globalization;

using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

internal static partial class LEncodeVideo
{
    internal static IReadOnlyList<string> LEncodeGeometryRead(LWorkCrop lCrop, LWorkMedia? lMedia = null)
    {
        var lFilters = new List<string>();

        if (lCrop.LWorkFlipHorizontal)
        {
            lFilters.Add("hflip");
        }

        if (lCrop.LWorkFlipVertical)
        {
            lFilters.Add("vflip");
        }

        string? lRotate = lCrop.LWorkCropRotation switch
        {
            90 => "transpose=1",
            180 => "transpose=1,transpose=1",
            270 => "transpose=2",
            _ => null
        };

        if (lRotate is not null)
        {
            lFilters.Add(lRotate);
        }

        LWorkCrop lGeometry = LEncodeGeometryNormalize(lCrop, lMedia);
        if (lGeometry.LWorkEdgeActive)
        {
            lFilters.Add(string.Create(
                CultureInfo.InvariantCulture,
                $"crop=in_w-{lGeometry.LWorkCropLeft}-{lGeometry.LWorkCropRight}:" +
                $"in_h-{lGeometry.LWorkCropTop}-{lGeometry.LWorkCropBottom}:" +
                $"{lGeometry.LWorkCropLeft}:{lGeometry.LWorkCropTop}"));
        }

        return lFilters;
    }

    private static LWorkCrop LEncodeGeometryNormalize(LWorkCrop lCrop, LWorkMedia? lMedia)
    {
        if (lMedia is null)
        {
            return lCrop;
        }

        if (!lMedia.LWorkMediaVideo)
        {
            return LWorkCrop.LWorkCropCreate();
        }

        (double lFrameWidth, double lFrameHeight) = LCropbox.LCropboxSourceResolve(
            lMedia.LWorkMediaWidth,
            lMedia.LWorkMediaHeight,
            LCropbox.LCropboxRotatedCheck(lCrop.LWorkCropRotation));
        return LCropbox.LCropboxEdgeNormalize(lCrop, lFrameWidth, lFrameHeight);
    }

    private static string LEncodeScaleResolve(string lSize, bool lReactive)
    {
        string[] lParts = lSize.Split('x');
        int lWidth = int.Parse(lParts[0], CultureInfo.InvariantCulture);
        int lHeight = int.Parse(lParts[1], CultureInfo.InvariantCulture);

        int lShortEdge = Math.Min(lWidth, lHeight);
        int lLongEdge = Math.Max(lWidth, lHeight);
        if (!lReactive || lShortEdge == lLongEdge)
        {
            return string.Create(CultureInfo.InvariantCulture, $"scale={lWidth}:{lHeight}");
        }

        int lEdgeSpan = lLongEdge - lShortEdge;
        return string.Create(
            CultureInfo.InvariantCulture,
            $"scale=w={lShortEdge}+{lEdgeSpan}*gte(iw\\,ih):h={lLongEdge}-{lEdgeSpan}*gte(iw\\,ih)");
    }

    private static string? LEncodeSizeRead(string lSize)
    {
        if (LEncode.LEncodeSourceCheck(lSize) || string.Equals(lSize, "Custom", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        string[] lParts = lSize.Split(
            ['x', 'X', '×'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lParts.Length != 2 || !int.TryParse(lParts[0], out int lWidth) || !int.TryParse(lParts[1], out int lHeight))
        {
            return null;
        }

        return $"{lWidth}x{lHeight}";
    }
}

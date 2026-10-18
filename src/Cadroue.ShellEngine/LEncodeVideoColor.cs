using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

internal static class LEncodeVideoColor
{
    internal static IReadOnlyList<string> LEncodeColorNormalize(LWorkItem lWorkItem, LEncoding lOutput)
    {
        LWorkMedia? lMedia = lWorkItem.LWorkSourceMedia;
        string lSourcePixel = lMedia?.LWorkMediaPixel ?? string.Empty;
        string lSourceRange = lMedia?.LWorkMediaRange ?? string.Empty;
        bool lFullRange = string.Equals(lSourceRange, "pc", StringComparison.OrdinalIgnoreCase)
            || lSourcePixel.StartsWith("yuvj", StringComparison.OrdinalIgnoreCase);

        string lPixel = lOutput.LEncodingVideo.LEncodingPixel;
        string lTargetPixel = !string.IsNullOrWhiteSpace(lPixel)
            && !string.Equals(lPixel, "Auto", StringComparison.OrdinalIgnoreCase)
                ? lPixel
                : string.IsNullOrWhiteSpace(lSourcePixel) ? "yuv420p" : lSourcePixel;

        return new[]
        {
            "scale=in_range=full:out_range=" + (lFullRange ? "pc" : "tv"),
            "format=" + lTargetPixel
        };
    }
}

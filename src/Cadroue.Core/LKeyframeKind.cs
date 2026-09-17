namespace Cadroue.Core;

public enum LKeyframeKind
{
    LKeyframeKindInter,
    LKeyframeKindIntra,
    LKeyframeKindNone
}

public static class LKeyframeCodec
{
    private static readonly HashSet<string> lKeyframeIntraCodecs = new(StringComparer.OrdinalIgnoreCase)
    {
        "prores", "prores_ks", "prores_aw",
        "dnxhd", "dnxhr",
        "mjpeg", "mjpegb", "jpeg2000", "jpegls",
        "dvvideo",
        "huffyuv", "ffvhuff", "utvideo", "magicyuv", "cfhd",
        "rawvideo", "v210", "v410", "v308", "v408", "r210", "r10k",
        "png", "tiff", "bmp", "targa", "apng"
    };

    public static LKeyframeKind LKeyframeKindResolve(LMediaInfo lMediaInfo) =>
        !lMediaInfo.LMediaVideoPresent
            ? LKeyframeKind.LKeyframeKindNone
            : LKeyframeKindResolve(lMediaInfo.LMediaVideoCodec);

    public static LKeyframeKind LKeyframeKindResolve(string? lKeyframeCodec) =>
        !string.IsNullOrWhiteSpace(lKeyframeCodec) && lKeyframeIntraCodecs.Contains(lKeyframeCodec.Trim())
            ? LKeyframeKind.LKeyframeKindIntra
            : LKeyframeKind.LKeyframeKindInter;

    public static TimeSpan LKeyframeFrameResolve(double lKeyframeRate) =>
        lKeyframeRate > 0 ? TimeSpan.FromSeconds(1d / lKeyframeRate) : TimeSpan.Zero;
}

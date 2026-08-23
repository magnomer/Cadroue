namespace Cadroue.Application;

public sealed record LHistogramCounts(
    int[] LHistogramLuminance,
    int[] LHistogramRed,
    int[] LHistogramGreen,
    int[] LHistogramBlue);

public static class LHistogram
{
    private const int LHistogramBinCount = 256;

    // Per-channel 256-bin value counts of a decoded RGBA frame plus a combined
    // Rec.709 luminance histogram, for the curve editor's behind-the-curve guide.
    // Fully transparent pixels are skipped; bytes are R,G,B,A in stored order.
    public static LHistogramCounts LHistogramCreate(
        IReadOnlyList<byte> lHistogramPixels, int lHistogramWidth, int lHistogramHeight)
    {
        var lHistogramLuminance = new int[LHistogramBinCount];
        var lHistogramRed = new int[LHistogramBinCount];
        var lHistogramGreen = new int[LHistogramBinCount];
        var lHistogramBlue = new int[LHistogramBinCount];

        if (lHistogramWidth <= 0 || lHistogramHeight <= 0)
        {
            return new LHistogramCounts(lHistogramLuminance, lHistogramRed, lHistogramGreen, lHistogramBlue);
        }

        for (int lHistogramIndex = 0; lHistogramIndex + 3 < lHistogramPixels.Count; lHistogramIndex += 4)
        {
            if (lHistogramPixels[lHistogramIndex + 3] == 0)
            {
                continue;
            }

            int lHistogramR = lHistogramPixels[lHistogramIndex];
            int lHistogramG = lHistogramPixels[lHistogramIndex + 1];
            int lHistogramB = lHistogramPixels[lHistogramIndex + 2];
            int lHistogramY = Math.Clamp(
                (int)Math.Round((0.2126 * lHistogramR) + (0.7152 * lHistogramG) + (0.0722 * lHistogramB)),
                0, LHistogramBinCount - 1);

            lHistogramRed[lHistogramR]++;
            lHistogramGreen[lHistogramG]++;
            lHistogramBlue[lHistogramB]++;
            lHistogramLuminance[lHistogramY]++;
        }

        return new LHistogramCounts(lHistogramLuminance, lHistogramRed, lHistogramGreen, lHistogramBlue);
    }
}

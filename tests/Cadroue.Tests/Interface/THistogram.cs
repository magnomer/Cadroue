using Cadroue.Application;

namespace Cadroue.Tests;

internal static class THistogram
{
    internal static LHistogramCounts THistogramCreate(byte[] pixels, int width, int height) =>
        LHistogram.LHistogramCreate(pixels, width, height);
}

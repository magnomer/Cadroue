# LHistogram.cs

## `public static LHistogramCounts LHistogramCreate(`

Per-channel 256-bin value counts of a decoded RGBA frame plus a combined Rec.709 luminance histogram.
They feed the curve editor's behind-the-curve guide.
Fully transparent pixels are skipped.
Bytes are R,G,B,A in stored order.

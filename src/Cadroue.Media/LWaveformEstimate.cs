namespace Cadroue.Media;

public static class LWaveformEstimate
{
    public static double[] LWaveformEnvelopeRead(byte[] lWaveformPeaks)
    {
        if (lWaveformPeaks.Length == 0)
        {
            return Array.Empty<double>();
        }

        var lWaveformEnvelope = new double[lWaveformPeaks.Length];
        for (int lWaveformIndex = 0; lWaveformIndex < lWaveformPeaks.Length; lWaveformIndex++)
        {
            lWaveformEnvelope[lWaveformIndex] = lWaveformPeaks[lWaveformIndex] / (double)LWaveform.LWaveformPeakMaximum;
        }

        return lWaveformEnvelope;
    }
}

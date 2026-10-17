namespace Cadroue.ShellEngine;

// One picked neutral sample as the white balance gate reads it.
public interface LWhitebalanceSamplePort
{
    // The pick's status token (Valid, Decode or Invalid), its gains and its sample colour. A failed pick is traced.
    (string LWhitebalanceStatus, double LWhitebalanceRed, double LWhitebalanceGreen, double LWhitebalanceBlue,
        int LWhitebalanceSampleRed, int LWhitebalanceSampleGreen, int LWhitebalanceSampleBlue)
        LWhitebalanceSampleRead();
}

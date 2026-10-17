namespace Cadroue.Core;

public sealed record LWorkWhitebalanceSettings(
    LWhitebalanceMethod LWorkWhitebalanceMethod,
    double LWorkWhitebalanceSaturation)
{
    public double LWorkWhitebalanceRed { get; init; } = 1;
    public double LWorkWhitebalanceGreen { get; init; } = 1;
    public double LWorkWhitebalanceBlue { get; init; } = 1;
    public int LWorkSampleRed { get; init; }
    public int LWorkSampleGreen { get; init; }
    public int LWorkSampleBlue { get; init; }
}

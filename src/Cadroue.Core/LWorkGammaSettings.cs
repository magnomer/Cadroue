namespace Cadroue.Core;

public sealed record LWorkGammaSettings(
    double LWorkGammaGlobal,
    double LWorkGammaRed,
    double LWorkGammaGreen,
    double LWorkGammaBlue,
    double LWorkGammaHighlight)
{
    // The five values in slot order: midtone, red, green, blue, highlight protection.
    public IReadOnlyList<double> LWorkSlotRead() =>
        [LWorkGammaGlobal, LWorkGammaRed, LWorkGammaGreen, LWorkGammaBlue, LWorkGammaHighlight];
}

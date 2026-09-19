namespace Cadroue.UIDeportment;

public sealed class LDivider
{
    public const double LDividerMinimum = 200;
    public const double LDividerMaximum = 520;

    private double lDividerYOrigin;
    private double lDividerHeightOrigin;
    private bool lDividerActive;

    public bool LDividerActive => lDividerActive;

    public void LDividerPressHandle(double lY, double lHeight)
    {
        lDividerActive = true;
        lDividerYOrigin = lY;
        lDividerHeightOrigin = lHeight;
    }

    public double LDividerMoveResolve(double lY, double lCurrentHeight)
    {
        if (!lDividerActive)
        {
            return lCurrentHeight;
        }

        return Math.Clamp(lDividerHeightOrigin + lDividerYOrigin - lY, LDividerMinimum, LDividerMaximum);
    }

    public void LDividerRelease() => lDividerActive = false;
}

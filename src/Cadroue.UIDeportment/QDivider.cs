namespace Cadroue.UIDeportment;

public sealed class QDivider
{
    public const double QDividerMinimum = 200;
    public const double QDividerMaximum = 520;

    private double qDividerYOrigin;
    private double qDividerHeightOrigin;
    private bool qDividerActive;

    public bool QDividerActive => qDividerActive;

    public void QDividerPressHandle(double lY, double lHeight)
    {
        qDividerActive = true;
        qDividerYOrigin = lY;
        qDividerHeightOrigin = lHeight;
    }

    public double QDividerMoveResolve(double lY, double lCurrentHeight)
    {
        if (!qDividerActive)
        {
            return lCurrentHeight;
        }

        return Math.Clamp(qDividerHeightOrigin + qDividerYOrigin - lY, QDividerMinimum, QDividerMaximum);
    }

    public void QDividerRelease() => qDividerActive = false;
}

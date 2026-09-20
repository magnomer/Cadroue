namespace Cadroue.UIDeportment;

public sealed class LFunnelDrag
{
    private readonly LFunnel lFunnel;
    private LFunnelRule? lFunnelDragRule;
    private double lFunnelDragX;
    private double lFunnelDragY;
    private double lFunnelOffsetX;
    private double lFunnelOffsetY;
    private bool lFunnelDragActive;

    public LFunnelDrag(LFunnel lOwner)
    {
        lFunnel = lOwner;
    }

    public LFunnelRule? LFunnelDragRule => lFunnelDragRule;

    public bool LFunnelDragActive => lFunnelDragActive;

    public double LFunnelOffsetX => lFunnelOffsetX;

    public double LFunnelOffsetY => lFunnelOffsetY;

    public void LFunnelPressHandle(LFunnelRule lRule, double lX, double lY, double lOffsetX, double lOffsetY)
    {
        lFunnelDragRule = lRule;
        lFunnelDragX = lX;
        lFunnelDragY = lY;
        lFunnelOffsetX = lOffsetX;
        lFunnelOffsetY = lOffsetY;
        lFunnelDragActive = false;
    }

    public bool LFunnelMoveCheck(LFunnelRule lRule, bool lLeftPressed) =>
        ReferenceEquals(lFunnelDragRule, lRule) && lLeftPressed;

    public bool LFunnelDragResolve(double lX, double lY, double lMinimumX, double lMinimumY)
    {
        if (lFunnelDragRule is null || lFunnelDragActive)
        {
            return false;
        }

        if (Math.Abs(lX - lFunnelDragX) < lMinimumX && Math.Abs(lY - lFunnelDragY) < lMinimumY)
        {
            return false;
        }

        lFunnelDragActive = true;
        return true;
    }

    public void LFunnelDragMove(double lPointer, IReadOnlyList<double> lCenters)
    {
        if (lFunnelDragRule is not { } lRule || !lFunnelDragActive)
        {
            return;
        }

        lFunnel.LFunnelRuleMove(lRule, LFunnelIndexResolve(lPointer, lCenters));
    }

    public bool LFunnelReleaseCheck(LFunnelRule lRule) => ReferenceEquals(lFunnelDragRule, lRule);

    public void LFunnelDragClear()
    {
        lFunnelDragRule = null;
        lFunnelDragActive = false;
    }

    public static double LFunnelCenterResolve(double lTop, double lHeight) => lTop + lHeight / 2;

    public static int LFunnelIndexResolve(double lPointer, IReadOnlyList<double> lCenters)
    {
        int lTarget = 0;
        for (int lIndex = 0; lIndex < lCenters.Count; lIndex++)
        {
            if (lPointer > lCenters[lIndex])
            {
                lTarget = lIndex + 1;
            }
        }

        return Math.Clamp(lTarget, 0, lCenters.Count);
    }
}

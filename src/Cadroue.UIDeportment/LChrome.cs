using Cadroue.Application;

namespace Cadroue.UIDeportment;

public sealed class LChrome
{
    private double lChromePressX;
    private double lChromePressY;
    private bool lChromeArmed;
    private bool lChromeDragActive;

    public bool LChromeArmed => lChromeArmed;

    public bool LChromeDragActive => lChromeDragActive;

    public static bool LChromeDebugVisible => LPreference.LPreferenceStateCurrent.LPreferenceDeveloperActive;

    public static bool LChromeCaptionResolve(bool lInteractive, bool lTab, bool lInside) =>
        !lInteractive && !lTab && lInside;

    public static bool LChromeDoubleCheck(int lClickCount, bool lCaption) => lClickCount == 2 && lCaption;

    public void LChromePressHandle(bool lCaption, double lX, double lY)
    {
        lChromeArmed = false;
        lChromeDragActive = false;
        if (!lCaption)
        {
            return;
        }

        lChromePressX = lX;
        lChromePressY = lY;
        lChromeArmed = true;
    }

    public bool LChromeMoveResolve(double lX, double lY, bool lLeftPressed, double lMinimumX, double lMinimumY)
    {
        if (!lChromeArmed || lChromeDragActive || !lLeftPressed)
        {
            return false;
        }

        if (Math.Abs(lX - lChromePressX) < lMinimumX && Math.Abs(lY - lChromePressY) < lMinimumY)
        {
            return false;
        }

        lChromeDragActive = true;
        return true;
    }

    public void LChromeReset()
    {
        lChromeArmed = false;
        lChromeDragActive = false;
    }

    public bool LChromeReleaseResolve(bool lLogo)
    {
        bool lClicked = lChromeArmed && !lChromeDragActive;
        LChromeReset();
        return lClicked && lLogo;
    }
}

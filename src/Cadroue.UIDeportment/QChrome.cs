using Cadroue.Application;

namespace Cadroue.UIDeportment;

public sealed class QChrome
{
    private double qChromePressX;
    private double qChromePressY;
    private bool qChromeArmed;
    private bool qChromeDragActive;

    public bool QChromeArmed => qChromeArmed;

    public bool QChromeDragActive => qChromeDragActive;

    public static bool QChromeDebugVisible => LPreference.LPreferenceStateCurrent.LPreferenceDeveloperActive;

    public static bool QChromeCaptionResolve(bool lInteractive, bool lTab, bool lInside) =>
        !lInteractive && !lTab && lInside;

    public static bool QChromeDoubleCheck(int lClickCount, bool lCaption) => lClickCount == 2 && lCaption;

    public void QChromePressHandle(bool lCaption, double lX, double lY)
    {
        qChromeArmed = false;
        qChromeDragActive = false;
        if (!lCaption)
        {
            return;
        }

        qChromePressX = lX;
        qChromePressY = lY;
        qChromeArmed = true;
    }

    public bool QChromeMoveResolve(double lX, double lY, bool lLeftPressed, double lMinimumX, double lMinimumY)
    {
        if (!qChromeArmed || qChromeDragActive || !lLeftPressed)
        {
            return false;
        }

        if (Math.Abs(lX - qChromePressX) < lMinimumX && Math.Abs(lY - qChromePressY) < lMinimumY)
        {
            return false;
        }

        qChromeDragActive = true;
        return true;
    }

    public void QChromeReset()
    {
        qChromeArmed = false;
        qChromeDragActive = false;
    }

    public bool QChromeReleaseResolve(bool lLogo)
    {
        bool lClicked = qChromeArmed && !qChromeDragActive;
        QChromeReset();
        return lClicked && lLogo;
    }
}

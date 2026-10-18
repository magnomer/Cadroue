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
        QChromeReset();
        if (!lCaption)
        {
            return;
        }

        QChromePressSet(lX, lY);
    }

    private void QChromePressSet(double lX, double lY)
    {
        qChromePressX = lX;
        qChromePressY = lY;
        qChromeArmed = true;
    }

    public bool QChromeMoveResolve(double lX, double lY, bool lLeftPressed, double lMinimumX, double lMinimumY)
    {
        if (!QChromeDragCheck(lX, lY, lLeftPressed, lMinimumX, lMinimumY))
        {
            return false;
        }

        QChromeDragSet();
        return true;
    }

    // A drag starts once an armed press, still held, moves past the minimum distance on either axis.
    private bool QChromeDragCheck(double lX, double lY, bool lLeftPressed, double lMinimumX, double lMinimumY)
    {
        if (!qChromeArmed || qChromeDragActive || !lLeftPressed)
        {
            return false;
        }

        return !(Math.Abs(lX - qChromePressX) < lMinimumX && Math.Abs(lY - qChromePressY) < lMinimumY);
    }

    private void QChromeDragSet() => qChromeDragActive = true;

    public void QChromeReset()
    {
        qChromeArmed = false;
        qChromeDragActive = false;
    }

    public bool QChromeReleaseResolve(bool lLogo)
    {
        bool lClicked = QChromeClickCheck();
        QChromeReset();
        return lClicked && lLogo;
    }

    private bool QChromeClickCheck() => qChromeArmed && !qChromeDragActive;
}

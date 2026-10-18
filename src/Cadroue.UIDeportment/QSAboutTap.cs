using Cadroue.Application;

namespace Cadroue.UIDeportment;

// The developer switch: ten taps, each within the window of the last, flip the developer preference.
public sealed class QSAboutTap
{
    private const int QSAboutTapTarget = 10;
    private const double QSAboutTapWindow = 1.5;

    private int qsAboutTapCount;
    private DateTime qsAboutTapLast;

    public bool QSAboutDeveloper => LPreference.LPreferenceStateCurrent.LPreferenceDeveloperActive;

    public bool QSAboutTapChange(DateTime lNow)
    {
        int lCount = QSAboutTapResolve(lNow);
        QSAboutTapSet(lCount, lNow);
        if (lCount < QSAboutTapTarget)
        {
            return false;
        }

        QSAboutTapReset();
        LPreference.LPreferenceDeveloperSet(!QSAboutDeveloper);
        return true;
    }

    private int QSAboutTapResolve(DateTime lNow) =>
        (lNow - qsAboutTapLast).TotalSeconds > QSAboutTapWindow ? 1 : qsAboutTapCount + 1;

    private void QSAboutTapSet(int lCount, DateTime lNow)
    {
        qsAboutTapCount = lCount;
        qsAboutTapLast = lNow;
    }

    private void QSAboutTapReset() => qsAboutTapCount = 0;
}

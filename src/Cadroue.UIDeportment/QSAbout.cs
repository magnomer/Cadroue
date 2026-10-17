using Cadroue.Infrastructure;
using Cadroue.Application;
using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

public sealed record QSAboutCredit(string QSAboutCreditName, string QSAboutCreditUrl);

public sealed class QSAbout
{
    public const string QSAboutProjectUrl = "https://github.com/magnomer/Cadroue";

    private const string QSAboutNoticeName = "THIRD-PARTY-NOTICES.md";
    private const int QSAboutTapTarget = 10;
    private const double QSAboutTapWindow = 1.5;

    private static readonly QSAboutCredit[] qsAboutCredits =
    [
        new("FFmpeg", "https://ffmpeg.org"),
        new("FlyleafLib", "https://github.com/SuRGeoNix/Flyleaf"),
        new("MPV", "https://mpv.io"),
        new("Phosphor Icons", "https://phosphoricons.com/"),
        new("SharpVectors", "https://github.com/ElinamLLC/SharpVectors")
    ];

    private readonly CUsher qsAboutUsher;
    private int qsAboutTapCount;
    private DateTime qsAboutTapLast;

    public QSAbout(CUsher lUsher)
    {
        qsAboutUsher = lUsher;
    }

    public event Action<string, string>? QSAboutErrorShown;

    public static IReadOnlyList<QSAboutCredit> QSAboutCredits => qsAboutCredits;

    public int QSAboutTapCount => qsAboutTapCount;

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

    public static string QSAboutVersionRead() => LProgramVersion.LProgramVersionRead();

    public void QSAboutNoticeOpen()
    {
        (string lPath, string? lFailure) =
            qsAboutUsher.CUsherProgramOpen(QSAboutNoticeName, "Notice could not be opened: {0}: {1}");
        QSAboutErrorRaise(lPath, lFailure);
    }

    public void QSAboutLinkOpen(string lUrl) =>
        QSAboutErrorRaise(lUrl, qsAboutUsher.CUsherLinkOpen(lUrl, "Link could not be opened: {0}: {1}"));

    private void QSAboutErrorRaise(string lResource, string? lDetail)
    {
        if (lDetail is not null)
        {
            QSAboutErrorShown?.Invoke(lResource, lDetail);
        }
    }
}

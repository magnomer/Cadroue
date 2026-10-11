using Cadroue.Infrastructure;
using Cadroue.Application;
using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

public sealed record LSAboutCredit(string LSAboutCreditName, string LSAboutCreditUrl);

public sealed class LSAbout
{
    public const string LSAboutProjectUrl = "https://github.com/magnomer/Cadroue";

    private const string LSAboutNoticeName = "THIRD-PARTY-NOTICES.md";
    private const int LSAboutTapTarget = 10;
    private const double LSAboutTapWindow = 1.5;

    private static readonly LSAboutCredit[] lsAboutCredits =
    [
        new("FFmpeg", "https://ffmpeg.org"),
        new("FlyleafLib", "https://github.com/SuRGeoNix/Flyleaf"),
        new("MPV", "https://mpv.io"),
        new("Phosphor Icons", "https://phosphoricons.com/"),
        new("SharpVectors", "https://github.com/ElinamLLC/SharpVectors")
    ];

    private readonly CUsher lsAboutUsher;
    private int lsAboutTapCount;
    private DateTime lsAboutTapLast;

    public LSAbout(LStrip lStrip)
    {
        lsAboutUsher = lStrip.LStripUsher;
    }

    public event Action<string, string>? LSAboutErrorShown;

    public static IReadOnlyList<LSAboutCredit> LSAboutCredits => lsAboutCredits;

    public int LSAboutTapCount => lsAboutTapCount;

    public bool LSAboutDeveloper => LPreference.LPreferenceStateCurrent.LPreferenceDeveloperActive;

    public bool LSAboutTapChange(DateTime lNow)
    {
        lsAboutTapCount = (lNow - lsAboutTapLast).TotalSeconds > LSAboutTapWindow ? 1 : lsAboutTapCount + 1;
        lsAboutTapLast = lNow;
        if (lsAboutTapCount < LSAboutTapTarget)
        {
            return false;
        }

        lsAboutTapCount = 0;
        LPreference.LPreferenceDeveloperSet(!LSAboutDeveloper);
        return true;
    }

    public static string LSAboutVersionRead() => LTraceLog.LTraceVersionRead();

    private string LSAboutNoticePath => lsAboutUsher.CUsherProgramResolve(LSAboutNoticeName);

    public void LSAboutNoticeOpen() =>
        LSAboutErrorRaise(LSAboutNoticePath, lsAboutUsher.CUsherLinkOpen(LSAboutNoticePath, "Notice"));

    public void LSAboutLinkOpen(string lUrl) => LSAboutErrorRaise(lUrl, lsAboutUsher.CUsherLinkOpen(lUrl, "Link"));

    private void LSAboutErrorRaise(string lResource, string? lDetail)
    {
        if (lDetail is not null)
        {
            LSAboutErrorShown?.Invoke(lResource, lDetail);
        }
    }
}

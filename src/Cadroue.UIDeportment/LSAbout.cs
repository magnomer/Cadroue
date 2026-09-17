using System.Reflection;
using Cadroue.Application;

namespace Cadroue.UIDeportment;

public sealed record LSAboutCredit(string LSAboutCreditName, string LSAboutCreditUrl);

public sealed class LSAbout
{
    public const string LSAboutProjectUrl = "https://github.com/magnomer/Cadroue";
    public const string LSAboutNoticeName = "THIRD-PARTY-NOTICES.md";

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

    private int lsAboutTapCount;
    private DateTime lsAboutTapLast;

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

    public static string LSAboutVersionRead()
    {
        Assembly lAssembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        string lVersion = lAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? lAssembly.GetName().Version?.ToString()
            ?? string.Empty;
        int lBuildMark = lVersion.IndexOf('+');
        return lBuildMark < 0 ? lVersion : lVersion[..lBuildMark];
    }
}

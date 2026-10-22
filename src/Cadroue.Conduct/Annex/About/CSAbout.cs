using System;
using System.Collections.Generic;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

public sealed record CSAboutCredit(string CSAboutCreditName, string CSAboutCreditUrl);

public sealed class CSAbout
{
    public const string CSAboutProjectUrl = "https://github.com/magnomer/Cadroue";

    private const string CSAboutNoticeName = "THIRD-PARTY-NOTICES.md";
    private const int CSAboutTapTarget = 10;
    private const double CSAboutTapWindow = 1.5;

    private static readonly CSAboutCredit[] csAboutCredits =
    [
        new("FFmpeg", "https://ffmpeg.org"),
        new("FlyleafLib", "https://github.com/SuRGeoNix/Flyleaf"),
        new("MPV", "https://mpv.io"),
        new("Phosphor Icons", "https://phosphoricons.com/"),
        new("SharpVectors", "https://github.com/ElinamLLC/SharpVectors")
    ];

    private readonly LAboutPort csAboutPort;
    private readonly CUsher csAboutUsher;
    private int csAboutTapCount;
    private DateTime csAboutTapLast;

    internal CSAbout(LAboutPort port, CUsher usher)
    {
        csAboutPort = port;
        csAboutUsher = usher;
    }

    public static IReadOnlyList<CSAboutCredit> CSAboutCredits => csAboutCredits;

    public bool CSAboutDeveloper => csAboutPort.LAboutDeveloperRead();

    public (string CSAboutPath, string? CSAboutFailure) CSAboutNoticeOpen()
    {
        (string path, string? failure) =
            csAboutUsher.CUsherProgramOpen(CSAboutNoticeName, "Notice could not be opened: {0}: {1}");
        return (path, failure);
    }

    public string? CSAboutLinkOpen(string url) =>
        csAboutUsher.CUsherLinkOpen(url, "Link could not be opened: {0}: {1}");

    public bool CSAboutTapChange(DateTime now)
    {
        int count = CSAboutTapResolve(now);
        CSAboutTapSet(count, now);
        if (count < CSAboutTapTarget)
        {
            return false;
        }

        CSAboutTapReset();
        csAboutPort.LAboutDeveloperSave(!CSAboutDeveloper);
        return true;
    }

    private int CSAboutTapResolve(DateTime now) =>
        (now - csAboutTapLast).TotalSeconds > CSAboutTapWindow ? 1 : csAboutTapCount + 1;

    private void CSAboutTapSet(int count, DateTime now)
    {
        csAboutTapCount = count;
        csAboutTapLast = now;
    }

    private void CSAboutTapReset() => csAboutTapCount = 0;
}

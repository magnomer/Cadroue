using Cadroue.Infrastructure;
using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

public sealed record QSAboutCredit(string QSAboutCreditName, string QSAboutCreditUrl);

public sealed class QSAbout
{
    public const string QSAboutProjectUrl = "https://github.com/magnomer/Cadroue";

    private const string QSAboutNoticeName = "THIRD-PARTY-NOTICES.md";

    private static readonly QSAboutCredit[] qsAboutCredits =
    [
        new("FFmpeg", "https://ffmpeg.org"),
        new("FlyleafLib", "https://github.com/SuRGeoNix/Flyleaf"),
        new("MPV", "https://mpv.io"),
        new("Phosphor Icons", "https://phosphoricons.com/"),
        new("SharpVectors", "https://github.com/ElinamLLC/SharpVectors")
    ];

    private readonly CUsher qsAboutUsher;

    public QSAbout(CUsher lUsher)
    {
        qsAboutUsher = lUsher;
    }

    public event Action<string, string>? QSAboutErrorShown;

    public static IReadOnlyList<QSAboutCredit> QSAboutCredits => qsAboutCredits;

    public QSAboutTap QSAboutTap { get; } = new();

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

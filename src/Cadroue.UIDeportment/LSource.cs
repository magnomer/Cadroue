using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.Media;

namespace Cadroue.UIDeportment;

public sealed class LSource
{
    private const string LSourceSidecarPattern = "*.cad";
    private const string LSourceReturnKey = "Return";

    private readonly LViewer lSourceViewer;
    private readonly bool lSourceAudioAllowed;
    private string lSourcePath = string.Empty;

    public event Action<string>? LSourcePathApply;
    public event Action<string, string>? LSourceRefuse;

    public LSource(LViewer lViewer, bool lAudioAllowed)
    {
        lSourceViewer = lViewer;
        lSourceAudioAllowed = lAudioAllowed;
        lSourceViewer.LViewerMediaChange += LSourceMediaHandle;
    }

    public string LSourcePath => lSourcePath;

    public bool LSourceAudioAllowed => lSourceAudioAllowed;

    public static string LSourcePlaceholderRead() => LLocalization.LLocalizationTextRead("Source.Empty.Notice");

    public static string LSourceTitleRead() => LLocalization.LLocalizationTextRead("Source.Dialog.Open");

    public static bool LSourcePlaceholderCheck(string lText) => string.IsNullOrWhiteSpace(lText);

    public string LSourceFilterRead()
    {
        string lVideoPattern = LSourcePatternRead(LMedia.LMediaVideoExtensions);
        string lAudioPattern = LSourcePatternRead(LMedia.LMediaAudioExtensions);
        return lSourceAudioAllowed
            ? LLocalization.LLocalizationFormat(
                "Source.Dialog.MediaProjectFilter", lVideoPattern, lAudioPattern, LSourceSidecarPattern)
            : LLocalization.LLocalizationFormat(
                "Source.Dialog.VideoProjectFilter", lVideoPattern, LSourceSidecarPattern);
    }

    public void LSourceDialogOpen(bool? lConfirmed, string lPath)
    {
        if (lConfirmed != true)
        {
            return;
        }

        LSourceOpen(lPath);
    }

    public bool LSourceKeyRun(string lKey, string lText)
    {
        if (!string.Equals(lKey, LSourceReturnKey, StringComparison.Ordinal))
        {
            return false;
        }

        string lPath = lText.Trim();
        if (!LUsher.LUsherFileExist(lPath))
        {
            return false;
        }

        return LSourceOpen(lPath);
    }

    public bool LSourceOpen(string lPath)
    {
        if (LMedia.LMediaAudioCheck(lPath) && !lSourceAudioAllowed)
        {
            LSourceRefuse?.Invoke(
                LLocalization.LLocalizationTextRead("Source.AudioOnly.Title"),
                LLocalization.LLocalizationTextRead("Source.AudioOnly.Message"));
            return false;
        }

        lSourceViewer.LViewerSource.LViewerSourceOpen(lPath);
        return true;
    }

    private void LSourceMediaHandle(LCargo lCargo)
    {
        lSourcePath = lCargo.LCargoSourcePath;
        LSourcePathApply?.Invoke(lSourcePath);
    }

    private static string LSourcePatternRead(IReadOnlyList<string> lExtensions) =>
        string.Join(";", lExtensions.Select(lExtension => $"*{lExtension}"));
}

using Cadroue.Conduct;
using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.Media;

namespace Cadroue.UIDeportment;

public sealed class QSource
{
    private const string QSourceSidecarPattern = "*.cad";
    private const string QSourceReturnKey = "Return";

    private readonly CUsher qSourceUsher;
    private readonly QViewer qSourceViewer;
    private readonly bool qSourceAudioAllowed;
    private string qSourcePath = string.Empty;

    public event Action<string>? QSourcePathApplied;
    public event Action<string, string>? QSourceRefuse;

    public QSource(CUsher lUsher, QViewer lViewer, bool lAudioAllowed)
    {
        qSourceUsher = lUsher;
        qSourceViewer = lViewer;
        qSourceAudioAllowed = lAudioAllowed;
        qSourceViewer.QViewerMediaChanged += QSourceMediaHandle;
    }

    public string QSourcePath => qSourcePath;

    public bool QSourceAudioAllowed => qSourceAudioAllowed;

    public static string QSourcePlaceholderRead() => LLocalization.LLocalizationTextRead("Source.Empty.Notice");

    public static string QSourceTitleRead() => LLocalization.LLocalizationTextRead("Source.Dialog.Open");

    public static bool QSourcePlaceholderCheck(string lText) => string.IsNullOrWhiteSpace(lText);

    public string QSourceFilterRead()
    {
        string lVideoPattern = QSourcePatternRead(LMedia.LMediaVideoExtensions);
        string lAudioPattern = QSourcePatternRead(LMedia.LMediaAudioExtensions);
        return qSourceAudioAllowed
            ? LLocalization.LLocalizationFormat(
                "Source.Dialog.MediaProjectFilter", lVideoPattern, lAudioPattern, QSourceSidecarPattern)
            : LLocalization.LLocalizationFormat(
                "Source.Dialog.VideoProjectFilter", lVideoPattern, QSourceSidecarPattern);
    }

    public void QSourceDialogOpen(bool? lConfirmed, string lPath)
    {
        if (lConfirmed != true)
        {
            return;
        }

        QSourceOpen(lPath);
    }

    public bool QSourceKeyRun(string lKey, string lText)
    {
        if (!string.Equals(lKey, QSourceReturnKey, StringComparison.Ordinal))
        {
            return false;
        }

        string lPath = lText.Trim();
        if (!qSourceUsher.CUsherFileExist(lPath))
        {
            return false;
        }

        return QSourceOpen(lPath);
    }

    public bool QSourceOpen(string lPath)
    {
        if (LMedia.LMediaAudioCheck(lPath) && !qSourceAudioAllowed)
        {
            QSourceRefuse?.Invoke(
                LLocalization.LLocalizationTextRead("Source.AudioOnly.Title"),
                LLocalization.LLocalizationTextRead("Source.AudioOnly.Message"));
            return false;
        }

        qSourceViewer.QViewerSource.QViewerSourceOpen(lPath);
        return true;
    }

    private void QSourceMediaHandle(LCargo lCargo)
    {
        qSourcePath = lCargo.LCargoSourcePath;
        QSourcePathApplied?.Invoke(qSourcePath);
    }

    private static string QSourcePatternRead(IReadOnlyList<string> lExtensions) =>
        string.Join(";", lExtensions.Select(lExtension => $"*{lExtension}"));
}

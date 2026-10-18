using Cadroue.Application;
using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

// The encoder window's output location: each mode keeps its own folder in the draft while another is shown.
public sealed class QSEncoderLocation
{
    private readonly CUsher qsEncoderUsher;
    private readonly LPreset qsEncoderDraft;
    private string? qsEncoderLocationMode;

    internal QSEncoderLocation(CUsher lUsher, LPreset lDraft)
    {
        qsEncoderUsher = lUsher;
        qsEncoderDraft = lDraft;
    }

    public string QSEncoderInitialResolve(string lCurrent) =>
        qsEncoderUsher.CUsherFolderExist(lCurrent) ? lCurrent : string.Empty;

    public static bool QSEncoderFolderCheck(string lMode) =>
        !string.Equals(lMode, QSEncoder.QSEncoderSourceMode, StringComparison.Ordinal);

    public static bool QSEncoderCustomCheck(string lMode) =>
        string.Equals(lMode, "Custom location", StringComparison.Ordinal);

    public static string QSEncoderFolderResolve(string lMode) => lMode switch
    {
        "Sibling" => "Encoder.Location.Sibling",
        "Custom location" => "Encoder.Location.Custom",
        _ => "Encoder.Location.Subfolder"
    };

    public static string QSEncoderStatusResolve(string lMode) => lMode switch
    {
        "Subfolder" => "Encoder.Location.SubfolderStatus",
        "Sibling" => "Encoder.Location.SiblingStatus",
        "Custom location" => "Encoder.Location.CustomStatus",
        _ => "Encoder.Location.Source"
    };

    public string QSEncoderLocationSelect(string lMode, string lFolderShown)
    {
        string lFolder = lFolderShown;
        if (QSEncoderLocationFind(lMode) is { } lPrevious)
        {
            qsEncoderDraft.LPresetLocationSet(lPrevious, lFolderShown.Trim());
            lFolder = qsEncoderDraft.LPresetLocationRead(lMode);
        }

        QSEncoderLocationSet(lMode);
        return lFolder;
    }

    private string? QSEncoderLocationFind(string lMode) =>
        qsEncoderLocationMode is not null && !string.Equals(qsEncoderLocationMode, lMode, StringComparison.Ordinal)
            ? qsEncoderLocationMode
            : null;

    private void QSEncoderLocationSet(string lMode) => qsEncoderLocationMode = lMode;
}

using System.Windows;
using System.Windows.Controls;
using Cadroue.Application;
using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

// The encoder window's output location: each mode keeps its own folder in the draft while another is shown.
public sealed class QSEncoderLocation
{
    private readonly CUsher qsEncoderUsher;
    private readonly CSEncoderLocation qsEncoderLocationGate;

    internal QSEncoderLocation(CUsher lUsher, CSEncoderLocation lLocation)
    {
        qsEncoderUsher = lUsher;
        qsEncoderLocationGate = lLocation;
    }

    public (string, string)[] QSEncoderLocationModes { get; } =
    [
        (CSEncoderDraft.CSEncoderDraftSource, "Encoder.Location.SourceFolder"),
        ("Subfolder", "Encoder.Location.Subfolder"),
        ("Sibling", "Encoder.Location.Sibling"),
        ("Custom location", "Encoder.Location.Custom")
    ];

    public string QSEncoderInitialResolve(string lCurrent) =>
        qsEncoderUsher.CUsherFolderExist(lCurrent) ? lCurrent : string.Empty;

    public string QSEncoderLocationRead(string lMode) => qsEncoderLocationGate.CSEncoderLocationRead(lMode);

    public string QSEncoderLocationSelect(string lMode, string lFolderShown) =>
        qsEncoderLocationGate.CSEncoderLocationSelect(lMode, lFolderShown);

    public void QSEncoderLocationUpdate(
        string lMode, TextBox lFolder, UIElement? lRow, Button? lBrowse, TextBlock? lLabel, TextBlock? lStatus)
    {
        lFolder.Text = QSEncoderLocationSelect(lMode, lFolder.Text);
        QSEncoderLocationApply(qsEncoderLocationGate.CSEncoderLocationShown, lRow, lBrowse, lLabel, lStatus);
    }

    private static void QSEncoderLocationApply(
        (bool, bool, string, string) lShown, UIElement? lRow, Button? lBrowse, TextBlock? lLabel, TextBlock? lStatus)
    {
        if (lRow is not null)
        {
            lRow.Visibility = lShown.Item1 ? Visibility.Visible : Visibility.Collapsed;
        }

        if (lBrowse is not null)
        {
            lBrowse.Visibility = lShown.Item2 ? Visibility.Visible : Visibility.Collapsed;
        }

        if (lLabel is not null)
        {
            lLabel.Text = QLocalizationCatalog.QLocalizationTextRead(lShown.Item3);
        }

        if (lStatus is not null)
        {
            lStatus.Text = QLocalizationCatalog.QLocalizationTextRead(lShown.Item4);
        }
    }
}

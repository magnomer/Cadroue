using Cadroue.Conduct;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

// The palette folder's files: imports, saves, and hides or deletes a palette on disk; the selection stays with
// the spectrum.
public sealed class QSSpectrumFile : CSSpectrumPort
{
    private readonly Func<string> qsSpectrumFolder;

    public QSSpectrumFile(Func<string> lFolder)
    {
        qsSpectrumFolder = lFolder;
    }

    public string QSSpectrumFolderRead() => qsSpectrumFolder();

    public CSSpectrumImportResult CSSpectrumPortImport(
        string lSourcePath, out string lTargetPath, out string? lLoadedName)
    {
        LSectionImportResult lResult = LSectionPalette.LSectionPaletteImport(
            qsSpectrumFolder, lSourcePath, out lTargetPath, out lLoadedName);
        return lResult switch
        {
            LSectionImportResult.LSectionImportInvalid => CSSpectrumImportResult.CSSpectrumImportInvalid,
            LSectionImportResult.LSectionImportReserved => CSSpectrumImportResult.CSSpectrumImportReserved,
            LSectionImportResult.LSectionImportFailed => CSSpectrumImportResult.CSSpectrumImportFailed,
            _ => CSSpectrumImportResult.CSSpectrumImportLoaded
        };
    }

    public CSSpectrumSnapshot CSSpectrumPortRead()
    {
        var lRows = new List<CSSpectrumRow>();
        foreach (LSectionEntry lEntry in LSectionPalette.LSectionEntriesRead(qsSpectrumFolder()))
        {
            lRows.Add(new CSSpectrumRow(
                lEntry.LSectionEntryName,
                lEntry.LSectionEntryColors,
                lEntry.LSectionEntryPath is null,
                lEntry.LSectionEntryPath));
        }

        return new CSSpectrumSnapshot(lRows);
    }

    public bool CSSpectrumPortRemove(string lName, bool lNative, string? lPath) =>
        LSectionPaletteStore.LSectionPaletteRemove(
            lNative ? qsSpectrumFolder() : null, lName, lNative, lPath);

    public bool CSSpectrumPortSave(string lTargetPath, string lName) =>
        LSectionPaletteStore.LSectionPaletteSave(
            lTargetPath, lName, LSectionPalette.LSectionHexRead(lName));
}

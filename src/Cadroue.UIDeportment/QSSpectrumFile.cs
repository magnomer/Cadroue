using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

public enum QSSpectrumResult
{
    QSSpectrumResultLoaded,
    QSSpectrumResultInvalid,
    QSSpectrumResultReserved,
    QSSpectrumResultFailed
}

// The palette folder's files: imports, saves, and hides or deletes a palette on disk; the selection stays with
// the spectrum.
public sealed class QSSpectrumFile
{
    private readonly Func<string> qsSpectrumFolder;

    public QSSpectrumFile(Func<string> lFolder)
    {
        qsSpectrumFolder = lFolder;
    }

    public string QSSpectrumFolderRead() => qsSpectrumFolder();

    public QSSpectrumResult QSSpectrumImport(string lSourcePath, out string lTargetPath)
    {
        lTargetPath = string.Empty;
        if (LSectionPaletteStore.LSectionPaletteRead(lSourcePath) is not { } lFile)
        {
            return QSSpectrumResult.QSSpectrumResultInvalid;
        }

        LSectionImportResult lResult = LSectionPaletteStore.LSectionPaletteImport(
            qsSpectrumFolder(), lSourcePath, lFile.LSectionPaletteName, out lTargetPath);
        return QSSpectrumResultResolve(lResult);
    }

    private static QSSpectrumResult QSSpectrumResultResolve(LSectionImportResult lResult) => lResult switch
    {
        LSectionImportResult.LSectionImportReserved => QSSpectrumResult.QSSpectrumResultReserved,
        LSectionImportResult.LSectionImportFailed => QSSpectrumResult.QSSpectrumResultFailed,
        _ => QSSpectrumResult.QSSpectrumResultLoaded
    };

    public bool QSSpectrumSave(string lTargetPath, string lName, string[] lColors) =>
        LSectionPaletteStore.LSectionPaletteSave(lTargetPath, lName, lColors);

    // A native palette is hidden in the folder's list; any other is deleted from its path. True when it is gone.
    public bool QSSpectrumRemove(string lName, bool lNative, string? lPath)
    {
        if (lNative)
        {
            string lFolder = qsSpectrumFolder();
            string[] lHidden = QSSpectrumHiddenResolve(LSectionPaletteStore.LSectionHiddenLoad(lFolder), lName);
            LSectionPaletteStore.LSectionHiddenSave(lFolder, lHidden);
            return true;
        }

        return lPath is not null && LSectionPaletteStore.LSectionPaletteDelete(lPath);
    }

    private static string[] QSSpectrumHiddenResolve(IEnumerable<string> lHidden, string lName) =>
        new HashSet<string>(lHidden, StringComparer.Ordinal) { lName }.ToArray();
}

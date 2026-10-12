using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

public enum QSSpectrumResult
{
    QSSpectrumResultLoaded,
    QSSpectrumResultInvalid,
    QSSpectrumResultReserved,
    QSSpectrumResultFailed
}

public sealed class QSSpectrum
{
    public const string QSSpectrumDefault = "Cadroue";

    private readonly Func<string> qsSpectrumFolder;
    private IReadOnlyList<string> qsSpectrumNames = [];
    private string qsSpectrumName;

    public event Action? QSSpectrumChanged;

    public QSSpectrum(string lName)
        : this(lName, LDepot.LDepotPaletteRead)
    {
    }

    public QSSpectrum(string lName, Func<string> lFolder)
    {
        qsSpectrumName = lName;
        qsSpectrumFolder = lFolder;
    }

    public string QSSpectrumName => qsSpectrumName;

    public IReadOnlyList<string> QSSpectrumNames => qsSpectrumNames;

    public string QSSpectrumFolderRead() => qsSpectrumFolder();

    public static bool QSSpectrumFixedCheck(string lName) =>
        string.Equals(lName, QSSpectrumDefault, StringComparison.Ordinal);

    public void QSSpectrumNamesSet(IReadOnlyList<string> lNames)
    {
        qsSpectrumNames = lNames;
        if (!lNames.Contains(qsSpectrumName, StringComparer.Ordinal))
        {
            qsSpectrumName = QSSpectrumDefault;
        }

        QSSpectrumChanged?.Invoke();
    }

    public void QSSpectrumSelect(string lName)
    {
        if (string.Equals(qsSpectrumName, lName, StringComparison.Ordinal))
        {
            return;
        }

        qsSpectrumName = lName;
        QSSpectrumChanged?.Invoke();
    }

    public QSSpectrumResult QSSpectrumImport(string lSourcePath, out string lTargetPath)
    {
        lTargetPath = string.Empty;
        if (LSectionPalette.LSectionPaletteRead(lSourcePath) is not { } lFile)
        {
            return QSSpectrumResult.QSSpectrumResultInvalid;
        }

        return LSectionPalette.LSectionPaletteImport(
            qsSpectrumFolder(), lSourcePath, lFile.LSectionPaletteName, out lTargetPath) switch
        {
            LSectionImportResult.LSectionImportReserved => QSSpectrumResult.QSSpectrumResultReserved,
            LSectionImportResult.LSectionImportFailed => QSSpectrumResult.QSSpectrumResultFailed,
            _ => QSSpectrumResult.QSSpectrumResultLoaded
        };
    }

    public bool QSSpectrumSave(string lTargetPath, string lName, string[] lColors) =>
        LSectionPalette.LSectionPaletteSave(lTargetPath, lName, lColors);

    public bool QSSpectrumRemove(string lName, bool lNative, string? lPath)
    {
        if (QSSpectrumFixedCheck(lName))
        {
            return false;
        }

        if (lNative)
        {
            string lFolder = qsSpectrumFolder();
            var lHidden = new HashSet<string>(LSectionPalette.LSectionHiddenLoad(lFolder), StringComparer.Ordinal)
            {
                lName
            };
            LSectionPalette.LSectionHiddenSave(lFolder, lHidden.ToArray());
        }
        else if (lPath is null || !LSectionPalette.LSectionPaletteDelete(lPath))
        {
            return false;
        }

        if (string.Equals(qsSpectrumName, lName, StringComparison.Ordinal))
        {
            qsSpectrumName = QSSpectrumDefault;
        }

        return true;
    }
}

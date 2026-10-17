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
        bool lKept = lNames.Contains(qsSpectrumName, StringComparer.Ordinal);
        QSSpectrumStateSet(lNames, QSSpectrumNameResolve(lKept));
    }

    private void QSSpectrumStateSet(IReadOnlyList<string> lNames, string lName)
    {
        qsSpectrumNames = lNames;
        qsSpectrumName = lName;
        QSSpectrumChanged?.Invoke();
    }

    private void QSSpectrumNameSet(string lName) => qsSpectrumName = lName;

    private string QSSpectrumNameResolve(bool lKept) => lKept ? qsSpectrumName : QSSpectrumDefault;

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

    public bool QSSpectrumRemove(string lName, bool lNative, string? lPath)
    {
        if (QSSpectrumFixedCheck(lName))
        {
            return false;
        }

        if (lNative)
        {
            string lFolder = qsSpectrumFolder();
            string[] lHidden = QSSpectrumHiddenResolve(LSectionPaletteStore.LSectionHiddenLoad(lFolder), lName);
            LSectionPaletteStore.LSectionHiddenSave(lFolder, lHidden);
        }
        else if (lPath is null || !LSectionPaletteStore.LSectionPaletteDelete(lPath))
        {
            return false;
        }

        bool lKept = !string.Equals(qsSpectrumName, lName, StringComparison.Ordinal);
        QSSpectrumNameSet(QSSpectrumNameResolve(lKept));
        return true;
    }

    private static string[] QSSpectrumHiddenResolve(IEnumerable<string> lHidden, string lName) =>
        new HashSet<string>(lHidden, StringComparer.Ordinal) { lName }.ToArray();
}

using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

public sealed class QSSpectrum
{
    public const string QSSpectrumDefault = "Cadroue";

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
        QSSpectrumFile = new QSSpectrumFile(lFolder);
    }

    public QSSpectrumFile QSSpectrumFile { get; }

    public string QSSpectrumName => qsSpectrumName;

    public IReadOnlyList<string> QSSpectrumNames => qsSpectrumNames;

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

    // The fixed palette stays; any other leaves the folder first, then the selection falls back when it was chosen.
    public bool QSSpectrumRemove(string lName, bool lNative, string? lPath)
    {
        if (QSSpectrumFixedCheck(lName) || !QSSpectrumFile.QSSpectrumRemove(lName, lNative, lPath))
        {
            return false;
        }

        bool lKept = !string.Equals(qsSpectrumName, lName, StringComparison.Ordinal);
        QSSpectrumNameSet(QSSpectrumNameResolve(lKept));
        return true;
    }
}

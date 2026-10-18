using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

public enum QSLoupeFloat
{
    QSLoupeFloatOff,
    QSLoupeFloatOwner,
    QSLoupeFloatTop
}

public sealed record QSLoupeButton(
    QSLoupeFloat QSLoupeButtonKind,
    string QSLoupeButtonText,
    bool QSLoupeButtonSelected);

// The loupe window's stacking choice: free, over its owner, or topmost; read from and saved to the preferences.
public sealed class QSLoupeFloatChoice
{
    private static readonly IReadOnlyDictionary<string, QSLoupeFloat> qLoupeFloatKinds =
        new Dictionary<string, QSLoupeFloat>
        {
            ["Off"] = QSLoupeFloat.QSLoupeFloatOff,
            ["Owner"] = QSLoupeFloat.QSLoupeFloatOwner,
            ["Top"] = QSLoupeFloat.QSLoupeFloatTop,
        };

    private static readonly IReadOnlyDictionary<QSLoupeFloat, string> qLoupeFloatTokens =
        new Dictionary<QSLoupeFloat, string>
        {
            [QSLoupeFloat.QSLoupeFloatOff] = "Off",
            [QSLoupeFloat.QSLoupeFloatOwner] = "Owner",
            [QSLoupeFloat.QSLoupeFloatTop] = "Top",
        };

    private static readonly (QSLoupeFloat, string)[] qLoupeButtons =
    [
        (QSLoupeFloat.QSLoupeFloatOff, "Loupe.Float.Off"),
        (QSLoupeFloat.QSLoupeFloatOwner, "Loupe.Float.Owner"),
        (QSLoupeFloat.QSLoupeFloatTop, "Loupe.Float.Top"),
    ];

    private QSLoupeFloat qLoupeFloat = QSLoupeFloat.QSLoupeFloatOwner;

    public event Action? QSLoupeFloatChanged;

    public QSLoupeFloat QSLoupeFloat => qLoupeFloat;

    public bool QSLoupeOwned => qLoupeFloat != QSLoupeFloat.QSLoupeFloatOff;

    public bool QSLoupeTopmost => qLoupeFloat == QSLoupeFloat.QSLoupeFloatTop;

    public IReadOnlyList<QSLoupeButton> QSLoupeButtonsRead() =>
        qLoupeButtons.Select(QSLoupeButtonResolve).ToList();

    public void QSLoupeFloatRestore() =>
        qLoupeFloat = QSLoupeFloatResolve(LPreference.LPreferenceStateCurrent.LPreferenceLoupeFloat);

    public void QSLoupeFloatSet(QSLoupeFloat lFloat)
    {
        QSLoupeFloatCommit(lFloat);
        QSLoupeFloatPersist(lFloat);
        QSLoupeFloatChanged?.Invoke();
    }

    private static QSLoupeFloat QSLoupeFloatResolve(string lToken) =>
        qLoupeFloatKinds.GetValueOrDefault(lToken, QSLoupeFloat.QSLoupeFloatOwner);

    private void QSLoupeFloatCommit(QSLoupeFloat lFloat) => qLoupeFloat = lFloat;

    private static void QSLoupeFloatPersist(QSLoupeFloat lFloat)
    {
        LPreferenceState lPreference = LPreference.LPreferenceStateCurrent.LPreferenceClone();
        lPreference.LPreferenceLoupeFloat = qLoupeFloatTokens[lFloat];
        LPreference.LPreferenceStateSet(lPreference);
    }

    private QSLoupeButton QSLoupeButtonResolve((QSLoupeFloat, string) lButton) => new(
        lButton.Item1,
        QLocalizationCatalog.QLocalizationTextRead(lButton.Item2),
        lButton.Item1 == qLoupeFloat);
}

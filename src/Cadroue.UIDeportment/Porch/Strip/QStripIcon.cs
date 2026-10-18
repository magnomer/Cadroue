using System.Windows.Media;

namespace Cadroue.UIDeportment;

public static class QStripIcon
{
    private const string QStripAccentKey = "PStripAccent";

    private static readonly IReadOnlyDictionary<string, ImageSource> qStripIcons =
        QStripCatalog.QStripKeysRead().ToDictionary(lKey => lKey, lKey => QStripIconLoad(lKey, null));

    private static readonly Lazy<IReadOnlyDictionary<string, ImageSource>> qStripActiveIcons =
        new(
            () => QStripCatalog.QStripKeysRead().ToDictionary(
                lKey => lKey, lKey => QStripIconLoad(lKey, QContract.QContractSheetFind<Brush>(QStripAccentKey))),
            LazyThreadSafetyMode.PublicationOnly);

    public static ImageSource QStripIconRead(string lKey) => qStripIcons[lKey];

    public static ImageSource QStripIconRead(string lKey, bool lActive) =>
        lActive ? qStripActiveIcons.Value[lKey] : qStripIcons[lKey];

    public static ImageSource? QStripIconFind(string lKey) => qStripIcons.GetValueOrDefault(lKey);

    private static ImageSource QStripIconLoad(string lKey, Brush? lTint) =>
        QIcon.QIconRead($"/PAsset/PTab/P{lKey}Button.svg", lTint);
}

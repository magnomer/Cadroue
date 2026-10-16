using System.Windows.Media;

namespace Cadroue.UIDeportment;

public static class QStripIcon
{
    private const string QStripAccentKey = "PStripAccent";

    private static readonly IReadOnlyDictionary<string, ImageSource> qStripIcons =
        QStrip.QStripKeysRead().ToDictionary(lKey => lKey, lKey => QStripIconLoad(lKey, null));

    private static readonly IReadOnlyDictionary<string, ImageSource> qStripActiveIcons =
        QStrip.QStripKeysRead().ToDictionary(
            lKey => lKey, lKey => QStripIconLoad(lKey, QContract.QContractSheetFind<Brush>(QStripAccentKey)));

    private static readonly IReadOnlyDictionary<bool, IReadOnlyDictionary<string, ImageSource>> qStripIconSets =
        new Dictionary<bool, IReadOnlyDictionary<string, ImageSource>>
        {
            [false] = qStripIcons,
            [true] = qStripActiveIcons,
        };

    public static ImageSource QStripIconRead(string lKey) => qStripIcons[lKey];

    public static ImageSource QStripIconRead(string lKey, bool lActive) => qStripIconSets[lActive][lKey];

    public static ImageSource? QStripIconFind(string lKey) => qStripIcons.GetValueOrDefault(lKey);

    private static ImageSource QStripIconLoad(string lKey, Brush? lTint) =>
        QIcon.QIconRead($"/PAsset/PTab/P{lKey}Button.svg", lTint);
}

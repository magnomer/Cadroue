using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PAsset;

namespace Cadroue.UIVeneer.PPorch;

public sealed class PTabIcon : IValueConverter
{
    private static readonly Brush pTabActiveBrush = PTabActiveCreate();

    private static readonly IReadOnlyDictionary<string, ImageSource> pTabIcons =
        LStrip.LStripKeysRead().ToDictionary(pKey => pKey, pKey => PIcon.PIconRead(PTabPathRead(pKey)));

    private static readonly IReadOnlyDictionary<string, ImageSource> pTabActiveIcons =
        LStrip.LStripKeysRead().ToDictionary(
            pKey => pKey, pKey => PIcon.PIconRead(PTabPathRead(pKey), pTabActiveBrush));

    private static readonly IReadOnlyDictionary<bool, IReadOnlyDictionary<string, ImageSource>> pTabSets =
        new Dictionary<bool, IReadOnlyDictionary<string, ImageSource>>
        {
            [false] = pTabIcons,
            [true] = pTabActiveIcons,
        };

    public bool PTabIconActive { get; set; }

    public static ImageSource PTabIconRead(string pKey) => pTabIcons[pKey];

    public static string PTabPathRead(string pKey) => $"/PAsset/PTab/P{pKey}Button.svg";

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        pTabSets[PTabIconActive][(string)value];

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    private static Brush PTabActiveCreate()
    {
        var pBrush = new SolidColorBrush(Color.FromRgb(0x4C, 0x86, 0xF7));
        pBrush.Freeze();
        return pBrush;
    }
}

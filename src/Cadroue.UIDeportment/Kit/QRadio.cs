using System.Windows;
using System.Windows.Controls;

namespace Cadroue.UIDeportment;

public static class QRadio
{
    private const string QRadioStyle = "PRadio";

    private const string QRadioSingleStyle = "PRadioSingle";

    private const string QRadioFirstStyle = "PRadioFirst";

    private const string QRadioMiddleStyle = "PRadioMiddle";

    private const string QRadioLastStyle = "PRadioLast";

    private const string QRadioSegmentHost = "PRadioSegment";

    public static Border QRadioSegmentBuild(params RadioButton[] lRadios)
    {
        ArgumentNullException.ThrowIfNull(lRadios);

        Border lHost = QContract.QContractSheetFind<Border>(QRadioSegmentHost);
        Panel lStrip = (Panel)lHost.Child;
        foreach ((RadioButton lRadio, int lIndex) in lRadios.Select((lRadio, lIndex) => (lRadio, lIndex)))
        {
            QRadioAttach(lRadio, lIndex == 0, lIndex == lRadios.Length - 1);
            lStrip.Children.Add(lRadio);
        }

        return lHost;
    }

    private static void QRadioAttach(RadioButton lRadio, bool lFirst, bool lLast)
    {
        lRadio.Style = (lFirst, lLast) switch
        {
            (true, true) => QContract.QContractSheetFind<Style>(QRadioSingleStyle),
            (true, false) => QContract.QContractSheetFind<Style>(QRadioFirstStyle),
            (false, true) => QContract.QContractSheetFind<Style>(QRadioLastStyle),
            (false, false) => QContract.QContractSheetFind<Style>(QRadioMiddleStyle),
        };
        QLook.QLookAttach(lRadio, QRadioStyle);
    }
}

using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Cadroue.UIDeportment;

public static class QScrollbar
{
    private const string QScrollbarStyle = "PScrollbar";

    private const string QScrollbarHorizontalStyle = "PScrollbarHorizontal";

    private const string QScrollbarThumbStyle = "PScrollbarThumb";

    private const string QScrollbarThumbPart = "PScrollbarThumb";

    private const string QScrollbarBackPart = "PScrollbarBack";

    private const string QScrollbarAheadPart = "PScrollbarAhead";

    public static void QScrollbarAttach(FrameworkElement lHost)
    {
        ArgumentNullException.ThrowIfNull(lHost);

        lHost.Resources[typeof(ScrollBar)] =
            new Style(typeof(ScrollBar), QContract.QContractSheetFind<Style>(QScrollbarStyle))
            {
                Setters = { new EventSetter(FrameworkElement.LoadedEvent, new RoutedEventHandler(QScrollbarLoad)) },
            };
    }

    private static void QScrollbarLoad(object lSender, RoutedEventArgs lEvent)
    {
        ScrollBar lBar = (ScrollBar)lSender;
        lBar.Style = lBar.Orientation == Orientation.Horizontal
            ? QContract.QContractSheetFind<Style>(QScrollbarHorizontalStyle)
            : QContract.QContractSheetFind<Style>(QScrollbarStyle);
        _ = lBar.ApplyTemplate();

        RepeatButton lBack = (RepeatButton)lBar.Template.FindName(QScrollbarBackPart, lBar);
        RepeatButton lAhead = (RepeatButton)lBar.Template.FindName(QScrollbarAheadPart, lBar);
        lBack.Click += (_, _) => ScrollBar.PageUpCommand.Execute(null, lBack);
        lAhead.Click += (_, _) => ScrollBar.PageDownCommand.Execute(null, lAhead);
        QLook.QLookAttach((Thumb)lBar.Template.FindName(QScrollbarThumbPart, lBar), QScrollbarThumbStyle);
    }
}

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

        Style lStyle = QScrollbarStyleCreate();
        QScrollbarLoadAttach(lStyle);
        QScrollbarStyleSet(lHost, lStyle);
    }

    private static Style QScrollbarStyleCreate() =>
        new(typeof(ScrollBar), QContract.QContractSheetFind<Style>(QScrollbarStyle));

    private static void QScrollbarLoadAttach(Style lStyle) =>
        lStyle.Setters.Add(new EventSetter(FrameworkElement.LoadedEvent, new RoutedEventHandler(QScrollbarLoad)));

    private static void QScrollbarStyleSet(FrameworkElement lHost, Style lStyle) =>
        lHost.Resources[typeof(ScrollBar)] = lStyle;

    private static void QScrollbarLoad(object lSender, RoutedEventArgs lEvent)
    {
        ScrollBar lBar = (ScrollBar)lSender;
        QScrollbarStyleApply(lBar);
        (RepeatButton lBack, RepeatButton lAhead, Thumb lThumb) = QScrollbarPartsBuild(lBar);
        QScrollbarPageAttach(lBack, lAhead);
        QLook.QLookAttach(lThumb, QScrollbarThumbStyle);
    }

    private static void QScrollbarStyleApply(ScrollBar lBar) =>
        lBar.Style = lBar.Orientation == Orientation.Horizontal
            ? QContract.QContractSheetFind<Style>(QScrollbarHorizontalStyle)
            : QContract.QContractSheetFind<Style>(QScrollbarStyle);

    private static (RepeatButton, RepeatButton, Thumb) QScrollbarPartsBuild(ScrollBar lBar)
    {
        _ = lBar.ApplyTemplate();
        RepeatButton lBack = (RepeatButton)lBar.Template.FindName(QScrollbarBackPart, lBar);
        RepeatButton lAhead = (RepeatButton)lBar.Template.FindName(QScrollbarAheadPart, lBar);
        Thumb lThumb = (Thumb)lBar.Template.FindName(QScrollbarThumbPart, lBar);
        return (lBack, lAhead, lThumb);
    }

    private static void QScrollbarPageAttach(RepeatButton lBack, RepeatButton lAhead)
    {
        lBack.Click += (_, _) => ScrollBar.PageUpCommand.Execute(null, lBack);
        lAhead.Click += (_, _) => ScrollBar.PageDownCommand.Execute(null, lAhead);
    }
}

using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Cadroue.UIDeportment;

public static class QScrollbar
{
    private const string QScrollbarStyle = "PScrollbar";

    private const string QScrollbarHorizontalStyle = "PScrollbarHorizontal";

    private const string QScrollbarThumbPart = "PScrollbarThumb";

    private const string QScrollbarBackPart = "PScrollbarBack";

    private const string QScrollbarAheadPart = "PScrollbarAhead";

    internal static void QScrollbarHook() =>
        EventManager.RegisterClassHandler(
            typeof(ScrollBar),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(QScrollbarLoad),
            true);

    // Every scrollbar below the host takes a style whose own Loaded setter wires it, so Loaded reaches it whoever
    // holds the host; where a reached surface also runs the class handler, the load guard keeps the work to once.
    public static void QScrollbarAttach(FrameworkElement lHost)
    {
        ArgumentNullException.ThrowIfNull(lHost);

        Style lStyle = new(typeof(ScrollBar), QContract.QContractSheetFind<Style>(QScrollbarStyle));
        lStyle.Setters.Add(new EventSetter(FrameworkElement.LoadedEvent, new RoutedEventHandler(QScrollbarLoad)));
        lHost.Resources[typeof(ScrollBar)] = lStyle;
    }

    // The horizontal style is BasedOn the vertical one, so the work keyed on the base runs once either way.
    private static void QScrollbarLoad(object lSender, RoutedEventArgs lEvent)
    {
        ScrollBar lBar = (ScrollBar)lSender;
        if (QLook.QLookStyleResolve(lBar, [QScrollbarStyle]) is null)
        {
            return;
        }

        QScrollbarStyleApply(lBar);
        (RepeatButton lBack, RepeatButton lAhead, Thumb lThumb) = QScrollbarPartsBuild(lBar);
        QScrollbarPageAttach(lBack, lAhead);
        QLook.QLookAttach(lThumb);
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

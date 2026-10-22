using Cadroue.Infrastructure;
using System.Windows;

namespace Cadroue.UIDeportment;

public sealed class QSFlyleafAnswer
{
    private readonly Delegate[] qsFlyleafAnswer;

    public QSFlyleafAnswer(
        QSOptions options,
        Window owner,
        Action<Window, string, string> announcement,
        Action<Window, string, string> warning)
        : this(
            options,
            (title, message) => announcement(owner, title, message),
            (title, message) => warning(owner, title, message))
    {
    }

    public QSFlyleafAnswer(
        QSOptions options,
        Action<string, string> announcement,
        Action<string, string> warning)
        : this(
            () => _ = options.QSOptionsFolderOpen(LFlyleafFolder.LFlyleafRootRead(), string.Empty),
            announcement,
            warning)
    {
    }

    public QSFlyleafAnswer(
        Action open,
        Action<string, string> announcement,
        Action<string, string> warning)
    {
        qsFlyleafAnswer = [open, announcement, warning];
    }

    public void QSFlyleafOpen() => ((Action)qsFlyleafAnswer[0])();

    public void QSFlyleafShow(bool success, string title, string message) =>
        ((Action<string, string>)qsFlyleafAnswer[success ? 1 : 2])(title, message);
}

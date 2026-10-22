using Cadroue.Infrastructure;
using System.Windows;

namespace Cadroue.UIDeportment;

public sealed class QSMpvAnswer
{
    private readonly Delegate[] qsMpvAnswer;

    public QSMpvAnswer(
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

    public QSMpvAnswer(
        QSOptions options,
        Action<string, string> announcement,
        Action<string, string> warning)
        : this(
            () => _ = options.QSOptionsFolderOpen(LMpvInstall.LMpvRootRead(), string.Empty),
            announcement,
            warning)
    {
    }

    public QSMpvAnswer(
        Action open,
        Action<string, string> announcement,
        Action<string, string> warning)
    {
        qsMpvAnswer = [open, announcement, warning];
    }

    public void QSMpvOpen() => ((Action)qsMpvAnswer[0])();

    public void QSMpvShow(bool success, string title, string message) =>
        ((Action<string, string>)qsMpvAnswer[success ? 1 : 2])(title, message);
}

using System.Windows;

namespace Cadroue.UIDeportment;

public sealed class QSWorkspaceAnswer
{
    private readonly Delegate[] qsWorkspaceAnswer;

    public QSWorkspaceAnswer(
        Window owner,
        Func<Window, string, string, string, bool> confirmation,
        Action<Window, string, string> warning,
        Action<Window, string, string> announcement)
        : this(
            (title, initial, continuation) =>
            {
                var dialog = new Microsoft.Win32.OpenFolderDialog
                {
                    Title = title,
                    InitialDirectory = initial
                };
                if (dialog.ShowDialog() == true)
                {
                    continuation(dialog.FolderName);
                }
            },
            (title, message, acceptance, continuation) =>
            {
                if (confirmation(owner, title, message, acceptance))
                {
                    continuation();
                }
            },
            (title, message) => warning(owner, title, message),
            (title, message) => announcement(owner, title, message))
    {
    }

    public QSWorkspaceAnswer(
        Action<string, string, Action<string>> folder,
        Action<string, string, string, Action> confirmation,
        Action<string, string> warning,
        Action<string, string> announcement)
    {
        qsWorkspaceAnswer = [folder, confirmation, warning, announcement];
    }

    public void QSWorkspaceFolderShow(string title, string initial, Action<string> continuation) =>
        ((Action<string, string, Action<string>>)qsWorkspaceAnswer[0])(title, initial, continuation);

    public void QSWorkspaceConfirmationShow(string title, string message, string acceptance, Action continuation) =>
        ((Action<string, string, string, Action>)qsWorkspaceAnswer[1])(title, message, acceptance, continuation);

    public void QSWorkspaceWarningShow(string title, string message) =>
        ((Action<string, string>)qsWorkspaceAnswer[2])(title, message);

    public void QSWorkspaceAnnouncementShow(string title, string message) =>
        ((Action<string, string>)qsWorkspaceAnswer[3])(title, message);
}

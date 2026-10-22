using Cadroue.Conduct;
using Microsoft.Win32;
using System.Windows;

namespace Cadroue.UIDeportment;

public sealed class QSRendererAnswer
{
    private readonly Delegate[] qsRendererAnswer;

    public QSRendererAnswer(
        QSOptions options,
        Window owner,
        Action<Window, Func<QSDiagnosis>> diagnosis)
        : this(
            path =>
            {
                var dialog = new OpenFolderDialog
                {
                    Title = QLocalizationCatalog.QLocalizationTextRead("Options.System.ChooseFFmpeg"),
                    InitialDirectory = path
                };
                return dialog.ShowDialog() == true ? dialog.FolderName : null;
            },
            path => _ = options.QSOptionsFolderOpen(path, string.Empty),
            () => diagnosis(owner, options.QSOptionsDiagnosisCreate))
    {
    }

    public QSRendererAnswer(
        Func<string, string?> folder,
        Action<string> open,
        Action diagnosis)
    {
        qsRendererAnswer = [folder, open, diagnosis];
    }

    public string? QSRendererFolderRead(string path) =>
        ((Func<string, string?>)qsRendererAnswer[0])(path);

    public void QSRendererPathOpen(string path) => ((Action<string>)qsRendererAnswer[1])(path);

    public void QSRendererDiagnosisShow() => ((Action)qsRendererAnswer[2])();
}

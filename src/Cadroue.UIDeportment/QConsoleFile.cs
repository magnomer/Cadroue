using Cadroue.Conduct;
using Microsoft.Win32;

namespace Cadroue.UIDeportment;

// The scene strip's file exchange: builds the export and import dialogs, hands their answer to the scene file gate
// and words the gate's failure as a warning.
public sealed class QConsoleFile
{
    private readonly CConsoleFile qConsoleFileGate;

    public QConsoleFile(CConsoleFile lGate)
    {
        qConsoleFileGate = lGate;
    }

    public event Action<string, string>? QConsoleWarningShown;

    public SaveFileDialog QConsoleExportBuild(string? lText) => new()
    {
        Title = QLocalizationCatalog.QLocalizationTextRead("Console.Scene.Dialog.Export"),
        Filter = QLocalizationCatalog.QLocalizationTextRead("Console.Scene.Dialog.Filter"),
        DefaultExt = qConsoleFileGate.CConsoleSceneExtension,
        AddExtension = true,
        FileName = qConsoleFileGate.CConsoleSceneResolve(
            lText, QLocalizationCatalog.QLocalizationTextRead("Console.Scene.DefaultName"))
    };

    public OpenFileDialog QConsoleImportBuild() => new()
    {
        Title = QLocalizationCatalog.QLocalizationTextRead("Console.Scene.Dialog.Import"),
        Filter = QLocalizationCatalog.QLocalizationTextRead("Console.Scene.Dialog.Filter"),
        DefaultExt = qConsoleFileGate.CConsoleSceneExtension,
        CheckFileExists = true
    };

    public void QConsoleExportCommit(bool? lConfirmed, string lPath, string? lText) =>
        QConsoleErrorRaise(
            qConsoleFileGate.CConsoleScenePersist(
                lConfirmed, lPath, lText, QLocalizationCatalog.QLocalizationTextRead("Console.Scene.DefaultName")),
            "Console.Scene.Dialog.Export");

    public void QConsoleImportCommit(bool? lConfirmed, string lPath) =>
        QConsoleErrorRaise(
            qConsoleFileGate.CConsoleSceneImport(
                lConfirmed, lPath, QLocalizationCatalog.QLocalizationTextRead("Console.Scene.ImportedName")),
            "Console.Scene.Dialog.Import");

    private void QConsoleErrorRaise(CConsoleText? lMessage, string lTitleKey)
    {
        if (lMessage is not null)
        {
            QConsoleWarningShown?.Invoke(
                QLocalizationCatalog.QLocalizationTextRead(lTitleKey), QConsoleLine.QConsoleTextRead(lMessage));
        }
    }
}

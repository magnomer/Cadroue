using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;
using System.Windows;
using System.Windows.Controls;

namespace Cadroue.UIDeportment;

public sealed class QSOptionsSystem
{
    private readonly CSOptions csOptions;
    private readonly QSWorkspace qsWorkspace;
    private readonly QSRenderer qsRenderer;
    private readonly QSOptionsField qsOptionsField = new();
    private FrameworkElement? psSystem;
    private QSWorkspaceAnswer? qsOptionsSystemAnswer;
    private Action? qsOptionsSystemNotice;
    private Action[] qsOptionsSystemPending = [];
    private Func<CSOptionsSystem>? qsOptionsSystemValue;

    public QSOptionsSystem(CSOptions options, QSWorkspace workspace, QSRenderer renderer)
    {
        csOptions = options;
        qsWorkspace = workspace;
        qsRenderer = renderer;
    }

    public FrameworkElement QSOptionsSystemAttach(
        FrameworkElement surface,
        QSWorkspaceAnswer workspace,
        QSRendererAnswer renderer,
        QSOptionsMode mode,
        Func<Border, string> choice)
    {
        QSOptionsSystemDetach();
        psSystem = surface;
        qsOptionsSystemAnswer = workspace;
        qsOptionsSystemPending = [QSOptionsSystemClear];
        LOptionsSnapshot snapshot = ((LOptionsPort)csOptions).CSOptionsSnapshotRead();
        _ = qsWorkspace.QSWorkspaceAttach(
            QContract.QContractFind<FrameworkElement>(surface, "PSSystemWorkspace"),
            snapshot.CSOptionsWorkspace,
            workspace,
            out Button done);
        QContract.QContractFind<ContentControl>(surface, "PSSystemFinished").Content = done;
        qsRenderer.QSRendererAttach(QContract.QContractFind<FrameworkElement>(surface, "PSSystemRenderer"), renderer);

        QContract.QContractFind<TextBlock>(surface, "PSSystemHistory").Text =
            QLocalizationCatalog.QLocalizationTextRead("Options.System.WorkRecord");
        QContract.QContractFind<TextBlock>(surface, "PSSystemUpkeep").Text =
            QLocalizationCatalog.QLocalizationTextRead("Options.System.Maintenance");
        QContract.QContractFind<TextBlock>(surface, "PSSystemExpiry").Text =
            QLocalizationCatalog.QLocalizationTextRead("Options.System.AutoDelete");
        QContract.QContractFind<TextBlock>(surface, "PSSystemAge").Text =
            QLocalizationCatalog.QLocalizationTextRead("Options.System.CleanupDays");
        QContract.QContractFind<TextBlock>(surface, "PSSystemCaution").Text =
            QLocalizationCatalog.QLocalizationTextRead("Options.System.CleanupNotice");
        QContract.QContractFind<TextBlock>(surface, "PSSystemLedger").Text =
            QLocalizationCatalog.QLocalizationTextRead("Options.System.FileRecord");
        QContract.QContractFind<TextBlock>(surface, "PSSystemStorage").Text =
            QLocalizationCatalog.QLocalizationTextRead("Options.System.Location");
        QContract.QContractFind<TextBlock>(surface, "PSSystemScope").Text =
            QLocalizationCatalog.QLocalizationTextRead("Options.System.FileRecordScope");
        QContract.QContractFind<TextBlock>(surface, "PSSystemService").Text =
            QLocalizationCatalog.QLocalizationTextRead("Options.System.Maintenance");

        CheckBox purge = QContract.QContractFind<CheckBox>(surface, "PSSystemPurge");
        QSOptionsField.QSOptionsFieldPrepare(
            purge,
            QLocalizationCatalog.QLocalizationTextRead("Options.System.CleanupCheck"),
            snapshot.CSOptionsCleanup);
        Slider days = QContract.QContractFind<Slider>(surface, "PSSystemSlider");
        QSOptionsField.QSOptionsFieldPrepare(days, snapshot.CSOptionsCleanupDays);
        qsOptionsField.QSOptionsFieldAttach(
            days,
            QContract.QContractFind<TextBlock>(surface, "PSSystemReadout"),
            QLocalizationCatalog.QLocalizationTextRead("Options.System.CleanupUnit"));
        QSOptionsSystemToggle(purge, new RoutedEventArgs());
        purge.Checked += QSOptionsSystemToggle;
        purge.Unchecked += QSOptionsSystemToggle;

        ContentControl location = QContract.QContractFind<ContentControl>(surface, "PSSystemLocation");
        QSOptionsSystemAttach(location, csOptions.CSOptionsRecordRead(), mode, choice);
        Button eraser = QContract.QContractFind<Button>(surface, "PSSystemEraser");
        eraser.Content = QLocalizationCatalog.QLocalizationTextRead("Options.System.ClearFileRecord");
        eraser.Click += QSOptionsSystemClear;
        qsOptionsSystemValue = () => new CSOptionsSystem(
            choice((Border)location.Content),
            QSOptionsField.QSOptionsFieldRead(purge),
            days.Value,
            qsRenderer.QSRendererApply());
        return surface;
    }

    private void QSOptionsSystemAttach(
        ContentControl location,
        string storage,
        QSOptionsMode mode,
        Func<Border, string> choice)
    {
        Action notice = () => QSOptionsSystemShow(choice((Border)location.Content));
        location.Content = mode(
            storage,
            notice,
            out _,
            new LLocalizationChoice("FileLocation", "Options.Record.FileLocation"),
            new LLocalizationChoice("Workspace", "Options.Record.Workspace"));
        qsOptionsSystemNotice = notice;
        notice();
    }

    public void QSOptionsSystemDetach()
    {
        if (psSystem is null)
        {
            return;
        }
        CheckBox purge = QContract.QContractFind<CheckBox>(psSystem, "PSSystemPurge");
        purge.Checked -= QSOptionsSystemToggle;
        purge.Unchecked -= QSOptionsSystemToggle;
        QContract.QContractFind<Button>(psSystem, "PSSystemEraser").Click -= QSOptionsSystemClear;
        qsOptionsField.QSOptionsFieldDetach();
        qsOptionsSystemPending[0] = static () => { };
        qsWorkspace.QSWorkspaceDetach();
        qsRenderer.QSRendererDetach();
        psSystem = null;
        qsOptionsSystemAnswer = null;
        qsOptionsSystemNotice = null;
    }

    public CSOptionsSystem QSOptionsSystemRead() => qsOptionsSystemValue!();

    public void QSOptionsSystemUpdate()
    {
        qsWorkspace.QSWorkspaceUpdate();
        if (qsOptionsSystemNotice is not null)
        {
            qsOptionsSystemNotice();
        }
    }

    private void QSOptionsSystemToggle(object sender, RoutedEventArgs _) =>
        QContract.QContractFind<UIElement>(psSystem!, "PSSystemRetention").Visibility =
            QSOptionsField.QSOptionsFieldRead(QContract.QContractFind<CheckBox>(psSystem!, "PSSystemPurge"))
                ? Visibility.Visible
                : Visibility.Collapsed;

    private void QSOptionsSystemShow(string storage)
    {
        (string key, string location) = csOptions.CSOptionsRecordResolve(storage);
        QContract.QContractFind<TextBlock>(psSystem!, "PSSystemBeside").Text =
            QLocalizationCatalog.QLocalizationFormat(key, location);
    }

    private void QSOptionsSystemClear(object sender, RoutedEventArgs _)
    {
        QSWorkspaceAnswer answer = qsOptionsSystemAnswer!;
        csOptions.CSOptionsSidecarCheck(
            QSOptionsSystemClear,
            () => answer.QSWorkspaceAnnouncementShow(
                QLocalizationCatalog.QLocalizationTextRead("Options.System.ClearFileRecord"),
                QLocalizationCatalog.QLocalizationTextRead("Options.System.NoFileRecord")));
    }

    private void QSOptionsSystemClear(string folder)
    {
        Action[] pending = qsOptionsSystemPending;
        qsOptionsSystemAnswer!.QSWorkspaceConfirmationShow(
            QLocalizationCatalog.QLocalizationTextRead("Options.System.ClearFileRecord"),
            QLocalizationCatalog.QLocalizationFormat("Options.System.ClearFileRecordConfirm", folder),
            QLocalizationCatalog.QLocalizationTextRead("Terms.Delete"),
            () => pending[0]());
    }

    private void QSOptionsSystemClear()
    {
        int removed = csOptions.CSOptionsSidecarClear();
        qsWorkspace.QSWorkspaceSizeUpdate();
        qsOptionsSystemAnswer!.QSWorkspaceAnnouncementShow(
            QLocalizationCatalog.QLocalizationTextRead("Options.System.ClearFileRecord"),
            QLocalizationCatalog.QLocalizationFormat("Options.System.FileRecordsRemoved", removed));
    }
}

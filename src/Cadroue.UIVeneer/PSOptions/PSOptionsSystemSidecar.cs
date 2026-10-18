using System.Windows;
using System.Windows.Controls;
using Cadroue.Core;
using Cadroue.Application;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed class PSOptionsSystemSidecar
{
    private static readonly LLocalizationChoice[] PSOptionsRecordItems =
    {
        new("FileLocation", "Options.Record.FileLocation"),
        new("Workspace", "Options.Record.Workspace")
    };

    private readonly QSOptions lsOptions;
    private readonly LPreferenceState lsOptionsDraft;
    private readonly Window psOptionsWindow;
    private readonly Action psSidecarSizeHandler;
    private readonly Border psOptionsRecordMode;
    private Action? psOptionsRecordNotice;

    internal PSOptionsSystemSidecar(QSOptions lsOptionsOwner, Window pWindow, Action pSizeHandler)
    {
        lsOptions = lsOptionsOwner;
        lsOptionsDraft = lsOptions.QSOptionsDraft;
        psOptionsWindow = pWindow;
        psSidecarSizeHandler = pSizeHandler;
        psOptionsRecordMode = PSMode.PSModeBuild(
            PSOptionsRecordResolve(),
            () => psOptionsRecordNotice?.Invoke(),
            PSOptionsRecordItems);
    }

    internal void PSSidecarDraftUpdate() =>
        PSSystemDraftUpdate(PSOptionsField.PSOptionsModeCheck(psOptionsRecordMode, "Workspace"));

    internal void PSSidecarNoticeUpdate() => psOptionsRecordNotice?.Invoke();

    internal UIElement PSSystemRecordBuild()
    {
        Button pRecordClear = PSInline.PSInlineButtonBuild(
            QLocalizationCatalog.QLocalizationTextRead("Options.System.ClearFileRecord"),
            190,
            new Thickness(0));
        PSSystemRecordAttach(pRecordClear);
        var pRecordBesideNotice = (TextBlock)PSNotice.PSNoticeBuild(string.Empty);
        PSSystemNoticeUpdate(pRecordBesideNotice);
        PSSystemNoticeAttach(pRecordBesideNotice);
        return PSSystemPlateBuild(pRecordClear, pRecordBesideNotice);
    }

    private string PSOptionsRecordResolve() =>
        lsOptionsDraft.LPreferenceRecordWorkspace ? "Workspace" : "FileLocation";

    private void PSSystemRecordAttach(Button pRecordClear) => pRecordClear.Click += (_, _) => PSSystemRecordClear();

    private void PSSystemNoticeUpdate(TextBlock pRecordBesideNotice)
    {
        PSSystemDraftUpdate(PSOptionsField.PSOptionsModeCheck(psOptionsRecordMode, "Workspace"));
        PSSystemNoticeShow(
            pRecordBesideNotice,
            QLocalizationCatalog.QLocalizationFormat(
                lsOptions.QSOptionsRecordResolve(), QSOptions.QSOptionsSidecarRead()));
    }

    private void PSSystemDraftUpdate(bool pWorkspace) => lsOptionsDraft.LPreferenceRecordWorkspace = pWorkspace;

    private static void PSSystemNoticeShow(TextBlock pRecordBesideNotice, string pText) =>
        pRecordBesideNotice.Text = pText;

    private void PSSystemNoticeAttach(TextBlock pRecordBesideNotice) =>
        psOptionsRecordNotice = () => PSSystemNoticeUpdate(pRecordBesideNotice);

    private UIElement PSSystemPlateBuild(Button pRecordClear, TextBlock pRecordBesideNotice) =>
        PSPlate.PSPlateBuild(QLocalizationCatalog.QLocalizationTextRead("Options.System.FileRecord"),
            PSField.PSFieldBuild(QLocalizationCatalog.QLocalizationTextRead("Options.System.Location"),
                psOptionsRecordMode),
            pRecordBesideNotice,
            PSNotice.PSNoticeBuild(QLocalizationCatalog.QLocalizationTextRead("Options.System.FileRecordScope")),
            PSField.PSFieldBuild(QLocalizationCatalog.QLocalizationTextRead("Options.System.Maintenance"),
                PSOptionsField.PSSystemRowBuild(pRecordClear)));

    private void PSSystemRecordClear()
    {
        string pRecordFolder = Cadroue.Infrastructure.LSidecarPath.LSidecarFolderRead();
        if (!lsOptions.QSOptionsFolderExist(pRecordFolder))
        {
            PSAnnouncement.PSAnnouncementShow(
                psOptionsWindow,
                QLocalizationCatalog.QLocalizationTextRead("Options.System.ClearFileRecord"),
                QLocalizationCatalog.QLocalizationTextRead("Options.System.NoFileRecord"));
            return;
        }

        if (!PSAlert.PSAlertConfirm(
                psOptionsWindow,
                QLocalizationCatalog.QLocalizationTextRead("Options.System.ClearFileRecord"),
                QLocalizationCatalog.QLocalizationFormat("Options.System.ClearFileRecordConfirm", pRecordFolder),
                QLocalizationCatalog.QLocalizationTextRead("Terms.Delete")))
        {
            return;
        }

        int pRemoved = Cadroue.Infrastructure.LSidecarPath.LSidecarFolderClear();
        psSidecarSizeHandler();
        PSAnnouncement.PSAnnouncementShow(
            psOptionsWindow,
            QLocalizationCatalog.QLocalizationTextRead("Options.System.ClearFileRecord"),
            QLocalizationCatalog.QLocalizationFormat("Options.System.FileRecordsRemoved", pRemoved));
    }
}

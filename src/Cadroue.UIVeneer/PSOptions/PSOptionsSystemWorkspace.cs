using System.Windows;
using System.Windows.Controls;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed partial class PSOptions
{
    private readonly Border psOptionsRecordMode;
    private Action? psOptionsRecordNotice;
    private TextBlock? psWorkspaceSize;
    private UIElement? psSystemMaintenanceNotice;
    private readonly List<Button> psSystemMaintenanceButtons = new();

    private UIElement PSSystemRecordBuild()
    {
        Button pRecordClear = PSInline.PSInlineButtonBuild(
            QLocalizationCatalog.QLocalizationTextRead("Options.System.ClearFileRecord"),
            190,
            new Thickness(0));
        pRecordClear.Click += (_, _) => PSSystemRecordClear();

        var pRecordButtonRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };
        pRecordButtonRow.Children.Add(pRecordClear);

        var pRecordBesideNotice = (TextBlock)PSNotice.PSNoticeBuild(string.Empty);
        void PSSystemNoticeUpdate()
        {
            lsOptionsDraft.LPreferenceRecordWorkspace =
                string.Equals(PSField.PSModeTextRead(psOptionsRecordMode), "Workspace", StringComparison.Ordinal);
            pRecordBesideNotice.Text = QLocalizationCatalog.QLocalizationFormat(
                lsOptions.QSOptionsRecordResolve(), QSOptions.QSOptionsSidecarRead());
        }
        PSSystemNoticeUpdate();
        psOptionsRecordNotice = PSSystemNoticeUpdate;

        return PSPlate.PSPlateBuild(QLocalizationCatalog.QLocalizationTextRead("Options.System.FileRecord"),
            PSField.PSFieldBuild(QLocalizationCatalog.QLocalizationTextRead("Options.System.Location"),
                psOptionsRecordMode),
            pRecordBesideNotice,
            PSNotice.PSNoticeBuild(QLocalizationCatalog.QLocalizationTextRead("Options.System.FileRecordScope")),
            PSField.PSFieldBuild(QLocalizationCatalog.QLocalizationTextRead("Options.System.Maintenance"),
                pRecordButtonRow));
    }

    private void PSSystemRecordClear()
    {
        string pRecordFolder = Cadroue.Infrastructure.LSidecarPath.LSidecarFolderRead();
        if (!lsOptions.QSOptionsFolderExist(pRecordFolder))
        {
            PSAnnouncement.PSAnnouncementShow(
                this,
                QLocalizationCatalog.QLocalizationTextRead("Options.System.ClearFileRecord"),
                QLocalizationCatalog.QLocalizationTextRead("Options.System.NoFileRecord"));
            return;
        }

        if (!PSAlert.PSAlertConfirm(
                this,
                QLocalizationCatalog.QLocalizationTextRead("Options.System.ClearFileRecord"),
                QLocalizationCatalog.QLocalizationFormat("Options.System.ClearFileRecordConfirm", pRecordFolder),
                QLocalizationCatalog.QLocalizationTextRead("Terms.Delete")))
        {
            return;
        }

        int pRemoved = Cadroue.Infrastructure.LSidecarPath.LSidecarFolderClear();
        PSWorkspaceSizeUpdate();
        PSAnnouncement.PSAnnouncementShow(
            this,
            QLocalizationCatalog.QLocalizationTextRead("Options.System.ClearFileRecord"),
            QLocalizationCatalog.QLocalizationFormat("Options.System.FileRecordsRemoved", pRemoved));
    }

    private void PSWorkspaceSizeUpdate()
    {
        if (psWorkspaceSize is not null)
        {
            psWorkspaceSize.Text = PSSystemSizeFormat(LDepot.LDepotSizeRead());
        }
    }

    private void PSSystemMaintenanceUpdate()
    {
        bool pApplied = PSWorkspaceAppliedCheck();
        foreach (Button pButton in psSystemMaintenanceButtons)
        {
            pButton.IsEnabled = pApplied;
        }

        if (psSystemMaintenanceNotice is not null)
        {
            psSystemMaintenanceNotice.Visibility = pApplied ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    private bool PSWorkspaceAppliedCheck() => QSOptions.QSOptionsAppliedCheck(psWorkspaceBox.Text);

    private void PSSystemDoneClear()
    {
        if (!PSWorkspaceAppliedCheck())
        {
            return;
        }

        if (!PSAlert.PSAlertConfirm(
                this,
                QLocalizationCatalog.QLocalizationTextRead("Options.System.ClearDoneTitle"),
                QLocalizationCatalog.QLocalizationFormat("Options.System.ClearDoneConfirm", LDepot.LDepotRootRead()),
                QLocalizationCatalog.QLocalizationTextRead("Terms.Delete")))
        {
            return;
        }

        int pRemoved = LDepot.LDepotFolderClear(
            LDepotFolder.LDepotFolderScheduled,
            LDepotFolder.LDepotFolderDone,
            LDepotFolder.LDepotFolderFailed,
            LDepotFolder.LDepotFolderCancelled);
        LDepotIndex.LDepotIndexRebuild();
        LDepotIndex.LDepotIndexCompact();
        PSWorkspaceSizeUpdate();
        PSAnnouncement.PSAnnouncementShow(
            this,
            QLocalizationCatalog.QLocalizationTextRead("Options.System.ClearDoneTitle"),
            QLocalizationCatalog.QLocalizationFormat("Options.System.WorkRecordsRemoved", pRemoved));
    }

    private void PSWorkspaceClear()
    {
        if (!PSWorkspaceAppliedCheck())
        {
            return;
        }

        string pWorkspaceRoot = LDepot.LDepotRootRead();
        if (LDepotRelocation.LDepotRunningCheck(pWorkspaceRoot))
        {
            PSWarning.PSWarningShow(
                this,
                QLocalizationCatalog.QLocalizationTextRead("Options.System.ClearWorkspaceTitle"),
                QLocalizationCatalog.QLocalizationTextRead("Options.System.WorkspaceRunning"));
            return;
        }

        if (!PSAlert.PSAlertConfirm(
                this,
                QLocalizationCatalog.QLocalizationTextRead("Options.System.ClearWorkspaceTitle"),
                QLocalizationCatalog.QLocalizationFormat("Options.System.ClearWorkspaceConfirm", pWorkspaceRoot),
                QLocalizationCatalog.QLocalizationTextRead("Terms.Reset")))
        {
            return;
        }

        LDepot.LDepotWorkspaceReset();
        LDepotIndex.LDepotIndexRebuild();
        LDepotIndex.LDepotIndexCompact();
        PSWorkspaceSizeUpdate();
        PSAnnouncement.PSAnnouncementShow(
            this,
            QLocalizationCatalog.QLocalizationTextRead("Options.System.ClearWorkspaceTitle"),
            QLocalizationCatalog.QLocalizationTextRead("Options.System.WorkspaceReset"));
    }

    private static string PSSystemSizeFormat(long pBytes)
    {
        string[] pUnits = { "B", "KB", "MB", "GB", "TB" };
        double pValue = pBytes;
        int pUnitIndex = 0;
        while (pValue >= 1024 && pUnitIndex < pUnits.Length - 1)
        {
            pValue /= 1024;
            pUnitIndex++;
        }

        return pUnitIndex == 0 ? $"{pBytes} {pUnits[0]}" : $"{pValue:0.##} {pUnits[pUnitIndex]}";
    }
}

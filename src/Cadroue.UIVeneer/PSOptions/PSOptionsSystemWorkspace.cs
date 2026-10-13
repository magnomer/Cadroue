using System.Windows;
using System.Windows.Controls;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.Application;
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
            LLocalization.LLocalizationTextRead("Options.System.ClearFileRecord"),
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
            pRecordBesideNotice.Text = LLocalization.LLocalizationFormat(
                lsOptions.QSOptionsRecordResolve(), QSOptions.QSOptionsSidecarRead());
        }
        PSSystemNoticeUpdate();
        psOptionsRecordNotice = PSSystemNoticeUpdate;

        return PSPlate.PSPlateBuild(LLocalization.LLocalizationTextRead("Options.System.FileRecord"),
            PSField.PSFieldBuild(LLocalization.LLocalizationTextRead("Options.System.Location"), psOptionsRecordMode),
            pRecordBesideNotice,
            PSNotice.PSNoticeBuild(LLocalization.LLocalizationTextRead("Options.System.FileRecordScope")),
            PSField.PSFieldBuild(LLocalization.LLocalizationTextRead("Options.System.Maintenance"), pRecordButtonRow));
    }

    private void PSSystemRecordClear()
    {
        string pRecordFolder = Cadroue.Infrastructure.LSidecarPath.LSidecarFolderRead();
        if (!lsOptions.QSOptionsFolderExist(pRecordFolder))
        {
            PSAnnouncement.PSAnnouncementShow(
                this,
                LLocalization.LLocalizationTextRead("Options.System.ClearFileRecord"),
                LLocalization.LLocalizationTextRead("Options.System.NoFileRecord"));
            return;
        }

        if (!PSAlert.PSAlertConfirm(
                this,
                LLocalization.LLocalizationTextRead("Options.System.ClearFileRecord"),
                LLocalization.LLocalizationFormat("Options.System.ClearFileRecordConfirm", pRecordFolder),
                LLocalization.LLocalizationTextRead("Terms.Delete")))
        {
            return;
        }

        int pRemoved = Cadroue.Infrastructure.LSidecarPath.LSidecarFolderClear();
        PSWorkspaceSizeUpdate();
        PSAnnouncement.PSAnnouncementShow(
            this,
            LLocalization.LLocalizationTextRead("Options.System.ClearFileRecord"),
            LLocalization.LLocalizationFormat("Options.System.FileRecordsRemoved", pRemoved));
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
                LLocalization.LLocalizationTextRead("Options.System.ClearDoneTitle"),
                LLocalization.LLocalizationFormat("Options.System.ClearDoneConfirm", LDepot.LDepotRootRead()),
                LLocalization.LLocalizationTextRead("Terms.Delete")))
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
            LLocalization.LLocalizationTextRead("Options.System.ClearDoneTitle"),
            LLocalization.LLocalizationFormat("Options.System.WorkRecordsRemoved", pRemoved));
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
                LLocalization.LLocalizationTextRead("Options.System.ClearWorkspaceTitle"),
                LLocalization.LLocalizationTextRead("Options.System.WorkspaceRunning"));
            return;
        }

        if (!PSAlert.PSAlertConfirm(
                this,
                LLocalization.LLocalizationTextRead("Options.System.ClearWorkspaceTitle"),
                LLocalization.LLocalizationFormat("Options.System.ClearWorkspaceConfirm", pWorkspaceRoot),
                LLocalization.LLocalizationTextRead("Terms.Reset")))
        {
            return;
        }

        LDepot.LDepotWorkspaceReset();
        LDepotIndex.LDepotIndexRebuild();
        LDepotIndex.LDepotIndexCompact();
        PSWorkspaceSizeUpdate();
        PSAnnouncement.PSAnnouncementShow(
            this,
            LLocalization.LLocalizationTextRead("Options.System.ClearWorkspaceTitle"),
            LLocalization.LLocalizationTextRead("Options.System.WorkspaceReset"));
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

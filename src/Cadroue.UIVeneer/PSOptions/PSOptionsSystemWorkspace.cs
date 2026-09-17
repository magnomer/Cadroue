using Cadroue.UIVeneer.PSCasement;
using System.Windows;
using System.Windows.Controls;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.Application;

using static Cadroue.UIVeneer.PSCasement.PSField;
using static Cadroue.UIVeneer.PSCasement.PSInline;
using static Cadroue.UIVeneer.PSCasement.PSNotice;
using static Cadroue.UIVeneer.PSCasement.PSPlate;

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
        Button pRecordClear = PSInlineButtonBuild(
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

        var pRecordBesideNotice = (TextBlock)PSNoticeBuild(
            LLocalization.LLocalizationTextRead("Options.System.FileRecordBeside"));
        void PSSystemNoticeUpdate()
        {
            bool pWorkspace = string.Equals(PSModeTextRead(psOptionsRecordMode), "Workspace", StringComparison.Ordinal);
            string pRecordWorkspacePath = System.IO.Path.Combine(
                LDepot.LDepotRootRead(),
                Cadroue.Infrastructure.LSidecarStore.LSidecarRecordFolder);
            pRecordBesideNotice.Text = pWorkspace
                ? LLocalization.LLocalizationFormat("Options.System.FileRecordWorkspace", pRecordWorkspacePath)
                : LLocalization.LLocalizationTextRead("Options.System.FileRecordBeside");
        }
        PSSystemNoticeUpdate();
        psOptionsRecordNotice = PSSystemNoticeUpdate;

        return PSPlateBuild(LLocalization.LLocalizationTextRead("Options.System.FileRecord"),
            PSFieldBuild(LLocalization.LLocalizationTextRead("Options.System.Location"), psOptionsRecordMode),
            pRecordBesideNotice,
            PSNoticeBuild(LLocalization.LLocalizationTextRead("Options.System.FileRecordScope")),
            PSFieldBuild(LLocalization.LLocalizationTextRead("Options.System.Maintenance"), pRecordButtonRow));
    }

    private void PSSystemRecordClear()
    {
        string pRecordFolder = Cadroue.Infrastructure.LSidecarStore.LSidecarFolderRead();
        if (!LUsher.LUsherFolderExist(pRecordFolder))
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

        int pRemoved = Cadroue.Infrastructure.LSidecarStore.LSidecarFolderClear();
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

    private bool PSWorkspaceAppliedCheck() =>
        string.Equals(
            LDepot.LDepotRootResolve(psWorkspaceBox.Text),
            LDepot.LDepotRootRead(),
            StringComparison.OrdinalIgnoreCase);

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
        if (LDepot.LDepotRunningCheck(pWorkspaceRoot))
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

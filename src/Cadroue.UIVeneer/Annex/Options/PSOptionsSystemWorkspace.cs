using System.Windows;
using System.Windows.Controls;
using Cadroue.Infrastructure;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed class PSOptionsSystemWorkspace
{
    internal required TextBox psWorkspaceBox;
    private readonly Button psSystemDoneButton;
    private readonly Button psWorkspaceClearButton;
    private readonly TextBlock psWorkspaceSize = PSWorkspaceSizeBuild();
    private readonly UIElement psSystemMaintenanceNotice = PSNotice.PSNoticeBuild(
        QLocalizationCatalog.QLocalizationTextRead("Options.System.WorkspaceUnapplied"));

    internal PSOptionsSystemWorkspace()
    {
        (psSystemDoneButton, psWorkspaceClearButton) = PSSystemClearBuild();
    }

    internal UIElement PSSystemWorkspaceBuild(QSOptions lsOptions, Window pWindow)
    {
        PSWorkspaceSizeUpdate();
        (Button pWorkspaceBrowse, Button pWorkspaceOpen) = PSWorkspaceButtonBuild();
        PSWorkspaceButtonAttach(pWorkspaceBrowse, pWorkspaceOpen, lsOptions);
        PSSystemClearAttach(psSystemDoneButton, psWorkspaceClearButton, pWindow);
        PSSystemMaintenanceAttach();
        PSSystemMaintenanceUpdate();
        UIElement pWorkspaceDefault = PSNotice.PSNoticeBuild(
            QLocalizationCatalog.QLocalizationFormat("Options.System.DefaultPath", LDepot.LDepotDefaultRead()));
        PSSystemDefaultUpdate(pWorkspaceDefault);
        PSSystemDefaultAttach(pWorkspaceDefault);
        return PSSystemWorkspaceBuild(pWorkspaceBrowse, pWorkspaceOpen, pWorkspaceDefault, psWorkspaceClearButton);
    }

    internal UIElement PSSystemDoneBuild() => PSOptionsField.PSSystemRowBuild(psSystemDoneButton);

    private static TextBlock PSWorkspaceSizeBuild() => new()
    {
        Foreground = PSField.PSFieldMuted,
        VerticalAlignment = VerticalAlignment.Center
    };

    private static (Button PSWorkspaceBrowse, Button PSWorkspaceFolder) PSWorkspaceButtonBuild()
    {
        Button pWorkspaceBrowse = PSInline.PSInlineIconBuild(
            PSOptionsFolder.PSOptionsBrowseIcon,
            QLocalizationCatalog.QLocalizationTextRead("Options.System.Browse"),
            new Thickness(8, 0, 0, 0));
        Button pWorkspaceOpen = PSInline.PSInlineIconBuild(
            PSOptionsFolder.PSOptionsOpenIcon,
            QLocalizationCatalog.QLocalizationTextRead("Options.System.Open"),
            new Thickness(6, 0, 0, 0));
        return (pWorkspaceBrowse, pWorkspaceOpen);
    }

    private void PSWorkspaceButtonAttach(Button pWorkspaceBrowse, Button pWorkspaceOpen, QSOptions lsOptions)
    {
        pWorkspaceBrowse.Click += (_, _) => PSOptionsFolder.PSSystemFolderRead(
            psWorkspaceBox,
            QLocalizationCatalog.QLocalizationTextRead("Options.System.ChooseWorkspace"),
            LDepot.LDepotDefaultRead());
        pWorkspaceOpen.Click += (_, _) =>
            lsOptions.QSOptionsFolderOpen(psWorkspaceBox.Text, LDepot.LDepotDefaultRead());
    }

    private static (Button PSSystemDone, Button PSSystemWorkspace) PSSystemClearBuild()
    {
        Button pDoneClear = PSInline.PSInlineButtonBuild(
            QLocalizationCatalog.QLocalizationTextRead("Options.System.ClearDone"),
            190,
            new Thickness(0));
        Button pWorkspaceClear = PSInline.PSInlineButtonBuild(
            QLocalizationCatalog.QLocalizationTextRead("Options.System.ClearWorkspace"),
            190,
            new Thickness(0));
        return (pDoneClear, pWorkspaceClear);
    }

    private void PSSystemClearAttach(Button pDoneClear, Button pWorkspaceClear, Window pWindow)
    {
        pDoneClear.Click += (_, _) => PSSystemDoneClear(pWindow);
        pWorkspaceClear.Click += (_, _) => PSWorkspaceClear(pWindow);
    }

    private void PSSystemMaintenanceAttach() => psWorkspaceBox.TextChanged += (_, _) => PSSystemMaintenanceUpdate();

    private void PSSystemDefaultUpdate(UIElement pWorkspaceDefault) =>
        PSSystemDefaultApply(pWorkspaceDefault, PSWorkspaceEmptyCheck());

    private bool PSWorkspaceEmptyCheck() => string.IsNullOrWhiteSpace(psWorkspaceBox.Text);

    private static void PSSystemDefaultApply(UIElement pWorkspaceDefault, bool pShown) =>
        pWorkspaceDefault.Visibility = pShown ? Visibility.Visible : Visibility.Collapsed;

    private void PSSystemDefaultAttach(UIElement pWorkspaceDefault) =>
        psWorkspaceBox.TextChanged += (_, _) => PSSystemDefaultUpdate(pWorkspaceDefault);

    private UIElement PSSystemWorkspaceBuild(
        Button pWorkspaceBrowse,
        Button pWorkspaceOpen,
        UIElement pWorkspaceDefault,
        Button pWorkspaceClear) =>
        PSPlate.PSPlateBuild(QLocalizationCatalog.QLocalizationTextRead("Options.System.Workspace"),
            PSField.PSFieldButtonBuild(
                QLocalizationCatalog.QLocalizationTextRead("Options.System.Location"),
                psWorkspaceBox,
                pWorkspaceBrowse,
                pWorkspaceOpen),
            pWorkspaceDefault,
            PSField.PSFieldBuild(QLocalizationCatalog.QLocalizationTextRead("Options.System.CurrentSize"),
                psWorkspaceSize),
            PSField.PSFieldBuild(QLocalizationCatalog.QLocalizationTextRead("Options.System.Maintenance"),
                PSOptionsField.PSSystemRowBuild(pWorkspaceClear)),
            psSystemMaintenanceNotice);

    internal void PSWorkspaceSizeUpdate() =>
        psWorkspaceSize.Text = PSSystemSizeFormat(LDepotUpkeep.LDepotSizeRead());

    internal void PSSystemMaintenanceUpdate()
    {
        bool pApplied = PSWorkspaceAppliedCheck();
        psSystemDoneButton.IsEnabled = pApplied;
        psWorkspaceClearButton.IsEnabled = pApplied;
        psSystemMaintenanceNotice.Visibility = pApplied ? Visibility.Collapsed : Visibility.Visible;
    }

    private bool PSWorkspaceAppliedCheck() => QSOptions.QSOptionsAppliedCheck(psWorkspaceBox.Text);

    private void PSSystemDoneClear(Window pWindow)
    {
        if (!PSWorkspaceAppliedCheck())
        {
            return;
        }

        if (!PSAlert.PSAlertConfirm(
                pWindow,
                QLocalizationCatalog.QLocalizationTextRead("Options.System.ClearDoneTitle"),
                QLocalizationCatalog.QLocalizationFormat("Options.System.ClearDoneConfirm", LDepot.LDepotRootRead()),
                QLocalizationCatalog.QLocalizationTextRead("Terms.Delete")))
        {
            return;
        }

        int pRemoved = LDepotUpkeep.LDepotFolderClear(
            LDepotFolder.LDepotFolderScheduled,
            LDepotFolder.LDepotFolderDone,
            LDepotFolder.LDepotFolderFailed,
            LDepotFolder.LDepotFolderCancelled);
        LDepotIndex.LDepotIndexRebuild();
        LDepotIndex.LDepotIndexCompact();
        PSWorkspaceSizeUpdate();
        PSAnnouncement.PSAnnouncementShow(
            pWindow,
            QLocalizationCatalog.QLocalizationTextRead("Options.System.ClearDoneTitle"),
            QLocalizationCatalog.QLocalizationFormat("Options.System.WorkRecordsRemoved", pRemoved));
    }

    private void PSWorkspaceClear(Window pWindow)
    {
        if (!PSWorkspaceAppliedCheck())
        {
            return;
        }

        string pWorkspaceRoot = LDepot.LDepotRootRead();
        if (LDepot.LDepotRunningCheck(pWorkspaceRoot))
        {
            PSWarning.PSWarningShow(
                pWindow,
                QLocalizationCatalog.QLocalizationTextRead("Options.System.ClearWorkspaceTitle"),
                QLocalizationCatalog.QLocalizationTextRead("Options.System.WorkspaceRunning"));
            return;
        }

        if (!PSAlert.PSAlertConfirm(
                pWindow,
                QLocalizationCatalog.QLocalizationTextRead("Options.System.ClearWorkspaceTitle"),
                QLocalizationCatalog.QLocalizationFormat("Options.System.ClearWorkspaceConfirm", pWorkspaceRoot),
                QLocalizationCatalog.QLocalizationTextRead("Terms.Reset")))
        {
            return;
        }

        LDepotUpkeep.LDepotWorkspaceReset();
        LDepotIndex.LDepotIndexRebuild();
        LDepotIndex.LDepotIndexCompact();
        PSWorkspaceSizeUpdate();
        PSAnnouncement.PSAnnouncementShow(
            pWindow,
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

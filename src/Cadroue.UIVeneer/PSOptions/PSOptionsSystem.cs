using System.Windows;
using System.Windows.Controls;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed class PSOptionsSystem
{
    internal readonly PSOptionsSystemWorkspace psOptionsWorkspace;
    internal readonly PSOptionsSystemSidecar psOptionsSidecar;
    internal readonly PSOptionsSystemFfmpeg psOptionsFfmpeg;
    internal readonly CheckBox psOptionsCleanupBox;
    internal readonly Slider psOptionsCleanupSlider;

    internal PSOptionsSystem(QSOptions lsOptions, Window pWindow)
    {
        psOptionsWorkspace = new PSOptionsSystemWorkspace
        {
            psWorkspaceBox = PSEntry.PSEntryBuild(lsOptions.QSOptionsDraft.LPreferenceWorkspaceFolder, 320)
        };
        psOptionsSidecar = new PSOptionsSystemSidecar(lsOptions, pWindow, psOptionsWorkspace.PSWorkspaceSizeUpdate);
        psOptionsFfmpeg = new PSOptionsSystemFfmpeg(lsOptions, pWindow);
        psOptionsCleanupBox = PSOptionsField.PSOptionsCheckBuild(
            QLocalizationCatalog.QLocalizationTextRead("Options.System.CleanupCheck"),
            lsOptions.QSOptionsDraft.LPreferenceCleanupActive);
        psOptionsCleanupSlider = PSOptionsField.PSOptionsSliderBuild(
            lsOptions.QSOptionsDraft.LPreferenceCleanupDays, 1, 365);
    }

    internal void PSSystemAppliedUpdate()
    {
        psOptionsWorkspace.PSSystemMaintenanceUpdate();
        psOptionsSidecar.PSSidecarNoticeUpdate();
    }

    internal UIElement PSSystemBuild(QSOptions lsOptions, Window pWindow)
    {
        UIElement pWorkspacePlate = psOptionsWorkspace.PSSystemWorkspaceBuild(lsOptions, pWindow);
        UIElement pFfmpegPlate = psOptionsFfmpeg.PSSystemFfmpegBuild();
        UIElement pCleanupRow = PSOptionsField.PSOptionsFieldBuild(
            QLocalizationCatalog.QLocalizationTextRead("Options.System.CleanupDays"),
            psOptionsCleanupSlider,
            QLocalizationCatalog.QLocalizationTextRead("Options.System.CleanupUnit"));
        PSSystemCleanupApply(pCleanupRow, PSOptionsField.PSOptionsCheckRead(psOptionsCleanupBox));
        PSSystemCleanupAttach(pCleanupRow);
        UIElement pWorkPlate = PSSystemWorkBuild(pCleanupRow);
        return PSSystemPanelBuild(pFfmpegPlate, pWorkspacePlate, pWorkPlate);
    }

    private static void PSSystemCleanupApply(UIElement pCleanupRow, bool pShown) =>
        pCleanupRow.Visibility = pShown ? Visibility.Visible : Visibility.Collapsed;

    private void PSSystemCleanupAttach(UIElement pCleanupRow)
    {
        psOptionsCleanupBox.Checked += (_, _) => PSSystemCleanupApply(pCleanupRow, true);
        psOptionsCleanupBox.Unchecked += (_, _) => PSSystemCleanupApply(pCleanupRow, false);
    }

    private UIElement PSSystemWorkBuild(UIElement pCleanupRow) =>
        PSPlate.PSPlateBuild(QLocalizationCatalog.QLocalizationTextRead(
            "Options.System.WorkRecord"),
            PSField.PSFieldBuild(QLocalizationCatalog.QLocalizationTextRead("Options.System.Maintenance"),
                psOptionsWorkspace.PSSystemDoneBuild()),
            PSField.PSFieldBuild(QLocalizationCatalog.QLocalizationTextRead("Options.System.AutoDelete"),
                psOptionsCleanupBox),
            pCleanupRow,
            PSNotice.PSNoticeBuild(QLocalizationCatalog.QLocalizationTextRead("Options.System.CleanupNotice")));

    private UIElement PSSystemPanelBuild(UIElement pFfmpegPlate, UIElement pWorkspacePlate, UIElement pWorkPlate)
    {
        var pPanel = new StackPanel();
        pPanel.Children.Add(pFfmpegPlate);
        pPanel.Children.Add(pWorkspacePlate);
        pPanel.Children.Add(pWorkPlate);
        pPanel.Children.Add(psOptionsSidecar.PSSystemRecordBuild());
        return pPanel;
    }
}

using System.Windows;
using System.Windows.Controls;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed class PSOptionsSystem
{
    internal readonly PSOptionsSystemSidecar psOptionsSidecar;
    internal readonly CheckBox psOptionsCleanupBox;
    internal readonly Slider psOptionsCleanupSlider;

    internal PSOptionsSystem(QSOptions lsOptions, Window pWindow)
    {
        psOptionsSidecar = new PSOptionsSystemSidecar(lsOptions, pWindow, lsOptions.QSWorkspace.QSWorkspaceSizeUpdate);
        psOptionsCleanupBox = PSOptionsField.PSOptionsCheckBuild(
            QLocalizationCatalog.QLocalizationTextRead("Options.System.CleanupCheck"),
            lsOptions.QSOptionsDraft.LPreferenceCleanupActive);
        psOptionsCleanupSlider = PSOptionsField.PSOptionsSliderBuild(
            lsOptions.QSOptionsDraft.LPreferenceCleanupDays, 1, 365);
    }

    internal void PSSystemAppliedUpdate(QSOptions lsOptions)
    {
        lsOptions.QSWorkspace.QSWorkspaceUpdate();
        psOptionsSidecar.PSSidecarNoticeUpdate();
    }

    internal UIElement PSSystemBuild(QSOptions lsOptions, Window pWindow)
    {
        var pWorkspace = new PSOptionsSystemWorkspace();
        UIElement pWorkspacePlate = lsOptions.QSWorkspace.QSWorkspaceAttach(
            pWorkspace,
            lsOptions.QSOptionsDraft.LPreferenceWorkspaceFolder,
            new QSWorkspaceAnswer(
                pWindow,
                (owner, title, question, action) => PSAlert.PSAlertConfirm(owner, title, question, action),
                PSWarning.PSWarningShow,
                PSAnnouncement.PSAnnouncementShow),
            out Button pDone);
        var pFfmpegPlate = new PSRenderer();
        lsOptions.QSRenderer.QSRendererAttach(
            pFfmpegPlate,
            new QSRendererAnswer(lsOptions, pWindow, PSDiagnosis.PSDiagnosisShow));
        UIElement pCleanupRow = PSOptionsField.PSOptionsFieldBuild(
            QLocalizationCatalog.QLocalizationTextRead("Options.System.CleanupDays"),
            psOptionsCleanupSlider,
            QLocalizationCatalog.QLocalizationTextRead("Options.System.CleanupUnit"));
        PSSystemCleanupApply(pCleanupRow, PSOptionsField.PSOptionsCheckRead(psOptionsCleanupBox));
        PSSystemCleanupAttach(pCleanupRow);
        UIElement pWorkPlate = PSSystemWorkBuild(pCleanupRow, pDone);
        return PSSystemPanelBuild(pFfmpegPlate, pWorkspacePlate, pWorkPlate);
    }

    private static void PSSystemCleanupApply(UIElement pCleanupRow, bool pShown) =>
        pCleanupRow.Visibility = pShown ? Visibility.Visible : Visibility.Collapsed;

    private void PSSystemCleanupAttach(UIElement pCleanupRow)
    {
        psOptionsCleanupBox.Checked += (_, _) => PSSystemCleanupApply(pCleanupRow, true);
        psOptionsCleanupBox.Unchecked += (_, _) => PSSystemCleanupApply(pCleanupRow, false);
    }

    private UIElement PSSystemWorkBuild(UIElement pCleanupRow, UIElement pDone) =>
        PSPlate.PSPlateBuild(QLocalizationCatalog.QLocalizationTextRead(
            "Options.System.WorkRecord"),
            PSField.PSFieldBuild(QLocalizationCatalog.QLocalizationTextRead("Options.System.Maintenance"),
                pDone),
            PSField.PSFieldBuild(QLocalizationCatalog.QLocalizationTextRead("Options.System.AutoDelete"),
                psOptionsCleanupBox),
            pCleanupRow,
            PSNotice.PSNoticeBuild(QLocalizationCatalog.QLocalizationTextRead("Options.System.CleanupNotice")));

    private FrameworkElement PSSystemPanelBuild(UIElement pFfmpegPlate, UIElement pWorkspacePlate, UIElement pWorkPlate)
    {
        var pPanel = new StackPanel();
        pPanel.Children.Add(pFfmpegPlate);
        pPanel.Children.Add(pWorkspacePlate);
        pPanel.Children.Add(pWorkPlate);
        pPanel.Children.Add(psOptionsSidecar.PSSystemRecordBuild());
        return pPanel;
    }
}

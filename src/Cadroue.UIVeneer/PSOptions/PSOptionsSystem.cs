using System.Windows;
using System.Windows.Controls;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.Application;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed partial class PSOptions
{
    private static readonly LLocalizationChoice[] PSOptionsRecordItems =
    {
        new("FileLocation", "Options.Record.FileLocation"),
        new("Workspace", "Options.Record.Workspace")
    };

    private readonly CheckBox psOptionsCleanupBox;
    private readonly Slider psOptionsCleanupSlider;

    private const string PSOptionsBrowseIcon = "/PAsset/PPanel/PBrowse.svg";
    private const string PSOptionsOpenIcon = "/PAsset/PPanel/POpen.svg";
    private const string PSOptionsDiagnosisIcon = "/PAsset/PPanel/PDiagnosis.svg";

    private readonly TextBox psWorkspaceBox;
    private readonly TextBox psSystemFfmpegBox;

    private UIElement PSSystemBuild()
    {
        psWorkspaceSize = PSWorkspaceSizeBuild();
        PSWorkspaceSizeUpdate();
        TextBlock pFfmpegState = PSSystemStateBuild();
        PSSystemStateAttach(pFfmpegState);
        (Button pWorkspaceBrowse, Button pWorkspaceOpen) = PSWorkspaceButtonBuild();
        PSWorkspaceButtonAttach(pWorkspaceBrowse, pWorkspaceOpen);
        (Button pFfmpegBrowse, Button pFfmpegOpen, Button pFfmpegDiagnosis) = PSSystemButtonBuild();
        PSSystemButtonAttach(pFfmpegBrowse, pFfmpegOpen, pFfmpegDiagnosis);
        (Button pDoneClear, Button pWorkspaceClear) = PSSystemClearBuild();
        PSSystemClearAttach(pDoneClear, pWorkspaceClear);
        PSSystemMaintenanceBuild(pDoneClear, pWorkspaceClear);
        PSSystemMaintenanceAttach();
        PSSystemMaintenanceUpdate();
        UIElement pCleanupRow = PSOptionsFieldBuild(
            QLocalizationCatalog.QLocalizationTextRead("Options.System.CleanupDays"),
            psOptionsCleanupSlider,
            QLocalizationCatalog.QLocalizationTextRead("Options.System.CleanupUnit"));
        PSSystemCleanupApply(pCleanupRow, PSOptionsCheckRead(psOptionsCleanupBox));
        PSSystemCleanupAttach(pCleanupRow);
        UIElement pWorkspaceDefault = PSNotice.PSNoticeBuild(
            QLocalizationCatalog.QLocalizationFormat("Options.System.DefaultPath", LDepot.LDepotDefaultRead()));
        PSSystemDefaultUpdate(pWorkspaceDefault);
        PSSystemDefaultAttach(pWorkspaceDefault);
        UIElement pFfmpegPlate = PSSystemFfmpegBuild(pFfmpegState, pFfmpegBrowse, pFfmpegOpen, pFfmpegDiagnosis);
        UIElement pWorkspacePlate = PSSystemWorkspaceBuild(
            pWorkspaceBrowse,
            pWorkspaceOpen,
            pWorkspaceDefault,
            pWorkspaceClear);
        UIElement pWorkPlate = PSSystemWorkBuild(pDoneClear, pCleanupRow);
        return PSSystemPanelBuild(pFfmpegPlate, pWorkspacePlate, pWorkPlate);
    }

    private static TextBlock PSWorkspaceSizeBuild() => new()
    {
        Foreground = PSField.PSFieldMuted,
        VerticalAlignment = VerticalAlignment.Center
    };

    private TextBlock PSSystemStateBuild() => new()
    {
        Foreground = PSField.PSFieldMuted,
        TextWrapping = TextWrapping.Wrap,
        Margin = PSNotice.PSNoticeMargin,
        Text = PSSystemFfmpegFormat(psSystemFfmpegBox.Text)
    };

    private void PSSystemStateAttach(TextBlock pFfmpegState) =>
        psSystemFfmpegBox.TextChanged += (_, _) => PSSystemStateUpdate(pFfmpegState);

    private void PSSystemStateUpdate(TextBlock pFfmpegState) =>
        PSSystemStateShow(pFfmpegState, PSSystemFfmpegFormat(psSystemFfmpegBox.Text));

    private static void PSSystemStateShow(TextBlock pFfmpegState, string pText) => pFfmpegState.Text = pText;

    private static (Button PSWorkspaceBrowse, Button PSWorkspaceFolder) PSWorkspaceButtonBuild()
    {
        Button pWorkspaceBrowse = PSInline.PSInlineIconBuild(
            PSOptionsBrowseIcon,
            QLocalizationCatalog.QLocalizationTextRead("Options.System.Browse"),
            new Thickness(8, 0, 0, 0));
        Button pWorkspaceOpen = PSInline.PSInlineIconBuild(
            PSOptionsOpenIcon,
            QLocalizationCatalog.QLocalizationTextRead("Options.System.Open"),
            new Thickness(6, 0, 0, 0));
        return (pWorkspaceBrowse, pWorkspaceOpen);
    }

    private void PSWorkspaceButtonAttach(Button pWorkspaceBrowse, Button pWorkspaceOpen)
    {
        pWorkspaceBrowse.Click += (_, _) => PSSystemFolderRead(
            psWorkspaceBox,
            QLocalizationCatalog.QLocalizationTextRead("Options.System.ChooseWorkspace"),
            LDepot.LDepotDefaultRead());
        pWorkspaceOpen.Click += (_, _) => PSSystemFolderOpen(psWorkspaceBox.Text, LDepot.LDepotDefaultRead());
    }

    private static (Button PSSystemBrowse, Button PSSystemFolder, Button PSSystemDiagnosis) PSSystemButtonBuild()
    {
        Button pFfmpegBrowse = PSInline.PSInlineIconBuild(
            PSOptionsBrowseIcon,
            QLocalizationCatalog.QLocalizationTextRead("Options.System.Browse"),
            new Thickness(8, 0, 0, 0));
        Button pFfmpegOpen = PSInline.PSInlineIconBuild(
            PSOptionsOpenIcon,
            QLocalizationCatalog.QLocalizationTextRead("Options.System.Open"),
            new Thickness(6, 0, 0, 0));
        Button pFfmpegDiagnosis = PSInline.PSInlineIconBuild(
            PSOptionsDiagnosisIcon,
            QLocalizationCatalog.QLocalizationTextRead("Options.System.Diagnosis"),
            new Thickness(6, 0, 0, 0));
        return (pFfmpegBrowse, pFfmpegOpen, pFfmpegDiagnosis);
    }

    private void PSSystemButtonAttach(Button pFfmpegBrowse, Button pFfmpegOpen, Button pFfmpegDiagnosis)
    {
        pFfmpegBrowse.Click += (_, _) => PSSystemFolderRead(
            psSystemFfmpegBox,
            QLocalizationCatalog.QLocalizationTextRead("Options.System.ChooseFFmpeg"),
            psSystemFfmpegBox.Text);
        pFfmpegOpen.Click += (_, _) => PSSystemFolderOpen(psSystemFfmpegBox.Text, string.Empty);
        pFfmpegDiagnosis.Click += (_, _) => PSDiagnosis.PSDiagnosisShow(this, lsOptions.QSOptionsDiagnosisCreate);
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

    private void PSSystemClearAttach(Button pDoneClear, Button pWorkspaceClear)
    {
        pDoneClear.Click += (_, _) => PSSystemDoneClear();
        pWorkspaceClear.Click += (_, _) => PSWorkspaceClear();
    }

    private void PSSystemMaintenanceBuild(Button pDoneClear, Button pWorkspaceClear)
    {
        psSystemMaintenanceButtons.Clear();
        psSystemMaintenanceButtons.Add(pDoneClear);
        psSystemMaintenanceButtons.Add(pWorkspaceClear);
        psSystemMaintenanceNotice = PSNotice.PSNoticeBuild(
            QLocalizationCatalog.QLocalizationTextRead("Options.System.WorkspaceUnapplied"));
    }

    private void PSSystemMaintenanceAttach() => psWorkspaceBox.TextChanged += (_, _) => PSSystemMaintenanceUpdate();

    private static void PSSystemCleanupApply(UIElement pCleanupRow, bool pShown) =>
        pCleanupRow.Visibility = pShown ? Visibility.Visible : Visibility.Collapsed;

    private void PSSystemCleanupAttach(UIElement pCleanupRow)
    {
        psOptionsCleanupBox.Checked += (_, _) => PSSystemCleanupApply(pCleanupRow, true);
        psOptionsCleanupBox.Unchecked += (_, _) => PSSystemCleanupApply(pCleanupRow, false);
    }

    private void PSSystemDefaultUpdate(UIElement pWorkspaceDefault) =>
        PSSystemDefaultApply(pWorkspaceDefault, PSWorkspaceEmptyCheck());

    private bool PSWorkspaceEmptyCheck() => string.IsNullOrWhiteSpace(psWorkspaceBox.Text);

    private static void PSSystemDefaultApply(UIElement pWorkspaceDefault, bool pShown) =>
        pWorkspaceDefault.Visibility = pShown ? Visibility.Visible : Visibility.Collapsed;

    private void PSSystemDefaultAttach(UIElement pWorkspaceDefault) =>
        psWorkspaceBox.TextChanged += (_, _) => PSSystemDefaultUpdate(pWorkspaceDefault);

    private UIElement PSSystemFfmpegBuild(
        TextBlock pFfmpegState,
        Button pFfmpegBrowse,
        Button pFfmpegOpen,
        Button pFfmpegDiagnosis) =>
        PSPlate.PSPlateBuild(QLocalizationCatalog.QLocalizationTextRead("Options.System.FFmpeg"),
            PSField.PSFieldButtonBuild(
                QLocalizationCatalog.QLocalizationTextRead("Options.System.Location"),
                psSystemFfmpegBox,
                pFfmpegBrowse,
                pFfmpegOpen,
                pFfmpegDiagnosis),
            pFfmpegState);

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
                psWorkspaceSize!),
            PSField.PSFieldBuild(QLocalizationCatalog.QLocalizationTextRead("Options.System.Maintenance"),
                PSSystemRowBuild(pWorkspaceClear)),
            psSystemMaintenanceNotice!);

    private UIElement PSSystemWorkBuild(Button pDoneClear, UIElement pCleanupRow) =>
        PSPlate.PSPlateBuild(QLocalizationCatalog.QLocalizationTextRead(
            "Options.System.WorkRecord"),
            PSField.PSFieldBuild(QLocalizationCatalog.QLocalizationTextRead("Options.System.Maintenance"),
                PSSystemRowBuild(pDoneClear)),
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
        pPanel.Children.Add(PSSystemRecordBuild());
        return pPanel;
    }

    private static StackPanel PSSystemRowBuild(Button pButton)
    {
        var pRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };
        pRow.Children.Add(pButton);
        return pRow;
    }

    private UIElement PSSystemFlyleafBuild()
    {
        Button pInstall = PSInline.PSInlineButtonBuild(PSSystemFlyleafFormat(), 160, new Thickness(0, 0, 8, 0));
        Button pBrowse = PSInline.PSInlineIconBuild(
            PSOptionsOpenIcon,
            QLocalizationCatalog.QLocalizationTextRead("Options.System.Open"),
            new Thickness(0));
        ProgressBar pProgress = PSSystemProgressBuild();
        var pFeed = new Progress<double>(pValue => PSSystemProgressApply(pProgress, pValue));
        PSSystemFlyleafAttach(pInstall, pBrowse, pProgress, pFeed);
        return PSSystemLineBuild(pInstall, pBrowse, pProgress);
    }

    private void PSSystemFlyleafAttach(Button pInstall, Button pBrowse, ProgressBar pProgress, Progress<double> pFeed)
    {
        pInstall.Click += async (_, _) => await PSSystemFlyleafRun(pInstall, pProgress, pFeed);
        pBrowse.Click += (_, _) => PSSystemFolderOpen(LFlyleaf.LFlyleafRootRead(), string.Empty);
    }

    private async Task PSSystemFlyleafRun(Button pInstall, ProgressBar pProgress, Progress<double> pFeed)
    {
        PSSystemBusyApply(pInstall, pProgress);
        LFlyleafInstallResult pResult;
        try
        {
            pResult = await LFlyleaf.LFlyleafInstallStart(pFeed);
        }
        finally
        {
            PSSystemProgressHide(pProgress);
        }

        PSSystemInstallShow(pInstall, PSSystemFlyleafFormat());
        PSSystemIdleApply(pInstall);
        string pFlyleafTitle = QLocalizationCatalog.QLocalizationTextRead("Options.System.LocalFlyleaf");
        if (pResult.LFlyleafInstallSuccess)
        {
            PSAnnouncement.PSAnnouncementShow(
                this,
                pFlyleafTitle,
                QLocalizationCatalog.QLocalizationTextRead("Flyleaf.Local.Install.Completed"));
        }
        else
        {
            PSWarning.PSWarningShow(
                this,
                pFlyleafTitle,
                QLocalizationCatalog.QLocalizationFormat(
                    "Flyleaf.Local.Install.Failed",
                    pResult.LFlyleafInstallMessage));
        }
    }

    private static void PSSystemProgressApply(ProgressBar pProgress, double pValue) => pProgress.Value = pValue;

    private static void PSSystemBusyApply(Button pInstall, ProgressBar pProgress)
    {
        pInstall.IsEnabled = false;
        pProgress.Value = 0;
        pProgress.Visibility = Visibility.Visible;
    }

    private static void PSSystemProgressHide(ProgressBar pProgress) => pProgress.Visibility = Visibility.Collapsed;

    private static void PSSystemInstallShow(Button pInstall, string pText) => pInstall.Content = pText;

    private static void PSSystemIdleApply(Button pInstall) => pInstall.IsEnabled = true;

    private static UIElement PSSystemLineBuild(Button pInstall, Button pBrowse, ProgressBar pProgress)
    {
        var pButtons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };
        pButtons.Children.Add(pInstall);
        pButtons.Children.Add(pBrowse);
        pButtons.Children.Add(pProgress);

        return PSField.PSFieldBuild(string.Empty, pButtons);
    }

    private static ProgressBar PSSystemProgressBuild() =>
        new()
        {
            Minimum = 0,
            Maximum = 1,
            Width = 220,
            Height = 8,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(16, 0, 0, 0),
            Foreground = null,
            Background = null,
            BorderThickness = new Thickness(0),
            Template = PSSystemTemplateBuild(),
            Visibility = Visibility.Collapsed
        };

    private static System.Windows.Controls.ControlTemplate PSSystemTemplateBuild()
    {
        const string pXaml = @"
<ControlTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
                 TargetType=""{x:Type ProgressBar}"">
    <Border CornerRadius=""4"" Background=""#E4E9F0"" ClipToBounds=""True"">
        <Grid>
            <Rectangle x:Name=""PART_Track"" />
            <Border x:Name=""PART_Indicator""
                    HorizontalAlignment=""Left""
                    CornerRadius=""4""
                    Background=""#4C86F7"" />
        </Grid>
    </Border>
</ControlTemplate>";
        return (System.Windows.Controls.ControlTemplate)System.Windows.Markup.XamlReader.Parse(pXaml);
    }

    private static string PSSystemFlyleafFormat() =>
        QLocalizationCatalog.QLocalizationTextRead(QSOptions.QSOptionsFlyleafResolve());


    private static void PSSystemFolderRead(TextBox pPathBox, string pDialogTitle, string pFallback)
    {
        string pInitial = PSSystemFolderResolve(pPathBox.Text, pFallback);
        var pDialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = pDialogTitle,
            InitialDirectory = pInitial
        };
        if (pDialog.ShowDialog() == true)
        {
            PSSystemPathShow(pPathBox, pDialog.FolderName);
        }
    }

    private static string PSSystemFolderResolve(string pPath, string pFallback) =>
        string.IsNullOrWhiteSpace(pPath) ? pFallback : pPath;

    private static void PSSystemPathShow(TextBox pPathBox, string pFolder) => pPathBox.Text = pFolder;

    private void PSSystemFolderOpen(string pFolder, string pFallback) =>
        lsOptions.QSOptionsFolderOpen(pFolder, pFallback);


    private static string PSSystemFfmpegFormat(string pFolder) =>
        QLocalizationCatalog.QLocalizationTextRead(QSOptions.QSOptionsFfmpegResolve(pFolder));
}

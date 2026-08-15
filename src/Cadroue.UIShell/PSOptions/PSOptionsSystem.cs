using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Cadroue.Core;
using Cadroue.Infrastructure;

using static Cadroue.UIShell.PSShared.PSField;

namespace Cadroue.UIShell;

internal sealed partial class PSOptions
{
    private const string PSOptionsBrowseIcon = "/PAssets/PPanels/PBrowse.svg";
    private const string PSOptionsOpenIcon = "/PAssets/PPanels/POpen.svg";
    private const string PSOptionsDiagnosisIcon = "/PAssets/PPanels/PDiagnosis.svg";

    private readonly TextBox psWorkspaceBox;
    private readonly TextBox psSystemFfmpegBox;

    private TextBlock? psWorkspaceSize;

    private UIElement PSSystemBuild()
    {
        psWorkspaceSize = new TextBlock
        {
            Foreground = PSFieldMuted,
            VerticalAlignment = VerticalAlignment.Center
        };
        PSWorkspaceSizeUpdate();

        var pFfmpegState = new TextBlock
        {
            Foreground = PSFieldMuted,
            TextWrapping = TextWrapping.Wrap,
            Margin = PSNoticeMargin,
            Text = PSSystemFfmpegFormat(psSystemFfmpegBox.Text)
        };
        psSystemFfmpegBox.TextChanged += (_, _) => pFfmpegState.Text = PSSystemFfmpegFormat(psSystemFfmpegBox.Text);

        Button pWorkspaceBrowse = PSInlineIconBuild(PSOptionsBrowseIcon, LLocalization.LLocalizationTextRead("Options.System.Browse"), new Thickness(8, 0, 0, 0));
        Button pWorkspaceOpen = PSInlineIconBuild(PSOptionsOpenIcon, LLocalization.LLocalizationTextRead("Options.System.Open"), new Thickness(6, 0, 0, 0));
        pWorkspaceBrowse.Click += (_, _) => PSSystemFolderRead(psWorkspaceBox, LLocalization.LLocalizationTextRead("Options.System.ChooseWorkspace"), LDepot.LDepotDefaultRead());
        pWorkspaceOpen.Click += (_, _) => PSSystemFolderOpen(psWorkspaceBox.Text, LDepot.LDepotDefaultRead());

        Button pFfmpegBrowse = PSInlineIconBuild(PSOptionsBrowseIcon, LLocalization.LLocalizationTextRead("Options.System.Browse"), new Thickness(8, 0, 0, 0));
        Button pFfmpegOpen = PSInlineIconBuild(PSOptionsOpenIcon, LLocalization.LLocalizationTextRead("Options.System.Open"), new Thickness(6, 0, 0, 0));
        Button pFfmpegDiagnosis = PSInlineIconBuild(PSOptionsDiagnosisIcon, LLocalization.LLocalizationTextRead("Options.System.Diagnosis"), new Thickness(6, 0, 0, 0));
        pFfmpegBrowse.Click += (_, _) => PSSystemFolderRead(psSystemFfmpegBox, LLocalization.LLocalizationTextRead("Options.System.ChooseFFmpeg"), psSystemFfmpegBox.Text);
        pFfmpegOpen.Click += (_, _) => PSSystemFolderOpen(psSystemFfmpegBox.Text, string.Empty);
        pFfmpegDiagnosis.Click += (_, _) => PSDiagnosis.PSDiagnosisShow(this);

        Button pDoneClear = PSInlineButtonBuild(LLocalization.LLocalizationTextRead("Options.System.ClearDone"), 190, new Thickness(0));
        Button pWorkspaceClear = PSInlineButtonBuild(LLocalization.LLocalizationTextRead("Options.System.ClearWorkspace"), 190, new Thickness(0));
        pDoneClear.Click += (_, _) => PSSystemDoneClear();
        pWorkspaceClear.Click += (_, _) => PSWorkspaceClear();

        var pWorkspaceClearRow = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        pWorkspaceClearRow.Children.Add(pWorkspaceClear);

        var pDoneClearRow = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        pDoneClearRow.Children.Add(pDoneClear);

        UIElement pCleanupRow = PSOptionsFieldBuild(LLocalization.LLocalizationTextRead("Options.System.CleanupDays"), psOptionsCleanupSlider, LLocalization.LLocalizationTextRead("Options.System.CleanupUnit"));
        pCleanupRow.Visibility = psOptionsCleanupBox.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        psOptionsCleanupBox.Checked += (_, _) => pCleanupRow.Visibility = Visibility.Visible;
        psOptionsCleanupBox.Unchecked += (_, _) => pCleanupRow.Visibility = Visibility.Collapsed;

        UIElement pWorkspaceDefault = PSNoticeBuild(LLocalization.LLocalizationFormat("Options.System.DefaultPath", LDepot.LDepotDefaultRead()));
        pWorkspaceDefault.Visibility = string.IsNullOrWhiteSpace(psWorkspaceBox.Text) ? Visibility.Visible : Visibility.Collapsed;
        psWorkspaceBox.TextChanged += (_, _) => pWorkspaceDefault.Visibility = string.IsNullOrWhiteSpace(psWorkspaceBox.Text) ? Visibility.Visible : Visibility.Collapsed;

        var pPanel = new StackPanel();
        pPanel.Children.Add(PSPlateBuild(LLocalization.LLocalizationTextRead("Options.System.FFmpeg"),
            PSFieldButtonBuild(LLocalization.LLocalizationTextRead("Options.System.Location"), psSystemFfmpegBox, pFfmpegBrowse, pFfmpegOpen, pFfmpegDiagnosis),
            pFfmpegState));
        pPanel.Children.Add(PSPlateBuild(LLocalization.LLocalizationTextRead("Options.System.Workspace"),
            PSFieldButtonBuild(LLocalization.LLocalizationTextRead("Options.System.Location"), psWorkspaceBox, pWorkspaceBrowse, pWorkspaceOpen),
            pWorkspaceDefault,
            PSFieldBuild(LLocalization.LLocalizationTextRead("Options.System.CurrentSize"), psWorkspaceSize),
            PSFieldBuild(LLocalization.LLocalizationTextRead("Options.System.Maintenance"), pWorkspaceClearRow)));
        pPanel.Children.Add(PSPlateBuild(LLocalization.LLocalizationTextRead("Options.System.WorkRecord"),
            PSFieldBuild(LLocalization.LLocalizationTextRead("Options.System.Maintenance"), pDoneClearRow),
            PSFieldBuild(LLocalization.LLocalizationTextRead("Options.System.AutoDelete"), psOptionsCleanupBox),
            pCleanupRow,
            PSNoticeBuild(LLocalization.LLocalizationTextRead("Options.System.CleanupNotice"))));
        pPanel.Children.Add(PSSystemRecordBuild());
        return pPanel;
    }

    private UIElement PSSystemFlyleafBuild()
    {
        Button pInstall = PSInlineButtonBuild(PSSystemFlyleafInstallText(), 160, new Thickness(0, 0, 8, 0));
        Button pBrowse = PSInlineIconBuild(PSOptionsOpenIcon, LLocalization.LLocalizationTextRead("Options.System.Open"), new Thickness(0));
        ProgressBar pProgress = PSSystemInstallProgressBuild();
        var pFeed = new Progress<double>(pValue => pProgress.Value = pValue);
        pInstall.Click += async (_, _) =>
        {
            pInstall.IsEnabled = false;
            pProgress.Value = 0;
            pProgress.Visibility = Visibility.Visible;
            LFlyleafInstallResult pResult;
            try
            {
                pResult = await LFlyleaf.LFlyleafInstallStart(pFeed);
            }
            finally
            {
                pProgress.Visibility = Visibility.Collapsed;
            }

            pInstall.Content = PSSystemFlyleafInstallText();
            pInstall.IsEnabled = true;
            MessageBox.Show(
                this,
                pResult.LFlyleafInstallSuccess
                    ? LLocalization.LLocalizationTextRead("Flyleaf.Local.Install.Completed")
                    : LLocalization.LLocalizationFormat("Flyleaf.Local.Install.Failed", pResult.LFlyleafInstallMessage),
                LLocalization.LLocalizationTextRead("Options.System.LocalFlyleaf"),
                MessageBoxButton.OK,
                pResult.LFlyleafInstallSuccess ? MessageBoxImage.Information : MessageBoxImage.Warning);
        };
        pBrowse.Click += (_, _) => PSSystemFolderOpen(LFlyleaf.LFlyleafRootRead(), string.Empty);

        var pButtons = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        pButtons.Children.Add(pInstall);
        pButtons.Children.Add(pBrowse);
        pButtons.Children.Add(pProgress);

        return PSFieldBuild(string.Empty, pButtons);
    }

    private static ProgressBar PSSystemInstallProgressBuild() =>
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
            Template = PSSystemInstallProgressTemplateBuild(),
            Visibility = Visibility.Collapsed
        };

    private static System.Windows.Controls.ControlTemplate PSSystemInstallProgressTemplateBuild()
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

    private static string PSSystemFlyleafInstallText() =>
        LLocalization.LLocalizationTextRead(
            LFlyleaf.LFlyleafInstalledCheck()
                ? "Options.System.ReinstallFlyleaf"
                : "Options.System.InstallFlyleaf");

    private UIElement PSSystemRecordBuild()
    {
        Button pRecordClear = PSInlineButtonBuild(LLocalization.LLocalizationTextRead("Options.System.ClearFileRecord"), 190, new Thickness(0));
        pRecordClear.Click += (_, _) => PSSystemRecordClear();

        var pRecordRow = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        pRecordRow.Children.Add(psOptionsRecordBeside);
        pRecordRow.Children.Add(psOptionsRecordWorkspace);

        var pRecordButtonRow = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        pRecordButtonRow.Children.Add(pRecordClear);

        string pRecordWorkspacePath = System.IO.Path.Combine(LDepot.LDepotRootRead(), Cadroue.Infrastructure.LSidecarStore.LSidecarRecordFolder);
        var pRecordBesideNotice = (TextBlock)PSNoticeBuild(LLocalization.LLocalizationTextRead("Options.System.FileRecordBeside"));
        void PSSystemRecordNoticeUpdate()
        {
            bool pWorkspace = psOptionsRecordWorkspace.IsChecked == true;
            pRecordBesideNotice.Text = pWorkspace
                ? LLocalization.LLocalizationFormat("Options.System.FileRecordWorkspace", pRecordWorkspacePath)
                : LLocalization.LLocalizationTextRead("Options.System.FileRecordBeside");
        }
        PSSystemRecordNoticeUpdate();
        psOptionsRecordBeside.Checked += (_, _) => PSSystemRecordNoticeUpdate();
        psOptionsRecordWorkspace.Checked += (_, _) => PSSystemRecordNoticeUpdate();

        return PSPlateBuild(LLocalization.LLocalizationTextRead("Options.System.FileRecord"),
            PSFieldBuild(LLocalization.LLocalizationTextRead("Options.System.Location"), pRecordRow),
            pRecordBesideNotice,
            PSNoticeBuild(LLocalization.LLocalizationTextRead("Options.System.FileRecordScope")),
            PSFieldBuild(LLocalization.LLocalizationTextRead("Options.System.Maintenance"), pRecordButtonRow));
    }

    private void PSSystemRecordClear()
    {
        string pRecordFolder = Cadroue.Infrastructure.LSidecarStore.LSidecarFolderRead();
        if (string.IsNullOrWhiteSpace(pRecordFolder) || !Directory.Exists(pRecordFolder))
        {
            MessageBox.Show(this, LLocalization.LLocalizationTextRead("Options.System.NoFileRecord"), LLocalization.LLocalizationTextRead("Options.System.ClearFileRecord"), MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        MessageBoxResult pAnswer = MessageBox.Show(
            this,
            LLocalization.LLocalizationFormat("Options.System.ClearFileRecordConfirm", pRecordFolder),
            LLocalization.LLocalizationTextRead("Options.System.ClearFileRecord"),
            MessageBoxButton.OKCancel,
            MessageBoxImage.Warning);
        if (pAnswer != MessageBoxResult.OK)
        {
            return;
        }

        int pRemoved = Cadroue.Infrastructure.LSidecarStore.LSidecarFolderClear();
        PSWorkspaceSizeUpdate();
        MessageBox.Show(this, LLocalization.LLocalizationFormat("Options.System.FileRecordsRemoved", pRemoved), LLocalization.LLocalizationTextRead("Options.System.ClearFileRecord"), MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void PSWorkspaceSizeUpdate()
    {
        if (psWorkspaceSize is not null)
        {
            psWorkspaceSize.Text = PSSystemSizeFormat(LDepot.LDepotSizeRead());
        }
    }

    private static void PSSystemFolderRead(TextBox pPathBox, string pDialogTitle, string pFallback)
    {
        var pDialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = pDialogTitle,
            InitialDirectory = string.IsNullOrWhiteSpace(pPathBox.Text) ? pFallback : pPathBox.Text
        };
        if (pDialog.ShowDialog() == true)
        {
            pPathBox.Text = pDialog.FolderName;
        }
    }

    private static void PSSystemFolderOpen(string pFolder, string pFallback)
    {
        string pTarget = string.IsNullOrWhiteSpace(pFolder) ? pFallback : pFolder;
        if (string.IsNullOrWhiteSpace(pTarget) || !Directory.Exists(pTarget))
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{pTarget}\"") { UseShellExecute = true });
        }
        catch (Exception)
        {
        }
    }

    private void PSSystemDoneClear()
    {
        MessageBoxResult pAnswer = MessageBox.Show(
            this,
            LLocalization.LLocalizationTextRead("Options.System.ClearDoneConfirm"),
            LLocalization.LLocalizationTextRead("Options.System.ClearDoneTitle"),
            MessageBoxButton.OKCancel,
            MessageBoxImage.Warning);
        if (pAnswer != MessageBoxResult.OK)
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
        MessageBox.Show(this, LLocalization.LLocalizationFormat("Options.System.WorkRecordsRemoved", pRemoved), LLocalization.LLocalizationTextRead("Options.System.ClearDoneTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void PSWorkspaceClear()
    {
        if (LDepot.LDepotRunningCheck(LDepot.LDepotRootRead()))
        {
            MessageBox.Show(this, LLocalization.LLocalizationTextRead("Options.System.WorkspaceRunning"), LLocalization.LLocalizationTextRead("Options.System.ClearWorkspaceTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        MessageBoxResult pAnswer = MessageBox.Show(
            this,
            LLocalization.LLocalizationTextRead("Options.System.ClearWorkspaceConfirm"),
            LLocalization.LLocalizationTextRead("Options.System.ClearWorkspaceTitle"),
            MessageBoxButton.OKCancel,
            MessageBoxImage.Warning);
        if (pAnswer != MessageBoxResult.OK)
        {
            return;
        }

        LDepot.LDepotWorkspaceReset();
        LDepotIndex.LDepotIndexRebuild();
        LDepotIndex.LDepotIndexCompact();
        PSWorkspaceSizeUpdate();
        MessageBox.Show(this, LLocalization.LLocalizationTextRead("Options.System.WorkspaceReset"), LLocalization.LLocalizationTextRead("Options.System.ClearWorkspaceTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
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

    private static string PSSystemFfmpegFormat(string pFolder)
    {
        if (string.IsNullOrWhiteSpace(pFolder))
        {
            return LLocalization.LLocalizationTextRead("Options.System.FFmpegBlank");
        }

        bool pProgramReady = LRendererLibrary.LRendererProgramExist(pFolder);
        bool pLibraryReady = LRendererLibrary.LRendererFolderValidate(pFolder);
        if (pProgramReady && pLibraryReady)
        {
            return LLocalization.LLocalizationTextRead("Options.System.FFmpegReady");
        }

        if (pProgramReady)
        {
            return LLocalization.LLocalizationTextRead("Options.System.FFmpegProgramOnly");
        }

        if (pLibraryReady)
        {
            return LLocalization.LLocalizationTextRead("Options.System.FFmpegLibraryOnly");
        }

        return LLocalization.LLocalizationTextRead("Options.System.FFmpegMissing");
    }
}

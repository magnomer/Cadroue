using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Cadroue.Core;

using static Cadroue.UIShell.PSShared.PSField;

namespace Cadroue.UIShell;

internal sealed partial class PSOptions
{
    private const string PSOptionsBrowseIconPath = "/PAssets/PPanels/PBrowse.svg";
    private const string PSOptionsOpenIconPath = "/PAssets/PPanels/POpen.svg";

    private readonly TextBox psWorkspaceBox;
    private readonly TextBox psFfmpegBox;

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
            Text = PSFfmpegFormat(psFfmpegBox.Text)
        };
        psFfmpegBox.TextChanged += (_, _) => pFfmpegState.Text = PSFfmpegFormat(psFfmpegBox.Text);

        Button pWorkspaceBrowse = PSInlineIconBuild(PSOptionsBrowseIconPath, LLocalization.LLocalizationTextRead("Options.System.Browse"), new Thickness(8, 0, 0, 0));
        Button pWorkspaceOpen = PSInlineIconBuild(PSOptionsOpenIconPath, LLocalization.LLocalizationTextRead("Options.System.Open"), new Thickness(6, 0, 0, 0));
        pWorkspaceBrowse.Click += (_, _) => PSFolderBrowse(psWorkspaceBox, LLocalization.LLocalizationTextRead("Options.System.ChooseWorkspace"), LDepot.LDepotDefaultRootRead());
        pWorkspaceOpen.Click += (_, _) => PSFolderOpen(psWorkspaceBox.Text, LDepot.LDepotDefaultRootRead());

        Button pFfmpegBrowse = PSInlineIconBuild(PSOptionsBrowseIconPath, LLocalization.LLocalizationTextRead("Options.System.Browse"), new Thickness(8, 0, 0, 0));
        Button pFfmpegOpen = PSInlineIconBuild(PSOptionsOpenIconPath, LLocalization.LLocalizationTextRead("Options.System.Open"), new Thickness(6, 0, 0, 0));
        pFfmpegBrowse.Click += (_, _) => PSFolderBrowse(psFfmpegBox, LLocalization.LLocalizationTextRead("Options.System.ChooseFFmpeg"), psFfmpegBox.Text);
        pFfmpegOpen.Click += (_, _) => PSFolderOpen(psFfmpegBox.Text, string.Empty);

        Button pDoneClear = PSInlineButtonBuild(LLocalization.LLocalizationTextRead("Options.System.ClearDone"), 210, new Thickness(0, 0, 8, 0));
        Button pWorkspaceClear = PSInlineButtonBuild(LLocalization.LLocalizationTextRead("Options.System.ClearWorkspace"), 140, new Thickness(0));
        pDoneClear.Click += (_, _) => PSDoneClear();
        pWorkspaceClear.Click += (_, _) => PSWorkspaceClear();

        var pClearRow = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        pClearRow.Children.Add(pDoneClear);
        pClearRow.Children.Add(pWorkspaceClear);

        var pPanel = new StackPanel();
        pPanel.Children.Add(PSPlateBuild(LLocalization.LLocalizationTextRead("Options.System.Workspace"),
            PSFieldButtonBuild(LLocalization.LLocalizationTextRead("Options.System.Location"), psWorkspaceBox, pWorkspaceBrowse, pWorkspaceOpen),
            PSNoticeBuild(LLocalization.LLocalizationFormat("Options.System.DefaultPath", LDepot.LDepotDefaultRootRead())),
            PSFieldBuild(LLocalization.LLocalizationTextRead("Options.System.CurrentSize"), psWorkspaceSize),
            PSFieldBuild(LLocalization.LLocalizationTextRead("Options.System.Maintenance"), pClearRow)));
        pPanel.Children.Add(PSPlateBuild(LLocalization.LLocalizationTextRead("Options.System.FFmpeg"),
            PSFieldButtonBuild(LLocalization.LLocalizationTextRead("Options.System.Location"), psFfmpegBox, pFfmpegBrowse, pFfmpegOpen),
            pFfmpegState));
        pPanel.Children.Add(PSFlyleafPlateBuild());
        pPanel.Children.Add(PSRecordPlateBuild());
        return pPanel;
    }

    private UIElement PSFlyleafPlateBuild()
    {
        var pState = new TextBlock
        {
            Foreground = PSFieldMuted,
            TextWrapping = TextWrapping.Wrap,
            Margin = PSNoticeMargin,
            Text = LFlyleafLocal.LFlyleafLocalStatusRead()
        };

        Button pInstall = PSInlineButtonBuild(LLocalization.LLocalizationTextRead("Options.System.InstallFlyleaf"), 160, new Thickness(0, 0, 8, 0));
        Button pOpen = PSInlineIconBuild(PSOptionsOpenIconPath, LLocalization.LLocalizationTextRead("Options.System.Open"), new Thickness(0));
        pInstall.Click += async (_, _) =>
        {
            pInstall.IsEnabled = false;
            pState.Text = LLocalization.LLocalizationTextRead("Options.System.FlyleafInstalling");
            LFlyleafLocalInstallResult pResult = await LFlyleafLocal.LFlyleafLocalInstallAsync();
            pState.Text = LFlyleafLocal.LFlyleafLocalStatusRead();
            pInstall.IsEnabled = true;
            MessageBox.Show(
                this,
                pResult.LFlyleafLocalInstallMessage,
                LLocalization.LLocalizationTextRead("Options.System.LocalFlyleaf"),
                MessageBoxButton.OK,
                pResult.LFlyleafLocalInstallSuccess ? MessageBoxImage.Information : MessageBoxImage.Warning);
        };
        pOpen.Click += (_, _) => PSFolderOpen(LFlyleafLocal.LFlyleafLocalRootRead(), string.Empty);

        var pButtons = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        pButtons.Children.Add(pInstall);
        pButtons.Children.Add(pOpen);

        return PSPlateBuild(LLocalization.LLocalizationTextRead("Options.System.LocalFlyleaf"),
            PSFieldBuild(LLocalization.LLocalizationTextRead("Options.System.PreviewEngine"), pButtons),
            pState,
            PSNoticeBuild(LLocalization.LLocalizationTextRead("Options.System.FlyleafNotice")));
    }

    private UIElement PSRecordPlateBuild()
    {
        Button pRecordClear = PSInlineButtonBuild(LLocalization.LLocalizationTextRead("Options.System.ClearFileRecord"), 190, new Thickness(0));
        pRecordClear.Click += (_, _) => PSRecordClear();

        var pRecordRow = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        pRecordRow.Children.Add(psRecordBeside);
        pRecordRow.Children.Add(psRecordWorkspace);

        var pRecordButtonRow = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        pRecordButtonRow.Children.Add(pRecordClear);

        return PSPlateBuild(LLocalization.LLocalizationTextRead("Options.System.FileRecord"),
            PSFieldBuild(LLocalization.LLocalizationTextRead("Options.System.Location"), pRecordRow),
            PSNoticeBuild(LLocalization.LLocalizationFormat(
                "Options.System.FileRecordNotice",
                System.IO.Path.Combine(LDepot.LDepotRootRead(), Cadroue.Media.LSidecarStore.LSidecarRecordFolderName))),
            PSFieldBuild(LLocalization.LLocalizationTextRead("Options.System.Maintenance"), pRecordButtonRow));
    }

    private void PSRecordClear()
    {
        string pRecordFolder = Cadroue.Media.LSidecarStore.LSidecarFolderRead();
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

        int pRemoved = Cadroue.Media.LSidecarStore.LSidecarFolderClear();
        PSWorkspaceSizeUpdate();
        MessageBox.Show(this, LLocalization.LLocalizationFormat("Options.System.FileRecordsRemoved", pRemoved), LLocalization.LLocalizationTextRead("Options.System.ClearFileRecord"), MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void PSWorkspaceSizeUpdate()
    {
        if (psWorkspaceSize is not null)
        {
            psWorkspaceSize.Text = PSSizeFormat(LDepot.LDepotSizeRead());
        }
    }

    private static void PSFolderBrowse(TextBox pPathBox, string pDialogTitle, string pFallback)
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

    private static void PSFolderOpen(string pFolder, string pFallback)
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

    private void PSDoneClear()
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

        int pRemoved = LDepot.LDepotFolderClear(LDepotFolder.LDepotFolderDone, LDepotFolder.LDepotFolderFailed);
        LDepotIndex.LDepotIndexRebuild();
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

        int pRemoved = LDepot.LDepotFolderClear(
            LDepotFolder.LDepotFolderScheduled,
            LDepotFolder.LDepotFolderDone,
            LDepotFolder.LDepotFolderFailed);
        LDepotIndex.LDepotIndexRebuild();
        PSWorkspaceSizeUpdate();
        MessageBox.Show(this, LLocalization.LLocalizationFormat("Options.System.WorkRecordsRemoved", pRemoved), LLocalization.LLocalizationTextRead("Options.System.ClearWorkspaceTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private static string PSSizeFormat(long pBytes)
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

    private static string PSFfmpegFormat(string pFolder)
    {
        if (string.IsNullOrWhiteSpace(pFolder))
        {
            return LLocalization.LLocalizationTextRead("Options.System.FFmpegBlank");
        }

        bool pProgramReady = LRendererSettings.LRendererProgramExist(pFolder);
        bool pLibraryReady = LRendererSettings.LRendererFolderValidate(pFolder);
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

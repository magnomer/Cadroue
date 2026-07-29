using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Cadroue.Core;

using static Cadroue.UIShell.PSShared.PSField;

namespace Cadroue.UIShell;

internal sealed partial class PSOptions
{
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

        Button pWorkspaceBrowse = PSInlineButtonBuild("Browse", 84, new Thickness(8, 0, 0, 0));
        Button pWorkspaceOpen = PSInlineButtonBuild("Open", 64, new Thickness(6, 0, 0, 0));
        pWorkspaceBrowse.Click += (_, _) => PSFolderBrowse(psWorkspaceBox, "Choose workspace folder", LDepot.LDepotDefaultRootRead());
        pWorkspaceOpen.Click += (_, _) => PSFolderOpen(psWorkspaceBox.Text, LDepot.LDepotDefaultRootRead());

        Button pFfmpegBrowse = PSInlineButtonBuild("Browse", 84, new Thickness(8, 0, 0, 0));
        Button pFfmpegOpen = PSInlineButtonBuild("Open", 64, new Thickness(6, 0, 0, 0));
        pFfmpegBrowse.Click += (_, _) => PSFolderBrowse(psFfmpegBox, "Choose FFmpeg folder", psFfmpegBox.Text);
        pFfmpegOpen.Click += (_, _) => PSFolderOpen(psFfmpegBox.Text, string.Empty);

        Button pDoneClear = PSInlineButtonBuild("Clear completed work records", 210, new Thickness(0, 0, 8, 0));
        Button pWorkspaceClear = PSInlineButtonBuild("Clear workspace", 140, new Thickness(0));
        pDoneClear.Click += (_, _) => PSDoneClear();
        pWorkspaceClear.Click += (_, _) => PSWorkspaceClear();

        var pClearRow = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        pClearRow.Children.Add(pDoneClear);
        pClearRow.Children.Add(pWorkspaceClear);

        var pPanel = new StackPanel();
        pPanel.Children.Add(PSPlateBuild("Workspace",
            PSFieldButtonBuild("Location", psWorkspaceBox, pWorkspaceBrowse, pWorkspaceOpen),
            PSNoticeBuild($"Blank uses the default: {LDepot.LDepotDefaultRootRead()}"),
            PSFieldBuild("Current size", psWorkspaceSize),
            PSFieldBuild("Maintenance", pClearRow)));
        pPanel.Children.Add(PSPlateBuild("FFmpeg",
            PSFieldButtonBuild("Location", psFfmpegBox, pFfmpegBrowse, pFfmpegOpen),
            pFfmpegState));
        return pPanel;
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
            "Delete every done and failed work record from the workspace? Queued and running work is kept.",
            "Clear completed work records",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Warning);
        if (pAnswer != MessageBoxResult.OK)
        {
            return;
        }

        int pRemoved = LDepot.LDepotFolderClear(LDepotFolder.LDepotFolderDone, LDepotFolder.LDepotFolderFailed);
        LDepotIndex.LDepotIndexRebuild();
        PSWorkspaceSizeUpdate();
        MessageBox.Show(this, $"{pRemoved} work record(s) were removed.", "Clear completed work records", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void PSWorkspaceClear()
    {
        if (LDepot.LDepotRunningCheck(LDepot.LDepotRootRead()))
        {
            MessageBox.Show(this, "The workspace still has running work. Stop it before clearing.", "Clear workspace", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        MessageBoxResult pAnswer = MessageBox.Show(
            this,
            "Delete every queued, done and failed work record from the workspace? This cannot be undone.",
            "Clear workspace",
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
        MessageBox.Show(this, $"{pRemoved} work record(s) were removed.", "Clear workspace", MessageBoxButton.OK, MessageBoxImage.Information);
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
            return "Blank: FFmpeg is used from PATH.";
        }

        bool pProgramReady = LRendererSettings.LRendererProgramExist(pFolder);
        bool pLibraryReady = LRendererSettings.LRendererFolderValidate(pFolder);
        if (pProgramReady && pLibraryReady)
        {
            return "ffmpeg.exe and the playback libraries were found. Restart to apply playback.";
        }

        if (pProgramReady)
        {
            return "ffmpeg.exe was found. The playback libraries were not, so playback stays on PATH.";
        }

        if (pLibraryReady)
        {
            return "The playback libraries were found, but ffmpeg.exe was not, so exporting stays on PATH.";
        }

        return "Neither ffmpeg.exe nor the playback libraries were found here. PATH is used instead.";
    }
}

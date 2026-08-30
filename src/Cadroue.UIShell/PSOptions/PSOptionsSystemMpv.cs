using System.Windows;
using Cadroue.UIShell.PSShared;
using System.Windows.Controls;
using Cadroue.Core;
using Cadroue.Infrastructure;

using static Cadroue.UIShell.PSShared.PSField;
using static Cadroue.UIShell.PSShared.PSInline;

namespace Cadroue.UIShell;

internal sealed partial class PSOptions
{
    private UIElement PSSystemMpvBuild()
    {
        Button pDownload = PSInlineButtonBuild(PSSystemMpvFormat(), 160, new Thickness(0, 0, 8, 0));
        Button pBrowse = PSInlineIconBuild(PSOptionsOpenIcon, LLocalization.LLocalizationTextRead("Options.System.Open"), new Thickness(0));
        ProgressBar pProgress = PSSystemProgressBuild();
        var pFeed = new Progress<double>(pValue => pProgress.Value = pValue);
        pDownload.Click += async (_, _) =>
        {
            pDownload.IsEnabled = false;
            pProgress.Value = 0;
            pProgress.Visibility = Visibility.Visible;
            LMpvInstallResult pResult;
            try
            {
                pResult = await LMpv.LMpvInstallStart(pFeed);
            }
            finally
            {
                pProgress.Visibility = Visibility.Collapsed;
            }

            pDownload.Content = PSSystemMpvFormat();
            pDownload.IsEnabled = true;
            psOptionsEngineEnable("Mpv", pResult.LMpvInstallSuccess || LMpv.LMpvInstalledCheck());
            string pMpvTitle = LLocalization.LLocalizationTextRead("Options.System.LocalMpv");
            if (pResult.LMpvInstallSuccess)
            {
                PSAnnouncement.PSAnnouncementShow(this, pMpvTitle, LLocalization.LLocalizationTextRead("Mpv.Local.Install.Completed"));
            }
            else
            {
                PSWarning.PSWarningShow(this, pMpvTitle, LLocalization.LLocalizationFormat("Mpv.Local.Install.Failed", pResult.LMpvInstallMessage));
            }
        };
        pBrowse.Click += (_, _) => PSSystemFolderOpen(LMpv.LMpvRootRead(), string.Empty);

        var pButtons = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        pButtons.Children.Add(pDownload);
        pButtons.Children.Add(pBrowse);
        pButtons.Children.Add(pProgress);

        return PSFieldBuild(string.Empty, pButtons);
    }

    private static string PSSystemMpvFormat() =>
        LLocalization.LLocalizationTextRead(
            LMpv.LMpvInstalledCheck()
                ? "Options.System.ReinstallMpv"
                : "Options.System.DownloadMpv");
}

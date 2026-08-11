using System.Windows;
using System.Windows.Controls;
using Cadroue.Core;
using Cadroue.Infrastructure;

using static Cadroue.UIShell.PSShared.PSField;

namespace Cadroue.UIShell;

internal sealed partial class PSOptions
{
    private UIElement PSSystemMpvBuild()
    {
        Button pDownload = PSInlineButtonBuild(PSSystemMpvInstallText(), 160, new Thickness(0, 0, 8, 0));
        Button pBrowse = PSInlineIconBuild(PSOptionsOpenIcon, LLocalization.LLocalizationTextRead("Options.System.Open"), new Thickness(0));
        pDownload.Click += async (_, _) =>
        {
            pDownload.IsEnabled = false;
            LMpvInstallResult pResult = await LMpv.LMpvInstallStart();
            pDownload.Content = PSSystemMpvInstallText();
            pDownload.IsEnabled = true;
            psOptionsEngineMpv.IsEnabled = pResult.LMpvInstallSuccess || LMpv.LMpvInstalledCheck();
            MessageBox.Show(
                this,
                pResult.LMpvInstallSuccess
                    ? LLocalization.LLocalizationTextRead("Mpv.Local.Install.Completed")
                    : LLocalization.LLocalizationFormat("Mpv.Local.Install.Failed", pResult.LMpvInstallMessage),
                LLocalization.LLocalizationTextRead("Options.System.LocalMpv"),
                MessageBoxButton.OK,
                pResult.LMpvInstallSuccess ? MessageBoxImage.Information : MessageBoxImage.Warning);
        };
        pBrowse.Click += (_, _) => PSSystemFolderOpen(LMpv.LMpvRootRead(), string.Empty);

        var pButtons = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        pButtons.Children.Add(pDownload);
        pButtons.Children.Add(pBrowse);

        return PSFieldBuild(string.Empty, pButtons);
    }

    private static string PSSystemMpvInstallText() =>
        LLocalization.LLocalizationTextRead(
            LMpv.LMpvInstalledCheck()
                ? "Options.System.ReinstallMpv"
                : "Options.System.DownloadMpv");
}

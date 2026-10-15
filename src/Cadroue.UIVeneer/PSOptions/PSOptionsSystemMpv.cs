using System.Windows;
using System.Windows.Controls;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed partial class PSOptions
{
    private UIElement PSSystemMpvBuild()
    {
        Button pDownload = PSInline.PSInlineButtonBuild(PSSystemMpvFormat(), 160, new Thickness(0, 0, 8, 0));
        Button pBrowse = PSInline.PSInlineIconBuild(
            PSOptionsOpenIcon,
            QLocalizationCatalog.QLocalizationTextRead("Options.System.Open"),
            new Thickness(0));
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
            await lsOptions.QSOptionsMpvUpdate();
            string pMpvTitle = QLocalizationCatalog.QLocalizationTextRead("Options.System.LocalMpv");
            if (pResult.LMpvInstallSuccess)
            {
                PSAnnouncement.PSAnnouncementShow(
                    this,
                    pMpvTitle,
                    QLocalizationCatalog.QLocalizationTextRead("Mpv.Local.Install.Completed"));
            }
            else
            {
                PSWarning.PSWarningShow(
                    this,
                    pMpvTitle,
                    QLocalizationCatalog.QLocalizationFormat("Mpv.Local.Install.Failed", pResult.LMpvInstallMessage));
            }
        };
        pBrowse.Click += (_, _) => PSSystemFolderOpen(LMpv.LMpvRootRead(), string.Empty);

        var pButtons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };
        pButtons.Children.Add(pDownload);
        pButtons.Children.Add(pBrowse);
        pButtons.Children.Add(pProgress);

        return PSField.PSFieldBuild(string.Empty, pButtons);
    }

    private static string PSSystemMpvFormat() =>
        QLocalizationCatalog.QLocalizationTextRead(QSOptions.QSOptionsMpvResolve());
}

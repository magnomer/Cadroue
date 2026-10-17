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
        var pFeed = new Progress<double>(pValue => PSSystemProgressApply(pProgress, pValue));
        PSSystemMpvAttach(pDownload, pBrowse, pProgress, pFeed);
        return PSSystemLineBuild(pDownload, pBrowse, pProgress);
    }

    private void PSSystemMpvAttach(Button pDownload, Button pBrowse, ProgressBar pProgress, Progress<double> pFeed)
    {
        pDownload.Click += async (_, _) => await PSSystemMpvRun(pDownload, pProgress, pFeed);
        pBrowse.Click += (_, _) => PSSystemFolderOpen(LMpvInstall.LMpvRootRead(), string.Empty);
    }

    private async Task PSSystemMpvRun(Button pDownload, ProgressBar pProgress, Progress<double> pFeed)
    {
        PSSystemBusyApply(pDownload, pProgress);
        LMpvInstallResult pResult;
        try
        {
            pResult = await LMpvInstall.LMpvInstallStart(pFeed);
        }
        finally
        {
            PSSystemProgressHide(pProgress);
        }

        PSSystemInstallShow(pDownload, PSSystemMpvFormat());
        PSSystemIdleApply(pDownload);
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
    }

    private static string PSSystemMpvFormat() =>
        QLocalizationCatalog.QLocalizationTextRead(QSOptions.QSOptionsMpvResolve());
}

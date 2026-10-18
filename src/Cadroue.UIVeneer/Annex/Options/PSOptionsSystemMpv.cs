using System.Windows;
using System.Windows.Controls;
using Cadroue.Infrastructure;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed class PSOptionsSystemMpv
{
    private readonly QSOptions lsOptions;
    private readonly Window psOptionsWindow;

    internal PSOptionsSystemMpv(QSOptions lsOptionsOwner, Window pWindow)
    {
        lsOptions = lsOptionsOwner;
        psOptionsWindow = pWindow;
    }

    internal UIElement PSSystemMpvBuild()
    {
        Button pDownload = PSInline.PSInlineButtonBuild(PSSystemMpvFormat(), 160, new Thickness(0, 0, 8, 0));
        Button pBrowse = PSInline.PSInlineIconBuild(
            PSOptionsFolder.PSOptionsOpenIcon,
            QLocalizationCatalog.QLocalizationTextRead("Options.System.Open"),
            new Thickness(0));
        ProgressBar pProgress = PSOptionsInstall.PSSystemProgressBuild();
        var pFeed = new Progress<double>(pValue => PSOptionsInstall.PSSystemProgressApply(pProgress, pValue));
        PSSystemMpvAttach(pDownload, pBrowse, pProgress, pFeed);
        return PSOptionsInstall.PSSystemLineBuild(pDownload, pBrowse, pProgress);
    }

    private void PSSystemMpvAttach(Button pDownload, Button pBrowse, ProgressBar pProgress, Progress<double> pFeed)
    {
        pDownload.Click += async (_, _) => await PSSystemMpvRun(pDownload, pProgress, pFeed);
        pBrowse.Click += (_, _) => lsOptions.QSOptionsFolderOpen(LMpvInstall.LMpvRootRead(), string.Empty);
    }

    private async Task PSSystemMpvRun(Button pDownload, ProgressBar pProgress, Progress<double> pFeed)
    {
        PSOptionsInstall.PSSystemBusyApply(pDownload, pProgress);
        LMpvInstallResult pResult;
        try
        {
            pResult = await LMpvInstall.LMpvInstallStart(pFeed);
        }
        finally
        {
            PSOptionsInstall.PSSystemProgressHide(pProgress);
        }

        PSOptionsInstall.PSSystemInstallShow(pDownload, PSSystemMpvFormat());
        PSOptionsInstall.PSSystemIdleApply(pDownload);
        await lsOptions.QSOptionsMpv.QSOptionsMpvUpdate();
        string pMpvTitle = QLocalizationCatalog.QLocalizationTextRead("Options.System.LocalMpv");
        if (pResult.LMpvInstallSuccess)
        {
            PSAnnouncement.PSAnnouncementShow(
                psOptionsWindow,
                pMpvTitle,
                QLocalizationCatalog.QLocalizationTextRead("Mpv.Local.Install.Completed"));
        }
        else
        {
            PSWarning.PSWarningShow(
                psOptionsWindow,
                pMpvTitle,
                QLocalizationCatalog.QLocalizationFormat("Mpv.Local.Install.Failed", pResult.LMpvInstallMessage));
        }
    }

    private static string PSSystemMpvFormat() =>
        QLocalizationCatalog.QLocalizationTextRead(QSOptions.QSOptionsMpvResolve());
}

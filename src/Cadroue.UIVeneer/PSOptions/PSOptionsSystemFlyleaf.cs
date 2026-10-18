using System.Windows;
using System.Windows.Controls;
using Cadroue.Infrastructure;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed class PSOptionsSystemFlyleaf
{
    private readonly QSOptions lsOptions;
    private readonly Window psOptionsWindow;

    internal PSOptionsSystemFlyleaf(QSOptions lsOptionsOwner, Window pWindow)
    {
        lsOptions = lsOptionsOwner;
        psOptionsWindow = pWindow;
    }

    internal UIElement PSSystemFlyleafBuild()
    {
        Button pInstall = PSInline.PSInlineButtonBuild(PSSystemFlyleafFormat(), 160, new Thickness(0, 0, 8, 0));
        Button pBrowse = PSInline.PSInlineIconBuild(
            PSOptionsFolder.PSOptionsOpenIcon,
            QLocalizationCatalog.QLocalizationTextRead("Options.System.Open"),
            new Thickness(0));
        ProgressBar pProgress = PSOptionsInstall.PSSystemProgressBuild();
        var pFeed = new Progress<double>(pValue => PSOptionsInstall.PSSystemProgressApply(pProgress, pValue));
        PSSystemFlyleafAttach(pInstall, pBrowse, pProgress, pFeed);
        return PSOptionsInstall.PSSystemLineBuild(pInstall, pBrowse, pProgress);
    }

    private void PSSystemFlyleafAttach(Button pInstall, Button pBrowse, ProgressBar pProgress, Progress<double> pFeed)
    {
        pInstall.Click += async (_, _) => await PSSystemFlyleafRun(pInstall, pProgress, pFeed);
        pBrowse.Click += (_, _) => lsOptions.QSOptionsFolderOpen(LFlyleafFolder.LFlyleafRootRead(), string.Empty);
    }

    private async Task PSSystemFlyleafRun(Button pInstall, ProgressBar pProgress, Progress<double> pFeed)
    {
        PSOptionsInstall.PSSystemBusyApply(pInstall, pProgress);
        LFlyleafInstallResult pResult;
        try
        {
            pResult = await LFlyleafInstall.LFlyleafInstallStart(pFeed);
        }
        finally
        {
            PSOptionsInstall.PSSystemProgressHide(pProgress);
        }

        PSOptionsInstall.PSSystemInstallShow(pInstall, PSSystemFlyleafFormat());
        PSOptionsInstall.PSSystemIdleApply(pInstall);
        string pFlyleafTitle = QLocalizationCatalog.QLocalizationTextRead("Options.System.LocalFlyleaf");
        if (pResult.LFlyleafInstallSuccess)
        {
            PSAnnouncement.PSAnnouncementShow(
                psOptionsWindow,
                pFlyleafTitle,
                QLocalizationCatalog.QLocalizationTextRead("Flyleaf.Local.Install.Completed"));
        }
        else
        {
            PSWarning.PSWarningShow(
                psOptionsWindow,
                pFlyleafTitle,
                QLocalizationCatalog.QLocalizationFormat(
                    "Flyleaf.Local.Install.Failed",
                    pResult.LFlyleafInstallMessage));
        }
    }

    private static string PSSystemFlyleafFormat() =>
        QLocalizationCatalog.QLocalizationTextRead(QSOptions.QSOptionsFlyleafResolve());
}

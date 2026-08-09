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
        var pState = new TextBlock
        {
            Foreground = PSFieldMuted,
            TextWrapping = TextWrapping.Wrap,
            Margin = PSNoticeMargin,
            Text = PSSystemMpvFormat()
        };

        Button pDownload = PSInlineButtonBuild(LLocalization.LLocalizationTextRead("Options.System.DownloadMpv"), 160, new Thickness(0, 0, 8, 0));
        Button pOpen = PSInlineIconBuild(PSOptionsOpenIcon, LLocalization.LLocalizationTextRead("Options.System.Open"), new Thickness(0));
        pDownload.Click += async (_, _) =>
        {
            pDownload.IsEnabled = false;
            pState.Text = LLocalization.LLocalizationTextRead("Options.System.MpvInstalling");
            LMpvInstallResult pResult = await LMpv.LMpvInstallStart();
            pState.Text = PSSystemMpvFormat();
            pDownload.IsEnabled = true;
            MessageBox.Show(
                this,
                pResult.LMpvInstallSuccess
                    ? LLocalization.LLocalizationTextRead("Mpv.Local.Install.Completed")
                    : LLocalization.LLocalizationFormat("Mpv.Local.Install.Failed", pResult.LMpvInstallMessage),
                LLocalization.LLocalizationTextRead("Options.System.LocalMpv"),
                MessageBoxButton.OK,
                pResult.LMpvInstallSuccess ? MessageBoxImage.Information : MessageBoxImage.Warning);
        };
        pOpen.Click += (_, _) => PSSystemFolderOpen(LMpv.LMpvRootRead(), string.Empty);

        var pButtons = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        pButtons.Children.Add(pDownload);
        pButtons.Children.Add(pOpen);

        return PSPlateBuild(LLocalization.LLocalizationTextRead("Options.System.LocalMpv"),
            PSFieldBuild(LLocalization.LLocalizationTextRead("Options.System.PreviewEngine"), pButtons),
            pState,
            PSNoticeBuild(LLocalization.LLocalizationTextRead("Options.System.MpvInstallNotice")));
    }

    private static string PSSystemMpvFormat() =>
        LMpv.LMpvInstalledCheck()
            ? LLocalization.LLocalizationFormat("Mpv.Local.Status.Installed", LMpv.LMpvFolderRead() ?? string.Empty)
            : LLocalization.LLocalizationFormat("Mpv.Local.Status.NotInstalled", LMpv.LMpvRootRead());

    private UIElement PSSystemRecheckBuild()
    {
        var pState = new TextBlock
        {
            Foreground = PSFieldMuted,
            TextWrapping = TextWrapping.Wrap,
            Margin = PSNoticeMargin,
            Text = PSSystemRecheckFormat(LRenderer.LRendererEngineRead())
        };

        Button pRecheck = PSInlineButtonBuild(LLocalization.LLocalizationTextRead("Options.System.RecheckEngine"), 200, new Thickness(0));
        pRecheck.Click += async (_, _) =>
        {
            pRecheck.IsEnabled = false;
            pState.Text = LLocalization.LLocalizationTextRead("Mpv.Local.Status.Measuring");
            LMpvProbe pOutcome = await LRenderer.LRendererEngineCheck();
            pState.Text = PSSystemRecheckFormat(
                pOutcome == LMpvProbe.LMpvProbeUsable
                    ? LPreviewEngine.LPreviewEngineMpv
                    : LPreviewEngine.LPreviewEngineFlyleaf);
            pRecheck.IsEnabled = true;
        };

        var pButtons = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        pButtons.Children.Add(pRecheck);

        return PSPlateBuild(LLocalization.LLocalizationTextRead("Options.System.RecheckMpv"),
            PSFieldBuild(LLocalization.LLocalizationTextRead("Options.System.PreviewEngine"), pButtons),
            pState,
            PSNoticeBuild(LLocalization.LLocalizationTextRead("Options.System.RecheckNotice")));
    }

    private static string PSSystemRecheckFormat(LPreviewEngine pEngine) =>
        pEngine == LPreviewEngine.LPreviewEngineMpv
            ? LLocalization.LLocalizationTextRead("Mpv.Local.Status.Usable")
            : LLocalization.LLocalizationTextRead("Mpv.Local.Status.Unusable");
}

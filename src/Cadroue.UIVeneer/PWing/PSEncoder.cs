using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.Infrastructure;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PWing;

internal sealed class PSEncoder : Window
{
    internal const string PSEncoderPlacementKey = "Encoder";

    internal const double PSEncoderWidthDefault = 820;
    internal const double PSEncoderHeightDefault = 770;
    internal const double PSEncoderWidthMinimum = 680;
    internal const double PSEncoderHeightMinimum = 770;

    private const double PSSheetTabWidth = 142;
    private const int PSSheetTabCount = 3;
    private const double PSSheetStripWidth = PSSheetTabWidth * PSSheetTabCount;

    private const string PSSheetOutputIcon = "/PAsset/PTab/PSSheetOutput.svg";
    private const string PSSheetVideoIcon = "/PAsset/PTab/PSSheetVideo.svg";
    private const string PSSheetAudioIcon = "/PAsset/PTab/PSSheetAudio.svg";

    private readonly QSEncoder lsEncoder;
    private readonly System.Action psEncoderSummary;
    private readonly PSEncoderOutput psEncoderOutput;
    private readonly PSEncoderVideo psEncoderVideo;
    private readonly PSEncoderAudio psEncoderAudio;
    private readonly PSGrabber psEncoderGrabber;

    public PSEncoder(QSEncoder lsEncoderOwner, System.Action pRefresh)
    {
        lsEncoder = lsEncoderOwner;
        psEncoderSummary = pRefresh;
        psEncoderOutput = new PSEncoderOutput(
            lsEncoder.QSEncoderDraft,
            lsEncoder.QSEncoderSuffix,
            lsEncoder.QSEncoderLocation,
            lsEncoder.QSEncoderTokenCreate(),
            this,
            PSEncoderContainerHandle);
        psEncoderVideo = new PSEncoderVideo(
            lsEncoder.QSEncoderVideo, lsEncoder.QSEncoderSize, lsEncoder.QSEncoderDraft, this);
        psEncoderAudio = new PSEncoderAudio(lsEncoder.QSEncoderAudio, lsEncoder.QSEncoderDraft, this);
        PSEncoderAttach();
        PSEncoderWindowApply();
        PSSubwindow.PSSubwindowApply(this);
        Content = PSEncoderBuild();
        PSGrabberPlacement.PSGrabberPlacementRestore(this, PSEncoderPlacementKey);
        psEncoderGrabber = new PSGrabber(this);
        psEncoderGrabber.PSGrabberAttach();
        PSCodecProbeDefer();
        PSEncoderCloseAttach();
    }

    // The output piece reports a container change; the encoder hands the new container to video and audio.
    private void PSEncoderContainerHandle()
    {
        string pContainer = psEncoderOutput.PSOutputContainerRead();
        psEncoderVideo.PSCodecContainerHandle(pContainer);
        psEncoderAudio.PSAudioContainerHandle(pContainer);
    }

    private void PSEncoderAttach()
    {
        lsEncoder.QSEncoderVideo.QSEncoderVideoChanged += psEncoderVideo.PSVideoRowsApply;
        lsEncoder.QSEncoderAudio.QSEncoderAudioChanged += psEncoderAudio.PSAudioRowsApply;
        lsEncoder.QSEncoderHelpAttach(PSEncoderHelpHandle);
        lsEncoder.QSEncoderSize.QSEncoderSizeChanged += psEncoderVideo.PSVideoSizeApply;
    }

    private void PSCodecProbeDefer() =>
        LTrialCatalog.LTrialSetDefer(() => Dispatcher.BeginInvoke(() =>
        {
            if (IsLoaded) psEncoderVideo.PSCodecContainerHandle(psEncoderOutput.PSOutputContainerRead());
        }));

    private void PSEncoderWindowApply()
    {
        Title = QLocalizationCatalog.QLocalizationTextRead("Encoder.Window.Title");
        Width = PSEncoderWidthDefault;
        Height = PSEncoderHeightDefault;
        MinWidth = PSEncoderWidthMinimum;
        MinHeight = PSEncoderHeightMinimum;
    }

    private void PSEncoderCloseAttach() => Closed += PSEncoderCloseHandle;

    private UIElement PSEncoderBuild() =>
        PSSubwindow.PSSubwindowBuild(this, PSSheetStripWidth, PSSheetControlBuild());

    private UIElement PSSheetControlBuild() => PSSheet.PSSheetControlBuild(
        PSSheetTabWidth,
        PSSheet.PSSheetBuild(
            QLocalizationCatalog.QLocalizationTextRead("Encoder.Sheet.Output"),
            PSSheetOutputIcon,
            PSEncoderRootBuild(PSSheet.PSSheetScrollBuild(PSOutputBuild()))),
        PSSheet.PSSheetBuild(
            QLocalizationCatalog.QLocalizationTextRead("Encoder.Sheet.Video"),
            PSSheetVideoIcon,
            PSEncoderRootBuild(PSSheet.PSSheetScrollBuild(PSVideoBuild()))),
        PSSheet.PSSheetBuild(
            QLocalizationCatalog.QLocalizationTextRead("Encoder.Sheet.Audio"),
            PSSheetAudioIcon,
            PSEncoderRootBuild(PSSheet.PSSheetScrollBuild(PSAudioBuild()))));

    private UIElement PSOutputBuild()
    {
        var pPanel = new StackPanel();
        pPanel.Children.Add(psEncoderOutput.PSOutputPlateBuild());
        return pPanel;
    }

    private UIElement PSVideoBuild()
    {
        var pPanel = new StackPanel();
        pPanel.Children.Add(psEncoderVideo.PSVideoPlateBuild());
        return pPanel;
    }

    private UIElement PSAudioBuild()
    {
        var pPanel = new StackPanel();
        pPanel.Children.Add(psEncoderAudio.PSAudioPlateBuild());
        return pPanel;
    }

    private UIElement PSEncoderRootBuild(UIElement pTabContent)
    {
        var pApply = PSFooter.PSFooterButtonBuild(QLocalizationCatalog.QLocalizationTextRead("Encoder.Button.Apply"));
        var pOk = PSFooter.PSFooterButtonBuild(QLocalizationCatalog.QLocalizationTextRead("Encoder.Button.OK"));
        var pCancel = PSFooter.PSFooterButtonBuild(QLocalizationCatalog.QLocalizationTextRead("Encoder.Button.Cancel"));
        PSEncoderFooterAttach(pApply, pOk, pCancel);
        return PSEncoderLayoutBuild(pTabContent, pApply, pOk, pCancel);
    }

    private void PSEncoderFooterAttach(Button pApply, Button pOk, Button pCancel)
    {
        pApply.Click += (_, _) => PSEncoderApply();
        pOk.Click += (_, _) => PSEncoderOkHandle();
        pCancel.Click += (_, _) => Close();
    }

    private void PSEncoderOkHandle()
    {
        PSEncoderApply();
        DialogResult = true;
    }

    private UIElement PSEncoderLayoutBuild(UIElement pTabContent, Button pApply, Button pOk, Button pCancel)
    {
        var pRoot = new DockPanel { Background = Brushes.White };
        var pFooter = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(12)
        };
        pFooter.Children.Add(pApply);
        pFooter.Children.Add(pOk);
        pFooter.Children.Add(pCancel);
        DockPanel.SetDock(pFooter, Dock.Bottom);
        pRoot.Children.Add(pFooter);
        pRoot.Children.Add(PSEncoderContentBuild(pTabContent));
        return pRoot;
    }

    private UIElement PSEncoderContentBuild(UIElement pTabContent)
    {
        var pPanel = new DockPanel { Margin = new Thickness(18) };
        pPanel.Children.Add(pTabContent);
        return pPanel;
    }

    private void PSEncoderApply()
    {
        PSEncoderDraftSet();
        lsEncoder.QSEncoderApply();
        psEncoderSummary();
    }

    private void PSEncoderDraftSet()
    {
        psEncoderOutput.PSOutputDraftSet();
        psEncoderVideo.PSVideoDraftSet();
        psEncoderAudio.PSAudioDraftSet();
    }

    private void PSEncoderHelpHandle() => Dispatcher.InvokeAsync(psEncoderAudio.PSAudioRowsApply);

    private void PSEncoderCloseHandle(object? sender, System.EventArgs e)
    {
        lsEncoder.QSEncoderClose();
        PSGrabberPlacement.PSGrabberPlacementSave(this, PSEncoderPlacementKey);
        psEncoderGrabber.PSGrabberDetach();
        PSEncoderCloseDetach();
    }

    private void PSEncoderCloseDetach() => Closed -= PSEncoderCloseHandle;
}
